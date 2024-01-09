using RPGCore.AI.HFSM;
using RPGCore.Animation;
using UnityEngine;

public partial class PlayerMovementController : StateMachineScriptController
{
	private AnimationPlayerManager animPlayer;
	private Vector2 moveVec = new Vector2();
	private Transform lookTarget;
	private PlayerManager playerManager;

	public override void Init()
	{
		animPlayer = gameObject.GetComponent<AnimationPlayerManager>();
		playerManager = gameObject.GetComponent<PlayerManager>();
		lookTarget = playerManager.lookTarget;
	}

	//Don't delete or modify the #region & #endregion

	#region Method

	//description:
	private void on_ProcessTalk_service(Service service, ServiceExecuteType type)
	{
		if (Input.GetKeyDown(KeyCode.T))
		{
			SetTrigger("Talk");
		}
	}

	//description:
	private void on_ProcessAttack_service(Service service, ServiceExecuteType type)
	{
		if (Input.GetMouseButtonDown(0))
		{
			SetTrigger("Attack");
		}
	}

	//description:
	private void on_FaceToTarget_service(Service service, ServiceExecuteType type)
	{
		Vector3 tForward = (lookTarget.position - gameObject.transform.position).normalized;
		tForward.y = 0;
		gameObject.transform.forward = Vector3.Lerp(gameObject.transform.forward, tForward, Time.deltaTime * 20);
	}

	//description:
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

	//description:
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

	//description:
	private void on_CheckGetHit_service(Service service, ServiceExecuteType type)
	{
		if (playerManager.beAttack)
		{
			if (executeState != "roll" && executeState != "dodge") SetTrigger("GetHit");
			playerManager.beAttack = false;
		}
	}

	//description:
	private void on_free_view_sprint_execute(State state, StateExecuteType type)
	{
		if (type == StateExecuteType.OnEnter)
		{
			animPlayer.RequestTransition("FreeViewSprint");
		}
	}

	//description:
	private void on_normal_talk_execute(State state, StateExecuteType type)
	{
		if (type == StateExecuteType.OnEnter)
		{
			animPlayer.RequestTransition("NormalTalk");
			PauseService("ProcessInput");
		}
		else if (type == StateExecuteType.OnExit)
		{
			ContinueService("ProcessInput");
		}
	}

	//description:
	private void on_normal_idle_execute(State state, StateExecuteType type)
	{
		if (type == StateExecuteType.OnEnter)
		{
			animPlayer.RequestTransition("NormalIdle");
		}
	}

	//description:
	private void on_normal_walk_execute(State state, StateExecuteType type)
	{
		if (type == StateExecuteType.OnEnter)
		{
			animPlayer.RequestTransition("NormalWalk");
		}
	}

	//description:
	private void on_look_to_target_idle_execute(State state, StateExecuteType type)
	{
		if (type == StateExecuteType.OnEnter)
		{
			animPlayer.RequestTransition("BattleIdle");
		}
	}

	//description:
	private void on_look_to_target_walk_execute(State state, StateExecuteType type)
	{
		if (Mathf.Abs(moveVec.x) == 1)
		{
			animPlayer.RequestTransition(moveVec.x == 1 ? "LookToTargetRight" : "LookToTargetLeft");
		}
		else if (Mathf.Abs(moveVec.y) == 1)
		{
			animPlayer.RequestTransition(moveVec.y == 1 ? "LookToTargetForward" : "LookToTargetBackward");
		}
	}

	//description:
	private void on_dodge_execute(State state, StateExecuteType type)
	{
		if (type == StateExecuteType.OnEnter)
		{
			if (moveVec.x == 1) { animPlayer.RequestTransition("DodgeRight"); }
			else if (moveVec.x == -1) { animPlayer.RequestTransition("DodgeLeft"); }
			PauseService("ProcessInput");
		}
		else if (type == StateExecuteType.OnExit)
		{
			ContinueService("ProcessInput");
		}
	}

	//description:
	private void on_free_view_idle_execute(State state, StateExecuteType type)
	{
		if (type == StateExecuteType.OnEnter)
		{
			animPlayer.RequestTransition("BattleIdle");
		}
	}

	//description:
	private void on_free_view_run_execute(State state, StateExecuteType type)
	{
		if (type == StateExecuteType.OnEnter)
		{
			animPlayer.RequestTransition("FreeViewRun");
		}
	}

	//description:
	private void on_roll_execute(State state, StateExecuteType type)
	{
		if (type == StateExecuteType.OnEnter)
		{
			animPlayer.RequestTransition("Roll");
			PauseService("ProcessInput");
		}
		else if (type == StateExecuteType.OnExit)
		{
			ContinueService("ProcessInput");
		}
	}

	//description:
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

	//description:
	private void on_get_hit_execute(State state, StateExecuteType type)
	{
		if (type == StateExecuteType.OnEnter)
		{
			animPlayer.RequestTransition("GetHit");
			PauseService("ProcessInput");
		}
		else if (type == StateExecuteType.OnExit)
		{
			ContinueService("ProcessInput");
		}
	}

	//description:
	private bool can_normal_talk_exit(State state)
	{
		return animPlayer.CurrentFinishPlaying;
	}

	//description:当Dodge的动画播放完成时自动回到Idle状态
	private bool can_dodge_exit(State state)
	{
		return animPlayer.CurrentFinishPlaying;
	}

	//description:当Roll的动画播放完成时自动回到Idle状态
	private bool can_roll_exit(State state)
	{
		return animPlayer.CurrentFinishPlaying;
	}

	//description:
	private bool can_attack_exit(State state)
	{
		return animPlayer.CurrentFinishPlaying || GetTrigger("RollOrDodge") || GetTrigger("GetHit");
	}

	//description:
	private bool can_get_hit_exit(State state)
	{
		return animPlayer.CurrentFinishPlaying;
	}

	//description:
	private bool can_dead_exit(State state)
	{
		return animPlayer.CurrentFinishPlaying;
	}

	#endregion Method
}