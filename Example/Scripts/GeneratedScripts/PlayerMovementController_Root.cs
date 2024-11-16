using RPGCore.AI.HFSM;
using UnityEngine;
public partial class PlayerMovementController : StateMachineScriptController
{
//Don't delete or modify the #region & #endregion
#region Method
	[Service("Root/ProcessInput")]
	private void on_ProcessInput_service(Service service, ServiceExecuteType type)
	{
		moveVec.x = (Input.GetKey(KeyCode.A) ? -1 : 0) + (Input.GetKey(KeyCode.D) ? 1 : 0);
		moveVec.y = (Input.GetKey(KeyCode.S) ? -1 : 0) + (Input.GetKey(KeyCode.W) ? 1 : 0);
		moveVec = moveVec.normalized;
		SetBool("IsIdle", moveVec.sqrMagnitude == 0);
		SetBool("IsWalk", moveVec.sqrMagnitude != 0);
		SetBool("IsRun", Input.GetKey(KeyCode.LeftShift));
		if (moveVec.sqrMagnitude != 0)
		{
			if (!GetBool("IsLookToTarget"))
			{
				gameObject.transform.forward = Vector3.Lerp(gameObject.transform.forward, new Vector3(moveVec.x, 0, moveVec.y), Time.deltaTime * 10);
			}
		}
		if (Input.GetKeyDown(KeyCode.Tab))
		{
			SetBool("IsOnBattle", !GetBool("IsOnBattle"));
		}
	}

	[Service("Root/CheckGetHit")]
	private void on_CheckGetHit_service(Service service, ServiceExecuteType type)
	{
		if (playerManager.beAttack)
		{
			if (executeState != "roll" && executeState != "dodge") SetTrigger("GetHit");
			playerManager.beAttack = false;
		}
	}

#endregion Method
}
