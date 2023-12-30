using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "APData_", menuName = "ScriptableObject/AnimationPlayer/Data")]
public class AnimationPlayerDataSO : ScriptableObject
{
	public string animName;
	public AnimationClip animClip;
	public AnimationPlayType animPlayType;
	public AnimationPriority animPriority;
	public bool canAbort;
	public AnimationAbortType abortType;
	public float multiplier;
	public float offset;
}

public enum AnimationPlayType
{
	Loop,
	Once,
	Reverse,
}

public enum AnimationAbortType
{
	All,
	OnlyHigherPriority,
	DisallowLowerPriority
}

public enum AnimationPriority
{
	Lowest = 0,
	Lower = 10,
	Low = 20,
	Normal = 100,
	High = 200,
	Higher = 1000,
	Highest = 10000
}