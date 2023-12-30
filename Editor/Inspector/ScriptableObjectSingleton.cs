using UnityEngine;

namespace DogFramework
{
	public class ScriptableObjectSingleton<T> : ScriptableObject where T : ScriptableObjectSingleton<T>
	{
		private static T m_instance;

		public static T instance
		{
			get
			{
				if (m_instance == null)
				{
					m_instance = ScriptableObject.CreateInstance<T>();
				}
				return m_instance;
			}
		}

		private void OnDisable()
		{
			m_instance = null;
		}
	}
}