using System.Collections.Generic;
using UnityEngine;

namespace RPGCore.AI.HFSM
{
	/// <summary>
	/// 运行时Controller
	/// </summary>
	public abstract class StateMachineScriptController
	{
		/// <summary>
		/// 当前Controller所属的GameObject
		/// </summary>
		public GameObject gameObject;

		/// <summary>
		/// 当前Controller所属的StateMachine Executor
		/// </summary>
		public StateMachineExecutor executor;

		/// <summary>
		/// 此运行时Controller所拥有的Parameters
		/// </summary>
		public Dictionary<string,Parameter> parameters = new Dictionary<string, Parameter>();

		/// <summary>
		///调用此方法构造运行时的状态机
		/// </summary>
		public abstract StateMachine ConstructStateMachine();

		/// <summary>
		/// 获取到当前正在执行的状态
		/// </summary>
		public string executeState => executor.executeStateStack.Peek().state.id;

		/// <summary>
		/// 当运行时状态机被构造时（On Game Awake）调用
		/// </summary>
		public virtual void Init()
		{
		}

		/// <summary>
		///将Controller中的Parameters复制一份用以Runtime
		/// </summary>
		public List<Parameter> PrepareParameters(List<Parameter> parameters)
		{
			if (this.parameters.Count != 0) return null;
			foreach (Parameter parameter in parameters)
			{
				Parameter param = new Parameter(parameter.name, parameter.type, parameter.baseValue);
				this.parameters[parameter.name] = param;
			}
			return parameters;
		}

		public void SetInteger(string id, int value)
		{
			Parameter data = parameters[id];
			if (data != null)
			{
				data.baseValue = value;
			}
		}

		public void SetFloat(string id, float value)
		{
			Parameter data = parameters[id];
			if (data != null)
			{
				data.baseValue = value;
			}
		}

		public void SetBool(string id, bool value)
		{
			Parameter data = parameters[id];
			if (data != null)
			{
				data.baseValue = value ? 1.0f : 0.0f;
			}
		}

		public void SetTrigger(string id)
		{
			Parameter data = parameters[id];
			if (data != null)
			{
				data.baseValue = 1.0f;
			}
		}

		public int GetInteger(string id) =>
			 Mathf.RoundToInt((parameters[id])?.baseValue ?? int.MinValue);

		public float GetFloat(string id) =>
			 parameters[id]?.baseValue ?? float.MinValue;

		public bool GetBool(string id) =>
			 (parameters[id]?.baseValue ?? 0.0f) >= 1.0f;

		public bool GetTrigger(string id) =>
			 (parameters[id]?.baseValue ?? 0.0f) >= 1.0f;

		public void PauseService(string serviceName)
		{
			foreach (var stateBundle in executor.currentExecuteState.executeStackSnapshot)
			{
				if (stateBundle.services != null)
				{
					foreach (var service in stateBundle.services)
					{
						if (service.id == serviceName)
						{
							service.Pause();
							return;
						}
					}
				}
			}
		}

		public void ContinueService(string serviceName)
		{
			foreach (var stateBundle in executor.currentExecuteState.executeStackSnapshot)
			{
				if (stateBundle.services != null)
				{
					foreach (var service in stateBundle.services)
					{
						if (service.id == serviceName)
						{
							service.Continue();
							return;
						}
					}
				}
			}
		}
	}
}