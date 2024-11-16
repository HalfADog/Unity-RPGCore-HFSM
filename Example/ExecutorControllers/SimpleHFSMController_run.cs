using RPGCore.AI.HFSM;
using UnityEngine;
public partial class SimpleHFSMController : StateMachineScriptController
{
//Don't delete or modify the #region & #endregion
#region Method
	[State("run")]
	private void on_run_execute(State state, StateExecuteType type)
	{
		if (type == StateExecuteType.OnEnter)
		{
			animationPlayer.RequestTransition("Run");
			Debug.Log("Run Execute.");
		}
		else if (type == StateExecuteType.OnLogic)
		{
			if (animationPlayer.CurrentFinishPlaying)
			{
				SetBool("IsRun", false);
				SetBool("IsWalk", false);
				SetBool("IsIdle", true);
			}
		}
	}

#endregion Method
}
