using RPGCore.AI.HFSM;
using UnityEngine;
public partial class PlayerMovementController : StateMachineScriptController
{
//Don't delete or modify the #region & #endregion
#region Method
	[State("normal_walk")]
	private void on_normal_walk_execute(State state, StateExecuteType type)
	{
		if (type == StateExecuteType.OnEnter)
		{
			animPlayer.RequestTransition("NormalWalk");
		}
	}

#endregion Method
}
