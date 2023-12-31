using DogFramework.EditorExtension;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class TestEditorWindow : EditorWindow
{
	private VisualElement Root;
	private IMGUIContainer stateGraphContaner;
	private IMGUIContainer parametersContaner;
	private IMGUIContainer stateLayerContaner;

	private Rect[] hSplitRects;
	private Rect[] vSplitRects;
	private Rect[] rects = new Rect[50];
	private Rect[] originalRects = new Rect[50];

	//[MenuItem("Tools/HFSM/Test Editor Window")]
	public static void ShowEditorWindow()
	{
		TestEditorWindow window = GetWindow<TestEditorWindow>("HFSM Editor Window");
		window.minSize = new Vector2(640, 460);
		window.Show();
	}

	public void CreateGUI()
	{
		// Import UXML
		var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Scripts/Unity-RPGCore-HFSM/Editor/Resources/HFSMEditorWindow.uxml");
		rootVisualElement.Add(visualTree.Instantiate());

		Root = rootVisualElement.Q<VisualElement>("Root");
		stateGraphContaner = Root.Q<IMGUIContainer>("StateGraphIMGUI");
		parametersContaner = Root.Q<IMGUIContainer>("ParametersIMGUI");
		stateLayerContaner = Root.Q<IMGUIContainer>("StateLayerIMGUI");

		stateGraphContaner.onGUIHandler += StateGraphOnGUI;
		parametersContaner.onGUIHandler += ParametersOnGUI;
		stateLayerContaner.onGUIHandler += StateLayerOnGUI;
	}

	private void Update()
	{
		Root.style.width = this.position.width;
		Root.style.height = this.position.height;
	}

	private void OnGUI()
	{
	}

	private Vector2 dragOffset = Vector2.zero;
	private bool init = false;
	private float zoomFactor = 1f;
	private Matrix4x4 transfromMatrix = Matrix4x4.identity;
	private bool repaint = false;
	private int dragingNodeIndex = -1;

	private void StateGraphOnGUI()
	{
		if (!init)
		{
			if (stateGraphContaner.contentRect.width > 0 && stateGraphContaner.contentRect.height > 0)
			{
				init = true;
				hSplitRects = stateGraphContaner.contentRect.SplitHorizontal(2, 1);
				hSplitRects[0] = hSplitRects[0];
				hSplitRects[1] = hSplitRects[1].Resize(0, -5);
				vSplitRects = hSplitRects[1].SplitVertical(1, 1);
				vSplitRects[0] = vSplitRects[0].Resize(0, 0, -5, 0);
				vSplitRects[1] = vSplitRects[1].Resize(-5, 0, 0, 0);
				float width = Random.Range(32, 64);
				for (int i = 0; i < rects.Length; i++)
				{
					rects[i].x = Random.Range(hSplitRects[0].xMin, hSplitRects[0].xMax - width);
					rects[i].y = Random.Range(hSplitRects[0].yMin, hSplitRects[0].yMax - width);
					rects[i].width = width;
					rects[i].height = width;
					originalRects[i] = rects[i];
				}
				rects = rects.NormalizationWith(hSplitRects[0]);
			}
		}
		else
		{
			hSplitRects = stateGraphContaner.contentRect.SplitHorizontal(2, 1);
			hSplitRects[0] = hSplitRects[0];
			hSplitRects[1] = hSplitRects[1].Resize(0, -5);
			vSplitRects = hSplitRects[1].SplitVertical(1, 1);
			vSplitRects[0] = vSplitRects[0].Resize(0, 0, -5, 0);
			vSplitRects[1] = vSplitRects[1].Resize(-5, 0, 0, 0);
			//rects = rects.NormalizationWith(hSplitRects[0]);

			EditorGUI.DrawRect(hSplitRects[0], new Color(0.2f, 0, 0.2f, 0.25f));

			EditorGUI.DrawRect(vSplitRects[0], new Color(0, 0, 0, 0.25f));
			EditorGUI.DrawRect(vSplitRects[1], new Color(0, 0, 0, 0.25f));
			GUIExtension.Begin();
			for (int i = 0; i < count; i++)
			{
				if (i + 1 < count)
				{
					if (lineType) GUIExtension.DrawArrowLine(rects[i].center, rects[i + 1].center, 5, Color.white);
					else GUIExtension.DrawBezierEdge(rects[i], rects[i + 1], 5, Color.white, edgeOrientation);
					//GUIExtension.DrawBezier2(rects[i].position, rects[i + 1].position, rects[i].center + new Vector2(0, 200), 5, Color.white);
				}
			}
			GUIExtension.End();
			for (int i = 0; i < count; i++)
			{
				GUI.Box(rects[i], i.ToString(), "flow node 0");
			}

			Rect[] rects2 = rects.NormalizationWith(vSplitRects[0]);
			EditorGUI.DrawRect(rects2.GetBoundingBox(), new Color(0.2f, 0, 0.2f, 0.25f));
			EditorGUI.DrawRect(hSplitRects[1], new Color(0.2f, 0, 0.2f, 1f));
			for (int i = 0; i < count; i++)
			{
				EditorGUI.DrawRect(rects2[i], Color.green);
			}
		}
		if (Event.current.type == EventType.MouseDrag && Event.current.button == 2)
		{
			if (Event.current.delta != Vector2.zero)
			{
				repaint = true;
			}
			dragOffset += Event.current.delta;
			Debug.Log(dragOffset);
			//dragOffset *= zoomFactor;
			Event.current.Use();
		}
		if (Event.current.type == EventType.ScrollWheel)
		{
			zoomFactor -= Mathf.Sign(Event.current.delta.y) / 20f;
			zoomFactor = Mathf.Clamp(zoomFactor, 0.2f, 2.0f);
			repaint = true;
			Event.current.Use();
		}
		if (Event.current.type == EventType.MouseDrag && Event.current.button == 0)
		{
			if (dragingNodeIndex == -1)
			{
				for (int i = 0; i < rects.Length; i++)
				{
					if (rects[i].MouseOn())
					{
						originalRects[i].position += Event.current.delta / zoomFactor;
						dragingNodeIndex = i;
						//Event.current.Use();
						break;
					}
				}
			}
			else
			{
				repaint = true;
				originalRects[dragingNodeIndex].position += Event.current.delta / zoomFactor;
			}
			Event.current.Use();
		}
		if (Event.current.type == EventType.MouseUp && Event.current.button == 0)
		{
			dragingNodeIndex = -1;
		}
		if (repaint)
		{
			var centerMat = Matrix4x4.Translate(-stateGraphContaner.contentRect.center);//-stateGraphContaner.contentRect.center
			var translationMat = Matrix4x4.Translate(dragOffset / zoomFactor);
			var scaleMat = Matrix4x4.Scale(Vector3.one * zoomFactor);

			transfromMatrix = centerMat.inverse * scaleMat * translationMat * centerMat;
			for (int i = 0; i < rects.Length; i++)
			{
				rects[i].position = transfromMatrix.MultiplyPoint(originalRects[i].position);
				rects[i].size = transfromMatrix.MultiplyVector(originalRects[i].size);
			}
			repaint = false;
		}
	}

	private int count = 10;
	private float strength = 200;
	private GUIExtension.EdgeOrientation edgeOrientation = GUIExtension.EdgeOrientation.Horizontal;
	private bool lineType = false;

	private void ParametersOnGUI()
	{
		Rect rect = parametersContaner.contentRect;
		rect.height = 32;
		if (GUI.Button(rect.Resize(-5, 0), "Regenerate"))
		{
			float width = Random.Range(32, 64);
			for (int i = 0; i < rects.Length; i++)
			{
				rects[i].x = Random.Range(hSplitRects[0].xMin, hSplitRects[0].xMax - width);
				rects[i].y = Random.Range(hSplitRects[0].yMin, hSplitRects[0].yMax - width);
				rects[i].width = width;
				rects[i].height = width;
				originalRects[i] = rects[i];
			}
			rects = rects.NormalizationWith(hSplitRects[0]);
			zoomFactor = 1.0f;
			dragOffset = Vector2.zero;
		}
		rect = rect.BelowBlock(32).Resize(-5);
		count = (int)GUI.HorizontalSlider(rect, (float)count, 2, 50);
		rect = rect.BelowBlock(32);
		if (GUI.Button(rect.Resize(-5, 0), "Switch Orientation"))
		{
			edgeOrientation = edgeOrientation == GUIExtension.EdgeOrientation.Horizontal
				? GUIExtension.EdgeOrientation.Vertical
				: GUIExtension.EdgeOrientation.Horizontal;
		}
		rect = rect.BelowBlock(32);
		if (GUI.Button(rect.Resize(-5, 0), "Switch LineType"))
		{
			lineType = !lineType;
		}
		rect = rect.BelowBlock(96).Resize(-5);
		EditorGUI.DrawRect(rect, new Color(0.2f, 0, 0.2f, 0.25f));
		rect = rect.Resize(-5);
		GUIExtension.Begin();
		GUIExtension.DrawPoint(rect.position, Color.green);
		GUIExtension.DrawPoint(rect.position + rect.size, Color.green);
		GUIExtension.DrawPoint(rect.center + new Vector2(-50, 30), Color.red);
		GUIExtension.DrawBezier2(rect.position, rect.position + rect.size, rect.center + new Vector2(0, 50), 5, Color.white);
		GUIExtension.End();
	}

	private void StateLayerOnGUI()
	{
		GUI.Box(stateLayerContaner.contentRect, "StateLayer");
	}
}