using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using static UnityEditor.Progress;

namespace RPGCore.AI.HFSM
{
	/// <summary>
	/// 状态机Controller;不用做运行时
	/// </summary>
	[CreateAssetMenu(fileName = "FSM Executor Controller", menuName = "State Machine/Executor Controller", order = 4)]
	public class StateMachineExecutorController : ScriptableObject
	{
		//controller对应的脚本名称
		public string realScriptControllerName;

		//controller中的parameter
		public List<Parameter> parameters = new List<Parameter>();

		//controller中的state
		public List<StateData> states = new List<StateData>();

		//controller中的StateMachine
		public List<StateMachineData> stateMachines = new List<StateMachineData>();

		//controller中的Transition
		public List<TransitionData> transitions = new List<TransitionData>();

		private string scriptableObjectAssetPath;

		//记录当前有哪些方法需要构造
		private Dictionary<string, string> stateMethods = new Dictionary<string, string>();

		private Dictionary<string, string> serviceMethods = new Dictionary<string, string>();
		private Dictionary<string, string> canExitMethods = new Dictionary<string, string>();

		//记录上一次生成了哪些方法 方便这次比较不同并更新修改
		public List<string> previousStateMethodsName = new List<string>();

		public List<string> previousServiceMethodsName = new List<string>();

		public List<string> previousCanExitMethodsName = new List<string>();

		private void Awake()
		{
			if (stateMachines.Find(sm => sm.id == "Root") == null)
			{
				StateMachineData root = new StateMachineData()
				{
					id = "Root",
					isRoot = true,
					stateType = StateType.StateMachine,
				};
				stateMachines.Add(root);
			}
		}

		//private void OnEnable()
		//{
		//	scriptableObjectAssetPath = AssetDatabase.GetAssetPath(this);
		//	if (scriptableObjectAssetPath != "")
		//	{
		//		scriptableObjectAssetPath = scriptableObjectAssetPath.Remove(scriptableObjectAssetPath.LastIndexOf("/") + 1);
		//	}
		//}

		/// <summary>
		/// 根据此获取运行时Controller
		/// </summary>
		public StateMachineScriptController GetController()
		{
			List<Type> types = new List<Type>();
			foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies().Where(assembly => assembly.GetName().Name.Contains("Assembly")))
			{
				List<Type> result = assembly.GetTypes().Where(type =>
				{
					return type.IsClass && !type.IsAbstract && type.IsSubclassOf(typeof(StateMachineScriptController)) && type.GetCustomAttribute<StateMachineControllerAttribute>() != null;
				}).ToList();
				types.AddRange(result);
			}
			Type target = types.Find(type => type.GetCustomAttribute<StateMachineControllerAttribute>().ControllerName == realScriptControllerName);
			if (target != null)
			{
				return (StateMachineScriptController)Activator.CreateInstance(target);
			}
			return null;
		}

		/// <summary>
		/// 根据运行时Controller生成并初始化状态机
		/// </summary>
		public StateMachine GetExecuteStateMachine(StateMachineExecutor executor, out StateMachineScriptController scriptController)
		{
			scriptController = GetController();
			if (scriptController != null)
			{
				scriptController.executor = executor;
				scriptController.gameObject = executor.gameObject;
				scriptController.PrepareParameters(parameters);
				scriptController.Init();
				return scriptController.ConstructStateMachine();
			}
			return null;
		}

		/// <summary>
		/// 根据此Controller存储的信息生成实际的运行时Controller
		/// </summary>
		public void GenerateScriptController()
		{
			scriptableObjectAssetPath = AssetDatabase.GetAssetPath(this);
			if (scriptableObjectAssetPath != "")
			{
				scriptableObjectAssetPath = scriptableObjectAssetPath.Remove(scriptableObjectAssetPath.LastIndexOf("/") + 1);
			}
			string filePath = Application.dataPath.Replace("Assets", "") + scriptableObjectAssetPath;
			realScriptControllerName = name.Replace(" ", "");
			GenerateConstructHFSMCode(filePath);
			GenerateHFSMCustomMethodCode(filePath);
			AssetDatabase.Refresh();
		}

		/// <summary>
		/// 生成构造层状态机的代码
		/// </summary>
		private void GenerateConstructHFSMCode(string filePath)
		{
			StringBuilder generateString = new StringBuilder();
			StateMachineData rootData = stateMachines.Find(sm => sm.isRoot);
			stateMethods.Clear();
			serviceMethods.Clear();
			canExitMethods.Clear();
			generateString.AppendLine($"		StateMachineHandler.BeginStateMachine(this, \"{rootData.id}\")");
			GenerateConstructStateMachineCode(rootData, generateString, 3);
			generateString.AppendLine("			.EndHandle();");
			generateString.AppendLine("		return StateMachineHandler.EndStateMachine();");
			string constructScript =
			"using RPGCore.AI.HFSM;\n" +
			"//Automatically generated code\n" +
			$"[StateMachineController(ControllerName = \"{realScriptControllerName}\")]\n" +
			$"public partial class {realScriptControllerName} : StateMachineScriptController\n" +
			"{\n" +
			"	public override StateMachine ConstructStateMachine()\n" +
			"	{\n" +
			generateString.ToString() +
			"	}\n" +
			"}\n";
			FileStream fileStream = File.Create(filePath + realScriptControllerName + "_Construct.cs");
			byte[] byteArray = Encoding.UTF8.GetBytes(constructScript);
			fileStream.Write(byteArray, 0, byteArray.Length);
			fileStream.Close();
		}

		/// <summary>
		/// 生成构造的状态机中自定义方法的代码
		/// </summary>
		private void GenerateHFSMCustomMethodCode(string filePath)
		{
			FileStream fileStream = null;
			StringBuilder generateString = new StringBuilder();
			byte[] byteArray;
			//如果脚本存在 则进行修改;不存在则直接创建
			if (File.Exists(filePath + realScriptControllerName + ".cs"))
			{
				fileStream = File.OpenRead(filePath + realScriptControllerName + ".cs");
				byte[] buffer = new byte[1024];
				int bytesRead = fileStream.Read(buffer, 0, buffer.Length);
				while (bytesRead > 0)
				{
					generateString.Append(Encoding.Default.GetString(buffer, 0, bytesRead));
					bytesRead = fileStream.Read(buffer, 0, buffer.Length);
				}
				fileStream.Close();
				GenerateCustomMethodCode(generateString);
				byteArray = Encoding.UTF8.GetBytes(generateString.ToString());
			}
			else
			{
				GenerateCustomMethodCode(generateString);
				string customMethodScript =
				"using RPGCore.AI.HFSM;\n" +
				"using UnityEngine;\n" +
				$"public partial class {realScriptControllerName} : StateMachineScriptController\n" +
				"{\n" +
				"	public override void Init()\n" +
				"	{\n" +
				"	}\n" +
				"//Don't delete or modify the #region & #endregion\n" +
				"#region Method\n" +
				generateString.ToString() +
				"#endregion Method\n" +
				"}\n";
				byteArray = Encoding.UTF8.GetBytes(customMethodScript);
			}
			fileStream = File.Create(filePath + realScriptControllerName + ".cs");
			fileStream.Write(byteArray, 0, byteArray.Length);
			fileStream.Close();
		}

		/// <summary>
		/// 自顶向下生成构造状态机代码；
		/// </summary>
		private void GenerateConstructStateMachineCode(StateMachineData stateMachineData, StringBuilder constructString, int level)
		{
			List<StateBaseData> allStates = new List<StateBaseData>();
			allStates.AddRange(states);
			allStates.AddRange(stateMachines);
			string interval = "";
			for (int i = 0; i < level; i++) { interval += "\t"; }
			//先生成service
			for (int i = 0; i < stateMachineData.services.Count; i++)
			{
				ServiceData service = stateMachineData.services[i];
				constructString.AppendLine(interval + $".AddService(\"{service.id}\",ServiceType.{service.serviceType.ToString()},{service.customInterval})"
					+ $".OnService(on_{service.id}_service)");
				serviceMethods[$"on_{service.id}_service"] = $"{service.description}";
			}
			//再添加所有的state与StateMachine
			for (int i = 0; i < stateMachineData.childStates.Count; i++)
			{
				StateBaseData state = allStates.Find(s => s.id == stateMachineData.childStates[i]);
				if (state.stateType == StateType.StateMachine)
				{
					constructString.AppendLine(interval + $".AddStateMachine(\"{state.id}\", {(state.id == stateMachineData.defaultState).ToString().ToLower()})");
					GenerateConstructStateMachineCode(state as StateMachineData, constructString, level + 1);
				}
				else
				{
					if ((state as StateData).isTemporary)
					{
						constructString.AppendLine(interval + $".AddTemporaryState(\"{state.id}\")" + $".OnExecute(on_{state.id}_execute)");
					}
					else
					{
						constructString.AppendLine(interval + $".AddState(\"{state.id}\", {(state.id == stateMachineData.defaultState).ToString().ToLower()})" + $".OnExecute(on_{state.id}_execute)");
					}
					if ((state as StateData).canExitHandle)
					{
						constructString.AppendLine('\t' + interval + $".CanExit(can_{state.id}_exit)");
						canExitMethods[$"can_{state.id}_exit"] = (state as StateData).canExitDescription;
					}
					stateMethods[$"on_{state.id}_execute"] = $"{state.description}";
				}
			}
			//最后处理所有的Transition
			for (int i = 0; i < stateMachineData.transitions.Count; i++)
			{
				TransitionData transition = transitions.Find(t => t.id == stateMachineData.transitions[i]);
				//switch to
				StateBaseData state = allStates.Find(s => s.id == transition.to);
				if (state.stateType == StateType.State)
				{
					bool isTemporary = states.Find(s => s.id == transition.to).isTemporary;
					constructString.AppendLine(interval + $".SwitchHandle(\"{transition.from}\")" + $".ToState(\"{transition.to}\",{isTemporary.ToString().ToLower()})");
				}
				else
				{
					constructString.AppendLine(interval + $".SwitchHandle(\"{transition.from}\")" + $".ToStateMachine(\"{transition.to}\")");
				}
				//conditions
				foreach (string conditionName in transition.baseConditionsName)
				{
					constructString.AppendLine(interval + $".Condition(()=>{conditionName})");
				}
				foreach (var parameterCondition in transition.parameterConditionDatas)
				{
					Parameter param = parameters.Find(p => p.name == parameterCondition.parameterName);
					if (param.type == ParameterType.Bool)
					{
						string value = parameterCondition.compareValue >= 1.0f ? "true" : "false";
						constructString.AppendLine("\t" + interval + $".BoolCondition(\"{param.name}\",{value})");
					}
					else if (param.type == ParameterType.Trigger)
					{
						constructString.AppendLine("\t" + interval + $".TriggerCondition(\"{param.name}\")");
					}
					else if (param.type == ParameterType.Int)
					{
						constructString.AppendLine("\t" + interval + $".IntCondition(\"{param.name}\",CompareType.{parameterCondition.compareType},{parameterCondition.compareValue})");
					}
					else if (param.type == ParameterType.Float)
					{
						constructString.AppendLine("\t" + interval + $".FloatCondition(\"{param.name}\",CompareType.{parameterCondition.compareType},{parameterCondition.compareValue})");
					}
				}
			}
			constructString.AppendLine(interval + ".FinishHandle()");
		}

		/// <summary>
		/// 根据当前Controller中的信息生成自定义方法的代码
		/// </summary>
		private void GenerateCustomMethodCode(StringBuilder constructString)
		{
			if (constructString.Length == 0)
			{
				if (serviceMethods.Count == 0)
				{
					constructString.Append("	//StateMachine Service Code Here\n");
					constructString.Append("	//private void serviceMethodName(Service service, ServiceExecuteType type)\n");
				}
				foreach (var service in serviceMethods)
				{
					foreach (var item in GetStateMachineServiceMethodCode(service.Key, service.Value))
					{
						constructString.Append(item + '\n');
					}
					previousServiceMethodsName.Add(service.Key);
				}
				foreach (var state in stateMethods)
				{
					foreach (var item in GetStateMethodCode(state.Key, state.Value))
					{
						constructString.Append(item + '\n');
					}
					previousStateMethodsName.Add(state.Key);
				}
				if (canExitMethods.Count == 0)
				{
					constructString.Append("	//State Can Exit Code Here\n");
					constructString.Append("	//private bool stateName(State state)\n");
				}
				foreach (var canexit in canExitMethods)
				{
					foreach (var item in GetCanExitMethodCode(canexit.Key, canexit.Value))
					{
						constructString.Append(item + '\n');
					}
					previousCanExitMethodsName.Add(canexit.Key);
				}
			}
			else
			{
				List<string> stateMethodsTemp = stateMethods.Keys.ToList();
				List<string> serviceMethodsTemp = serviceMethods.Keys.ToList();
				List<string> canExitMethodsTemp = canExitMethods.Keys.ToList();
				//先找到新的没有生成的method
				foreach (var state in previousStateMethodsName)
				{
					if (stateMethods.ContainsKey(state))
					{
						stateMethods.Remove(state);
					}
				}
				foreach (var service in previousServiceMethodsName)
				{
					if (serviceMethods.ContainsKey(service))
					{
						serviceMethods.Remove(service);
					}
				}
				foreach (var canExit in previousCanExitMethodsName)
				{
					if (canExitMethods.ContainsKey(canExit))
					{
						canExitMethods.Remove(canExit);
					}
				}
				//记录当前有哪些method
				previousStateMethodsName.Clear();
				previousServiceMethodsName.Clear();
				previousCanExitMethodsName.Clear();
				previousServiceMethodsName = serviceMethodsTemp;
				previousStateMethodsName = stateMethodsTemp;
				previousCanExitMethodsName = canExitMethodsTemp;
				if (serviceMethods.Count != 0 || stateMethods.Count != 0 || canExitMethods.Count != 0)
				{
					List<string> newConstructString = new List<string>();
					string[] lines = constructString.ToString().Split("\n").Where(str => str != "").ToArray();
					int methodBegin = lines.ToList().FindIndex(str => str.Contains("#region Method")) + 1;
					int methodEnd = lines.ToList().FindIndex(str => str.Contains("#endregion Method"));
					bool hasNewState = stateMethods.Count != 0;
					bool hasNewService = serviceMethods.Count != 0;
					bool hasNewCanExit = canExitMethods.Count != 0;
					bool finishUpdate = false;
					for (int i = 0; i < methodBegin; i++)
					{
						newConstructString.Add(lines[i]);
					}
					for (int i = methodBegin; i < methodEnd; i++)
					{
						string line = lines[i];
						if (line.Contains("private") && !finishUpdate)
						{
							if (line.Contains("StateExecuteType") && hasNewState)
							{
								string des = newConstructString.Last();
								newConstructString.RemoveAt(newConstructString.FindLastIndex(str => str == des));
								foreach (var state in stateMethods)
								{
									string[] codes = GetStateMethodCode(state.Key, state.Value);
									foreach (var item in codes)
									{
										newConstructString.Add(item);
									}
								}
								newConstructString.Add(des);
								hasNewState = false;
							}
							else if (line.Contains("ServiceExecuteType") && hasNewService)
							{
								string des = newConstructString.Last();
								newConstructString.RemoveAt(newConstructString.FindLastIndex(str => str == des));
								foreach (var service in serviceMethods)
								{
									string[] codes = GetStateMachineServiceMethodCode(service.Key, service.Value);
									foreach (var item in codes)
									{
										newConstructString.Add(item);
									}
								}
								newConstructString.Add(des);
								hasNewService = false;
							}
							else if (line.Contains("bool") && hasNewCanExit)
							{
								string des = newConstructString.Last();
								newConstructString.RemoveAt(newConstructString.FindLastIndex(str => str == des));
								foreach (var canExit in canExitMethods)
								{
									string[] codes = GetCanExitMethodCode(canExit.Key, canExit.Value);
									foreach (var item in codes)
									{
										newConstructString.Add(item);
									}
								}
								newConstructString.Add(des);
								hasNewCanExit = false;
							}
							finishUpdate = !hasNewService && !hasNewState && !hasNewCanExit;
							newConstructString.Add(line);
						}
						else
						{
							newConstructString.Add(line);
						}
					}
					for (int i = methodEnd; i < lines.Length; i++)
					{
						newConstructString.Add(lines[i]);
					}
					constructString.Clear();
					foreach (string str in newConstructString)
					{
						constructString.Append(str + '\n');
					}
				}
			}
		}

#if UNITY_EDITOR

		/// <summary>
		/// 保存修改
		/// </summary>
		public void Save()
		{
			EditorUtility.SetDirty(this);
			AssetDatabase.SaveAssets();
		}

		/// <summary>
		///创建一个State
		/// </summary>
		public void CreateState(Rect rect, StateMachineData currentStateMachine)
		{
			int count = states.Count(s => s.id.Contains("NewState"));
			StateData state = new StateData()
			{
				id = "New State " + count,
				stateType = StateType.State,
				position = rect,
				isDefault = currentStateMachine.childStates.Count == 0,
			};
			states.Add(state);
			currentStateMachine.childStates.Add(state.id);
			if (state.isDefault)
			{
				currentStateMachine.defaultState = state.id;
			}
			Save();
		}

		/// <summary>
		///创建一个StateMachine
		/// </summary>
		public void CreateStateMachine(Rect rect, StateMachineData currentStateMachine)
		{
			int count = stateMachines.Count(s => s.id.Contains("New StateMachine"));
			StateMachineData stateMachine = new StateMachineData()
			{
				id = "New StateMachine " + count,
				stateType = StateType.StateMachine,
				position = rect,
				isDefault = currentStateMachine.childStates.Count == 0,
			};
			stateMachines.Add(stateMachine);
			currentStateMachine.childStates.Add(stateMachine.id);
			Save();
		}

		/// <summary>
		///创建一个Transition
		/// </summary>
		public void CreateTransition(StateMachineData stateMachine, StateBaseData from, StateBaseData to)
		{
			if (to.id == StateMachine.anyState ||
				to.id == StateMachine.entryState ||
				from == to)
			{
				return;
			}
			TransitionData transition = new TransitionData()
			{
				id = GUID.Generate().ToString(),
				from = from.id,
				to = to.id
			};
			transitions.Add(transition);
			stateMachine.transitions.Add(transition.id);
			Save();
		}

		/// <summary>
		///创建一个Parameter
		/// </summary>
		public void CreateParamter(ParameterType parameterType)
		{
			int count = parameters.Count(p => p.name.Contains("New Param"));
			string name = "New Param " + count;
			Parameter parameter = new Parameter(name, parameterType, 0.0f);
			parameters.Add(parameter);
			Save();
		}

		/// <summary>
		///创建一个Transition的ParamterCondition
		/// </summary>
		public void CreateParamterCondition(TransitionData transition)
		{
			ParameterConditionData parameterCondition = new ParameterConditionData();
			Parameter defaultParam = parameters.First();
			if (defaultParam != null)
			{
				parameterCondition.parameterName = defaultParam.name;
				parameterCondition.compareType = CompareType.Equal;
				parameterCondition.compareValue = 0.0f;
			}
			transition.parameterConditionDatas.Add(parameterCondition);
		}

		/// <summary>
		///创建一个StateMachine的Service
		/// </summary>
		public void CreateService(StateMachineData stateMachine, ServiceType serviceType)
		{
			ServiceData service = new ServiceData();
			service.serviceType = serviceType;
			int count = stateMachine.services.Count(s => s.id.Contains("NewService"));
			service.id = "NewService" + count;
			stateMachine.services.Add(service);
		}

		/// <summary>
		/// 删除State或StateMachine
		/// </summary>
		public void DeleteState(StateMachineData stateMachine, StateBaseData state)
		{
			if (state != null)
			{
				if (state.stateType == StateType.State)
				{
					states.Remove(states.Find(s => s.id == state.id));
				}
				else
				{
					StateMachineData machineData = (state as StateMachineData);
					List<StateBaseData> allStates = new List<StateBaseData>();
					allStates.AddRange(states.FindAll(s => machineData.childStates.Contains(s.id)));
					allStates.AddRange(stateMachines.FindAll(s => machineData.childStates.Contains(s.id)));
					foreach (var s in allStates)
					{
						DeleteState((state as StateMachineData), s);
					}
					stateMachines.Remove(stateMachines.Find(s => s.id == state.id));
					foreach (var transition in machineData.transitions)
					{
						transitions.Remove(transitions.Find(t => t.id == transition));
					}
				}
				stateMachine.childStates.Remove(state.id);
			}
			Save();
		}

		/// <summary>
		/// 删除Transition
		/// </summary>
		public void DeleteTransition(StateMachineData stateMachine, TransitionData transition)
		{
			stateMachine.transitions.Remove(transition.id);
			transitions.Remove(transition);
			Save();
		}

		/// <summary>
		/// 删除State或StateMachine时同步删除与此链接的Transition
		/// </summary>
		public void DeleteTransition(StateMachineData stateMachine, StateBaseData state)
		{
			List<TransitionData> datas = transitions.FindAll(t => t.from == state.id || t.to == state.id);
			stateMachine.transitions.RemoveAll(t => datas.Select(d => d.id).Contains(t));
			transitions.RemoveAll(t => datas.Contains(t));
			Save();
		}

		/// <summary>
		/// 删除一个Parameter
		/// </summary>
		public void DeleteParameter(int index)
		{
			Parameter parameter = parameters[index];
			foreach (var t in transitions)
			{
				var condition = t.parameterConditionDatas.Find(pc => pc.parameterName == parameter.name);
				t.parameterConditionDatas.Remove(condition);
			}
			parameters.Remove(parameter);
			Save();
		}

		/// <summary>
		/// 删除一个Transition中的ParameterCondition
		/// </summary>
		public void DeleteParameterCondition(TransitionData transition, int index)
		{
			transition.parameterConditionDatas.RemoveAt(index);
		}

		/// <summary>
		/// 删除一个StateMachine中的Service
		/// </summary>
		public void DeleteService(StateMachineData stateMachine, ServiceData service)
		{
			stateMachine.services.Remove(service);
		}

		/// <summary>
		/// 重命名一个Parameter
		/// </summary>
		public void RenameParameter(Parameter parameter, string newName)
		{
			if (string.IsNullOrEmpty(newName))
				return;
			if (parameters.Select(p => p.name).Contains(newName))
				return;
			foreach (var t in transitions)
			{
				var condition = t.parameterConditionDatas.Find(pc => pc.parameterName == parameter.name);
				if (condition != null)
				{
					condition.parameterName = newName;
				}
			}
			parameter.name = newName;
			Save();
		}

		/// <summary>
		/// 重命名一个State或StateMachine
		/// </summary>
		public void RenameState(StateBaseData state, string newName)
		{
			if (string.IsNullOrEmpty(newName)) return;
			foreach (var sm in stateMachines)
			{
				int index = sm.childStates.FindIndex(s => s == state.id);
				if (index != -1)
				{
					sm.childStates[index] = newName;
					if (state.isDefault)
					{
						sm.defaultState = newName;
					}
					break;
				}
			}
			foreach (var t in transitions)
			{
				if (t.to == state.id)
				{
					t.to = newName;
				}
				else if (t.from == state.id)
				{
					t.from = newName;
				}
			}
			state.id = newName;
		}

#endif

		private string[] GetStateMethodCode(string stateMethodName, string description)
		{
			string[] result = new string[4];
			result[0] = $"	//description:{description}";
			result[1] = $"	private void {stateMethodName}(State state, StateExecuteType type)";
			result[2] = "	{";
			result[3] = "	}";
			return result;
		}

		private string[] GetStateMachineServiceMethodCode(string serviceMethodName, string description)
		{
			string[] result = new string[4];
			result[0] = $"	//description:{description}";
			result[1] = $"	private void {serviceMethodName}(Service service, ServiceExecuteType type)";
			result[2] = "	{";
			result[3] = "	}";
			return result;
		}

		private string[] GetCanExitMethodCode(string stateName, string description)
		{
			string[] result = new string[5];
			result[0] = $"	//description:{description}";
			result[1] = $"	private bool {stateName}(State state)";
			result[2] = "	{";
			result[3] = "		return true;";
			result[4] = "	}";
			return result;
		}
	}
}