using RPGCore.AI.HFSM;
//Automatically generated code
[StateMachineController(ControllerName = "TestController")]
public partial class TestController : StateMachineScriptController
{
	public override StateMachine ConstructStateMachine()
	{
		StateMachineHandler.BeginStateMachine(this, "Root")
			.AddState("111", true).OnExecute(on_111_execute)
			.AddState("222", false).OnExecute(on_222_execute)
			.AddState("333", false).OnExecute(on_333_execute)
			.AddTemporaryState("444").OnExecute(on_444_execute)
				.CanExit(can_444_exit)
			.AddStateMachine("sm", false)
				.AddService("NewService0",ServiceType.CustomInterval,1).OnService(on_NewService0_service)
				.AddState("sms", true).OnExecute(on_sms_execute)
				.FinishHandle()
			.SwitchHandle("111").ToState("222",false)
				.BoolCondition("1t2",true)
			.SwitchHandle("222").ToState("333",false)
				.BoolCondition("2t3",true)
			.SwitchHandle("333").ToState("111",false)
				.BoolCondition("3t1",true)
			.SwitchHandle("Any").ToState("444",true)
				.TriggerCondition("at4")
			.SwitchHandle("Any").ToStateMachine("sm")
				.TriggerCondition("atsm")
			.SwitchHandle("sm").ToState("111",false)
				.TriggerCondition("smt1")
			.FinishHandle()
			.EndHandle();
		return StateMachineHandler.EndStateMachine();
	}
}
