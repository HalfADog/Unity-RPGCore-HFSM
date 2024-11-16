using System;

namespace RPGCore.AI.HFSM
{
	public class StateMachineHandlerResult
	{
		/// <summary>
		/// 上一次操作的结果；
		/// </summary>
		public StateMachineHandlerResult previousHandleResult => m_previousHandleResult;

		protected StateMachineHandlerResult m_previousHandleResult;

		/// <summary>
		/// 当前操作的State或StateMachine的父StateMachine
		/// </summary>
		public StateMachine parentStateMachine => m_parentStateMachine;

		protected StateMachine m_parentStateMachine;

		/// <summary>
		/// 当前操作过的State或StateMachine
		/// </summary>
		public StateBase handledState => m_handledState;

		protected StateBase m_handledState;

		/// <summary>
		/// 由上一次操作创建的一条Transition
		/// </summary>
		public Transition createdTransition => m_createdTransition;

		protected Transition m_createdTransition;

		/// <summary>
		/// 由上一次操作创建的一个Service
		/// </summary>
		public Service createdService => m_createdService;

		private Service m_createdService;

		/// <summary>
		/// 返回当前是否开启了新的一层操作；创建StateMachine后第一次操作此字段为真
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
		/// 为当前操作的StateMachine添加一个state
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
		/// 为当前操作的StateMachine添加一个StateMachine
		/// </summary>
		public StateMachineHandlerResult AddStateMachine(string stateMachineId, bool isDefault = false)
		{
			//Debug.Log($"add [{stateMachineId}] state machine to [{parentStateMachine.id}].");
			var stateMachine = parentStateMachine.AddStateMachine(stateMachineId, isDefault);
			return new StateMachineHandlerResult(IsBeginNewLevel ? this : previousHandleResult, stateMachine, stateMachine);
		}

		/// <summary>
		/// 为当前操作的state或StateMachine添加一条到目标state的Transition
		/// 如果目标state不存在，则创建一个state
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
		/// 为当前操作的state或StateMachine添加一条到目标StateMachine的Transition
		/// 如果目标StateMachine不存在，则创建一个如果目标StateMachine不存在
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
		/// 为当前操作后创建的Transition添加 baseCondition
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
		/// 为当前操作后创建的Transition添加 paramterCondition
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
		/// 创建一条与当前操作后创建的Transition方向相反的Transition
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
		/// 改变当前操作的State或StateMachine
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
		/// 为state添加enter回调
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
		/// 为state添加logic回调
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
		/// 为state添加exit回调
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
		/// 为state添加execute回调
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
		/// 为state添加CanExit回调
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
		/// 为StateMachine添加Service
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
		/// 为StateMachine添加BeginService回调
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
		/// 为StateMachine添加Service回调
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
		/// 为StateMachine添加EndService回调
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
		/// 为StateMachine添加ExecuteService回调
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
		/// 结束当前操作的StateMachine，即返回上一层StateMachine继续操作
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
		/// 结束操作
		/// </summary>
		public void EndHandle()
		{
			//Debug.Log($"end handle [{parentStateMachine.id}] state machine");
		}
	}
}