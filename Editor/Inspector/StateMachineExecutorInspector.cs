using UnityEditor;
using UnityEngine;

namespace RPGCore.AI.HFSM
{
	[CustomEditor(typeof(StateMachineExecutor))]
	public class StateMachineExecutorInspector : Editor
	{
		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();
			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("Open Editor"))
			{
				HFSMEditorWindow.ShowEditorWindow();
			}
			if (GUILayout.Button("Generate Script"))
			{
				(serializedObject.targetObject as StateMachineExecutor).executorController.GenerateScriptController();
			}
			EditorGUILayout.EndHorizontal();
		}
	}
}