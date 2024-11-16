using RPGCore.AI.HFSM;
using UnityEngine;
public partial class PlayerMovementController : StateMachineScriptController
{
//Don't delete or modify the #region & #endregion
#region Method
	[State("dodge")]
	private void on_dodge_execute(State state, StateExecuteType type)
	{
		if (type == StateExecuteType.OnEnter)
		{
			if (moveVec.x == 1) { animPlayer.RequestTransition("DodgeRight"); }
			else if (moveVec.x == -1) { animPlayer.RequestTransition("DodgeLeft"); }
			PauseService("ProcessInput");
		}
		else if (type == StateExecuteType.OnExit)
		{
			ContinueService("ProcessInput");
		}
	}
	[CanExit("dodge","当Dodge的动画播放完成时自动回到Idle状态")]
	private bool can_dodge_exit(State state)
	{
		return animPlayer.CurrentFinishPlaying;
	}

#endregion Method
}
