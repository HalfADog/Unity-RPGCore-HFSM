using RPGCore.AI.HFSM;
using UnityEngine;
public partial class PlayerMovementController : StateMachineScriptController
{
//Don't delete or modify the #region & #endregion
#region Method
	[State("attack")]
	private void on_attack_execute(State state, StateExecuteType type)
	{
		if (type == StateExecuteType.OnEnter)
		{
			animPlayer.RequestTransition("BattleAttack");
			PauseService("ProcessInput");
		}
		else if (type == StateExecuteType.OnExit)
		{
			ContinueService("ProcessInput");
		}
	}
	[CanExit("attack")]
	private bool can_attack_exit(State state)
	{
		return animPlayer.CurrentFinishPlaying || GetTrigger("RollOrDodge") || GetTrigger("GetHit");
	}

#endregion Method
}
