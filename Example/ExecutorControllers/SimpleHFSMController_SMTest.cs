using RPGCore.AI.HFSM;
using UnityEngine;
public partial class SimpleHFSMController : StateMachineScriptController
{
//Don't delete or modify the #region & #endregion
#region Method
	[Service("SMTest/SMService")]
	private void on_SMService_service(Service service, ServiceExecuteType type)
	{
		if (type == ServiceExecuteType.Service) Debug.Log("SMService Execute");
	}

#endregion Method
}
