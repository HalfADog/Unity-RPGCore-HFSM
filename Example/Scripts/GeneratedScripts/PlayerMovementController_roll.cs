using RPGCore.AI.HFSM;
using UnityEngine;
public partial class PlayerMovementController : StateMachineScriptController
{
//Don't delete or modify the #region & #endregion
#region Method
	[State("roll")]
	private void on_roll_execute(State state, StateExecuteType type)
	{
		if (type == StateExecuteType.OnEnter)
		{
			animPlayer.RequestTransition("Roll");
			PauseService("ProcessInput");
		}
		else if (type == StateExecuteType.OnExit)
		{
			ContinueService("ProcessInput");
		}
	}
	[CanExit("roll","当Roll的动画播放完成时自动回到Idle状态")]
	private bool can_roll_exit(State state)
	{
		return animPlayer.CurrentFinishPlaying;
	}

#endregion Method
}
