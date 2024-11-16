using DogFramework.EditorExtension;
using System;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace RPGCore.AI.HFSM
{
	public class GraphParameterLayer : GraphLayer
	{
		private ReorderableList reorderableList;
		private Vector2 scrollView;

		private Rect left_container;
		private Rect right_container;

		private bool isRenaming;
		private string tempname;

		public GraphParameterLayer(EditorWindow hFSMEditorWindow) : base(hFSMEditorWindow)
		{
		}

		public override void OnGUI(Rect rect)
		{
			base.OnGUI(rect);
			GUILayout.Label("Parameters", GUILayout.MinHeight(18));
			GUI.Box(rect, string.Empty, GUI.skin.GetStyle("CN Box"));

			if (reorderableList == null)
			{
				reorderableList = new ReorderableList(this.context.HFSMController.parameters, typeof(Parameter), true, false, true, true);

				reorderableList.onAddCallback += AddParamter;
				reorderableList.onRemoveCallback += RemoveParamter;
				reorderableList.drawElementCallback += DrawOneParamter;

				reorderableList.onCanAddCallback += CanAddOrDeleteParamter;
			}
			if (Application.isPlaying && this.context.executor != null)
			{
				reorderableList.list = this.context.executor.scriptController.parameters.Values.ToList();
			}
			else
			{
				reorderableList.list = this.context.HFSMController.parameters;
			}

			scrollView = GUILayout.BeginScrollView(scrollView);
			reorderableList.DoLayoutList();
			GUILayout.EndScrollView();
		}

		public override void ProcessEvent()
		{
			base.ProcessEvent();
		}

		private bool CanAddOrDeleteParamter(ReorderableList list)
		{
			return Application.isPlaying == false;
		}

		/// <summary>
		/// 绘制单条参数
		/// </summary>
		/// <param name="rect"></param>
		/// <param name="index"></param>
		/// <param name="isActive"></param>
		/// <param name="isFocused"></param>
		private void DrawOneParamter(Rect rect, int index, bool isActive, bool isFocused)
		{
			if (index < 0 || index > this.context.HFSMController.parameters.Count - 1)
				return;
			Parameter parameterData = null;
			if (Application.isPlaying)
			{
				parameterData = context.executor.scriptController.parameters.Values.ToList()[index];
			}
			else
			{
				parameterData = this.context.HFSMController.parameters[index];
			}

			left_container.Set(rect.x, rect.y, rect.width * 0.5f, rect.height);
			right_container.Set(left_container.x + left_container.width, left_container.y, rect.width * 0.5f, rect.height);

			if (isFocused && EventExtension.IsMouseDown(0))
			{
				isRenaming = true;
			}

			//参数名
			if (!Application.isPlaying && isRenaming && reorderableList.index == index)
			{
				EditorGUI.BeginChangeCheck();
				tempname = EditorGUI.DelayedTextField(left_container, parameterData.name);
				if (EditorGUI.EndChangeCheck())
				{
					this.context.HFSMController.RenameParameter(parameterData, tempname);
					isRenaming = false;
				}
			}
			else
			{
				EditorGUI.LabelField(left_container, parameterData.name);
			}

			if (isRenaming && EventExtension.IsMouseDown(0))
			{
				isRenaming = false;
			}

			switch (parameterData.type)
			{
				case ParameterType.Float:
					parameterData.baseValue = EditorGUI.DelayedFloatField(right_container, parameterData.baseValue);
					break;

				case ParameterType.Int:
					parameterData.baseValue = EditorGUI.DelayedFloatField(right_container, parameterData.baseValue);
					break;

				case ParameterType.Bool:
					parameterData.baseValue = EditorGUI.Toggle(right_container, parameterData.baseValue == 1) ? 1 : 0;
					break;

				case ParameterType.Trigger:
					GUIStyle style = new GUIStyle("Radio");
					style.alignment = TextAnchor.MiddleCenter;
					parameterData.baseValue = EditorGUI.Toggle(right_container.Resize(0, -5, 0, 5), parameterData.baseValue == 1, style) ? 1 : 0;
					break;
			}
		}

		private void RemoveParamter(ReorderableList list)
		{
			this.context.HFSMController.DeleteParameter(list.index);
		}

		/// <summary>
		/// 添加参数
		/// </summary>
		/// <param name="list"></param>
		private void AddParamter(ReorderableList list)
		{
			GenericMenu genericMenu = new GenericMenu();

			for (int i = 0; i < Enum.GetNames(typeof(ParameterType)).Length; i++)
			{
				ParameterType parameterType = (ParameterType)Enum.GetValues(typeof(ParameterType)).GetValue(i);
				genericMenu.AddItem(new GUIContent(Enum.GetNames(typeof(ParameterType))[i]), false, () =>
				{
					this.context.HFSMController.CreateParamter(parameterType);
				});
			}
			genericMenu.ShowAsContext();
		}
	}
}