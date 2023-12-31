using DogFramework.EditorExtension;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RPGCore.AI.HFSM
{
	public class GraphLayer
	{
		protected Rect posotion;
		private Matrix4x4 transfromMatrix = Matrix4x4.identity;
		public HFSMEditorWindow editorWindow { get; private set; }
		public Context context => editorWindow.context;

		public GraphLayer(EditorWindow hFSMEditorWindow)
		{
			this.editorWindow = (HFSMEditorWindow)hFSMEditorWindow;
		}

		public virtual void OnGUI(Rect rect)
		{
			posotion = rect;
			UpdateTranslateMatrix();
		}

		public virtual void ProcessEvent()
		{
		}

		public virtual void Update()
		{
		}

		public void UpdateTranslateMatrix()
		{
			var centerMat = Matrix4x4.Translate(-posotion.center);
			var translationMat = Matrix4x4.Translate(this.context.dragOffset / this.context.zoomFactor);
			var scaleMat = Matrix4x4.Scale(Vector3.one * this.context.zoomFactor);

			this.transfromMatrix = centerMat.inverse * scaleMat * translationMat * centerMat;
		}

		public Rect GetTransfromRect(Rect rect)
		{
			Rect resulte = new Rect();
			resulte.position = transfromMatrix.MultiplyPoint(rect.position);
			resulte.size = transfromMatrix.MultiplyVector(rect.size);
			return resulte;
		}

		public Vector2 MousePosition(Vector2 mousePosition)
		{
			Vector2 center = mousePosition + (mousePosition - this.posotion.center) * (1 - this.context.zoomFactor) / this.context.zoomFactor;
			center -= this.context.dragOffset / this.context.zoomFactor;
			return center;
		}

		public bool IsMouseOverAnyState(List<StateBaseData> states)
		{
			if (states != null)
			{
				foreach (var item in states)
				{
					if (GetTransfromRect(item.position).MouseOn())
					{
						return true;
					}
				}
			}
			return false;
		}
	}
}