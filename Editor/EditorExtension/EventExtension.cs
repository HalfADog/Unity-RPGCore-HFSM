using UnityEngine;

namespace DogFramework.EditorExtension
{
	public static class EventExtension
	{
		/// <summary>
		/// 鼠标按下
		/// </summary>
		public static bool IsMouseDown(int button = -1)
		{
			if (button == -1)
			{
				return Event.current.type == EventType.MouseDown;
			}
			else if (button == 0 || button == 1 || button == 2)
			{
				return Event.current.button == button && Event.current.type == EventType.MouseDown;
			}
			return false;
		}

		/// <summary>
		/// 鼠标抬起
		/// </summary>
		public static bool IsMouseUp(int button = -1)
		{
			if (button == -1)
			{
				return Event.current.type == EventType.MouseUp;
			}
			else if (button == 0 || button == 1 || button == 2)
			{
				return Event.current.button == button && Event.current.type == EventType.MouseUp;
			}
			return false;
		}
	}
}