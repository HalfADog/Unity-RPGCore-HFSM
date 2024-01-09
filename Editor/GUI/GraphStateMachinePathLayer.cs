using DogFramework.EditorExtension;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RPGCore.AI.HFSM
{
	public class GraphStateMachinePathLayer : GraphLayer
	{
		private float pathWidth = 96f;
		private int selectedIndex = -1;

		private Dictionary<string, GUIStyle> styles = new Dictionary<string, GUIStyle>();

		public GraphStateMachinePathLayer(EditorWindow hFSMEditorWindow) : base(hFSMEditorWindow)
		{
			styles["LeftBackground"] = new GUIStyle("GUIEditor.BreadcrumbLeftBackground");
			styles["LeftBackground"].alignment = TextAnchor.MiddleCenter;
			styles["MidBackground"] = new GUIStyle("GUIEditor.BreadcrumbMidBackground");
			styles["MidBackground"].alignment = TextAnchor.MiddleCenter;
		}

		public override void OnGUI(Rect rect)
		{
			base.OnGUI(rect);
			rect.width = 5;
			for (int i = 0; i < context.stateMachinePath.Count; i++)
			{
				rect = rect.RightBlock(GUILayoutUtility.GetRect(new GUIContent(context.stateMachinePath[i].id), GUIStyle.none, GUILayout.ExpandWidth(false)).width + 20);
				if (GUI.Button(rect, context.stateMachinePath[i].id, styles[i == 0 ? "LeftBackground" : "MidBackground"]))
				{
					selectedIndex = i;
				}
			}
		}

		public override void ProcessEvent()
		{
			base.ProcessEvent();
			if (selectedIndex != -1)
			{
				if (context.stateMachinePath[selectedIndex].id != context.currentStateMachine.id)
				{
					context.currentStateMachine = context.stateMachinePath[selectedIndex];
					StateMachineData[] temp = context.stateMachinePath.Take(selectedIndex + 1).ToArray();
					context.stateMachinePath.Clear();
					context.stateMachinePath.AddRange(temp);
					editorWindow.Repaint();
				}
				//点击Root时打开Root状态机的Inspector
				if (context.stateMachinePath[selectedIndex].id == "Root")
				{
					StateMachineInspectorHelper.instance.Inspector(context.HFSMController, context.stateMachinePath[selectedIndex]);
					context.ClearAllSelectNode();
				}
				selectedIndex = -1;
			}
		}
	}
}