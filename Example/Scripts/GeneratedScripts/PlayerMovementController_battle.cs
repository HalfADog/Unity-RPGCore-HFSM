using RPGCore.AI.HFSM;
using UnityEngine;
public partial class PlayerMovementController : StateMachineScriptController
{
//Don't delete or modify the #region & #endregion
#region Method
	[Service("battle/ProcessRollOrDodge")]
	private void on_ProcessRollOrDodge_service(Service service, ServiceExecuteType type)
	{
		if (Input.GetMouseButtonDown(1))
		{
			SetTrigger("RollOrDodge");
		}
		if (Input.GetKeyDown(KeyCode.V))
		{
			SetBool("IsLookToTarget", !GetBool("IsLookToTarget"));
		}
	}

	[Service("battle/ProcessAttack")]
	private void on_ProcessAttack_service(Service service, ServiceExecuteType type)
	{
		if (Input.GetMouseButtonDown(0))
		{
			SetTrigger("Attack");
		}
	}

#endregion Method
}
