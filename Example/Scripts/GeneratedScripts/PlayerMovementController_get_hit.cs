using RPGCore.AI.HFSM;
using UnityEngine;
public partial class PlayerMovementController : StateMachineScriptController
{
//Don't delete or modify the #region & #endregion
#region Method
	[State("get_hit")]
	private void on_get_hit_execute(State state, StateExecuteType type)
	{
		if (type == StateExecuteType.OnEnter)
		{
			animPlayer.RequestTransition("GetHit");
			PauseService("ProcessInput");
		}
		else if (type == StateExecuteType.OnExit)
		{
			ContinueService("ProcessInput");
		}
	}
	[CanExit("get_hit")]
	private bool can_get_hit_exit(State state)
	{
		return animPlayer.CurrentFinishPlaying;
	}

#endregion Method
}
