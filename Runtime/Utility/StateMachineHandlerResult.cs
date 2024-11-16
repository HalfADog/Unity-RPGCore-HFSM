using System;

namespace RPGCore.AI.HFSM
{
	public class StateMachineHandlerResult
	{
		/// <summary>
		/// ��һ�β����Ľ����
		/// </summary>
		public StateMachineHandlerResult previousHandleResult => m_previousHandleResult;

		protected StateMachineHandlerResult m_previousHandleResult;

		/// <summary>
		/// ��ǰ������State��StateMachine�ĸ�StateMachine
		/// </summary>
		public StateMachine parentStateMachine => m_parentStateMachine;

		protected StateMachine m_parentStateMachine;

		/// <summary>
		/// ��ǰ��������State��StateMachine
		/// </summary>
		public StateBase handledState => m_handledState;

		protected StateBase m_handledState;

		/// <summary>
		/// ����һ�β���������һ��Transition
		/// </summary>
		public Transition createdTransition => m_createdTransition;

		protected Transition m_createdTransition;

		/// <summary>
		/// ����һ�β���������һ��Service
		/// </summary>
		public Service createdService => m_createdService;

		private Service m_createdService;

		/// <summary>
		/// ���ص�ǰ�Ƿ������µ�һ�����������StateMachine���һ�β������ֶ�Ϊ��
		/// </summary>
		private bool IsBeginNewLevel => parentStateMachine.id.Equals(handledState.id);

		public StateMachineHandlerResult(StateMachineHandlerResult previousHandleResult,
			StateMachine parentStateMachine,
			StateBase handledState,
			Transition createdTransition = null,
			Service createdService = null)
		{
			m_previousHandleResult = previousHandleResult;
			m_parentStateMachine = parentStateMachine;
			m_handledState = handledState;
			m_createdTransition = createdTransition;
			m_createdService = createdService;
		}

		/// <summary>
		/// Ϊ��ǰ������StateMachine���һ��state
		/// </summary>
		public StateMachineHandlerResult AddState(string stateId, bool isDefault = false)
		{
			//Debug.Log($"add [{stateId}] state to [{parentStateMachine.id}].");
			var state = parentStateMachine.AddState(stateId, isDefault);
			return new StateMachineHandlerResult(IsBeginNewLevel ? this : previousHandleResult, parentStateMachine, state);
		}

		public StateMachineHandlerResult AddTemporaryState(string stateId)
		{
			//Debug.Log($"add [{stateId}] state to [{parentStateMachine.id}].");
			var state = parentStateMachine.AddState(stateId);
			state.SetIsTemporary(true);
			return new StateMachineHandlerResult(IsBeginNewLevel ? this : previousHandleResult, parentStateMachine, state);
		}

		/// <summary>
		/// Ϊ��ǰ������StateMachine���һ��StateMachine
		/// </summary>
		public StateMachineHandlerResult AddStateMachine(string stateMachineId, bool isDefault = false)
		{
			//Debug.Log($"add [{stateMachineId}] state machine to [{parentStateMachine.id}].");
			var stateMachine = parentStateMachine.AddStateMachine(stateMachineId, isDefault);
			return new StateMachineHandlerResult(IsBeginNewLevel ? this : previousHandleResult, stateMachine, stateMachine);
		}

		/// <summary>
		/// Ϊ��ǰ������state��StateMachine���һ����Ŀ��state��Transition
		/// ���Ŀ��state�����ڣ��򴴽�һ��state
		/// </summary>
		public StateMachineHandlerResult ToState(string stateId, bool isTemporary = false)
		{
			if (IsBeginNewLevel)
			{
				throw new Exception($"have not any handled state in {parentStateMachine.id} state machine." +
					$"you should first add a state or state machine and then call 'ToState'.");
			}
			else
			{
				var state = parentStateMachine.AddState(stateId);
				state.SetIsTemporary(isTemporary);
				//Debug.Log($"add transition from [{handledState.id}] to [{state.id}]");
				return new StateMachineHandlerResult(previousHandleResult, parentStateMachine, state,
					parentStateMachine.AddTransition(handledState, state));
			}
		}

		/// <summary>
		/// Ϊ��ǰ������state��StateMachine���һ����Ŀ��StateMachine��Transition
		/// ���Ŀ��StateMachine�����ڣ��򴴽�һ�����Ŀ��StateMachine������
		/// </summary>
		public StateMachineHandlerResult ToStateMachine(string stateMachineId)
		{
			if (IsBeginNewLevel)
			{
				throw new Exception($"have any handled state in {parentStateMachine.id} state machine." +
					$"you should first add a state or state machine and then call 'ToStateMachine'.");
			}
			else
			{
				bool smIsExist = parentStateMachine.Contains(stateMachineId);
				var stateMachine = parentStateMachine.AddStateMachine(stateMachineId);
				m_createdTransition = parentStateMachine.AddTransition(handledState, stateMachine);
				//Debug.Log($"add transition from [{handledState.id}] to [{stateMachine.id}]");
				return new StateMachineHandlerResult(previousHandleResult, smIsExist ? parentStateMachine : stateMachine, smIsExist ? handledState : stateMachine,
					createdTransition);
			}
		}

		/// <summary>
		/// Ϊ��ǰ�����󴴽���Transition��� baseCondition
		/// </summary>
		public StateMachineHandlerResult Condition(Func<Transition, bool> condition)
		{
			if (m_createdTransition != null)
			{
				m_createdTransition.AddBaseCondition(condition);
				//Debug.Log($"add condition from [{createdTransition.from.id}] to [{createdTransition.to.id}]");
				return new StateMachineHandlerResult(previousHandleResult, parentStateMachine, handledState, createdTransition);
			}
			throw new Exception($"no transition have been created to add conditions." +
				$"you should call this after 'ToState' or 'ToStateMachine'.");
		}

		/// <summary>
		/// Ϊ��ǰ�����󴴽���Transition��� paramterCondition
		/// </summary>
		public StateMachineHandlerResult Condition(string paramterName, ParameterType parameterType, CompareType compareType, object value)
		{
			if (m_createdTransition != null)
			{
				Parameter parameter = StateMachineHandler.currentHandledController.parameters[paramterName];
				if (parameter == null)
				{
					throw new Exception($"paramter named {paramterName} is not exist.");
				}
				m_createdTransition.AddParamterCondition(parameter, compareType, value);
				//Debug.Log($"add condition from [{createdTransition.from.id}] to [{createdTransition.to.id}]");
				return new StateMachineHandlerResult(previousHandleResult, parentStateMachine, handledState, createdTransition);
			}
			throw new Exception($"no transition have been created to add conditions." +
				$"you should call this after 'ToState' or 'ToStateMachine'.");
		}

		public StateMachineHandlerResult BoolCondition(string paramterName, bool value)
		{
			return Condition(paramterName, ParameterType.Bool, CompareType.Equal, value);
		}

		public StateMachineHandlerResult IntCondition(string paramterName, CompareType compareType, int value)
		{
			return Condition(paramterName, ParameterType.Int, compareType, value);
		}

		public StateMachineHandlerResult FloatCondition(string paramterName, CompareType compareType, float value)
		{
			return Condition(paramterName, ParameterType.Float, compareType, value);
		}

		public StateMachineHandlerResult TriggerCondition(string paramterName)
		{
			return Condition(paramterName, ParameterType.Trigger, CompareType.Equal, true);
		}

		/// <summary>
		/// ����һ���뵱ǰ�����󴴽���Transition�����෴��Transition
		/// </summary>
		public StateMachineHandlerResult Reverse(bool reverseCondition = false)
		{
			if (m_createdTransition != null)
			{
				//Debug.Log($"reverse transition from [{createdTransition.from.id}] to [{createdTransition.to.id}]");
				m_createdTransition = m_createdTransition.Reverse(reverseCondition);
				return new StateMachineHandlerResult(previousHandleResult, parentStateMachine, handledState, createdTransition);
			}
			throw new Exception($"no transition have been created to reverse." +
				$"you should call this after 'ToState','ToStateMachine' or 'Condition'.");
		}

		/// <summary>
		/// �ı䵱ǰ������State��StateMachine
		/// </summary>
		public StateMachineHandlerResult SwitchHandle(string stateId)
		{
			StateBase state = parentStateMachine.states.Find(s => s.id.Equals(stateId));
			if (state != null)
			{
				//Debug.Log($"switch handle state to [{state.id}]");
				//if (state.stateType == StateType.State)
				//{
				return new StateMachineHandlerResult(previousHandleResult, parentStateMachine, state);
				//}
				//else
				//{
				//	return new StateMachineHandlerResult(previousHandleResult, state as StateMachine, state);
				//}
			}
			throw new Exception($"[{stateId}] in state machine [{parentStateMachine.id}] is not exist.");
		}

		/// <summary>
		/// Ϊstate���enter�ص�
		/// </summary>
		public StateMachineHandlerResult OnEnter(Action<State> enter)
		{
			if (handledState.stateType == StateType.State)
			{
				//Debug.Log($"set [{handledState.id}] state OnEnter action ");
				(handledState as State).SetOnEnter(enter);
				return new StateMachineHandlerResult(previousHandleResult, parentStateMachine, handledState, createdTransition);
			}
			throw new Exception("only state can set action");
		}

		/// <summary>
		/// Ϊstate���logic�ص�
		/// </summary>
		public StateMachineHandlerResult OnLogic(Action<State> logic)
		{
			if (handledState.stateType == StateType.State)
			{
				//Debug.Log($"set [{handledState.id}] state OnLogic action ");
				(handledState as State).SetOnLogic(logic);
				return new StateMachineHandlerResult(previousHandleResult, parentStateMachine, handledState, createdTransition);
			}
			throw new Exception("only state can set action");
		}

		/// <summary>
		/// Ϊstate���exit�ص�
		/// </summary>
		public StateMachineHandlerResult OnExit(Action<State> exit)
		{
			if (handledState.stateType == StateType.State)
			{
				//Debug.Log($"set [{handledState.id}] state OnExit action ");
				(handledState as State).SetOnExit(exit);
				return new StateMachineHandlerResult(previousHandleResult, parentStateMachine, handledState, createdTransition);
			}
			throw new Exception("only state can set action");
		}

		/// <summary>
		/// Ϊstate���execute�ص�
		/// </summary>
		public StateMachineHandlerResult OnExecute(Action<State, StateExecuteType> execute)
		{
			if (handledState.stateType == StateType.State)
			{
				//Debug.Log($"set [{handledState.id}] state OnExecute action ");
				(handledState as State).SetOnExecute(execute);
				return new StateMachineHandlerResult(previousHandleResult, parentStateMachine, handledState, createdTransition);
			}
			throw new Exception("only state can set execute");
		}

		/// <summary>
		/// Ϊstate���CanExit�ص�
		/// </summary>
		public StateMachineHandlerResult CanExit(Func<State, bool> canExit)
		{
			if (handledState.stateType == StateType.State)
			{
				(handledState as State).SetCanExit(canExit);
				return new StateMachineHandlerResult(previousHandleResult, parentStateMachine, handledState, createdTransition);
			}
			throw new Exception("only state can set CanExit");
		}

		/// <summary>
		/// ΪStateMachine���Service
		/// </summary>
		public StateMachineHandlerResult AddService(string serviceId, ServiceType serviceType = ServiceType.Update, float customInterval = 0f)
		{
			if (handledState.stateType == StateType.StateMachine)
			{
				//Debug.Log($"add service [{serviceId}] to [{handledState.id}] state machine.");
				return new StateMachineHandlerResult(previousHandleResult, parentStateMachine,
					handledState, createdTransition, (handledState as StateMachine).AddService(serviceId: serviceId, type: serviceType, customInterval: customInterval));
			}
			throw new Exception("only state machine can add Service");
		}

		/// <summary>
		/// ΪStateMachine���BeginService�ص�
		/// </summary>
		public StateMachineHandlerResult OnBeginService(Action<Service> beginService)
		{
			if (handledState.stateType == StateType.StateMachine)
			{
				//Debug.Log($"set [{handledState.id}] state machine [{createdService.id}] begin service action ");
				m_createdService.SetBeginService(beginService);
				return new StateMachineHandlerResult(previousHandleResult, parentStateMachine, handledState, createdTransition, createdService);
			}
			throw new Exception("only state machine can set beginService");
		}

		/// <summary>
		/// ΪStateMachine���Service�ص�
		/// </summary>
		public StateMachineHandlerResult OnLogicService(Action<Service> service)
		{
			if (handledState.stateType == StateType.StateMachine)
			{
				//Debug.Log($"set [{handledState.id}] state machine [{createdService.id}] logic service action ");
				m_createdService.SetSercive(service);
				return new StateMachineHandlerResult(previousHandleResult, parentStateMachine, handledState, createdTransition, createdService);
			}
			throw new Exception("only state machine can set service");
		}

		/// <summary>
		/// ΪStateMachine���EndService�ص�
		/// </summary>
		public StateMachineHandlerResult OnEndService(Action<Service> endService)
		{
			if (handledState.stateType == StateType.StateMachine)
			{
				//Debug.Log($"set [{handledState.id}] state machine [{createdService.id}] end service action ");
				m_createdService.SetEndService(endService);
				return new StateMachineHandlerResult(previousHandleResult, parentStateMachine, handledState, createdTransition, createdService);
			}
			throw new Exception("only state machine can set endService");
		}

		/// <summary>
		/// ΪStateMachine���ExecuteService�ص�
		/// </summary>
		public StateMachineHandlerResult OnService(Action<Service, ServiceExecuteType> service)
		{
			if (handledState.stateType == StateType.StateMachine)
			{
				//Debug.Log($"set [{handledState.id}] state machine [{createdService.id}] service action ");
				m_createdService.SetExecuteService(service);
				return new StateMachineHandlerResult(previousHandleResult, parentStateMachine, handledState, createdTransition, createdService);
			}
			throw new Exception("only state machine can call OnExecute");
		}

		/// <summary>
		/// ������ǰ������StateMachine����������һ��StateMachine��������
		/// </summary>
		public StateMachineHandlerResult FinishHandle()
		{
			//Debug.Log($"finish handle [{previousHandleResult.handledState.id}] state machine");
			if (previousHandleResult == null || previousHandleResult.previousHandleResult == null) return this;
			return new StateMachineHandlerResult(previousHandleResult.previousHandleResult,
				previousHandleResult.previousHandleResult.parentStateMachine,
				previousHandleResult.handledState,
				previousHandleResult.createdTransition);
		}

		/// <summary>
		/// ��������
		/// </summary>
		public void EndHandle()
		{
			//Debug.Log($"end handle [{parentStateMachine.id}] state machine");
		}
	}
}