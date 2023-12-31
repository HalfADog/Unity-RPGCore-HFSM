using System;
using System.Collections.Generic;

namespace RPGCore.AI.HFSM
{
	public enum CompareType
	{
		Greater,
		Less,
		Equal,
		GreaterEqual,
		LessEqual,
		NotEqual
	}

	public class ParameterCondition
	{
		public Parameter parameter;
		public CompareType compareType;
		public float compareValue;

		private static Dictionary<CompareType, Func<float, float, bool>> m_compareExecutor
			= new Dictionary<CompareType, Func<float, float, bool>>();

		static ParameterCondition()
		{
			m_compareExecutor.Clear();
			m_compareExecutor[CompareType.Greater] = (s, t) => s > t;
			m_compareExecutor[CompareType.Less] = (s, t) => s < t;
			m_compareExecutor[CompareType.Equal] = (s, t) => s == t;
			m_compareExecutor[CompareType.GreaterEqual] = (s, t) => s >= t;
			m_compareExecutor[CompareType.LessEqual] = (s, t) => s <= t;
			m_compareExecutor[CompareType.NotEqual] = (s, t) => s != t;
		}

		public ParameterCondition(Parameter parameter, CompareType compareType, object value)
		{
			this.parameter = parameter;
			this.compareType = compareType;
			if (parameter.type == ParameterType.Trigger)
			{
				value = 1.0f;
				this.compareType = CompareType.Equal;
			}
			SetCompareValue(value);
		}

		public void SetCompareValue(object value)
		{
			if (parameter.type == ParameterType.Float)
			{
				compareValue = (float)value;
			}
			else if (parameter.type == ParameterType.Int)
			{
				int v = (int)value;
				compareValue = (float)v;
			}
			else if (parameter.type == ParameterType.Bool)
			{
				bool v = (bool)value;
				compareValue = v ? 1.0f : 0.0f;
			}
			else
			{
				compareValue = 1.0f;
			}
		}

		public bool ExecuteCompare()
		{
			return m_compareExecutor[compareType](parameter.baseValue, compareValue);
		}
	}

	[Serializable]
	public class ParameterConditionData
	{
		public string parameterName;
		public CompareType compareType;
		public float compareValue;
	}
}