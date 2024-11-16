using DogFramework.EditorExtension;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RPGCore.AI.HFSM
{
	public class GraphStateLayer : GraphLayer
	{
		private StateStyle m_stateStyle = new StateStyle();
		private bool isSelecting = false;
		private Vector2 startSelectPosition;
		private Rect selectBox = new Rect();
		private GUIStyle selectBoxStyle = new GUIStyle("SelectionRect");
		private float runStateProcess;
		private Rect runStateProcessRect;
		private GUIStyle runStateProcessBkStyle = new GUIStyle("MeLivePlayBackground");
		private GUIStyle runStateProcessStyle = new GUIStyle("MeLivePlayBar");
		private float clickedTime = -1f;

		public GraphStateLayer(EditorWindow hFSMEditorWindow) : base(hFSMEditorWindow)
		{
		}

		public override void OnGUI(Rect rect)
		{
			base.OnGUI(rect);
			List<StateBaseData> states = context.currentChildStatesData;
			if (Event.current.type == EventType.Repaint)
			{
				if (context.HFSMController == null)
					return;
				m_stateStyle.ApplyZoomFactory(this.context.zoomFactor);

				for (int i = 0; i < states.Count; i++)
				{
					DrawState(states[i]);
				}
			}
		}

		public override void ProcessEvent()
		{
			base.ProcessEvent();

			if (this.context.HFSMController == null)
				return;

			#region 选中

			List<StateBaseData> states = context.currentChildStatesData;
			foreach (StateBaseData item in states)
			{
				StateOnMouseClick(item);
			}
			if (this.context.nextStateMachine != null)
			{
				this.context.selectedStates.Clear();
				this.context.currentStateMachine = this.context.nextStateMachine;
				this.context.stateMachinePath.Add(this.context.nextStateMachine);
				this.context.nextStateMachine = null;
				this.context.zoomFactor = 0.3f;
				this.context.dragOffset = Vector2.zero;
				editorWindow.Repaint();
				return;
			}

			#endregion 选中

			#region 框选

			if (EventExtension.IsMouseDown(0) && !IsMouseOverAnyState(states))
			{
				isSelecting = true;
				startSelectPosition = Event.current.mousePosition;
			}

			if (EventExtension.IsMouseUp(0))
			{
				if (isSelecting)
				{
					isSelecting = false;
					DrawSelectBox();
					this.editorWindow.Repaint();
				}
			}

			//移除窗口
			if (Event.current.type == EventType.MouseLeaveWindow) { isSelecting = false; }

			DrawSelectBox();

			#endregion 框选

			#region 拖拽Node

			if (Event.current.type == EventType.MouseDrag && Event.current.button == 0)
			{
				if (!isSelecting)
				{
					foreach (StateBaseData item in this.context.selectedStates)
					{
						item.position.position += Event.current.delta / this.context.zoomFactor;
						EditorUtility.SetDirty(this.context.HFSMController);
					}
				}
				Event.current.Use();
			}

			#endregion 拖拽Node

			#region 右键菜单

			if (EventExtension.IsMouseUp(1))
			{
				foreach (var item in states)
				{
					if (GetTransfromRect(item.position).MouseOn())
					{
						CreateMenu(item);
						Event.current.Use();
					}
				}
			}

			#endregion 右键菜单

			#region 删除

			if (Event.current.keyCode == KeyCode.Delete && this.context.selectedStates != null && this.context.selectedStates.Count > 0)
			{
				DeleteNode();
				this.editorWindow.Repaint();
			}

			#endregion 删除
		}

		public override void Update()
		{
			if (Application.isPlaying && this.context.executor != null && this.context.executor.currentExecuteState != null)
			{
				runStateProcess += Time.deltaTime;
				runStateProcess %= 1;
			}
		}

		private void CreateMenu(StateBaseData item)
		{
			bool is_any = item.id == StateMachine.anyState;
			bool is_entry = item.id == StateMachine.entryState;
			var genericMenu = new GenericMenu();

			if (is_entry)
			{
				genericMenu.AddItem(new GUIContent("Make Transition"), false, null);
			}
			else if (is_any)
			{
				genericMenu.AddItem(new GUIContent("Make Transition"), false, () =>
				{
					//TOOD:
					this.context.StartPriviewTransition(item);
				});
			}
			else
			{
				genericMenu.AddItem(new GUIContent("Make Transition"), false, () =>
				{
					//TOOD:
					this.context.StartPriviewTransition(item);
				});
				genericMenu.AddItem(new GUIContent("Delete"), false, () =>
				{
					//TOOD:删除状态
					DeleteNode();
				});
				genericMenu.AddItem(new GUIContent("Set DefaltState"), false, () =>
				{
					SetDefaultState(item);
				});
				genericMenu.AddSeparator(string.Empty);
				genericMenu.AddItem(new GUIContent("Edit Script"), false, () =>
				{
					//TODO:打开文件并跳转到对应行
					context.HFSMController.JumpToScript(item);
				});
			}
			genericMenu.ShowAsContext();
		}

		private void SetDefaultState(StateBaseData state)
		{
			if (state.isDefault == true) return;
			List<StateBaseData> states = context.currentChildStatesData;
			foreach (StateBaseData item in states)
			{
				item.isDefault = false;
			}
			state.isDefault = true;
			context.currentStateMachine.defaultState = state.id;
			this.context.HFSMController.Save();
		}

		private void DeleteNode()
		{
			foreach (StateBaseData item in this.context.selectedStates)
			{
				context.HFSMController.DeleteState(context.currentStateMachine, item);
				context.HFSMController.DeleteTransition(context.currentStateMachine, item);
			}
			context.selectedStates.Clear();
			context.UpdateCurrentChildStatesData();
			context.UpdateCurrentTransitionData();
		}

		private void StateOnMouseClick(StateBaseData data)
		{
			if (GetTransfromRect(data.position).MouseOn())
			{
				if (EventExtension.IsMouseDown(0) || EventExtension.IsMouseDown(1))
				{
					this.context.selectedTransition = null;
					if (this.context.selectedStates.Contains(data))
					{
						if (EventExtension.IsMouseDown(0) && data.stateType == StateType.StateMachine)
						{
							float clickedInterval = Time.time - clickedTime;
							if (clickedInterval <= 0.3f)
							{
								this.context.nextStateMachine = (data as StateMachineData);
								clickedTime = -1f;
							}
							else
								clickedTime = Time.time;
						}
						Event.current.Use();
						return;
					}
					this.context.selectedStates.Clear();
					this.context.selectedStates.Add(data);
					//FSMStateInspectorHelper.Instance.Inspector(this.context.RunTimeFSMContorller, data);
					if (data.stateType == StateType.State)
						StateInspectorHelper.instance.Inspector(context.HFSMController, data as StateData);
					else
						StateMachineInspectorHelper.instance.Inspector(context.HFSMController, data as StateMachineData);
					//是否在预览添加过渡
					if (this.context.isPreviewTransition)
					{
						//添加过渡
						this.context.HFSMController.CreateTransition(context.currentStateMachine, context.preFrom, data);
						this.context.StopPriviewTransition();
						this.context.UpdateCurrentTransitionData();
					}

					Event.current.Use();
				}
			}
		}

		private void DrawState(StateBaseData data)
		{
			if (data != null)
			{
				Rect rect = GetTransfromRect(data.position);
				if (!posotion.Overlaps(rect))
					return;
				GUI.Box(rect, data.id, GetStateStyle(data));
				//this.context.executor.currentExecuteState != null &&
				//this.context.executor.currentExecuteState.id == data.id
				bool isExecute = false;
				if (Application.isPlaying)
				{
					isExecute = context.executor.executeStateStack.Select(s => s.state.id).Contains(data.id);
				}
				if (Application.isPlaying && this.context.executor != null && isExecute)
				{
					runStateProcessRect.Set(rect.x, rect.y + rect.height * 3 / 4, rect.width, rect.height / 4);
					GUI.Box(runStateProcessRect, string.Empty, runStateProcessBkStyle);
					runStateProcessRect.Set(rect.x, rect.y + rect.height * 3 / 4, rect.width, rect.height / 4);
					runStateProcessRect.width *= runStateProcess;
					GUI.Box(runStateProcessRect, string.Empty, runStateProcessStyle);
					this.editorWindow.Repaint();
				}
			}
		}

		private GUIStyle GetStateStyle(StateBaseData data)
		{
			bool isSelect = context.selectedStates.Contains(data);
			bool isState = data.stateType == StateType.State;
			if (Application.isPlaying && this.context.executor != null && this.context.executor.currentExecuteState != null && this.context.executor.currentExecuteState.id == data.id)
			{
				return m_stateStyle.GetStyle(isSelect ? Styles.OrangeOn : Styles.Orange, isState);
			}
			else if (!Application.isPlaying && data.isDefault)
			{
				return m_stateStyle.GetStyle(isSelect ? Styles.OrangeOn : Styles.Orange, isState);
			}
			else if (data.id == "Entry")
			{
				return m_stateStyle.GetStyle(isSelect ? Styles.GreenOn : Styles.Green, isState);
			}
			else if (data.id == "Any")
			{
				return m_stateStyle.GetStyle(isSelect ? Styles.MiutOn : Styles.Miut, isState);
			}
			else if (isState && (data as StateData).isTemporary)
			{
				return m_stateStyle.GetStyle(isSelect ? Styles.YellowOn : Styles.Yellow, isState);
			}
			else
			{
				return m_stateStyle.GetStyle(isSelect ? Styles.NormalOn : Styles.Normal, isState);
			}
		}

		private void DrawSelectBox()
		{
			if (!isSelecting) { selectBox = Rect.zero; return; }

			Vector2 detal = Event.current.mousePosition - startSelectPosition;
			selectBox.center = startSelectPosition + (detal / 2);
			selectBox.width = Mathf.Abs(detal.x);
			selectBox.height = Mathf.Abs(detal.y);

			GUI.Button(selectBox, "", selectBoxStyle);

			this.context.ClearAllSelectNode();

			foreach (StateBaseData item in context.currentChildStatesData)
			{
				CheckSelectBoxSelectNode(item);
			}
		}

		private void CheckSelectBoxSelectNode(StateBaseData data)
		{
			if (GetTransfromRect(data.position).Overlaps(selectBox, true))
			{
				this.context.selectedStates.Add(data);
			}
		}
	}
}