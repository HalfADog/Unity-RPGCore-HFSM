using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DogFramework.EditorExtension
{
	public static class GUIExtension
	{
		public enum ArrowLocation
		{
			Tail = 0,
			Head = 1,
			Center = 2
		}

		public enum EdgeOrientation
		{
			Horizontal,
			Vertical
		}

		public static void Begin()
		{
			Handles.BeginGUI();
		}

		public static void End()
		{
			Handles.EndGUI();
		}

		public static void DrawLine(Vector2 point1, Vector2 point2, float width, Color color)
		{
			Handles.color = color;
			Handles.DrawAAPolyLine(width, point1, point2);
		}

		public static void DrawLine(float width, Color color, params Vector2[] points)
		{
			Handles.color = color;
			Handles.DrawAAPolyLine(width, points.Select(point => new Vector3(point.x, point.y)).ToArray());
		}

		public static void DrawArrowLine(Vector2 from, Vector2 to, float width, Color color,
			ArrowLocation arrowLocation = ArrowLocation.Center, float arrowSize = 5, int arrowCount = 1)
		{
			DrawLine(from, to, width, color);
			Vector2 direction = to - from;
			for (int i = 0; i < arrowCount; i++)
			{
				float offset = i * arrowSize * 2;
				if (arrowLocation == ArrowLocation.Center)
				{
					offset -= arrowCount * arrowSize;
				}
				else if (arrowLocation == ArrowLocation.Head)
				{
					offset -= arrowCount * arrowSize * 2;
				}
				Vector2 arrowCenter = from + (direction / (float)arrowLocation) + direction.normalized * offset;
				DrawArrow(arrowCenter, direction, arrowSize, color);
			}
		}

		public static void DrawArrow(Vector2 position, Vector2 direction, float arrowSize, Color color)
		{
			Handles.color = color;
			Vector2 cross = Vector3.Cross(direction, Vector3.forward);
			Vector3[] triangles = new Vector3[]
			{
				position + cross.normalized * arrowSize,
				position - cross.normalized * arrowSize,
				position + (direction).normalized * arrowSize * 2
			};
			Handles.DrawAAConvexPolygon(triangles);
		}

		public static void DrawBezierEdge(Rect output, Rect input, float width, Color color, EdgeOrientation orientation = EdgeOrientation.Vertical)
		{
			Handles.color = color;
			Vector3 o = new Vector3(output.center.x, output.center.y);
			Vector3 i = new Vector3(input.center.x, input.center.y);
			Vector3 tangent;
			if (orientation == EdgeOrientation.Vertical)
			{
				//float yd = o.y > i.y ? 1 : -1;
				float yd = -1;
				tangent = new Vector3(0, yd * Mathf.Lerp(Mathf.Lerp(5, 50,
					Mathf.Pow(Mathf.Clamp01(Mathf.Abs(o.x - i.x) / 300), 0.35f)), 200,
					Mathf.Pow(Mathf.Clamp01(Mathf.Abs(o.y - i.y) / 300), 2.0f)));
				//float outy = o.y > i.y ? output.y : output.yMax;
				//float iny = o.y > i.y ? input.yMax : input.y;
				o.y = output.yMax;
				i.y = input.y;
			}
			else
			{
				//float xd = o.x > i.x ? 1 : -1;
				float xd = -1;
				tangent = new Vector3(xd * Mathf.Lerp(Mathf.Lerp(5, 50,
					Mathf.Pow(Mathf.Clamp01(Mathf.Abs(o.x - i.x) / 300), 0.35f)), 200,
					Mathf.Pow(Mathf.Clamp01(Mathf.Abs(o.x - i.x) / 300), 2.0f)), 0);
				//float outx = o.x > i.x ? output.x : output.xMax;
				//float inx = o.x > i.x ? input.xMax : input.x;
				o.x = output.xMax;
				i.x = input.x;
			}
			Handles.DrawBezier(o, i, o - tangent, i + tangent, color, null, width);
			//DrawBezier3(o, i, o - tangent, i + tangent, width, color, 50);
		}

		public static void DrawBezier2(Vector2 start, Vector2 end, Vector2 handle, float width, Color color, int sample = 50)
		{
			Vector3[] points = new Vector3[sample];
			Vector2 q1, q2, target;
			for (int i = 0; i < sample; i++)
			{
				float t = (float)i / (float)(sample - 1);
				q1 = Vector2.Lerp(start, handle, t);
				q2 = Vector2.Lerp(handle, end, t);
				target = Vector2.Lerp(q1, q2, t);
				points[i] = target;
			}
			Handles.color = color;
			Handles.DrawAAPolyLine(width, points);
		}

		public static void DrawBezier3(Vector2 start, Vector2 end, Vector2 handle1, Vector2 handle2, float width, Color color, int sample = 50)
		{
			Vector3[] points = new Vector3[sample];
			Vector2 q1, q2, q3;
			Vector2 p1, p2;
			Vector2 target;
			for (int i = 0; i < sample; i++)
			{
				float t = (float)i / (float)(sample - 1);
				q1 = Vector2.Lerp(start, handle1, t);
				q2 = Vector2.Lerp(handle1, handle2, t);
				q3 = Vector2.Lerp(handle2, end, t);
				p1 = Vector2.Lerp(q1, q2, t);
				p2 = Vector2.Lerp(q2, q3, t);
				target = Vector2.Lerp(p1, p2, t);
				points[i] = target;
			}
			Handles.color = color;
			Handles.DrawAAPolyLine(width, points);
		}

		public static void DrawCircle(Vector2 center, float r, Color color, bool solid = true)
		{
			Handles.color = color;
			if (solid) Handles.DrawSolidArc(center, Vector3.forward, Vector3.left, 360, r);
			else Handles.DrawWireArc(center, Vector3.forward, Vector3.left, 360, r);
		}

		public static void DrawPoint(Vector2 position, Color color)
		{
			Handles.color = color;
			Handles.DrawSolidArc(position, Vector3.forward, Vector3.left, 360, 2.5f);
		}
	}
}