using RPGCore.AI.HFSM;
using UnityEngine;

public partial class TestController : StateMachineScriptController
{
	public override void Init()
	{
	}

	//Don't delete or modify the #region & #endregion

	#region Method

	//description:
	private void on_NewService0_service(Service service, ServiceExecuteType type)
	{
		if (type == ServiceExecuteType.Service)
		{
			Debug.Log("Service Execute.");
		}
	}

	//StateMachine Service Code Here
	//private void serviceMethodName(Service service, ServiceExecuteType type)
	//description:
	private void on_sms_execute(State state, StateExecuteType type)
	{
		if (type == StateExecuteType.OnEnter) Debug.Log("sms Execute.");
	}

	//description:
	private void on_444_execute(State state, StateExecuteType type)
	{
		if (type == StateExecuteType.OnEnter) Debug.Log("444 Execute.");
	}

	//description:
	private void on_111_execute(State state, StateExecuteType type)
	{
		if (type == StateExecuteType.OnEnter) Debug.Log("111 Execute.");
		if (type == StateExecuteType.OnLogic)
		{
			if (state.timer.Elapsed >= 1)
			{
				SetBool("1t2", true);
				SetBool("2t3", false);
				SetBool("3t1", false);
			}
		}
	}

	//description:
	private void on_222_execute(State state, StateExecuteType type)
	{
		if (type == StateExecuteType.OnEnter) Debug.Log("222 Execute.");
		if (type == StateExecuteType.OnLogic)
		{
			if (state.timer.Elapsed >= 1)
			{
				SetBool("1t2", false);
				SetBool("2t3", true);
				SetBool("3t1", false);
			}
		}
	}

	//description:
	private void on_333_execute(State state, StateExecuteType type)
	{
		if (type == StateExecuteType.OnEnter) Debug.Log("333 Execute.");
		if (type == StateExecuteType.OnLogic)
		{
			if (state.timer.Elapsed >= 1)
			{
				SetBool("1t2", false);
				SetBool("2t3", false);
				SetBool("3t1", true);
			}
		}
	}

	//description:
	private bool can_444_exit(State state)
	{
		return state.timer.Elapsed >= 1;
	}

	//State Can Exit Code Here
	//private bool stateName(State state)

	#endregion Method
}