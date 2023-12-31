using System.Collections.Generic;
using UnityEngine;

namespace RPGCore.AI.HFSM
{
	public enum Styles
	{
		Normal = 0,
		Bule,
		Miut,
		Green,
		Yellow,
		Orange,
		Red,
		NormalOn,
		BuleOn,
		MiutOn,
		GreenOn,
		YellowOn,
		OrangeOn,
		RedOn,
		SM_Normal,
		SM_Bule,
		SM_Miut,
		SM_Green,
		SM_Yellow,
		SM_Orange,
		SM_Red,
		SM_NormalOn,
		SM_BuleOn,
		SM_MiutOn,
		SM_GreenOn,
		SM_YellowOn,
		SM_OrangeOn,
		SM_RedOn,
	}

	public class StateStyle
	{
		private Dictionary<Styles, GUIStyle> styles;

		public StateStyle()
		{
			styles = new Dictionary<Styles, GUIStyle>();
			for (int i = 0; i <= 6; i++)
			{
				styles.Add((Styles)i, new GUIStyle($"flow node {i}"));
				styles.Add((Styles)(i + 7), new GUIStyle($"flow node {i} on"));
				styles.Add((Styles)(i + 14), new GUIStyle($"flow node hex {i}"));
				styles.Add((Styles)(i + 21), new GUIStyle($"flow node hex {i} on"));
			}
		}

		public GUIStyle GetStyle(Styles style, bool isState = true)
		{
			return styles[style + (isState ? 0 : 14)];
		}

		public void ApplyZoomFactory(float zoomFactory)
		{
			for (int i = 0; i < styles.Count; i++)
			{
				styles[(Styles)i].fontSize = (int)Mathf.Lerp(5, 30, zoomFactory);
				if (i < 14)
				{
					styles[(Styles)i].contentOffset = new Vector2(0, Mathf.Lerp(-30, -20, zoomFactory));
				}
			}
		}
	}
}