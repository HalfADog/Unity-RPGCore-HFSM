using RPGCore.AI.HFSM;
using UnityEngine;
public partial class SimpleHFSMController : StateMachineScriptController
{
//Don't delete or modify the #region & #endregion
#region Method
	[State("idle")]
	private void on_idle_execute(State state, StateExecuteType type)
	{
		if (type == StateExecuteType.OnEnter)
		{
			animationPlayer.RequestTransition("Idle");
			Debug.Log("Idle Execute.");
		}
		else if (type == StateExecuteType.OnLogic)
		{
			if (animationPlayer.CurrentFinishPlaying)
			{
				SetBool("IsIdle", false);
				SetBool("IsRun", false);
				SetBool("IsWalk", true);
			}
		}
	}

#endregion Method
}
