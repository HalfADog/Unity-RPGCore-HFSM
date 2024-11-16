using System.Collections.Generic;
using UnityEngine;

namespace RPGCore.AI.HFSM
{
	/// <summary>
	/// ����ʱController
	/// </summary>
	public abstract class StateMachineScriptController
	{
		/// <summary>
		/// ��ǰController������GameObject
		/// </summary>
		public GameObject gameObject;

		/// <summary>
		/// ��ǰController������StateMachine Executor
		/// </summary>
		public StateMachineExecutor executor;

		/// <summary>
		/// ������ʱController��ӵ�е�Parameters
		/// </summary>
		public Dictionary<string,Parameter> parameters = new Dictionary<string, Parameter>();

		/// <summary>
		///���ô˷�����������ʱ��״̬��
		/// </summary>
		public abstract StateMachine ConstructStateMachine();

		/// <summary>
		/// ��ȡ����ǰ����ִ�е�״̬
		/// </summary>
		public string executeState => executor.executeStateStack.Peek().state.id;

		/// <summary>
		/// ������ʱ״̬��������ʱ��On Game Awake������
		/// </summary>
		public virtual void Init()
		{
		}

		/// <summary>
		///��Controller�е�Parameters����һ������Runtime
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