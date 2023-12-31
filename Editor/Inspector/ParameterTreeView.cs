using System.Linq;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace RPGCore.AI.HFSM
{
	public class ParameterTreeView : TreeView
	{
		private StateMachineExecutorController controller;
		private ParameterConditionData conditionData;

		public ParameterTreeView(TreeViewState state, StateMachineExecutorController controller, ParameterConditionData conditionData) : base(state)
		{
			this.controller = controller;
			this.conditionData = conditionData;

			showBorder = true;//±ß¿ò
			showAlternatingRowBackgrounds = true;//½»ÌæÏÔÊ¾
		}

		protected override TreeViewItem BuildRoot()
		{
			TreeViewItem root = new TreeViewItem(-1, -1);

			if (controller != null)
			{
				for (int i = 0; i < controller.parameters.Count; i++)
				{
					root.AddChild(new TreeViewItem(i, 0, controller.parameters[i].name));
				}
			}

			return root;
		}

		protected override void RowGUI(RowGUIArgs args)
		{
			base.RowGUI(args);
			if (args.label == conditionData.parameterName)
			{
				GUI.Label(args.rowRect, "¡Ì");
			}
		}

		protected override void SingleClickedItem(int id)
		{
			base.SingleClickedItem(id);
			string paramterName = FindItem(id, rootItem).displayName;

			Parameter parameterData = controller.parameters.Where(x => x.name == paramterName).FirstOrDefault();
			if (parameterData != null)
			{
				conditionData.parameterName = parameterData.name;
				switch (parameterData.type)
				{
					case ParameterType.Float:
					case ParameterType.Int:
						conditionData.compareType = CompareType.Greater;
						break;

					case ParameterType.Bool:
					case ParameterType.Trigger:
						conditionData.compareType = CompareType.Equal;
						break;
				}
				controller.Save();
			}
		}
	}
}