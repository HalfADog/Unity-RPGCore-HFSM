using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace RPGCore.AI.HFSM
{
	public class HFSMEditorWindow : EditorWindow
	{
		private VisualElement Root;
		private IMGUIContainer stateGraphContaner;
		private IMGUIContainer parametersContaner;
		private IMGUIContainer stateMachinePathContaner;
		private VisualElement noticeBoard;
		private ToolbarButton generateButton;
		private Context m_context = new Context();
		public Context context => m_context;
		private List<GraphLayer> graphLayerList = new List<GraphLayer>();
		private GraphLayer paramterLayer = null;
		private GraphLayer stateMachinePathLayer = null;

		[MenuItem("Tools/HFSM Editor Window")]
		public static void ShowEditorWindow()
		{
			HFSMEditorWindow window = GetWindow<HFSMEditorWindow>();
			window.minSize = new Vector2(640, 460);
			GUIContent content = EditorGUIUtility.IconContent("AnimatorStateMachine Icon");
			content.text = "HFSM Editor Window";
			window.titleContent = content;
			window.Show();
		}

		public void CreateGUI()
		{
			VisualTreeAsset visualTree = null;
			foreach (var guid in AssetDatabase.FindAssets("t:VisualTreeAsset"))
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				if (path.Contains("HFSMEditorWindow.uxml"))
				{
					visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
				}
			}
			if (visualTree != null)
			{
				rootVisualElement.Add(visualTree.Instantiate());
				Root = rootVisualElement.Q<VisualElement>("Root");
				Root.style.width = position.width;
				Root.style.height = position.height;
				stateGraphContaner = Root.Q<IMGUIContainer>("StateGraphIMGUI");
				parametersContaner = Root.Q<IMGUIContainer>("ParametersIMGUI");
				stateMachinePathContaner = Root.Q<IMGUIContainer>("StateLayerIMGUI");
				noticeBoard = Root.Q<VisualElement>("Notice");
				generateButton = Root.Q<ToolbarButton>("GenerateButton");
				generateButton.RegisterCallback<ClickEvent>(callback => 
				{
					context.HFSMController.GenerateScriptController();
				});
				stateMachinePathContaner.onGUIHandler += StateMachinePathOnGUI;
				parametersContaner.onGUIHandler += ParametersOnGUI;
				stateGraphContaner.onGUIHandler += StateGraphOnGUI;
			}
			else
			{
				Debug.LogWarning("Can't find HFSMEditorWindow.uxml asset file,Check is it exist.");
			}
		}

		private void OnGUI()
		{
			if (Root == null || Root.style == null) return;
			if (position.width != Root.style.width || position.height != Root.style.height)
			{
				Root.style.width = position.width;
				Root.style.height = position.height;
			}
		}

		private void Update()
		{
			noticeBoard.style.visibility = context.HFSMController == null ? Visibility.Visible : Visibility.Hidden;
			foreach (var item in graphLayerList)
			{
				item.Update();
			}
		}

		private void OnSelectionChange()
		{
			if (Selection.activeObject as StateMachineExecutorController != null || Selection.activeObject as GameObject != null)
			{
				Repaint();
			}
		}

		private void StateGraphOnGUI()
		{
			if (graphLayerList.Count == 0)
			{
				InitGraphLayers();
			}

			foreach (var item in graphLayerList)
			{
				item.OnGUI(stateGraphContaner.contentRect);
			}

			for (int i = graphLayerList.Count - 1; i >= 0; i--)
			{
				graphLayerList[i].ProcessEvent();
			}
		}

		private void ParametersOnGUI()
		{
			if (this.context.HFSMController == null)
				return;

			if (paramterLayer == null)
			{
				paramterLayer = new GraphParameterLayer(this);
			}
			paramterLayer.OnGUI(parametersContaner.contentRect);
			paramterLayer.ProcessEvent();
		}

		private void StateMachinePathOnGUI()
		{
			if (this.context.HFSMController == null)
				return;

			if (stateMachinePathLayer == null)
			{
				stateMachinePathLayer = new GraphStateMachinePathLayer(this);
			}
			stateMachinePathLayer.OnGUI(stateMachinePathContaner.contentRect);
			stateMachinePathLayer.ProcessEvent();
		}

		private void InitGraphLayers()
		{
			graphLayerList.Add(new GraphBackgroundLayer(this));
			graphLayerList.Add(new GraphTransitionLayer(this));
			graphLayerList.Add(new GraphStateLayer(this));
		}
	}
}