using System;
using UnityEditor;
using UnityEngine;

namespace RPGCore.AI.HFSM
{
	public abstract class ParameterConditionInspector
	{
		public abstract void OnGUI(Rect rect, StateMachineExecutorController contorller, ParameterConditionData conditionData);
	}

	public class ParameterFloatConditionInspector : ParameterConditionInspector
	{
		private Rect left_rect;
		private Rect right_rect;

		public override void OnGUI(Rect rect, StateMachineExecutorController contorller, ParameterConditionData conditionData)
		{
			left_rect.Set(rect.x, rect.y, rect.width * 0.5f, rect.height);
			right_rect.Set(left_rect.x + left_rect.width, rect.y, rect.width * 0.5f, rect.height);

			//条件
			if (EditorGUI.DropdownButton(left_rect, new GUIContent(conditionData.compareType.ToString()), FocusType.Passive))
			{
				GenericMenu genericMenu = new GenericMenu();

				for (int i = 0; i < Enum.GetValues(typeof(CompareType)).Length; i++)
				{
					CompareType compareType = (CompareType)Enum.GetValues(typeof(CompareType)).GetValue(i);

					if (compareType == CompareType.Equal || compareType == CompareType.NotEqual) { continue; }

					genericMenu.AddItem(new GUIContent(compareType.ToString()), conditionData.compareType == compareType, () =>
					{
						conditionData.compareType = compareType;
						contorller.Save();
					});
				}

				genericMenu.ShowAsContext();
			}

			//目标值
			conditionData.compareValue = EditorGUI.FloatField(right_rect, conditionData.compareValue);
			UnityEditor.EditorUtility.SetDirty(contorller);
		}
	}

	public class ParameterIntConditionInspector : ParameterConditionInspector
	{
		private Rect left_rect = new Rect();
		private Rect right_rect = new Rect();

		public override void OnGUI(Rect rect, StateMachineExecutorController contorller, ParameterConditionData conditionData)
		{
			left_rect.Set(rect.x, rect.y, rect.width / 2, rect.height);
			right_rect.Set(rect.x + rect.width / 2, rect.y, rect.width / 2, rect.height);

			//条件
			if (EditorGUI.DropdownButton(left_rect, new GUIContent(conditionData.compareType.ToString()), FocusType.Keyboard))
			{
				GenericMenu genericMenu = new GenericMenu();

				for (int i = 0; i < Enum.GetValues(typeof(CompareType)).Length; i++)
				{
					CompareType compareType = (CompareType)Enum.GetValues(typeof(CompareType)).GetValue(i);

					genericMenu.AddItem(new GUIContent(compareType.ToString()), conditionData.compareType == compareType, () =>
					{
						conditionData.compareType = compareType;
						contorller.Save();
					});
				}
				genericMenu.ShowAsContext();
			}

			//目标值
			conditionData.compareValue = EditorGUI.IntField(right_rect, (int)conditionData.compareValue);
			UnityEditor.EditorUtility.SetDirty(contorller);
		}
	}

	public class ParameterBoolConditionInspector : ParameterConditionInspector
	{
		public override void OnGUI(Rect rect, StateMachineExecutorController controller, ParameterConditionData conditionData)
		{
			if (EditorGUI.DropdownButton(rect, new GUIContent(conditionData.compareValue == 1 ? "True" : "False"), FocusType.Keyboard))
			{
				GenericMenu genericMenu = new GenericMenu();
				genericMenu.AddItem(new GUIContent("True"), conditionData.compareValue == 1, () =>
				{
					conditionData.compareValue = 1;
					controller.Save();
				});

				genericMenu.AddItem(new GUIContent("False"), conditionData.compareValue == 0, () =>
				{
					conditionData.compareValue = 0;
					controller.Save();
				});
				genericMenu.ShowAsContext();
			}
		}
	}

	public class ParameterTriggerConditionInspector : ParameterConditionInspector
	{
		public override void OnGUI(Rect rect, StateMachineExecutorController controller, ParameterConditionData conditionData)
		{
			//if (EditorGUI.DropdownButton(rect, new GUIContent(conditionData.compareValue == 1 ? "True" : "False"), FocusType.Keyboard))
			//{
			//	GenericMenu genericMenu = new GenericMenu();
			//	genericMenu.AddItem(new GUIContent("True"), conditionData.compareValue == 1, () =>
			//	{
			//		conditionData.compareValue = 1;
			//		controller.Save();
			//	});

			//	genericMenu.AddItem(new GUIContent("False"), conditionData.compareValue == 0, () =>
			//	{
			//		conditionData.compareValue = 0;
			//		controller.Save();
			//	});
			//	genericMenu.ShowAsContext();
			//}
		}
	}
}