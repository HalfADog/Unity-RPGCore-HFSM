using DogFramework;
using DogFramework.EditorExtension;
using System;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace RPGCore.AI.HFSM
{
	[CustomEditor(typeof(StateMachineInspectorHelper))]
	public class StateMachineInspector : Editor
	{
		public string stateName;
		private ReorderableList reorderableList;
		private Rect left_container;
		private Rect right_container;
		private bool isRenaming;
		private string tempname;

		private void OnEnable()
		{
			StateMachineInspectorHelper helper = target as StateMachineInspectorHelper;
			if (helper == null) return;
			reorderableList = new ReorderableList(helper.stateMachineData.services, typeof(string), true, false, true, true);
			reorderableList.onAddCallback += AddService;
			reorderableList.onRemoveCallback += RemoveService;
			reorderableList.drawElementCallback += DrawElement;
		}

		public override void OnInspectorGUI()
		{
			StateMachineInspectorHelper helper = target as StateMachineInspectorHelper;
			if (helper == null) return;
			bool disable = EditorApplication.isPlaying;
			EditorGUI.BeginDisabledGroup(disable);
			EditorGUILayout.BeginVertical();
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("state name", GUILayout.Width(80));
			string newName = helper.stateMachineData.id;
			disable = newName == "Root";
			EditorGUI.BeginDisabledGroup(disable);
			EditorGUI.BeginChangeCheck();
			newName = EditorGUILayout.DelayedTextField(newName);
			if (EditorGUI.EndChangeCheck() && newName != stateName)
			{
				helper.HFSMController.RenameState(helper.stateMachineData, newName);
				stateName = newName;
				EditorUtility.SetDirty(helper.HFSMController);
			}
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Space();
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("services", GUILayout.Width(80));
			if (reorderableList.list != helper.stateMachineData.services)
				reorderableList.list = helper.stateMachineData.services;
			reorderableList.DoLayoutList();
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Space();
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("description", GUILayout.Width(80));
			string description = helper.stateMachineData.description;
			EditorGUI.BeginChangeCheck();
			description = EditorGUILayout.DelayedTextField(description);
			if (EditorGUI.EndChangeCheck())
			{
				helper.stateMachineData.description = description;
			}
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.Space();
			EditorGUILayout.BeginHorizontal();
			EditorGUI.BeginDisabledGroup(!helper.HFSMController.controllerConfig.DisperseGenerate || helper.HFSMController.controllerConfig.DisperseAll);
			EditorGUILayout.LabelField(new GUIContent("independent", "Generate script files independently"), GUILayout.Width(80));
			helper.stateMachineData.independentGenerate = GUILayout.Toggle(helper.stateMachineData.independentGenerate, "", GUILayout.Width(20));
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.EndVertical();
			EditorGUI.EndDisabledGroup();
		}

		protected override void OnHeaderGUI()
		{
			StateMachineInspectorHelper helper = target as StateMachineInspectorHelper;
			if (helper == null) return;
			EditorGUILayout.Space();
			EditorGUILayout.BeginHorizontal();

			GUILayout.Label(EditorGUIUtility.IconContent("icons/processed/unityeditor/animations/animatorstate icon.asset"), GUILayout.Width(30), GUILayout.Height(30));
			EditorGUILayout.LabelField("Name", style: "HeaderLabel", GUILayout.Width(50));

			EditorGUILayout.LabelField(helper.stateMachineData.id);

			EditorGUILayout.EndHorizontal();

			var rect = EditorGUILayout.BeginHorizontal();

			EditorGUILayout.Space();
			Handles.color = Color.black;
			Handles.DrawLine(new Vector2(rect.x, rect.y), new Vector2(rect.x + rect.width, rect.y));
			EditorGUILayout.Space();

			EditorGUILayout.EndHorizontal();
		}

		private void DrawElement(Rect rect, int index, bool isActive, bool isFocused)
		{
			StateMachineInspectorHelper helper = target as StateMachineInspectorHelper;
			if (helper == null) return;
			left_container = rect.SplitVertical(1, 2)[0];
			right_container = rect.SplitVertical(1, 2)[1];
			ServiceData service = helper.stateMachineData.services[index];
			if (isFocused && EventExtension.IsMouseDown(0))
			{
				isRenaming = true;
			}
			if (isRenaming && reorderableList.index == index)
			{
				EditorGUI.BeginChangeCheck();
				tempname = EditorGUI.DelayedTextField(left_container, service.id);
				if (EditorGUI.EndChangeCheck())
				{
					helper.HFSMController.RenameService(service, tempname);
					isRenaming = false;
				}
			}
			else
			{
				EditorGUI.LabelField(left_container, service.id);
			}
			if (isRenaming && EventExtension.IsMouseDown(0))
			{
				isRenaming = false;
			}
			if (service.serviceType != ServiceType.CustomInterval)
			{
				GUI.Box(right_container.Resize(2), "");
				EditorGUI.LabelField(right_container, service.serviceType.ToString());
			}
			else
			{
				Rect[] rects = right_container.SplitVertical(3, 1);
				GUI.Box(rects[0].Resize(2), "");
				EditorGUI.LabelField(rects[0], service.serviceType.ToString());
				service.customInterval = EditorGUI.FloatField(rects[1], service.customInterval);
			}
		}

		private void RemoveService(ReorderableList list)
		{
			StateMachineInspectorHelper helper = target as StateMachineInspectorHelper;
			if (helper == null || list.index < 0) return;
			ServiceData service = helper.stateMachineData.services[list.index];
			helper.HFSMController.DeleteService(helper.stateMachineData, service);
		}

		private void AddService(ReorderableList list)
		{
			StateMachineInspectorHelper helper = target as StateMachineInspectorHelper;
			if (helper == null) return;
			GenericMenu genericMenu = new GenericMenu();

			for (int i = 0; i < Enum.GetNames(typeof(ServiceType)).Length; i++)
			{
				ServiceType serviceType = (ServiceType)Enum.GetValues(typeof(ServiceType)).GetValue(i);
				genericMenu.AddItem(new GUIContent(Enum.GetNames(typeof(ServiceType))[i]), false, () =>
				{
					helper.HFSMController.CreateService(helper.stateMachineData, serviceType);
				});
			}
			genericMenu.ShowAsContext();
		}
	}

	public class StateMachineInspectorHelper : ScriptableObjectSingleton<StateMachineInspectorHelper>
	{
		public StateMachineExecutorController HFSMController;
		public StateMachineData stateMachineData;

		public void Inspector(StateMachineExecutorController HFSMController, StateMachineData stateMachineData)
		{
			this.HFSMController = HFSMController;
			this.stateMachineData = stateMachineData;
			Selection.activeObject = this;
		}
	}
}