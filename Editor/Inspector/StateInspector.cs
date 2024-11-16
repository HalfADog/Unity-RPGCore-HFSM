using DogFramework;
using UnityEditor;
using UnityEngine;

namespace RPGCore.AI.HFSM
{
	[CustomEditor(typeof(StateInspectorHelper))]
	public class StateInspector : Editor
	{
		public string stateName;

		public override void OnInspectorGUI()
		{
			StateInspectorHelper helper = target as StateInspectorHelper;
			if (helper == null) return;
			bool disable = EditorApplication.isPlaying || helper.stateData.id == StateMachine.entryState || helper.stateData.id == StateMachine.anyState;
			EditorGUI.BeginDisabledGroup(disable);
			EditorGUILayout.BeginVertical();

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("state name", GUILayout.Width(80));
			string newName = helper.stateData.id;
			EditorGUI.BeginChangeCheck();
			newName = EditorGUILayout.DelayedTextField(newName);
			if (EditorGUI.EndChangeCheck() && newName != stateName)
			{
				helper.HFSMController.RenameState(helper.stateData, newName);
				stateName = newName;
			}
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.Space();
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("description", GUILayout.Width(80));
			string description = helper.stateData.description;
			EditorGUI.BeginChangeCheck();
			description = EditorGUILayout.DelayedTextField(description);
			if (EditorGUI.EndChangeCheck())
			{
				helper.HFSMController.RenameState(helper.stateData, description, true, false);
			}
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.Space();
			disable = helper.HFSMController.transitions.Find(t => t.to == helper.stateData.id)?.from != StateMachine.anyState;
			EditorGUI.BeginDisabledGroup(disable);
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("is temporary", GUILayout.Width(80));
			helper.stateData.isTemporary = GUILayout.Toggle(helper.stateData.isTemporary, "", GUILayout.Width(20));
			EditorGUILayout.EndHorizontal();
			EditorGUI.EndDisabledGroup();

			EditorGUILayout.Space();
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("handle exit", GUILayout.Width(80));
			helper.stateData.canExitHandle = GUILayout.Toggle(helper.stateData.canExitHandle, "",GUILayout.Width(20));
			EditorGUI.BeginDisabledGroup(!helper.stateData.canExitHandle);
			EditorGUI.BeginChangeCheck();
			string ceDescription = EditorGUILayout.DelayedTextField(helper.stateData.canExitDescription);
			if (EditorGUI.EndChangeCheck())
			{
				helper.HFSMController.RenameState(helper.stateData, ceDescription, true, true);
			}
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.Space();
			EditorGUILayout.BeginHorizontal();
			EditorGUI.BeginDisabledGroup(!helper.HFSMController.controllerConfig.DisperseGenerate || helper.HFSMController.controllerConfig.DisperseAll);
			EditorGUILayout.LabelField(new GUIContent("independent", "Generate script files independently"), GUILayout.Width(80));
			helper.stateData.independentGenerate = GUILayout.Toggle(helper.stateData.independentGenerate, "", GUILayout.Width(20));
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.EndVertical();
			EditorGUI.EndDisabledGroup();
		}

		protected override void OnHeaderGUI()
		{
			StateInspectorHelper helper = target as StateInspectorHelper;
			if (helper == null) return;
			EditorGUILayout.Space();
			EditorGUILayout.BeginHorizontal();

			GUILayout.Label(EditorGUIUtility.IconContent("icons/processed/unityeditor/animations/animatorstate icon.asset"), GUILayout.Width(30), GUILayout.Height(30));
			EditorGUILayout.LabelField("Name",style: "HeaderLabel", GUILayout.Width(50));

			EditorGUILayout.LabelField(helper.stateData.id);

			EditorGUILayout.EndHorizontal();

			var rect = EditorGUILayout.BeginHorizontal();

			EditorGUILayout.Space();
			Handles.color = Color.black;
			Handles.DrawLine(new Vector2(rect.x, rect.y), new Vector2(rect.x + rect.width, rect.y));
			EditorGUILayout.Space();

			EditorGUILayout.EndHorizontal();
		}
	}

	public class StateInspectorHelper : ScriptableObjectSingleton<StateInspectorHelper>
	{
		public StateMachineExecutorController HFSMController;
		public StateData stateData;

		public void Inspector(StateMachineExecutorController HFSMController, StateData stateData)
		{
			this.HFSMController = HFSMController;
			this.stateData = stateData;
			Selection.activeObject = this;
		}
	}
}