using DogFramework;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace RPGCore.AI.HFSM
{
	[CustomEditor(typeof(TransitionInspectorHelper))]
	public class TransitionInspector : Editor
	{
		private ReorderableList reorderableList;

		private Rect left_container;
		private Rect right_container;

		private Dictionary<ParameterType, ParameterConditionInspector> conditionInspectors = new Dictionary<ParameterType, ParameterConditionInspector>();
		private Rect popRect;

		private void OnEnable()
		{
			TransitionInspectorHelper helper = target as TransitionInspectorHelper;
			if (helper == null) return;
			reorderableList = new ReorderableList(helper.transitionData.parameterConditionDatas, typeof(ParameterConditionData), true, true, true, true);
			reorderableList.onAddCallback += AddCondition;
			reorderableList.onRemoveCallback += RemoveCondition;
			reorderableList.drawElementCallback += DrawElement;
			InitConditionInspectors();
		}

		public override void OnInspectorGUI()
		{
			TransitionInspectorHelper helper = target as TransitionInspectorHelper;
			if (helper == null) return;
			reorderableList.list = helper.transitionData.parameterConditionDatas;
			reorderableList.DoLayoutList();
		}

		protected override void OnHeaderGUI()
		{
			TransitionInspectorHelper helper = target as TransitionInspectorHelper;
			if (helper == null) return;

			EditorGUILayout.Space();
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label(EditorGUIUtility.IconContent("icons/processed/unityeditor/animations/animatorstatetransition icon.asset"), GUILayout.Width(30), GUILayout.Height(30));

			GUILayout.Label($" {helper.transitionData.from} ---> {helper.transitionData.to} ");

			EditorGUILayout.EndHorizontal();

			var rect = EditorGUILayout.BeginHorizontal();

			EditorGUILayout.Space();
			Handles.color = Color.black;
			Handles.DrawLine(new Vector2(rect.x, rect.y), new Vector2(rect.x + rect.width, rect.y));
			EditorGUILayout.Space();

			EditorGUILayout.EndHorizontal();
		}

		private void InitConditionInspectors()
		{
			conditionInspectors.Add(ParameterType.Int, new ParameterIntConditionInspector());
			conditionInspectors.Add(ParameterType.Float, new ParameterFloatConditionInspector());
			conditionInspectors.Add(ParameterType.Bool, new ParameterBoolConditionInspector());
			conditionInspectors.Add(ParameterType.Trigger, new ParameterTriggerConditionInspector());
		}

		private void DrawElement(Rect rect, int index, bool isActive, bool isFocused)
		{
			TransitionInspectorHelper helper = target as TransitionInspectorHelper;
			if (helper == null) return;

			ParameterConditionData conditionData = helper.transitionData.parameterConditionDatas[index];

			left_container.Set(rect.x, rect.y, rect.width * 0.5f, rect.height);
			right_container.Set(left_container.x + left_container.width, left_container.y, rect.width * 0.5f, rect.height);
			//左半边 条件的参数
			if (helper.transitionData.parameterConditionDatas.Count > 0)
			{
				if (EditorGUI.DropdownButton(left_container, new GUIContent(conditionData.parameterName), FocusType.Keyboard))
				{
					//TODO 弹出下拉菜单
					popRect.Set(rect.x, rect.y + 2, rect.width / 2, rect.height);
					PopupWindow.Show(popRect, new ParametersPopWindow(rect.width / 2, conditionData, helper.HFSMController));
				}
			}

			//右半边 条件的目标值(类型)
			Parameter parameterData = helper.HFSMController.parameters.Where(x => x.name == conditionData.parameterName).FirstOrDefault();
			if (parameterData != null)
			{
				//参数类型绘制Type
				if (conditionInspectors.Keys.Contains(parameterData.type))
				{
					conditionInspectors[parameterData.type].OnGUI(right_container, helper.HFSMController, conditionData);
				}
			}
		}

		private void RemoveCondition(ReorderableList list)
		{
			TransitionInspectorHelper helper = target as TransitionInspectorHelper;
			if (helper == null) return;
			helper.HFSMController.DeleteParameterCondition(helper.transitionData, list.index);
		}

		private void AddCondition(ReorderableList list)
		{
			TransitionInspectorHelper helper = target as TransitionInspectorHelper;
			if (helper == null) return;
			helper.HFSMController.CreateParamterCondition(helper.transitionData);
		}
	}

	public class TransitionInspectorHelper : ScriptableObjectSingleton<TransitionInspectorHelper>
	{
		public StateMachineExecutorController HFSMController;
		public TransitionData transitionData;

		public void Inspector(StateMachineExecutorController HFSMController, TransitionData transitionData)
		{
			this.HFSMController = HFSMController;
			this.transitionData = transitionData;
			Selection.activeObject = this;
		}
	}
}