using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace RPGCore.AI.HFSM
{
	/// <summary>
	/// 状态机Controller;不用做运行时
	/// </summary>
	[CreateAssetMenu(fileName = "FSM Executor Controller", menuName = "State Machine/Executor Controller", order = 4)]
	public class StateMachineExecutorController : ScriptableObject
	{
		[HideInInspector]
		public string realScriptControllerName;

		//controller中的parameter
		public List<Parameter> parameters = new List<Parameter>();

		//controller中的state
		public List<StateData> states = new List<StateData>();

		//controller中的StateMachine
		public List<StateMachineData> stateMachines = new List<StateMachineData>()
		{
			new StateMachineData()
			{
				id = "Root",
				isRoot = true,
				stateType = StateType.StateMachine,
			}
		};

		//controller中的Transition
		public List<TransitionData> transitions = new List<TransitionData>();

		private string scriptableObjectAssetPath;

		//记录当前有哪些方法需要构造
		private Dictionary<string, string> stateMethods = new Dictionary<string, string>();
		private Dictionary<string, string> serviceMethods = new Dictionary<string, string>();
		private Dictionary<string, string> canExitMethods = new Dictionary<string, string>();

		//记录所有方法的信息包括代码
		public List<MethodBlock> methodBlocks = new List<MethodBlock>();
		public string beforeMethod;
		public string afterMethod;
		public bool isGenerated;
		//配置文件SO
		public StateMachineControllerConfig controllerConfig = new StateMachineControllerConfig();
		//文件生成位置
		public string generateFilePath = "";
		/// <summary>
		/// 根据此获取运行时Controller
		/// </summary>
		public StateMachineScriptController GetController()
		{
			List<Type> types = new List<Type>();
			foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				if (assembly.FullName.Contains("UnityEngine") || assembly.FullName.Contains("UnityEditor") ||
					assembly.FullName.Contains("Unity") || assembly.FullName.Contains("System") ||
					assembly.FullName.Contains("Microsoft")) continue;
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
#if UNITY_EDITOR
		/// <summary>
		/// 根据此Controller存储的信息生成实际的运行时Controller
		/// </summary>
		public void GenerateScriptController()
		{
			if (controllerConfig.CustomFilePath && controllerConfig.FilePath != "")
			{
				generateFilePath = Application.dataPath.Replace("Assets", "") + controllerConfig.FilePath+"/";
			}
			else
			{
				scriptableObjectAssetPath = AssetDatabase.GetAssetPath(this);
				if (scriptableObjectAssetPath != "")
				{
					scriptableObjectAssetPath = scriptableObjectAssetPath.Remove(scriptableObjectAssetPath.LastIndexOf("/") + 1);
				}
				generateFilePath = Application.dataPath.Replace("Assets", "") + scriptableObjectAssetPath;
			}
			realScriptControllerName = name.Replace(" ", "");
			GenerateConstructScript(generateFilePath);
			GenerateMethodScripts(generateFilePath);
			AssetDatabase.Refresh();
		}

		/// <summary>
		/// 生成构造层状态机的代码脚本
		/// </summary>
		private void GenerateConstructScript(string filePath)
		{
			StringBuilder generateString = new StringBuilder();
			StateMachineData rootData = stateMachines.Find(sm => sm.isRoot);
			stateMethods.Clear();
			serviceMethods.Clear();
			canExitMethods.Clear();
			generateString.AppendLine($"		StateMachineHandler.BeginStateMachine(this, \"{rootData.id}\")");
			GenerateConstructCode(rootData, generateString, 3);
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
		/// 自顶向下生成构造状态机代码；
		/// </summary>
		private void GenerateConstructCode(StateMachineData stateMachineData, StringBuilder constructString, int level)
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
					+ $".OnService(on_{service.id.Replace(" ","")}_service)");
				serviceMethods[stateMachineData.id+"/"+service.id] = $"{service.description}";
			}
			//再添加所有的state与StateMachine
			for (int i = 0; i < stateMachineData.childStates.Count; i++)
			{
				StateBaseData state = allStates.Find(s => s.id == stateMachineData.childStates[i]);
				if (state.stateType == StateType.StateMachine)
				{
					constructString.AppendLine(interval + $".AddStateMachine(\"{state.id}\", {(state.id == stateMachineData.defaultState).ToString().ToLower()})");
					GenerateConstructCode(state as StateMachineData, constructString, level + 1);
				}
				else
				{
					if ((state as StateData).isTemporary)
					{
						constructString.AppendLine(interval + $".AddTemporaryState(\"{state.id}\")" + $".OnExecute(on_{state.id.Replace(" ", "")}_execute)");
					}
					else
					{
						constructString.AppendLine(interval + $".AddState(\"{state.id}\", {(state.id == stateMachineData.defaultState).ToString().ToLower()})" + $".OnExecute(on_{state.id.Replace(" ", "")}_execute)");
					}
					if ((state as StateData).canExitHandle)
					{
						constructString.AppendLine('\t' + interval + $".CanExit(can_{state.id.Replace(" ", "")}_exit)");
						canExitMethods[state.id] = (state as StateData).canExitDescription;
					}
					stateMethods[state.id] = $"{state.description}";
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
		/// 生成方法代码文件
		/// </summary>
		public void GenerateMethodScripts(string filePath)
		{
			//如果当前Controller创建后没有生成过脚本文件
			if (!isGenerated)
			{
				//先将Controller中的内容转化为MethodBlocks中的数据
				foreach (var service in serviceMethods)
				{
					MethodBlock block = new MethodBlock(MethodType.Service, service.Key, service.Value);
					if (controllerConfig.DisperseGenerate)
					{
						block.independentGenerate = (stateMachines.Find(s => s.id == block.targetName.Split("/")[0])?.independentGenerate ?? false) || controllerConfig.DisperseAll;
					}
					else 
					{
						block.independentGenerate = false;
					}
					methodBlocks.Add(block);
				}
				foreach (var state in stateMethods)
				{
					MethodBlock block = new MethodBlock(MethodType.State, state.Key, state.Value);
					if (controllerConfig.DisperseGenerate)
					{
						block.independentGenerate = (states.Find(s => s.id == block.targetName)?.independentGenerate ?? false) || controllerConfig.DisperseAll;
					}
					else
					{
						block.independentGenerate = false;
					}
					methodBlocks.Add(block);
				}
				foreach (var canExit in canExitMethods)
				{
					MethodBlock block = new MethodBlock(MethodType.CanExit, canExit.Key, canExit.Value);
					//block.independentGenerate = states.Find(s => s.id == block.targetName)?.independentGenerate ?? false;
					methodBlocks.Add(block);
				}
				//后根据MethodBlocks中的信息生成脚本文件
				beforeMethod =
					"using RPGCore.AI.HFSM;\n" +
					"using UnityEngine;\n" +
					$"public partial class {realScriptControllerName} : StateMachineScriptController\n" +
					"{\n" +
					"	public override void Init()\n" +
					"	{\n" +
					"	}\n" +
					"//Don't delete or modify the #region & #endregion\n" +
					"#region Method\n";
				afterMethod =
					"#endregion Method\n" +
					"}\n";
				GenerateDefaultMethodsScript(filePath,methodBlocks.Where(mb => !mb.independentGenerate).ToList());
				foreach (var mb in methodBlocks.Where(mb => mb.independentGenerate))
				{
					GenerateIndependentMethodScript(filePath,mb);
				}
				isGenerated = true;
			}
			//如果生成过就根据Controller中的内容更新MethodBlocks中的信息
			else 
			{
				//先根据脚本文件更新methodblock中的内容
				UpdateMethodBlocksInfo(filePath+realScriptControllerName + "_Default.cs",true);
				foreach (var mb in methodBlocks.Where(mb=>mb.independentGenerate))
				{
					string suffix = $"_{mb.targetName}";
					if (mb.methodType == MethodType.Service) suffix = $"_{mb.targetName.Split("/")[1]}";
					UpdateMethodBlocksInfo(filePath + realScriptControllerName + suffix + ".cs");
				}
				//然后把已经删除的Controller中对应的MethodBlock的内容删除
				methodBlocks.ForEach(mb => mb.isDeleted = true);
				foreach (var service in serviceMethods)
				{
					var m = methodBlocks.Find(mb => mb.methodType == MethodType.Service && mb.targetName == service.Key);
					if (m == null)
					{
						MethodBlock block = new MethodBlock(MethodType.Service, service.Key, service.Value);
						methodBlocks.Add(block);
						m = block;
					}
					else m.isDeleted = false;
					bool pres = m.independentGenerate;
					if (controllerConfig.DisperseGenerate)
					{
						m.independentGenerate = (stateMachines.Find(s => s.id == m.targetName.Split("/")[0])?.independentGenerate ?? false) || controllerConfig.DisperseAll;
					}
					else
					{
						m.independentGenerate = false;
					}
					if (pres != m.independentGenerate) 
					{
						try
						{
							File.Delete(filePath + realScriptControllerName + $"_{m.targetName.Split("/")[0]}.cs");
						}
						catch
						{
							Debug.LogError("File delete failed");
						}
					}
				}
				foreach (var state in stateMethods)
				{
					var m = methodBlocks.Find(mb => mb.methodType == MethodType.State && mb.targetName == state.Key);
					if (m == null)
					{
						MethodBlock block = new MethodBlock(MethodType.State, state.Key, state.Value);
						methodBlocks.Add(block);
						m = block;
					}
					else m.isDeleted = false;
					bool pres = m.independentGenerate;
					if (controllerConfig.DisperseGenerate)
					{
						m.independentGenerate = (states.Find(s => s.id == m.targetName)?.independentGenerate ?? false) || controllerConfig.DisperseAll;
					}
					else 
					{
						m.independentGenerate = false;
					}
					if (pres != m.independentGenerate)
					{
						try
						{
							File.Delete(filePath + realScriptControllerName + $"_{m.targetName}.cs");
						}
						catch
						{
							Debug.LogError("File delete failed");
						}
					}
				}
				foreach (var canExit in canExitMethods)
				{
					var m = methodBlocks.Find(mb => mb.methodType == MethodType.CanExit && mb.targetName == canExit.Key);
					if (m == null)
					{
						MethodBlock block = new MethodBlock(MethodType.CanExit, canExit.Key, canExit.Value);
						methodBlocks.Add(block);
					}
					else m.isDeleted = false;
				}
				methodBlocks.RemoveAll(mb => mb.isDeleted);
				//最后生成脚本文件
				GenerateDefaultMethodsScript(filePath, methodBlocks.Where(mb => !mb.independentGenerate).ToList());
				foreach (var mb in methodBlocks.Where(mb => mb.independentGenerate))
				{
					GenerateIndependentMethodScript(filePath, mb);
				}
			}
			Save();
		}

		/// <summary>
		/// 生成非独立生成的方法代码脚本
		/// </summary>
		public void GenerateDefaultMethodsScript(string filePath,List<MethodBlock> targets) 
		{
			StringBuilder builder = new StringBuilder();
			byte[] byteArray;
			builder.Append(beforeMethod);
			builder.Append("\t//Service Methods\n");
			foreach (var mb in targets.Where(mb => mb.methodType == MethodType.Service))
			{
				mb.linePosition = builder.ToString().Split("\n").Length + 1;
				builder.Append(mb.MethodCode);
				builder.AppendLine();
			}
			builder.Append("\t//State Methods\n");
			foreach (var mb in targets.Where(mb => mb.methodType == MethodType.State))
			{
				mb.linePosition = builder.ToString().Split("\n").Length + 1;
				builder.Append(mb.MethodCode);
				var canExit = targets.Find(ce => ce.methodType == MethodType.CanExit && ce.targetName == mb.targetName);
				if (canExit != null)
				{
					builder.Append(canExit.MethodCode);
				}
				builder.AppendLine();
			}
			builder.Append(afterMethod);
			byteArray = Encoding.UTF8.GetBytes(builder.ToString());
			FileStream fileStream;
			fileStream = File.Create(filePath + realScriptControllerName+"_Default" + ".cs");
			fileStream.Write(byteArray, 0, byteArray.Length);
			fileStream.Close();

		}
		/// <summary>
		/// 生成需要独立生的方法代码脚本
		/// </summary>
		/// <param name="filePath"></param>
		/// <param name="iMethodBlock"></param>
		public void GenerateIndependentMethodScript(string filePath,MethodBlock iMethodBlock)
		{
			//TODO:针对于StateMachine中的Service来说,应该生成到同一个脚本文件中。
			StringBuilder builder = new StringBuilder();
			byte[] byteArray;
			builder.Append(
					"using RPGCore.AI.HFSM;\n" +
					"using UnityEngine;\n" +
					$"public partial class {realScriptControllerName} : StateMachineScriptController\n" +
					"{\n"+
					"//Don't delete or modify the #region & #endregion\n" +
					"#region Method\n");
			string suffix = "";
			if (iMethodBlock.methodType == MethodType.Service)
			{
				builder.Append(iMethodBlock.MethodCode);
				builder.AppendLine();
				suffix = $"_{iMethodBlock.targetName.Split("/")[0]}";
				foreach (var mb in methodBlocks.Where(m =>m.methodType == MethodType.Service && m.targetName != iMethodBlock.targetName && m.targetName.Split("/")[0] == iMethodBlock.targetName.Split("/")[0]))
				{
					builder.Append(mb.MethodCode);
					builder.AppendLine();
					mb.independentGenerate = false;
				}
			}
			else if (iMethodBlock.methodType == MethodType.State) 
			{
				builder.Append(iMethodBlock.MethodCode);
				var canExit = methodBlocks.Find(ce => ce.methodType == MethodType.CanExit && ce.targetName == iMethodBlock.targetName);
				if (canExit != null)
				{
					builder.Append(canExit.MethodCode);
				}
				builder.AppendLine();
				suffix = $"_{iMethodBlock.targetName}";
			}
			builder.Append("#endregion Method\n" + "}\n");
			byteArray = Encoding.UTF8.GetBytes(builder.ToString());
			FileStream fileStream;
			fileStream = File.Create(filePath + realScriptControllerName + suffix + ".cs");
			fileStream.Write(byteArray, 0, byteArray.Length);
			fileStream.Close();
		}
		/// <summary>
		/// 更新在指定脚本文件中的方法的方法体
		/// </summary>
		private void UpdateMethodBlocksInfo(string path,bool updateHeadAndTailBlock = false)
		{
			FileStream fileStream;
			StringBuilder methodsCode = new StringBuilder();
			//如果脚本存在 则进行修改
			if (File.Exists(path))
			{
				//读取现有代码
				fileStream = File.OpenRead(path);
				byte[] buffer = new byte[1024];
				int bytesRead = fileStream.Read(buffer, 0, buffer.Length);
				while (bytesRead > 0)
				{
					methodsCode.Append(Encoding.Default.GetString(buffer, 0, bytesRead));
					bytesRead = fileStream.Read(buffer, 0, buffer.Length);
				}
				fileStream.Close();

				List<string> lines = methodsCode.ToString().Split("\n").ToList();
				int methodBegin = lines.FindIndex(str => str.Contains("#region Method"));
				int methodEnd = lines.FindIndex(str => str.Contains("#endregion Method"));
				int leftBraceCount = 1, rightBraceCount = 0;
				string line = "";
				for (int i = methodBegin + 1; i <= methodEnd - 1; i++)
				{
					line = lines[i];
					if (Regex.IsMatch(line, "\\[.*\\(.*\\)\\]"))
					{
						//找到对应的MethodBlock并更新方法体
						int first = line.IndexOf("\"", 0, line.Length);
						int second = line.IndexOf("\"", first + 1, line.Length - first - 1);
						string name = line.Substring(first + 1, second - first - 1);
						MethodBlock block = methodBlocks.Find(mb => name == mb.targetName && mb.methodType.ToString() == line.Replace("\t", "").Substring(1, mb.methodType.ToString().Length));
						if (block != null)
						{
							//跳过第一个‘{’
							while (!lines[i].Contains("{")) i++;
							StringBuilder builder = new StringBuilder();
							//拿到方法体
							while (true)
							{
								i++;
								if (lines[i].Contains("{"))
								{
									++leftBraceCount;
								}
								if (lines[i].Contains("}"))
								{
									++rightBraceCount;
								}
								if (leftBraceCount == rightBraceCount) break;
								builder.Append(lines[i] + "\n");
							}
							//更新methodblock中的方法体
							block.methodBodyLines = builder.ToString();
							leftBraceCount = 1;
							rightBraceCount = 0;
						}
					}
				}
				if (updateHeadAndTailBlock) 
				{
					beforeMethod = "";
					afterMethod = "";
					for (int i = 0; i <= methodBegin; i++) { beforeMethod += lines[i] + "\n"; }
					for (int i = methodEnd; i < lines.Count; i++) { afterMethod += lines[i] + "\n"; }
				}
				Save();
			}
		}

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
		public StateData CreateState(Rect rect, StateMachineData currentStateMachine)
		{
			int count = states.Count(s => s.id.Contains("New State"));
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
			return state;
		}

		/// <summary>
		///创建一个StateMachine
		/// </summary>
		public StateMachineData CreateStateMachine(Rect rect, StateMachineData currentStateMachine)
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
			CreateState(new Rect(600, 400, StateBase.stateWidth, StateBase.stateHeight), stateMachine);
			Save();
			return stateMachine;
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
			Save();
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
			Save();
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
			Save();
		}

		/// <summary>
		/// 删除一个StateMachine中的Service
		/// </summary>
		public void DeleteService(StateMachineData stateMachine, ServiceData service)
		{
			stateMachine.services.Remove(service);
			Save();
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
		public void RenameState(StateBaseData state, string @new, bool description = false, bool canExit = false)
		{
			if (state.stateType == StateType.State)
			{
				if (canExit)
				{
					var m = methodBlocks.Find(mb => mb.methodType == MethodType.CanExit && mb.targetName == state.id);
					if (m != null)
					{
						m.targetDescription = @new;
						m.Update();
					}
					(state as StateData).canExitDescription = @new;
					Save();
					return;
				}
				else if (description)
				{
					var m = methodBlocks.Find(mb => mb.methodType == MethodType.State && mb.targetName == state.id);
					if (m != null)
					{
						m.targetDescription = @new;
						m.Update();
					}
					state.description = @new;
					Save();
					return;
				}
				else
				{
					if (string.IsNullOrEmpty(@new)) return;
					foreach (var mb in methodBlocks.Where(mb => mb.methodType != MethodType.Service && mb.targetName == state.id))
					{
						mb.targetName = @new;
						mb.Update();
					}
				}
			}
			foreach (var sm in stateMachines)
			{
				int index = sm.childStates.FindIndex(s => s == state.id);
				if (index != -1)
				{
					sm.childStates[index] = @new;
					if (state.isDefault)
					{
						sm.defaultState = @new;
					}
					break;
				}
			}
			foreach (var t in transitions)
			{
				if (t.to == state.id)
				{
					t.to = @new;
				}
				else if (t.from == state.id)
				{
					t.from = @new;
				}
			}
			state.id = @new;
			Save();
		}

		/// <summary>
		/// 重命名一个Service
		/// </summary>
		public void RenameService(ServiceData serviceData, string newName)
		{
			if (string.IsNullOrEmpty(newName)) return;
			foreach (var mb in methodBlocks.Where(mb => mb.methodType == MethodType.Service))
			{
				mb.targetName = newName;
				mb.Update();
			}
			serviceData.id = newName;
		}

		/// <summary>
		/// 跳转到对应的脚本文件
		/// </summary>
		public void JumpToScript(StateBaseData item) 
		{
			MethodBlock mb = methodBlocks.Find(
				m=>m.methodType != MethodType.CanExit 
				&& (item.stateType == StateType.State ? m.targetName : m.targetName.Split("/")[0]) == item.id
			);
			if (mb != null)
			{
				string filePath = controllerConfig.CustomFilePath? controllerConfig.FilePath + "/" : scriptableObjectAssetPath;
				if (controllerConfig.DisperseGenerate && (item.independentGenerate || controllerConfig.DisperseAll))
				{ 
					filePath += realScriptControllerName + $"_{item.id}.cs";
				}
				else
				{
					filePath += realScriptControllerName + "_Default.cs";
				}
				Debug.Log(filePath);
				if (!AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<TextAsset>(filePath), mb.linePosition)) 
				{
					Debug.Log("Jump failure.");
				}
			}
			else 
			{
				Debug.Log("Perhaps no corresponding code is currently generated, please generate the code and try again.");
			}
		}
#endif
	}

	public enum MethodType
	{
		State,
		Service,
		CanExit
	}

	/// <summary>
	/// 记录一个State或Service或CanExit方法代码块
	/// </summary>
	[System.Serializable]
	public class MethodBlock
	{
		public MethodType methodType;
		public string targetName;
		public string targetDescription;
		public string methodAttributeLine = "";
		public string methodHeadLine = "";
		public string methodBodyLines = "";
		public int linePosition = 0;
		public bool independentGenerate = false;
		public bool isDeleted = false;

		public string MethodCode
		{
			get
			{
				if (methodType == MethodType.CanExit)
				{
					if (methodBodyLines == "") methodBodyLines = "\t\treturn false;\n";
				}
				return
					methodAttributeLine +
					methodHeadLine +
					"\t{\n" +
					methodBodyLines +
					"\t}\n";
			}
		}

		public MethodBlock(MethodType methodType, string id, string description, bool independentGenerate = false)
		{
			this.methodType = methodType;
			this.targetName = id;
			this.targetDescription = description;
			Update();
			this.independentGenerate = independentGenerate;
		}

		/// <summary>
		/// 更新方法代码
		/// </summary>
		public void Update()
		{
			string attribute = "";
			if (methodType == MethodType.State) attribute = "State";
			else if (methodType == MethodType.Service) attribute = "Service";
			else attribute = "CanExit";
			if (targetDescription == "") methodAttributeLine = $"[{attribute}(\"{targetName}\")]\n";
			else methodAttributeLine = $"[{attribute}(\"{targetName}\",\"{targetDescription}\")]\n";
			if (methodType == MethodType.State) methodHeadLine = $"private void on_{targetName.Replace(" ", "")}_execute(State state, StateExecuteType type)\n";
			else if (methodType == MethodType.Service) methodHeadLine = $"private void on_{targetName.Split("/")[1].Replace(" ", "")}_service(Service service, ServiceExecuteType type)\n";
			else methodHeadLine = $"private bool can_{targetName.Replace(" ", "")}_exit(State state)\n";
			methodAttributeLine = "\t" + methodAttributeLine;
			methodHeadLine = "\t" + methodHeadLine;
		}
	}
}