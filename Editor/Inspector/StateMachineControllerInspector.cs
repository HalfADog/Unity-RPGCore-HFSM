using DogFramework.EditorExtension;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RPGCore.AI.HFSM
{
	[CustomEditor(typeof(StateMachineExecutorController))]
	public class StateMachineControllerInspector : Editor
	{
		bool foldout = false;
		bool showHiddenData = false;
		public override void OnInspectorGUI()
		{
			StateMachineExecutorController controller = (StateMachineExecutorController)target;
			StateMachineControllerConfig config = controller.controllerConfig;
			//================================
			GUILayout.BeginVertical();
			GUILayout.Label("Controller Configs", "WhiteLargeLabel");
			GUILayout.Space(10);
			//================================

			GUILayout.BeginHorizontal();
			GUILayout.Label(new GUIContent("Custom Path", "Customize the path of file generation"),GUILayout.Width(128));
			config.CustomFilePath=GUILayout.Toggle(config.CustomFilePath, "",GUILayout.Width(20));
			EditorGUI.BeginDisabledGroup(!config.CustomFilePath);
			EditorGUI.BeginDisabledGroup(true);
			config.FilePath = GUILayout.TextField(config.FilePath);
			EditorGUI.EndDisabledGroup();
			if (Event.current.type == EventType.DragUpdated)
			{
				if (GUILayoutUtility.GetLastRect().MouseOn())
				{
					DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
					Event.current.Use();
				}
			}
			else if (Event.current.type == EventType.DragPerform)
			{
				if (GUILayoutUtility.GetLastRect().MouseOn())
				{
					string path = DragAndDrop.paths[0];
					if (!path.Contains("."))
					{
						config.FilePath = path;
					}
					Event.current.Use();
				}
			}

			GUILayout.Button(EditorGUIUtility.IconContent("Project"), GUILayout.Width(32));
			EditorGUI.EndDisabledGroup();
			GUILayout.EndHorizontal();
			GUILayout.Space(5);
			//================================

			//GUILayout.BeginHorizontal();
			//GUILayout.Label(new GUIContent("Use Namespace", "Use namespace wrapping to generate code"), GUILayout.Width(128));
			//config.UseNamespace = GUILayout.Toggle(config.UseNamespace, "", GUILayout.Width(20));
			//EditorGUI.BeginDisabledGroup(!config.UseNamespace);
			//config.Namespace = GUILayout.TextField(config.Namespace);
			//EditorGUI.EndDisabledGroup();
			//GUILayout.EndHorizontal();
			//GUILayout.Space(5);
			//================================

			GUILayout.BeginHorizontal();
			GUILayout.Label(new GUIContent("Disperse Generate", "Generate each state/service into a different script"), GUILayout.Width(128));
			config.DisperseGenerate = GUILayout.Toggle(config.DisperseGenerate, "", GUILayout.Width(20));
			GUILayout.Label("", GUILayout.Width(10));
			//GUILayout.BeginVertical();
			if (config.DisperseGenerate)
			{
				GUILayout.Label("Disperse All", GUILayout.Width(128));
				config.DisperseAll = GUILayout.Toggle(config.DisperseAll, "", GUILayout.Width(20));
			}
			//GUILayout.EndVertical();
			GUILayout.EndHorizontal();
			GUILayout.Space(5);
			//================================

			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Open Editor"))
			{
				HFSMEditorWindow.ShowEditorWindow();
			}
			if (GUILayout.Button("Generate Script"))
			{
				controller.GenerateScriptController();
			}
			GUILayout.EndHorizontal();
			GUILayout.Space(10);
			//================================
			GUILayout.BeginHorizontal();
			GUILayout.Label(new GUIContent("Show Hidden Data"), GUILayout.Width(128));
			showHiddenData = GUILayout.Toggle(showHiddenData, "", GUILayout.Width(20));
			GUILayout.EndHorizontal();
			if (showHiddenData) 
			{
				DrawDefaultInspector();
			}
			//================================
			GUILayout.EndVertical();
		}
	}
}