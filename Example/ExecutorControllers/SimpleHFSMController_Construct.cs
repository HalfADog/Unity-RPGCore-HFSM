using RPGCore.AI.HFSM;
//Automatically generated code
[StateMachineController(ControllerName = "SimpleHFSMController")]
public partial class SimpleHFSMController : StateMachineScriptController
{
	public override StateMachine ConstructStateMachine()
	{
		StateMachineHandler.BeginStateMachine(this, "Root")
			.AddState("idle", true).OnExecute(on_idle_execute)
			.AddState("walk", false).OnExecute(on_walk_execute)
			.AddState("run", false).OnExecute(on_run_execute)
			.AddTemporaryState("GetHit").OnExecute(on_GetHit_execute)
				.CanExit(can_GetHit_exit)
			.AddStateMachine("SMTest", false)
				.AddService("SMService",ServiceType.CustomInterval,1).OnService(on_SMService_service)
				.AddState("Attack", true).OnExecute(on_Attack_execute)
				.AddState("Roll", false).OnExecute(on_Roll_execute)
				.AddState("Skill", false).OnExecute(on_Skill_execute)
				.SwitchHandle("Attack").ToState("Roll",false)
					.BoolCondition("IsRoll",true)
				.SwitchHandle("Roll").ToState("Skill",false)
					.BoolCondition("IsSkill",true)
				.SwitchHandle("Skill").ToState("Attack",false)
					.BoolCondition("IsAttack",true)
				.FinishHandle()
			.AddTemporaryState("Pause").OnExecute(on_Pause_execute)
				.CanExit(can_Pause_exit)
			.SwitchHandle("idle").ToState("walk",false)
				.BoolCondition("IsWalk",true)
			.SwitchHandle("walk").ToState("run",false)
				.BoolCondition("IsRun",true)
			.SwitchHandle("run").ToState("idle",false)
				.BoolCondition("IsIdle",true)
			.SwitchHandle("Any").ToState("GetHit",true)
				.TriggerCondition("IsGetHit")
			.SwitchHandle("Any").ToStateMachine("SMTest")
				.TriggerCondition("ToSM")
			.SwitchHandle("SMTest").ToState("idle",false)
				.TriggerCondition("ExitSM")
			.SwitchHandle("Any").ToState("Pause",true)
				.BoolCondition("Pause",true)
			.FinishHandle()
			.EndHandle();
		return StateMachineHandler.EndStateMachine();
	}
}
