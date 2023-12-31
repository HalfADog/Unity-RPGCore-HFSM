using UnityEngine;

namespace RPGCore.AI.HFSM
{
	public class GraphConst
	{
		public static Color Grid_Color { get; } = new Color(0, 0, 0, 0.2f);
		public static Color Backgraound_Color { get; } = new Color(0, 0, 0, 0.25f);
		public static Color Select_Color { get; } = new Color(100, 200, 255, 255) / 255;
		public static int stateWidth => 480;
		public static int stateHeight => 90;
	}
}