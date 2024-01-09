using System;
using System.Collections.Generic;

namespace RPGCore.AI.HFSM
{
	public enum TransitionType
	{
		Normal,
		Global
	}

	public class Transition
	{
		public TransitionType transitionType => m_transitionType;
		private TransitionType m_transitionType = TransitionType.Normal;
		public StateBase from => m_from;
		private StateBase m_from;
		public StateBase to => m_to;
		private StateBase m_to;
		public StateMachine parentStateMachine => m_parentStateMachine;
		private StateMachine m_parentStateMachine;
		public Func<Transition, bool> baseCondition => m_baseConditions;
		private Func<Transition, bool> m_baseConditions;

		public List<ParameterCondition> parameterConditions => m_parameterConditions;
		private List<ParameterCondition> m_parameterConditions = null;

		private bool m_addedConditionIsBase = false;

		public Transition(StateMachine parent, StateBase from, StateBase to, Func<Transition, bool> condition = null, TransitionType type = TransitionType.Normal)
		{
			m_parentStateMachine = parent;
			m_from = from;
			m_to = to;
			m_baseConditions = condition;
			m_transitionType = type;
		}

		public Transition AddBaseCondition(Func<Transition, bool> condition)
		{
			m_baseConditions += condition;
			m_addedConditionIsBase = true;
			return this;
		}

		public Transition AddParamterCondition(Parameter parameter, CompareType compareType, object value)
		{
			if (m_parameterConditions == null)
			{
				m_parameterConditions = new List<ParameterCondition>();
			}
			ParameterCondition parameterCondition = new ParameterCondition(parameter, compareType, value);
			m_parameterConditions.Add(parameterCondition);
			m_addedConditionIsBase = false;
			return this;
		}

		public virtual void BeginTransition()
		{
		}

		public bool ShouldTransition()
		{
			bool baseConditions = true;
			bool parameterConditions = true;
			bool fromCanExit = true;
			if (m_baseConditions != null)
			{
				baseConditions = baseCondition(this);
			}
			if (m_parameterConditions != null)
			{
				foreach (var pCondition in m_parameterConditions)
				{
					if (!pCondition.ExecuteCompare())
					{
						parameterConditions = false;
						break;
					}
				}
			}
			if (from != null && from.stateType == StateType.State)
			{
				fromCanExit = (from as State).OnExitRequset();
			}
			return baseConditions && parameterConditions && fromCanExit;
		}

		public Transition Reverse(bool reverseCondition = false)
		{
			if (from.id.Equals(parentStateMachine.Any.id))
			{
				throw new Exception("can not add a Transition to Any");
			}
			if (reverseCondition)
			{
				if (m_addedConditionIsBase)
				{
					Func<Transition, bool> rCondition = t => !(baseCondition.Invoke(this));
					return parentStateMachine.AddTransition(to, from, reverseCondition ? rCondition : null);
				}
			}
			return parentStateMachine.AddTransition(to, from);
		}

		/// <summary>
		/// 重置所有的Trigger；此方法应在状态机在一帧中全部执行完之后执行
		/// </summary>
		public void ResetTriggers()
		{
			if (m_parameterConditions != null)
			{
				foreach (var pCondition in m_parameterConditions)
				{
					if (pCondition.parameter.type == ParameterType.Trigger)
					{
						pCondition.parameter.baseValue = 0.0f;
					}
				}
			}
		}
	}

	[Serializable]
	public class TransitionData
	{
		public string id;
		public string from;
		public string to;
		public List<string> baseConditionsName = new List<string>();
		public List<ParameterConditionData> parameterConditionDatas = new List<ParameterConditionData>();
	}
}