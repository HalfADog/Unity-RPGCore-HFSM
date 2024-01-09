using RPGCore.AI.HFSM;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerManager : MonoBehaviour
{
	public Transform lookTarget;
	public Text text;
	private StateMachineExecutor executor;

	[HideInInspector]
	public bool beAttack;

	// Start is called before the first frame update
	private void Start()
	{
		executor = GetComponent<StateMachineExecutor>();
	}

	// Update is called once per frame
	private void Update()
	{
		text.text = "Current:" + (executor.scriptController.GetBool("IsOnBattle") ? "Battle" : "Normal");
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.gameObject.tag == "Attacker")
		{
			beAttack = true;
		}
	}
}