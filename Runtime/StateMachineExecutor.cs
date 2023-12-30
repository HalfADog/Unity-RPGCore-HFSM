using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using DogFramework;

namespace RPGCore.AI.HFSM
{
	public class StateMachineExecutor : MonoBehaviour
	{
		/// <summary>
		/// ��ǰִ�е�Controller
		/// </summary>
		public StateMachineExecutorController executorController;

		private StateMachineScriptController m_scriptController;
		public StateMachineScriptController scriptController => m_scriptController;

		/// <summary>
		/// ��ǰִ�е�Controller�ĸ�״̬��
		/// </summary>
		public StateMachine rootStateMachine => m_rootStateMachine;

		private StateMachine m_rootStateMachine;

		//����ʱ״̬��ִ��ջ
		private Stack<StateBundle> m_executeStateStack = new Stack<StateBundle>();

		public Stack<StateBundle> executeStateStack => m_executeStateStack;

		//��¼��ǰִ�е�״̬
		public State currentExecuteState => m_currentExecuteState;

		private State m_currentExecuteState = null;

		//��¼Stateִ����ʷ ����¼8��
		private RingStack<StateBundle> m_executeStateHistory = new RingStack<StateBundle>(8);

		private void Awake()
		{
			m_rootStateMachine = executorController.GetExecuteStateMachine(this, out m_scriptController);
			//if (m_rootStateMachine != null)
			//{
			//	ShowStateMachineTree(0, m_rootStateMachine);
			//}
		}

		private void Start()
		{
			InitStateMachineExecute();
			UpdateStackMachine();
		}

		private void FixedUpdate()
		{
			ExecuteStateMachineService(ServiceType.FixedUpdate);
		}

		private void Update()
		{
			ExecuteStateMachineService(ServiceType.Update);
			ExecuteStateMachineService(ServiceType.CustomInterval);
			m_currentExecuteState.OnLogic();
		}

		private void LateUpdate()
		{
			UpdateStackMachine();
		}

		/// <summary>
		/// ״̬��ִ�г�ʼ��
		/// </summary>
		public void InitStateMachineExecute()
		{
			m_executeStateStack.Clear();
			m_currentExecuteState = null;
			if (m_rootStateMachine != null)
			{
				FillExecuteStateStack(m_rootStateMachine);
			}
		}

		/// <summary>
		/// ִ��״̬������
		/// </summary>
		public void ExecuteStateMachineService(ServiceType type)
		{
			if (m_executeStateStack.Count == 0) return;
			foreach (var bundle in m_executeStateStack.Reverse())
			{
				if (bundle.services != null)
				{
					foreach (Service service in bundle.services.Where(s => s.serviceType == type))
					{
						if (type != ServiceType.CustomInterval) service.OnSercive();
						else
						{
							if (service.timer.Elapsed >= service.customInterval)
							{
								service.OnSercive();
								service.timer.Reset();
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// ����ִ��ת�� ��ִ��ջ�׵�ջ�� ��Global��Normal
		/// </summary>
		public Transition TryTransition()
		{
			List<StateBundle> stateBundles = m_executeStateStack.Reverse().ToList();
			//��ǰͨ����ת��
			Transition passedTransition = null;
			for (int i = 0; i < stateBundles.Count(); i++)
			{
				StateBundle bundle = stateBundles[i];
				//�����Ե�ǰ״̬Ϊ����Transition
				if (bundle.transitions is not null)
				{
					foreach (Transition transition in bundle.transitions)
					{
						if (transition.ShouldTransition() && (executeStateStack.Peek().state as State).OnExitRequset())
						{
							passedTransition = transition;
							break;
						}
					}
				}
				//���Ե�ǰ״̬�е�GlobalTransition
				var gTrans = bundle.GetGlobalTransitions();
				if (gTrans is not null)
				{
					foreach (Transition transition in gTrans)
					{
						if (transition.ShouldTransition() && (executeStateStack.Peek().state as State).OnExitRequset())
						{
							passedTransition = transition;
							break;
						}
					}
				}
				//������ʱ״̬�Ƿ�ִ��ת��
				if (bundle.state.stateType == StateType.State && (bundle.state as State).isTemporary)
				{
					State state = bundle.state as State;
					if (state.OnExitRequset())
					{
						passedTransition = new Transition(null, state, m_executeStateHistory.Peek().state.parentStateMachine);
						break;
					}
				}
			}
			return passedTransition;
		}

		/// <summary>
		/// ����״̬��ִ��
		/// </summary>
		public void UpdateStackMachine()
		{
			Transition passedTransition = TryTransition();
			if (passedTransition is not null)
			{
				StateBase transState = passedTransition.transitionType ==
					TransitionType.Global ? passedTransition.parentStateMachine : passedTransition.from;
				StateBase toState = passedTransition.to;
				//��ǰת����״̬�ǲ���һ����ʱ״̬
				if (transState.stateType == StateType.State && (transState as State).isTemporary)
				{
					m_executeStateStack.Clear();
					if (toState.parentStateMachine != null)
					{
						foreach (var state in toState.parentStateMachine.executeStackSnapshot.Reverse())
						{
							m_executeStateStack.Push(state);
						}
					}
					transState.OnExit();
				}
				else
				{
					//�Ȱ�ת��ǰ��״̬��ջ
					while (true)
					{
						StateBundle popState = m_executeStateStack.Pop();
						if (popState.state.stateType != StateType.StateMachine) popState.state.OnExit();
						else popState.services.ForEach(s => s.OnEndService());
						if (popState.state.id == transState.id)
						{
							if (passedTransition.transitionType == TransitionType.Global) m_executeStateStack.Push(popState);
							break;
						}
					}
				}
				//Debug.Log("toState : " + toState.id);
				//�ٽ�ת�����״̬��ջ
				FillExecuteStateStack(toState);
			}
		}

		/// <summary>
		/// ���ݴ����State���ִ��״̬ջ
		/// </summary>
		private void FillExecuteStateStack(StateBase state)
		{
			while (state.stateType == StateType.StateMachine)
			{
				m_executeStateStack.Push(new StateBundle(state));
				if (state.executeStackSnapshot == null)
				{
					state.SetExecuteStackSnapshot(m_executeStateStack.ToArray());
				}
				(state as StateMachine).services.ForEach(service => { service.OnBeginService(); });
				state = (state as StateMachine).defaultState;
				if (state is null)
				{
					Debug.LogError($"Can not find the default state/state machine in [{state.id}] state machine.");
					return;
				}
			}
			state.OnEnter();
			m_executeStateStack.Push(new StateBundle(state));
			if (state.executeStackSnapshot == null)
			{
				state.SetExecuteStackSnapshot(m_executeStateStack.ToArray());
			}
			m_currentExecuteState = state as State;
			//������ǰ״̬������ʱ״̬�ż�¼��ִ����ʷ��
			if (!(state as State).isTemporary) m_executeStateHistory.Push(m_executeStateStack.Peek());
			//Debug.Log("current:"+m_executeStateHistory.Peek().state.id+" count:"+m_executeStateHistory.Length);
		}

		//�����ã���ӡ״̬������״�ṹ
		private void ShowStateMachineTree(int level, StateMachine stateMachine)
		{
			string sj = "";
			for (int i = 0; i < level; i++) { sj += "\t"; }
			Debug.Log(sj + "��" + stateMachine.id + "��" + $"t:{stateMachine.transitions.Count} "
				+ $"global t:{stateMachine.transitions.FindAll(t => t.transitionType == TransitionType.Global).Count}");
			foreach (var state in stateMachine.states)
			{
				if (state.stateType == StateType.StateMachine)
				{
					ShowStateMachineTree(level + 1, state as StateMachine);
				}
				else
				{
					if (state.id != "Any")
					{
						Debug.Log(sj + "\t" + state.id);
					}
				}
			}
		}
	}

	public struct StateBundle
	{
		public StateBase state;
		public List<Transition> transitions;
		public List<Service> services;

		public StateBundle(StateBase s)
		{
			state = s;
			transitions = s.GetParentTransitionsStartWith();
			services = s.stateType == StateType.StateMachine ? (s as StateMachine).services : null;
		}

		/// <summary>
		/// ��ȡ����ǰStateMachine������ǵĻ����µ�GlobalTransition
		/// </summary>
		public List<Transition> GetGlobalTransitions()
		{
			if (state.stateType == StateType.StateMachine)
			{
				return (state as StateMachine).transitions.FindAll(t => t.transitionType == TransitionType.Global);
			}
			return null;
		}
	}
}