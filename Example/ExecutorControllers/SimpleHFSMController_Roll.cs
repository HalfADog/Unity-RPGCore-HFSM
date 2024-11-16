using RPGCore.AI.HFSM;
using UnityEngine;
public partial class SimpleHFSMController : StateMachineScriptController
{
//Don't delete or modify the #region & #endregion
#region Method
	[State("Roll")]
	private void on_Roll_execute(State state, StateExecuteType type)
	{
		if (type == StateExecuteType.OnEnter)
		{
			animationPlayer.RequestTransition("Roll");
			Debug.Log("Roll Execute.");
		}
		else if (type == StateExecuteType.OnLogic)
		{
			if (animationPlayer.CurrentFinishPlaying)
			{
				SetBool("IsAttack", false);
				SetBool("IsRoll", false);
				SetBool("IsSkill", true);
			}
		}
	}

#endregion Method
}
