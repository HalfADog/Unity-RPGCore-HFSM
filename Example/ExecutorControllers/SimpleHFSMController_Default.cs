using RPGCore.AI.HFSM;
using RPGCore.Animation;
using UnityEngine;
public partial class SimpleHFSMController : StateMachineScriptController
{
	private AnimationPlayerManager animationPlayer;
	public override void Init()
	{
		animationPlayer = gameObject.GetComponent<AnimationPlayerManager>();
	}
//Don't delete or modify the #region & #endregion
#region Method
	//Service Methods
	//State Methods
#endregion Method
}



