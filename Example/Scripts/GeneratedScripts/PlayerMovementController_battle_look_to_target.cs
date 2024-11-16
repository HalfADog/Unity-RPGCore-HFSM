using RPGCore.AI.HFSM;
using UnityEngine;
public partial class PlayerMovementController : StateMachineScriptController
{
//Don't delete or modify the #region & #endregion
#region Method
	[Service("battle_look_to_target/FaceToTarget")]
	private void on_FaceToTarget_service(Service service, ServiceExecuteType type)
	{
		Vector3 tForward = (lookTarget.position - gameObject.transform.position).normalized;
		tForward.y = 0;
		gameObject.transform.forward = Vector3.Lerp(gameObject.transform.forward, tForward, Time.deltaTime * 20);
	}

#endregion Method
}
