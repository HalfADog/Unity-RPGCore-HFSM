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
		/// 当前执行的Controller
		/// </summary>
		public StateMachineExecutorController executorController;

		private StateMachineScriptController m_scriptController;
		public StateMachineScriptController scriptController => m_scriptController;

		/// <summary>
		/// 当前执行的Controller的根状态机
		/// </summary>
		public StateMachine rootStateMachine => m_rootStateMachine;

		private StateMachine m_rootStateMachine;

		//运行时状态机执行栈
		private Stack<StateBundle> m_executeStateStack = new Stack<StateBundle>();

		public Stack<StateBundle> executeStateStack => m_executeStateStack;

		//记录当前执行的状态
		public State currentExecuteState => m_currentExecuteState;

		private State m_currentExecuteState = null;

		//记录State执行历史 最大记录8个
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
		/// 状态机执行初始化
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
		/// 执行状态机服务
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
		/// 尝试执行转换 由执行栈底到栈顶 先Global再Normal
		/// </summary>
		public Transition TryTransition()
		{
			List<StateBundle> stateBundles = m_executeStateStack.Reverse().ToList();
			//当前通过的转换
			Transition passedTransition = null;
			for (int i = 0; i < stateBundles.Count(); i++)
			{
				StateBundle bundle = stateBundles[i];
				//尝试以当前状态为起点的Transition
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
				//尝试当前状态中的GlobalTransition
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
				//尝试临时状态是否执行转换
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
		/// 更新状态机执行
		/// </summary>
		public void UpdateStackMachine()
		{
			Transition passedTransition = TryTransition();
			if (passedTransition is not null)
			{
				StateBase transState = passedTransition.transitionType ==
					TransitionType.Global ? passedTransition.parentStateMachine : passedTransition.from;
				StateBase toState = passedTransition.to;
				//当前转换的状态是不是一个临时状态
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
					//先把转换前的状态出栈
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
				//再将转换后的状态入栈
				FillExecuteStateStack(toState);
			}
		}

		/// <summary>
		/// 根据传入的State填充执行状态栈
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
			//仅当当前状态不是临时状态才记录进执行历史中
			if (!(state as State).isTemporary) m_executeStateHistory.Push(m_executeStateStack.Peek());
			//Debug.Log("current:"+m_executeStateHistory.Peek().state.id+" count:"+m_executeStateHistory.Length);
		}

		//测试用，打印状态机的树状结构
		private void ShowStateMachineTree(int level, StateMachine stateMachine)
		{
			string sj = "";
			for (int i = 0; i < level; i++) { sj += "\t"; }
			Debug.Log(sj + "【" + stateMachine.id + "】" + $"t:{stateMachine.transitions.Count} "
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
		/// 获取到当前StateMachine（如果是的话）下的GlobalTransition
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