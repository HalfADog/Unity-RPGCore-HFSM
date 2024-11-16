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
	/// ״̬��Controller;����������ʱ
	/// </summary>
	[CreateAssetMenu(fileName = "FSM Executor Controller", menuName = "State Machine/Executor Controller", order = 4)]
	public class StateMachineExecutorController : ScriptableObject
	{
		[HideInInspector]
		public string realScriptControllerName;

		//controller�е�parameter
		public List<Parameter> parameters = new List<Parameter>();

		//controller�е�state
		public List<StateData> states = new List<StateData>();

		//controller�е�StateMachine
		public List<StateMachineData> stateMachines = new List<StateMachineData>()
		{
			new StateMachineData()
			{
				id = "Root",
				isRoot = true,
				stateType = StateType.StateMachine,
			}
		};

		//controller�е�Transition
		public List<TransitionData> transitions = new List<TransitionData>();

		private string scriptableObjectAssetPath;

		//��¼��ǰ����Щ������Ҫ����
		private Dictionary<string, string> stateMethods = new Dictionary<string, string>();
		private Dictionary<string, string> serviceMethods = new Dictionary<string, string>();
		private Dictionary<string, string> canExitMethods = new Dictionary<string, string>();

		//��¼���з�������Ϣ��������
		public List<MethodBlock> methodBlocks = new List<MethodBlock>();
		public string beforeMethod;
		public string afterMethod;
		public bool isGenerated;
		//�����ļ�SO
		public StateMachineControllerConfig controllerConfig = new StateMachineControllerConfig();
		//�ļ�����λ��
		public string generateFilePath = "";
		/// <summary>
		/// ���ݴ˻�ȡ����ʱController
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
		/// ��������ʱController���ɲ���ʼ��״̬��
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
		/// ���ݴ�Controller�洢����Ϣ����ʵ�ʵ�����ʱController
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
		/// ���ɹ����״̬���Ĵ���ű�
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
		/// �Զ��������ɹ���״̬�����룻
		/// </summary>
		private void GenerateConstructCode(StateMachineData stateMachineData, StringBuilder constructString, int level)
		{
			List<StateBaseData> allStates = new List<StateBaseData>();
			allStates.AddRange(states);
			allStates.AddRange(stateMachines);
			string interval = "";
			for (int i = 0; i < level; i++) { interval += "\t"; }
			//������service
			for (int i = 0; i < stateMachineData.services.Count; i++)
			{
				ServiceData service = stateMachineData.services[i];
				constructString.AppendLine(interval + $".AddService(\"{service.id}\",ServiceType.{service.serviceType.ToString()},{service.customInterval})"
					+ $".OnService(on_{service.id.Replace(" ","")}_service)");
				serviceMethods[stateMachineData.id+"/"+service.id] = $"{service.description}";
			}
			//��������е�state��StateMachine
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
			//��������е�Transition
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
		/// ���ɷ��������ļ�
		/// </summary>
		public void GenerateMethodScripts(string filePath)
		{
			//�����ǰController������û�����ɹ��ű��ļ�
			if (!isGenerated)
			{
				//�Ƚ�Controller�е�����ת��ΪMethodBlocks�е�����
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
				//�����MethodBlocks�е���Ϣ���ɽű��ļ�
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
			//������ɹ��͸���Controller�е����ݸ���MethodBlocks�е���Ϣ
			else 
			{
				//�ȸ��ݽű��ļ�����methodblock�е�����
				UpdateMethodBlocksInfo(filePath+realScriptControllerName + "_Default.cs",true);
				foreach (var mb in methodBlocks.Where(mb=>mb.independentGenerate))
				{
					string suffix = $"_{mb.targetName}";
					if (mb.methodType == MethodType.Service) suffix = $"_{mb.targetName.Split("/")[1]}";
					UpdateMethodBlocksInfo(filePath + realScriptControllerName + suffix + ".cs");
				}
				//Ȼ����Ѿ�ɾ����Controller�ж�Ӧ��MethodBlock������ɾ��
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
				//������ɽű��ļ�
				GenerateDefaultMethodsScript(filePath, methodBlocks.Where(mb => !mb.independentGenerate).ToList());
				foreach (var mb in methodBlocks.Where(mb => mb.independentGenerate))
				{
					GenerateIndependentMethodScript(filePath, mb);
				}
			}
			Save();
		}

		/// <summary>
		/// ���ɷǶ������ɵķ�������ű�
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
		/// ������Ҫ�������ķ�������ű�
		/// </summary>
		/// <param name="filePath"></param>
		/// <param name="iMethodBlock"></param>
		public void GenerateIndependentMethodScript(string filePath,MethodBlock iMethodBlock)
		{
			//TODO:�����StateMachine�е�Service��˵,Ӧ�����ɵ�ͬһ���ű��ļ��С�
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
		/// ������ָ���ű��ļ��еķ����ķ�����
		/// </summary>
		private void UpdateMethodBlocksInfo(string path,bool updateHeadAndTailBlock = false)
		{
			FileStream fileStream;
			StringBuilder methodsCode = new StringBuilder();
			//����ű����� ������޸�
			if (File.Exists(path))
			{
				//��ȡ���д���
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
						//�ҵ���Ӧ��MethodBlock�����·�����
						int first = line.IndexOf("\"", 0, line.Length);
						int second = line.IndexOf("\"", first + 1, line.Length - first - 1);
						string name = line.Substring(first + 1, second - first - 1);
						MethodBlock block = methodBlocks.Find(mb => name == mb.targetName && mb.methodType.ToString() == line.Replace("\t", "").Substring(1, mb.methodType.ToString().Length));
						if (block != null)
						{
							//������һ����{��
							while (!lines[i].Contains("{")) i++;
							StringBuilder builder = new StringBuilder();
							//�õ�������
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
							//����methodblock�еķ�����
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

#if UNITY_EDITOR

		/// <summary>
		/// �����޸�
		/// </summary>
		public void Save()
		{
			EditorUtility.SetDirty(this);
			AssetDatabase.SaveAssets();
		}

		/// <summary>
		///����һ��State
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
		///����һ��StateMachine
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
		///����һ��Transition
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
		///����һ��Parameter
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
		///����һ��Transition��ParamterCondition
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
		///����һ��StateMachine��Service
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
		/// ɾ��State��StateMachine
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
		/// ɾ��Transition
		/// </summary>
		public void DeleteTransition(StateMachineData stateMachine, TransitionData transition)
		{
			stateMachine.transitions.Remove(transition.id);
			transitions.Remove(transition);
			Save();
		}

		/// <summary>
		/// ɾ��State��StateMachineʱͬ��ɾ��������ӵ�Transition
		/// </summary>
		public void DeleteTransition(StateMachineData stateMachine, StateBaseData state)
		{
			List<TransitionData> datas = transitions.FindAll(t => t.from == state.id || t.to == state.id);
			stateMachine.transitions.RemoveAll(t => datas.Select(d => d.id).Contains(t));
			transitions.RemoveAll(t => datas.Contains(t));
			Save();
		}

		/// <summary>
		/// ɾ��һ��Parameter
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
		/// ɾ��һ��Transition�е�ParameterCondition
		/// </summary>
		public void DeleteParameterCondition(TransitionData transition, int index)
		{
			transition.parameterConditionDatas.RemoveAt(index);
			Save();
		}

		/// <summary>
		/// ɾ��һ��StateMachine�е�Service
		/// </summary>
		public void DeleteService(StateMachineData stateMachine, ServiceData service)
		{
			stateMachine.services.Remove(service);
			Save();
		}

		/// <summary>
		/// ������һ��Parameter
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
		/// ������һ��State��StateMachine
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
		/// ������һ��Service
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
		/// ��ת����Ӧ�Ľű��ļ�
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
	/// ��¼һ��State��Service��CanExit���������
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
		/// ���·�������
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