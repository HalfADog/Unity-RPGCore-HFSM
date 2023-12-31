using UnityEngine;

namespace RPGCore.AI.HFSM
{
	public enum ParameterType
	{
		Int,
		Float,
		Bool,
		Trigger
	}

	[System.Serializable]
	public class Parameter
	{
		[SerializeField]
		protected string m_name;

		public string name
		{ get { return m_name; } set { m_name = value; } }

		[SerializeField]
		protected ParameterType m_type;

		public ParameterType type => m_type;

		public float baseValue;

		public Parameter(string name, ParameterType type, float value)
		{
			m_name = name;
			m_type = type;
			baseValue = value;
		}
	}
}