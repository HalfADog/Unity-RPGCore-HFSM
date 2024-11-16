using System;
using System.Collections.Generic;
using UnityEngine;

namespace RPGCore.AI.HFSM
{
	public enum ServiceType
	{
		Update,
		FixedUpdate,
		CustomInterval
	}

	public enum ServiceExecuteType
	{
		BeginService,
		Service,
		EndService
	}

	public class StateMachine : StateBase
	{
		public static string anyState => "Any";
		public static string entryState => "Entry";

		public List<StateBase> states => m_states;
		private List<StateBase> m_states;
		public bool isRoot => m_isRoot;
		private bool m_isRoot;
		public List<Transition> transitions => m_transitions;
		private List<Transition> m_transitions;
		public StateBase activeState => m_activeState;
		private StateBase m_activeState;
		public StateBase defaultState => m_defaultState;
		private StateBase m_defaultState = null;
		public List<Service> services => m_services;
		private List<Service> m_services;

		public State Any { get; } = new State(anyState);
		public State Entry { get; } = new State(entryState);

		public StateMachine(string id)
		{
			m_id = id;
			m_stateType = StateType.StateMachine;
			m_states = new List<StateBase>();
			m_transitions = new List<Transition>();
			m_services = new List<Service>();
			m_defaultState = null;
			m_states.Add(Any);
			m_states.Add(Entry);
		}

		public State AddState(State state, bool defaultState = false)
		{
			m_states.Add(state);
			state.SetParentStateMachine(this);
			if (defaultState) m_defaultState = state;
			return state;
		}

		public State AddState(string stateid, bool defaultState = false)
		{
			State state = null;
			state = m_states.Find(s => s.id.Equals(stateid)) as State;
			if (state == null)
			{
				state = new State(stateid);
				return AddState(state, defaultState);
			}
			Debug.LogWarning($"state {stateid} in {this.id} has already exist.");
			return state;
		}

		public StateMachine AddStateMachine(StateMachine stateMachine, bool defaultState = false)
		{
			m_states.Add(stateMachine);
			stateMachine.SetParentStateMachine(this);
			if (defaultState) m_defaultState = stateMachine;
			return stateMachine;
		}

		public StateMachine AddStateMachine(string stateMachineId, bool defaultState = false)
		{
			StateMachine stateMachine = null;
			stateMachine = m_states.Find(sm => sm.id.Equals(stateMachineId)) as StateMachine;
			if (stateMachine == null)
			{
				stateMachine = new StateMachine(stateMachineId);
				return AddStateMachine(stateMachine, defaultState);
			}
			Debug.LogWarning($"state machine {stateMachineId} in {this.id} has already exist.");
			return stateMachine;
		}

		public Transition AddTransition(string fromId, string toId,
			Func<Transition, bool> condition = null)
		{
			var from = m_states.Find(s => s.id.Equals(fromId));
			var to = m_states.Find(s => s.id.Equals(toId));
			return AddTransition(from, to, condition);
		}

		public Transition AddTransition(StateBase from,
			StateBase to,
			Func<Transition, bool> condition = null)
		{
			Transition transition = null;
			transition = new Transition(this, from, to, condition, from.id.Equals(Any.id) ? TransitionType.Global : TransitionType.Normal);
			m_transitions.Add(transition);
			return transition;
		}

		public Service AddService(string serviceId,
			Action<Service> service = null,
			Action<Service> beginService = null,
			Action<Service> endService = null,
			ServiceType type = ServiceType.Update, float customInterval = 0f)
		{
			Service _service = m_services.Find(s => s.id.Equals(serviceId));
			if (_service == null)
			{
				_service = new Service(serviceId, service, beginService, endService, type, customInterval);
				m_services.Add(_service);
			}
			return _service;
		}

		public bool Contains(string stateId) => m_states.Find(sm => sm.id.Equals(stateId)) != null;

		public void SetRoot() => m_isRoot = true;
	}

	[Serializable]
	public class StateMachineData : StateBaseData
	{
		public bool isRoot;
		public List<string> childStates = new List<string>();
		public string defaultState;
		public List<string> transitions = new List<string>();
		public List<ServiceData> services = new List<ServiceData>();

		[HideInInspector]
		public StateData any;

		[HideInInspector]
		public StateData entry;

		public StateMachineData()
		{
			any = new StateData()
			{
				id = StateMachine.anyState,
				stateType = StateType.State,
				position = new Rect(0, 100, StateBase.stateWidth, StateBase.stateHeight)
			};
			entry = new StateData()
			{
				id = StateMachine.entryState,
				stateType = StateType.State,
				position = new Rect(0, 400, StateBase.stateWidth, StateBase.stateHeight)
			};
			stateType = StateType.StateMachine;
		}
	}
}