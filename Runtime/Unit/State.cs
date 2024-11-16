using System;
using UnityEngine;

namespace RPGCore.AI.HFSM
{
	public enum StateExecuteType
	{
		OnEnter,
		OnLogic,
		OnExit,
	}

	public class State : StateBase
	{
		private Action<State> m_onEnter;
		private Action<State> m_onLogic;
		private Action<State> m_onExit;
		private Action<State, StateExecuteType> m_onExecute;
		private Func<State, bool> m_canExit;

		public bool isTemporary => m_isTemporary;
		protected bool m_isTemporary;

		public Timer timer => m_timer;
		private Timer m_timer;

		public State(string id,
			Action<State> enter = null,
			Action<State> logic = null,
			Action<State> exit = null,
			Func<State, bool> canExit = null)
		{
			m_id = id;
			m_stateType = StateType.State;
			//m_needsExitTime = false;
			m_onEnter = enter;
			m_onLogic = logic;
			m_onExit = exit;
			m_canExit = canExit;
			m_timer = new Timer();
		}

		public void SetOnEnter(Action<State> enter)
		{
			m_onEnter = enter ?? throw new ArgumentNullException(nameof(enter));
		}

		public void SetOnLogic(Action<State> logic)
		{
			m_onLogic = logic ?? throw new ArgumentNullException(nameof(logic));
		}

		public void SetOnExit(Action<State> exit)
		{
			m_onExit = exit ?? throw new ArgumentNullException(nameof(exit));
		}

		public void SetOnExecute(Action<State, StateExecuteType> execute)
		{
			m_onExecute = execute ?? throw new ArgumentNullException(nameof(execute));
		}

		public void SetCanExit(Func<State, bool> canExit)
		{
			m_canExit = canExit ?? throw new ArgumentNullException(nameof(canExit));
		}

		public override void OnEnter()
		{
			m_timer.Reset();
			if (m_onEnter is not null)
			{
				m_onEnter.Invoke(this);
				return;
			}
			OnExecute(StateExecuteType.OnEnter);
		}

		public override void OnLogic()
		{
			if (m_onLogic is not null)
			{
				m_onLogic.Invoke(this);
				return;
			}
			OnExecute(StateExecuteType.OnLogic);
		}

		public override void OnExit()
		{
			if (m_onExit is not null)
			{
				m_onExit.Invoke(this);
				return;
			}
			OnExecute(StateExecuteType.OnExit);
		}

		public void OnExecute(StateExecuteType executeType)
		{
			m_onExecute?.Invoke(this, executeType);
		}

		public override bool OnExitRequset()
		{
			return m_canExit?.Invoke(this) ?? true;
		}

		/// <summary>
		/// ÉèÖÃ×´Ì¬ÊÇ·ñÎªÁÙÊ±×´Ì¬
		/// </summary>
		public void SetIsTemporary(bool isTemporary) => m_isTemporary = isTemporary;
	}

	[Serializable]
	public class StateData : StateBaseData
	{
		public bool isTemporary = false;
		public bool canExitHandle = false;
		public string canExitDescription;
		public StateData()
		{
			stateType = StateType.State;
		}
	}
}