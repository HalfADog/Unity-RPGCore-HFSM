using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RPGCore.AI.HFSM
{
	public enum StateType
	{
		State,
		StateMachine
	}

	public class StateBase
	{
#if UNITY_EDITOR
		public static int stateWidth => 480;
		public static int stateHeight => 90;
#endif
		public string id => m_id;
		protected string m_id;

		public StateType stateType => m_stateType;
		protected StateType m_stateType;

		public StateMachine parentStateMachine => m_parentStateMachine;
		protected StateMachine m_parentStateMachine;

		public StateBundle[] executeStackSnapshot => m_executeStackSnapshot;
		protected StateBundle[] m_executeStackSnapshot = null;

		public virtual void OnEnter()
		{ }

		public virtual void OnLogic()
		{ }

		public virtual void OnExit()
		{ }

		public virtual bool OnExitRequset()
		{ return true; }

		/// <summary>
		/// ���ø�״̬��
		/// </summary>
		public void SetParentStateMachine(StateMachine parent)
		{
			m_parentStateMachine = parent;
		}

		/// <summary>
		/// �ڸ�StateMachine�л�ȡ���Ե�ǰstate��StateMachineΪ����Transition
		/// </summary>
		public List<Transition> GetParentTransitionsStartWith()
		{
			if (parentStateMachine is null) return null;
			return parentStateMachine.transitions.Where(t => t.transitionType != TransitionType.Global && t.from.id == id).ToList();
		}

		/// <summary>
		/// ���õ�ǰ״ִ̬��ʱ��ִ��ջ���գ�������ʱ��ִ��ջ�ָ�����ǰ״ִ̬��ʱ������
		/// </summary>
		public void SetExecuteStackSnapshot(StateBundle[] stateBundles)
		{
			if (m_executeStackSnapshot != null) return;
			m_executeStackSnapshot = stateBundles;
		}
	}

	[Serializable]
	public class StateBaseData
	{
#if UNITY_EDITOR

		[HideInInspector]
		public Rect position;

		[HideInInspector]
		public bool isExecuting;

		//�Ƿ�������ɽű�
		[HideInInspector]
		public bool independentGenerate;
#endif
		public string id;
		public StateType stateType;
		public bool isDefault;

		[Multiline]
		public string description;
	}
}