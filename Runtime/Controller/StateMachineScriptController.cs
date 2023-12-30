using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

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
		public List<Parameter> parameters = new List<Parameter>();

		/// <summary>
		///���ô˷�����������ʱ��״̬��
		/// </summary>
		public abstract StateMachine ConstructStateMachine();

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
				this.parameters.Add(param);
			}
			return parameters;
		}

		public void SetInteger(string id, int value)
		{
			Parameter data = parameters.Find(param => param.type == ParameterType.Int && param.name == id);
			if (data != null)
			{
				data.baseValue = value;
			}
		}

		public void SetFloat(string id, float value)
		{
			Parameter data = parameters.Find(param => param.type == ParameterType.Float && param.name == id);
			if (data != null)
			{
				data.baseValue = value;
			}
		}

		public void SetBool(string id, bool value)
		{
			Parameter data = parameters.Find(param => param.type == ParameterType.Bool && param.name == id);
			if (data != null)
			{
				data.baseValue = value ? 1.0f : 0.0f;
			}
		}

		public void SetTrigger(string id)
		{
			Parameter data = parameters.Find(param => param.type == ParameterType.Bool && param.name == id);
			if (data != null)
			{
				data.baseValue = 1.0f;
			}
		}

		public int GetInteger(string id) =>
			 Mathf.RoundToInt((parameters.Find(param => param.type == ParameterType.Int && param.name == id))?.baseValue ?? int.MinValue);

		public float GetFloat(string id) =>
			 parameters.Find(param => param.type == ParameterType.Float && param.name == id)?.baseValue ?? float.MinValue;

		public bool GetBool(string id) =>
			 (parameters.Find(param => param.type == ParameterType.Bool && param.name == id)?.baseValue ?? 0.0f) >= 1.0f;

		public bool GetTrigger(string id) =>
			 (parameters.Find(param => param.type == ParameterType.Trigger && param.name == id)?.baseValue ?? 0.0f) >= 1.0f;
	}
}