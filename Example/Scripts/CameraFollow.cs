using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
	public GameObject target;
	private Vector3 posOffset;

	private void Start()
	{
		posOffset = transform.position - target.transform.position;
	}

	private void Update()
	{
		transform.position = posOffset + target.transform.position;
	}
}