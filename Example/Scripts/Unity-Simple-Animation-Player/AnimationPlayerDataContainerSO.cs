using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "APDataContainer_", menuName = "ScriptableObject/AnimationPlayer/DataContainer")]
public class AnimationPlayerDataContainerSO : ScriptableObject
{
	public AnimationPlayerDataSO defaultAnimation;
	public AvatarMask defaultMask;
	public List<AnimationPlayerDataSO> datas = new List<AnimationPlayerDataSO>();
	public List<AnimationLayerMaskData> layerMasks = new List<AnimationLayerMaskData>();
}

[System.Serializable]
public class AnimationLayerMaskData
{
	public string maskName;

	public AvatarMask mask;
}