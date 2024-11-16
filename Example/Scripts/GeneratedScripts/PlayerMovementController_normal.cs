using RPGCore.AI.HFSM;
using UnityEngine;
public partial class PlayerMovementController : StateMachineScriptController
{
//Don't delete or modify the #region & #endregion
#region Method
	[Service("normal/ProcessTalk")]
	private void on_ProcessTalk_service(Service service, ServiceExecuteType type)
	{
		if (Input.GetKeyDown(KeyCode.T))
		{
			SetTrigger("Talk");
		}
	}

#endregion Method
}
