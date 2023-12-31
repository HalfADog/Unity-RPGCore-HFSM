using DogFramework.EditorExtension;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RPGCore.AI.HFSM
{
	public class GraphTransitionLayer : GraphLayer
	{
		public static Color Select_Color { get; } = new Color(100, 200, 255, 255) / 255;

		public GraphTransitionLayer(EditorWindow hFSMEditorWindow) : base(hFSMEditorWindow)
		{
		}

		public override void OnGUI(Rect rect)
		{
			base.OnGUI(rect);

			StateBaseData defualtState = this.context.currentChildStatesData.Where(x => x.isDefault).FirstOrDefault();
			StateBaseData enterState = this.context.currentChildStatesData.Where(x => x.id == StateMachine.entryState).FirstOrDefault();

			//Ä¬ÈÏ×´Ì¬
			DrawTransition(enterState, defualtState, Color.yellow);

			//ÆäËû×´Ì¬
			foreach (TransitionData item in this.context.currentTransitionData)
			{
				DrawTransition(item.from, item.to, item == this.context.selectedTransition ? Select_Color : Color.white);
			}

			//»æÖÆÔ¤ÀÀ
			if (this.context.isPreviewTransition)
			{
				GUIExtension.Begin();
				if (this.context.preTo == null || this.context.preTo.id == StateMachine.entryState || this.context.preTo.id == StateMachine.anyState)
				{
					GUIExtension.DrawArrowLine(GetTransfromRect(this.context.preFrom.position).center, Event.current.mousePosition, 5, Color.white);
				}
				else
				{
					GUIExtension.DrawArrowLine(GetTransfromRect(this.context.preFrom.position).center, this.context.preTo.position.center, 5, Color.white);
				}
				GUIExtension.End();
				this.editorWindow.Repaint();
			}
		}

		public override void ProcessEvent()
		{
			TransitionOnMouseClick();

			if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Delete)
			{
				if (this.context.selectedTransition != null)
				{
					this.context.HFSMController.DeleteTransition(this.context.currentStateMachine, this.context.selectedTransition);
					this.context.selectedTransition = null;
					context.UpdateCurrentTransitionData();
					this.editorWindow.Repaint();
					Event.current.Use();
				}
				//FSMTranslationFactory.DeleteTransition(this.Context.RunTimeFSMContorller, this.Context.SelectTransition);
			}
		}

		private void TransitionOnMouseClick()
		{
			if (EventExtension.IsMouseUp(0))
			{
				foreach (TransitionData item in this.context.currentTransitionData)
				{
					StateBaseData fromSatteData = this.context.currentChildStatesData.Where(x => x.id == item.from).FirstOrDefault();
					StateBaseData toStateData = this.context.currentChildStatesData.Where(x => x.id == item.to).FirstOrDefault();
					if (fromSatteData == null || toStateData == null) return;

					Rect fromRect = GetTransfromRect(fromSatteData.position);
					Rect toRect = GetTransfromRect(toStateData.position);

					Vector2 offset = GetTransitionOffset(fromRect.center, toRect.center);
					Vector2 fromPos = fromRect.center + offset;
					Vector2 toPos = toRect.center + offset;

					float width = Mathf.Clamp(Mathf.Abs(toPos.x - fromPos.x), 10f, Mathf.Abs(toPos.x - fromPos.x));
					float height = Mathf.Clamp(Mathf.Abs(toPos.y - fromPos.y), 10f, Mathf.Abs(toPos.y - fromPos.y));
					Rect rect = new Rect(0, 0, width, height);
					rect.center = fromPos + (toPos - fromPos) * 0.5f;

					if (rect.MouseOn() && (context.selectedStates.Count == 0 || !context.selectedStates[0].position.Contains(MousePosition(Event.current.mousePosition))))
					{
						if (GetMinDistanceToLine(fromPos, toPos, Event.current.mousePosition))
						{
							this.context.selectedTransition = item;
							ShowInspactor(item);
							Event.current.Use();
							break;
						}
					}
				}
			}
		}

		private void ShowInspactor(TransitionData translationData)
		{
			//FSMTranslationInspectorHelper.Instance.Inspector(this.Context.RunTimeFSMContorller, translationData);
			TransitionInspectorHelper.instance.Inspector(context.HFSMController, translationData);
		}

		private bool GetMinDistanceToLine(Vector2 start, Vector2 end, Vector2 point)
		{
			Vector2 direction = end - start;
			Vector2 start2point = point - start;

			Vector2 projectDir = start2point.magnitude * Vector2.Dot(direction.normalized, start2point.normalized) * direction.normalized;
			Vector2 pointProject = start + projectDir;
			float distance = Vector3.Distance(pointProject, point);

			if (distance < 5)
			{
				return true;
			}
			return false;
		}

		private void DrawTransition(string start, string end, Color color, bool isShowArrow = true)
		{
			StateBaseData fromState = this.context.currentChildStatesData.Where(x => x.id == start).FirstOrDefault();
			StateBaseData toState = this.context.currentChildStatesData.Where(x => x.id == end).FirstOrDefault();
			DrawTransition(fromState, toState, color, isShowArrow);
		}

		private void DrawTransition(StateBaseData start, StateBaseData end, Color color, bool isShowArrow = true)
		{
			if (start == null || end == null) return;

			Rect startRect = GetTransfromRect(start.position);
			Rect endRect = GetTransfromRect(end.position);

			Vector2 offset = GetTransitionOffset(startRect.center, endRect.center);

			if (this.posotion.Contains(startRect.center + offset) ||
				this.posotion.Contains(endRect.center + offset) ||
				this.posotion.Contains((endRect.center - startRect.center) * 0.5f + startRect.center + offset))
			{
				GUIExtension.DrawArrowLine(startRect.center + offset, endRect.center + offset, 5, color);
			}
		}

		private Vector2 GetTransitionOffset(Vector2 origin, Vector2 traget)
		{
			Vector2 direction = traget - origin;

			Vector2 offset = Vector2.zero;

			if (Mathf.Abs(direction.y) > Mathf.Abs(direction.x))
			{
				//ÉÏÏÂ
				offset.x += direction.y < 0 ? 10 : -10;
			}
			else
			{
				//×óÓÒ
				offset.y += direction.x < 0 ? 10 : -10;
			}

			return offset * this.context.zoomFactor;
		}
	}
}