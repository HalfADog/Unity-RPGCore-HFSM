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
	//Service Methods
	//State Methods
#endregion Method
}













