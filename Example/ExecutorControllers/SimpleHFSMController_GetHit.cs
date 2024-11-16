using RPGCore.AI.HFSM;
using UnityEngine;
public partial class SimpleHFSMController : StateMachineScriptController
{
//Don't delete or modify the #region & #endregion
#region Method
	[State("GetHit")]
	private void on_GetHit_execute(State state, StateExecuteType type)
	{
		if (type == StateExecuteType.OnEnter) animationPlayer.RequestTransition("GetHit");
	}
	[CanExit("GetHit")]
	private bool can_GetHit_exit(State state)
	{
		return animationPlayer.CurrentFinishPlaying;
	}

#endregion Method
}
