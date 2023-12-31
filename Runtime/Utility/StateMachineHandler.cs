namespace RPGCore.AI.HFSM
{
	public class StateMachineHandler
	{
		/// <summary>
		/// ��ǰ������StateMachine
		/// </summary>
		public static StateMachine currentHandledStateMachine;

		/// <summary>
		/// ��ǰ������Controller
		/// </summary>
		public static StateMachineScriptController currentHandledController;

		/// <summary>
		/// ��ʼһ��StateMachine�Ĳ���
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