using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RPGCore.AI.HFSM
{
	public class Context
	{
		private StateMachineExecutorController m_HFSMController;

		public StateMachineExecutorController HFSMController
		{
			get
			{
				var controller = GetController();
				if (controller != null && controller != m_HFSMController)
				{
					m_HFSMController = controller;
					Reset();
				}
				return m_HFSMController;
			}
		}

		private StateMachineExecutor m_executor;

		public StateMachineExecutor executor
		{
			get
			{
				if (Application.isPlaying)
				{
					if ((Selection.activeObject as GameObject) != null && (Selection.activeObject as GameObject).GetComponent<StateMachineExecutor>() != null)
					{
						m_executor = (Selection.activeObject as GameObject).GetComponent<StateMachineExecutor>();
					}
				}
				return m_executor;
			}
		}

		public List<StateBaseData> selectedStates = new List<StateBaseData>();

		public TransitionData selectedTransition = null;

		private StateMachineData m_currentStateMachine;

		public StateMachineData currentStateMachine
		{
			get { return m_currentStateMachine; }
			set
			{
				if (m_currentStateMachine != value)
				{
					m_currentStateMachine = value;
					UpdateCurrentChildStatesData();
					UpdateCurrentTransitionData();
				}
			}
		}

		public StateMachineData nextStateMachine = null;
		public List<StateMachineData> stateMachinePath = new List<StateMachineData>();

		public List<StateBaseData> currentChildStatesData = new List<StateBaseData>();

		public List<TransitionData> currentTransitionData = new List<TransitionData>();

		public bool isPreviewTransition;
		public StateBaseData preFrom;
		public StateBaseData preTo;

		public float zoomFactor { get; set; } = 0.3f;

		public Vector2 dragOffset { get; set; } = Vector2.zero;

		public StateMachineExecutorController GetController()
		{
			if ((Selection.activeObject as StateMachineExecutorController) != null)
			{
				return (StateMachineExecutorController)Selection.activeObject;
			}
			if ((Selection.activeObject as GameObject) != null && (Selection.activeObject as GameObject).GetComponent<StateMachineExecutor>() != null)
			{
				if ((Selection.activeObject as GameObject).GetComponent<StateMachineExecutor>().executorController != null)
				{
					return (Selection.activeObject as GameObject).GetComponent<StateMachineExecutor>().executorController;
				}
			}
			return null;
		}

		public void StartPriviewTransition(StateBaseData fromState)
		{
			isPreviewTransition = true;
			this.preFrom = fromState;
		}

		public void StopPriviewTransition()
		{
			isPreviewTransition = false;
			this.preFrom = null;
		}

		public void Reset()
		{
			currentStateMachine = HFSMController.stateMachines.Find(sm => sm.isRoot);
			stateMachinePath.Clear();
			stateMachinePath.Add(currentStateMachine);
			this.zoomFactor = 0.3f;
			this.dragOffset = Vector2.zero;
		}

		public void ClearAllSelectNode()
		{
			selectedStates.Clear();
			selectedTransition = null;
		}

		public void UpdateCurrentChildStatesData()
		{
			currentChildStatesData.Clear();
			currentChildStatesData.AddRange(m_HFSMController.states.FindAll(s => m_currentStateMachine.childStates.Contains(s.id)));
			currentChildStatesData.AddRange(m_HFSMController.stateMachines.FindAll(s => m_currentStateMachine.childStates.Contains(s.id)));
			currentChildStatesData.Add(m_currentStateMachine.any);
			currentChildStatesData.Add(m_currentStateMachine.entry);
		}

		public void UpdateCurrentTransitionData()
		{
			currentTransitionData.Clear();
			currentTransitionData.AddRange(m_HFSMController.transitions.FindAll(t => m_currentStateMachine.transitions.Contains(t.id)));
		}
	}
}