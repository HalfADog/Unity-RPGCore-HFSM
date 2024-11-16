using RPGCore.AI.HFSM;
using UnityEngine;
public partial class SimpleHFSMController : StateMachineScriptController
{
//Don't delete or modify the #region & #endregion
#region Method
	[State("Pause")]
	private void on_Pause_execute(State state, StateExecuteType type)
	{
		if (type == StateExecuteType.OnEnter)
		{
			Debug.Log("Pause");
			animationPlayer.Stop();
		}
		else if (type == StateExecuteType.OnExit)
		{
			animationPlayer.Play();
		}
	}
	[CanExit("Pause")]
	private bool can_Pause_exit(State state)
	{
		return !GetBool("Pause");
	}

#endregion Method
}
