using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DogFramework.EditorExtension
{
	/// <summary>
	/// 对Rect进行功能扩展
	/// </summary>
	public static class RectExtension
	{
		#region 调整Rect大小

		public static Rect Resize(this Rect rect, float left, float top, float right, float bottom)
		{
			rect.x -= left;
			rect.y -= top;
			rect.width += right + left;
			rect.height += bottom + top;
			return rect;
		}

		public static Rect Resize(this Rect rect, float leftRight, float topBottom)
		{
			return rect.Resize(leftRight, topBottom, leftRight, topBottom);
		}

		public static Rect Resize(this Rect rect, float value)
		{
			return rect.Resize(value, value, value, value);
		}

		#endregion 调整Rect大小

		#region 垂直或水平分割Rect

		public static Rect[] SplitHorizontal(this Rect rect, float topRatio, float bottomRatio)
		{
			Rect topRect = new Rect();
			Rect bottomRect = new Rect();
			topRect.x = rect.x;
			topRect.y = rect.y;
			topRect.width = rect.width;
			topRect.height = rect.height * (topRatio / (topRatio + bottomRatio));
			bottomRect.x = rect.x;
			bottomRect.y = rect.y + topRect.height;
			bottomRect.width = rect.width;
			bottomRect.height = rect.height - topRect.height;
			Rect[] result = { topRect, bottomRect };
			return result;
		}

		public static Rect[] SplitVertical(this Rect rect, float leftRatio, float rightRatio)
		{
			Rect leftRect = new Rect();
			Rect rightRect = new Rect();
			leftRect.x = rect.x;
			leftRect.y = rect.y;
			leftRect.width = rect.width * (leftRatio / (leftRatio + rightRatio));
			leftRect.height = rect.height;
			rightRect.x = rect.x + leftRect.width;
			rightRect.y = rect.y;
			rightRect.width = rect.width - leftRect.width;
			rightRect.height = rect.height;
			Rect[] result = { leftRect, rightRect };
			return result;
		}

		#endregion 垂直或水平分割Rect

		#region 移动Rect

		public static Rect Move(this Rect rect, float mx, float my)
		{
			//Matrix4x4 transMatrix = Matrix4x4.Translate(new Vector3(mx, my));
			//rect.position = transMatrix.MultiplyPoint(rect.position);
			return rect.Move(new Vector2(mx, my));
			//return rect;
		}

		public static Rect Move(this Rect rect, Vector2 mv)
		{
			rect.position += mv;
			return rect;
		}

		public static Rect MoveTo(this Rect rect, float tx, float ty)
		{
			rect.x = tx;
			rect.y = ty;
			return rect;
		}

		public static Rect MoveTo(this Rect rect, Vector2 mv)
		{
			return rect.MoveTo(mv.x, mv.y);
		}

		#endregion 移动Rect

		#region 获取周围的相邻的Rect

		public static Rect BelowBlock(this Rect rect, float height)
		{
			rect.y = rect.yMax;
			rect.height = height;
			return rect;
		}

		public static Rect AboveBlock(this Rect rect, float height)
		{
			rect.y -= height;
			rect.height = height;
			return rect;
		}

		public static Rect RightBlock(this Rect rect, float width)
		{
			rect.x = rect.xMax;
			rect.width = width;
			return rect;
		}

		public static Rect LeftBlock(this Rect rect, float width)
		{
			rect.x -= width;
			rect.width = width;
			return rect;
		}

		#endregion 获取周围的相邻的Rect

		#region 鼠标是否在Rect内

		public static bool MouseOn(this Rect rect)
		{
			return rect.Contains(Event.current.mousePosition);
		}

		#endregion 鼠标是否在Rect内

		#region 获取一组Rect的包围盒

		public static Rect GetBoundingBox(this Rect[] rects)
		{
			Rect result = new Rect(10000, 10000, 0, 0);
			for (int i = 0; i < rects.Length; i++)
			{
				result.x = result.x > rects[i].x ? rects[i].x : result.x;
				result.y = result.y > rects[i].y ? rects[i].y : result.y;
				result.width = result.width > rects[i].xMax ? result.width : rects[i].xMax;
				result.height = result.height > rects[i].yMax ? result.height : rects[i].yMax;
			}
			result.width -= result.x;
			result.height -= result.y;
			return result;
		}

		#endregion 获取一组Rect的包围盒

		#region 将一组Rect以指定的Rect进行归一化

		public static Rect[] NormalizationWith(this Rect[] rects, Rect rangeRect, bool useScale = true)
		{
			Rect bbox = rects.GetBoundingBox();
			float wScaleRatio = rangeRect.width / bbox.width;
			float hScaleRatio = rangeRect.height / bbox.height;
			Vector2 positionOffset = rangeRect.position - bbox.position;
			Rect[] result = new Rect[rects.Length];
			rects.CopyTo(result, 0);
			for (int i = 0; i < result.Length; i++)
			{
				result[i].position += positionOffset;
				Vector2 rPos = result[i].position - rangeRect.position;
				result[i].x = rangeRect.position.x + rPos.x * wScaleRatio;
				result[i].y = rangeRect.position.y + rPos.y * hScaleRatio;
				if (useScale)
				{
					result[i].width *= wScaleRatio;
					result[i].height *= hScaleRatio;
				}
			}
			return result;
		}

		#endregion 将一组Rect以指定的Rect进行归一化

		#region 选择一组Rect的一部分

		public static Rect[] SelectPart(this Rect[] rects, Rect rangeRect, bool cullInvisible = false)
		{
			Rect bbox = rects.GetBoundingBox();
			rangeRect.position += bbox.position;
			EditorGUI.DrawRect(rangeRect, new Color(0.2f, 0.2f, 0.6f, 0.3f));
			List<Rect> result = new List<Rect>();
			for (int i = 0; i < rects.Length; i++)
			{
				if (rects[i].Overlaps(rangeRect))
				{
					Rect rect = rects[i];
					if (cullInvisible)
					{
						float x = rects[i].x < rangeRect.x ? rangeRect.x : rects[i].x;
						float y = rects[i].y < rangeRect.y ? rangeRect.y : rects[i].y;
						rect.width = ((rects[i].xMax < rangeRect.xMax) && (rects[i].x > rangeRect.x))
							? rects[i].width
							: (rects[i].x > rangeRect.x) ? (rangeRect.xMax - x) : (rects[i].xMax - x);
						rect.height = ((rects[i].yMax < rangeRect.yMax) && (rects[i].y > rangeRect.y))
							? rects[i].height
							: (rects[i].y > rangeRect.y) ? (rangeRect.yMax - y) : (rects[i].yMax - y);
						rect.x = x;
						rect.y = y;
					}
					result.Add(rect);
				}
			}
			return result.ToArray();
		}

		#endregion 选择一组Rect的一部分

		#region 移动一组Rect

		public static Rect[] Move(this Rect[] rects, float mx, float my)
		{
			for (int i = 0; i < rects.Length; i++)
			{
				rects[i].Move(mx, my);
			}
			return rects;
		}

		public static Rect[] Move(this Rect[] rects, Vector2 mv)
		{
			return rects.Move(mv.x, mv.y);
		}

		public static Rect[] MoveTo(this Rect[] rects, float tx, float ty)
		{
			return rects.MoveTo(new Vector2(tx, ty));
		}

		public static Rect[] MoveTo(this Rect[] rects, Vector2 mv)
		{
			Rect bbox = rects.GetBoundingBox();
			Vector2 posOffset = mv - bbox.position;
			for (int i = 0; i < rects.Length; i++)
			{
				rects[i] = rects[i].Move(posOffset);
			}
			return rects;
		}

		#endregion 移动一组Rect
	}
}