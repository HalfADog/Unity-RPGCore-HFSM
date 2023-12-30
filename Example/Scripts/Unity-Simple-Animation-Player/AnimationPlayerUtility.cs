using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// 环形数组栈
/// </summary>
/// <typeparam name="TValue"></typeparam>
public class RingStack<TValue> : RingArray<TValue>
{
	//栈顶位置
	private int topIndex;

	public RingStack(int stackLength) : base(stackLength)
	{
		topIndex = 0;
	}

	/// <summary>
	/// 返回栈顶项
	/// </summary>
	/// <returns></returns>
	public TValue Peek()
	{
		if (isEmpty(topIndex)) return default(TValue);
		return datas[topIndex];
	}

	/// <summary>
	/// 弹出栈顶项
	/// </summary>
	/// <returns></returns>
	public TValue Pop()
	{
		if (isEmpty(topIndex)) return default(TValue);
		TValue temp = datas[topIndex];
		MarkEmpty(topIndex, true);
		topIndex -= 1;
		if (topIndex < 0)
		{
			topIndex = length - 1;
		}
		return temp;
	}

	/// <summary>
	/// 在栈顶压入一项
	/// </summary>
	/// <param name="value"></param>
	public void Push(TValue value)
	{
		topIndex += 1;
		if (topIndex == length)
		{
			topIndex = 0;
		}
		datas[topIndex] = value;
		MarkEmpty(topIndex, false);
	}

	public override bool isEmpty()
	{
		return isEmpty(topIndex);
	}
}

/// <summary>
/// 环形数组队列
/// </summary>
/// <typeparam name="TValue"></typeparam>
public class RingQueue<TValue> : RingArray<TValue>
{
	//队头位置
	protected int headIndex = 0;

	//队尾位置
	protected int tailIndex = 0;

	public RingQueue(int length) : base(length)
	{
		headIndex = length - 1;
		tailIndex = length - 1;
	}

	/// <summary>
	/// 返回队头元素
	/// </summary>
	/// <returns></returns>
	public TValue Peek()
	{
		if (isEmpty(headIndex)) return default(TValue);
		return datas[headIndex];
	}

	/// <summary>
	/// 出队 返回队头并删除
	/// </summary>
	/// <returns></returns>
	public TValue Dequeue()
	{
		if (isEmpty(headIndex)) return default(TValue);
		TValue temp = datas[headIndex];
		MarkEmpty(headIndex, true);
		headIndex -= 1;
		if (headIndex < 0)
		{
			headIndex = length - 1;
		}
		if (headIndex < tailIndex)
		{
			headIndex = tailIndex;
		}
		return temp;
	}

	/// <summary>
	/// 入队 添加到队尾
	/// </summary>
	/// <param name="value"></param>
	/// <returns></returns>
	public void Enqueue(TValue value)
	{
		datas[tailIndex] = value;
		MarkEmpty(tailIndex, false);
		int pretail = tailIndex;
		tailIndex -= 1;
		if (tailIndex < 0)
		{
			tailIndex = length - 1;
		}
		if (tailIndex == headIndex)
		{
			tailIndex = pretail;
		}
	}

	public override bool isEmpty()
	{
		return headIndex == tailIndex;
	}
}

/// <summary>
/// 环形队列增强版 可以对队尾元素进行操作
/// </summary>
/// <typeparam name="TValue"></typeparam>
public class RingQueueEx<TValue> : RingQueue<TValue>
{
	public RingQueueEx(int length) : base(length)
	{
	}

	/// <summary>
	/// 返回队尾元素但不删除
	/// </summary>
	/// <returns></returns>
	public TValue PeekTail()
	{
		if (headIndex == tailIndex) return default(TValue);
		var newtail = tailIndex + 1;
		if (newtail == length)
		{
			newtail = 0;
		}
		return datas[newtail];
	}

	/// <summary>
	/// 返回队尾元素并删除
	/// </summary>
	/// <returns></returns>
	public TValue PopTail()
	{
		if (headIndex == tailIndex) return default(TValue);
		var newtail = tailIndex + 1;
		if (newtail == length)
		{
			newtail = 0;
		}
		tailIndex = newtail;
		MarkEmpty(newtail, true);
		return datas[newtail];
	}
}

/// <summary>
/// 环形数组
/// </summary>
/// <typeparam name="TValue"></typeparam>
public abstract class RingArray<TValue>
{
	//数据
	protected TValue[] datas;

	//数组长度
	protected int length;

	public int Size => length;

	public int Length
	{
		get
		{
			int count = 0;
			for (int i = 0; i < length; i++)
			{
				if (!isEmpty(i)) count++;
			}
			return count;
		}
	}

	protected bool[] isEmptyMark;

	public RingArray(int length)
	{
		this.length = length;
		this.datas = new TValue[length];
		this.isEmptyMark = new bool[length];
		for (int i = 0; i < length; i++)
		{
			isEmptyMark[i] = true;
		}
	}

	protected void MarkEmpty(int position, bool mark)
	{
		if (position < 0 && position >= length) return;
		isEmptyMark[position] = mark;
	}

	protected bool isEmpty(int position)
	{
		if (position < 0 && position >= length) return false;
		return isEmptyMark[position];
	}

	public virtual bool isEmpty()
	{
		for (int i = 0; i < length; i++)
		{
			if (isEmptyMark[i]) return true;
		}
		return false;
	}

	public void Clear()
	{
		for (int i = 0; i < length; i++)
		{
			datas[i] = default(TValue);
			isEmptyMark[i] = true;
		}
	}
}