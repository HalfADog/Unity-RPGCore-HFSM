namespace RPGCore.AI.HFSM
{
	public class StateMachineHandler
	{
		/// <summary>
		/// 当前操作的StateMachine
		/// </summary>
		public static StateMachine currentHandledStateMachine;

		/// <summary>
		/// 当前操作的Controller
		/// </summary>
		public static StateMachineScriptController currentHandledController;

		/// <summary>
		/// 开始一个StateMachine的操作
		/// </summary>
		/// <param name="stateId"></param>
		/// <returns></returns>
		public static StateMachineHandlerResult BeginStateMachine(StateMachineScriptController controller, string stateId)
		{
			StateMachine stateMachine = new StateMachine(stateId);
			stateMachine.SetRoot();
			currentHandledStateMachine = stateMachine;
			currentHandledController = controller;
			StateMachineHandlerResult result = new StateMachineHandlerResult(null, stateMachine, stateMachine);
			return result;
		}

		public static StateMachine EndStateMachine()
		{
			return currentHandledStateMachine;
		}
	}
}