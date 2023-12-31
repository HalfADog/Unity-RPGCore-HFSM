using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace RPGCore.AI.HFSM
{
	public class ParametersPopWindow : PopupWindowContent
	{
		private float width;
		private ParameterConditionData conditionData;
		private StateMachineExecutorController controller;

		private SearchField searchField;
		private Rect searchRect;
		private float searchHeight = 25f;

		private Rect labelRect;
		private float labelHeight;

		private ParameterTreeView paramterTree;
		private TreeViewState paramterTreeState;
		private Rect paramterRect;

		public ParametersPopWindow(float width, ParameterConditionData conditionData, StateMachineExecutorController controller)
		{
			this.width = width;
			this.conditionData = conditionData;
			this.controller = controller;
		}

		public override void OnGUI(Rect rect)
		{
			if (paramterTree == null)
			{
				if (paramterTreeState == null)
				{
					paramterTreeState = new TreeViewState();
				}
				paramterTree = new ParameterTreeView(paramterTreeState, controller, conditionData);
				paramterTree.Reload();
			}

			//搜索框
			if (searchField == null)
			{
				searchField = new SearchField();
			}
			searchRect.Set(rect.x + 5, rect.y + 5, this.width - 10, searchHeight);
			paramterTree.searchString = searchField.OnGUI(searchRect, paramterTree.searchString);

			//标签
			labelRect.Set(rect.x, rect.y, rect.width, labelHeight);
			EditorGUI.LabelField(labelRect, conditionData.parameterName, GUI.skin.GetStyle("AC BoldHeader"));

			//参数列表
			paramterRect.Set(rect.x, rect.y + searchHeight + labelHeight, rect.width, rect.height - searchHeight - labelHeight);
			paramterTree.OnGUI(paramterRect);
		}

		public override Vector2 GetWindowSize()
		{
			return new Vector2(this.width, 120);
		}
	}
}