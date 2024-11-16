using RPGCore.AI.HFSM;
using UnityEngine;
public partial class PlayerMovementController : StateMachineScriptController
{
//Don't delete or modify the #region & #endregion
#region Method
	[State("look_to_target_walk")]
	private void on_look_to_target_walk_execute(State state, StateExecuteType type)
	{
		if (Mathf.Abs(moveVec.x) == 1)
		{
			animPlayer.RequestTransition(moveVec.x == 1 ? "LookToTargetRight" : "LookToTargetLeft");
		}
		else if (Mathf.Abs(moveVec.y) == 1)
		{
			animPlayer.RequestTransition(moveVec.y == 1 ? "LookToTargetForward" : "LookToTargetBackward");
		}
	}

#endregion Method
}
