using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace RPGCore.Animation
{
	public class AnimationPlayerManager : MonoBehaviour
	{
		//构建图所用部分

		private PlayableGraph graph;
		private AnimationPlayableOutput output;
		private AnimationLayerMixerPlayable layerMixerPlayable;
		private AnimationMixerPlayable mainMixerPlayable;
		private AnimationMixerPlayable subMixerPlayable;
		private AnimationClipPlayable currentPlayingPlayable;
		private AnimationClipPlayable transitionTargetPlayable;
		private AnimationClipPlayable layerCurrentPlayingPlayable;
		private AnimationClipPlayable layerTransitionTargetPlayable;

		//普通转换

		private bool hasTransition;
		private float transitionStartTime;

		//层转换

		private bool hasLayerTransition;
		private float layerTransitionStartTime;
		private float layerExitTransitionStartTime;
		private bool hasLayerAnimation;
		private bool enterLayerAnimation;
		private bool exitLayerAnimation;
		private float layerAnimationWeight;

		//记录栈 记录播放过的动画信息

		private static readonly int runtimeStackLogMaxLength = 8;
		private RingStack<AnimationRuntimeInfo> animationRuntimeStackLog = new RingStack<AnimationRuntimeInfo>(runtimeStackLogMaxLength);
		private RingStack<AnimationRuntimeInfo> animationLayerRuntimeStackLog = new RingStack<AnimationRuntimeInfo>(runtimeStackLogMaxLength);

		//转换请求队列

		private static readonly int transitionQueueMaxLength = 8;
		private RingQueueEx<AnimationPlayerDataSO> animationTransitionQueue = new RingQueueEx<AnimationPlayerDataSO>(transitionQueueMaxLength);
		private RingQueueEx<AnimationPlayerDataSO> animationLayerTransitionQueue = new RingQueueEx<AnimationPlayerDataSO>(transitionQueueMaxLength);

		//当前正在播放的项

		private AnimationRuntimeInfo currentPlayingAnimation;
		private AnimationRuntimeInfo currentLayerPlayingAnimation;

		/// <summary>
		/// 返回当前正在播放的动画是否完成播放
		/// </summary>
		public bool CurrentFinishPlaying
		{
			get
			{
				if (hasTransition)
				{
					return currentTransitionToAnimation.CheckFinished();
				}
				return currentPlayingAnimation.finishPlaying;
			}
		}

		/// <summary>
		/// 返回当前正在播放的层级动画是否完成播放 0未完成 1完成 -1层级动画未开启
		/// </summary>
		public int CurrentLayerFinishPlaying
		{
			get
			{
				if (hasLayerAnimation)
				{
					if (hasLayerTransition)
					{
						return currentLayerTransitionToAnimation.CheckFinished() ? 1 : 0;
					}
					return currentLayerPlayingAnimation.finishPlaying ? 1 : 0;
				}
				return -1;
			}
		}

		//当前要转换到的项

		private AnimationRuntimeInfo currentTransitionToAnimation;
		private AnimationRuntimeInfo currentLayerTransitionToAnimation;

		//随机数 用来判断是否为同一时间的请求

		private int randomChecker;
		private int currentChecker = -1;
		private int currentLayerChecker = -1;

		//要用到的所有动画数据
		public AnimationPlayerDataContainerSO animationDataContainer;

		//转换时间
		[Range(0.1f, 2f)]
		public float transitionTime = 0.1f;

		public void Awake()
		{
			InitializePlayableGraph();
		}

		private void Update()
		{
			//更新随机检查器 用以判断同一时刻的转换请求
			randomChecker = Random.Range(1, 100000);
			//更新当前播放动画的信息
			currentPlayingAnimation.CheckFinished();
			//更新当前播放的层级动画的信息
			if (hasLayerAnimation && currentLayerPlayingAnimation != null)
			{
				currentLayerPlayingAnimation.CheckFinished();
			}
			//处理转换
			ProcessTransitionRequest();
			//处理层级转换
			if (hasLayerAnimation)
			{
				ProcessLayerTransitionRequest();
			}
			//Debug.Log(CurrentFinishPlaying);
		}

		/// <summary>
		/// 初始化
		/// </summary>
		private void InitializePlayableGraph()
		{
			//创建
			graph = PlayableGraph.Create(name + " Animation Player");
			output = AnimationPlayableOutput.Create(graph, "Output", GetComponent<Animator>());
			layerMixerPlayable = AnimationLayerMixerPlayable.Create(graph, 2);
			mainMixerPlayable = AnimationMixerPlayable.Create(graph, 2);
			subMixerPlayable = AnimationMixerPlayable.Create(graph, 2);
			currentPlayingPlayable = AnimationClipPlayable.Create(graph, animationDataContainer.defaultAnimation.animClip);
			transitionTargetPlayable = AnimationClipPlayable.Create(graph, null);
			layerCurrentPlayingPlayable = AnimationClipPlayable.Create(graph, null);
			layerTransitionTargetPlayable = AnimationClipPlayable.Create(graph, null);
			//链接
			graph.Connect(currentPlayingPlayable, 0, mainMixerPlayable, 0);
			graph.Connect(transitionTargetPlayable, 0, mainMixerPlayable, 1);
			graph.Connect(layerCurrentPlayingPlayable, 0, subMixerPlayable, 0);
			graph.Connect(layerTransitionTargetPlayable, 0, subMixerPlayable, 1);
			graph.Connect(mainMixerPlayable, 0, layerMixerPlayable, 0);
			graph.Connect(subMixerPlayable, 0, layerMixerPlayable, 1);
			//初始化
			mainMixerPlayable.SetInputWeight(0, 1);
			mainMixerPlayable.SetInputWeight(1, 0);
			subMixerPlayable.SetInputWeight(0, 0);
			subMixerPlayable.SetInputWeight(1, 0);
			layerMixerPlayable.SetLayerMaskFromAvatarMask(1, animationDataContainer.defaultMask);
			layerMixerPlayable.SetInputWeight(0, 1);
			layerMixerPlayable.SetInputWeight(1, 1);
			output.SetSourcePlayable(layerMixerPlayable);
			graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
			currentPlayingAnimation = new AnimationRuntimeInfo(animationDataContainer.defaultAnimation, 0);
			//运行
			graph.Play();
		}

		/// <summary>
		/// 提交一个动画转换请求 等待处理
		/// </summary>
		/// <param name="animationName"></param>
		public void RequestTransition(string animationName)
		{
			AnimationPlayerDataSO requestItem = GetAnimationData(animationName);
			if (requestItem == null)
			{
				Debug.LogWarning($"the \"{animationName}\" AnimationPlayerData is not exist!");
				return;
			}

			//如果是在同一时间请求 那么currentChecker应该与randomChecker相等
			//因为在同一时间内randomChecker不会改变
			bool isSameTime = (currentChecker == randomChecker);
			currentChecker = randomChecker;
			//判断此请求是否可以入转换队列
			//首先判断队列是否为空
			//如果为空则直接将请求入队
			if (animationTransitionQueue.isEmpty())
			{
				//如果当前有转换 那么判断的对象应该是当前转换项
				//如果与当前转换项相同那么这个请求就忽略 不用重复转换了
				if (hasTransition)
				{
					if (requestItem.animName != currentTransitionToAnimation.data.animName)
					{
						animationTransitionQueue.Enqueue(requestItem);
					}
				}
				//如果没有 判断的对象才是当前播放项
				else
				{
					if (requestItem.animName != currentPlayingAnimation.data.animName)
					{
						animationTransitionQueue.Enqueue(requestItem);
					}
				}
			}
			//如果队列不为空
			else
			{
				//查看队尾元素
				var qtail = animationTransitionQueue.PeekTail();
				//首先要确定是不是在同一时间请求的 如果是就直接都入队 不考虑优先级但要考虑重复问题
				if (isSameTime)
				{
					if (requestItem.animName != qtail.animName)
					{
						animationTransitionQueue.Enqueue(requestItem);
					}
				}
				//如果不是同一时间请求的就继续进行判断
				else
				{
					//与队尾请求比较
					//如果两个请求相等则忽略当前请求
					if (qtail.animName == requestItem.animName)
					{
						return;
					}
					//如果两个请求不相等且当前请求的优先级大于队尾请求的优先级
					//把队尾优先级小于或等于当前请求的项移除
					//直到队尾项的优先级大于当前请求或队列为空
					while (requestItem.animPriority >= qtail.animPriority)
					{
						//如果两个优先级相等
						if (requestItem.animPriority == qtail.animPriority && qtail.canAbort)
						{
							//且队尾项只能被更高优先级的请求打断则跳过
							if (qtail.abortType == AnimationAbortType.OnlyHigherPriority)
							{
								break;
							}
						}
						animationTransitionQueue.PopTail();
						if (animationTransitionQueue.isEmpty()) break;
						qtail = animationTransitionQueue.PeekTail();
					}
					//一切顺利 将当前请求入队
					animationTransitionQueue.Enqueue(requestItem);
				}
			}
		}

		/// <summary>
		/// 处理转换请求
		/// </summary>
		private void ProcessTransitionRequest()
		{
			//判断当前是否正在执行转换
			//如果当前没有执行转换
			if (!hasTransition)
			{
				//判断转换请求队列是否为空
				//如果不为空 说明当前有转换请求
				if (!animationTransitionQueue.isEmpty())
				{
					//判断当前播放项能否被打断执行转换
					//如果允许被打断或播放完成同样可以执行转换
					bool canAbort = false;
					if (currentPlayingAnimation.data.canAbort)
					{
						AnimationPlayerDataSO rq = animationTransitionQueue.Peek();
						switch (currentPlayingAnimation.data.abortType)
						{
							case AnimationAbortType.All:
								canAbort = true;
								break;

							case AnimationAbortType.OnlyHigherPriority:
								if (rq.animPriority > currentPlayingAnimation.data.animPriority)
								{
									canAbort = true;
								}
								break;

							case AnimationAbortType.DisallowLowerPriority:
								if (rq.animPriority >= currentPlayingAnimation.data.animPriority)
								{
									canAbort = true;
								}
								break;
						}
					}
					if (canAbort || currentPlayingAnimation.finishPlaying)
					{
						//在下一帧执行转换 并获取到当前转换项
						hasTransition = true;
						currentTransitionToAnimation = new AnimationRuntimeInfo(animationTransitionQueue.Dequeue(), Time.time);

						//将目标anim与端口1绑定 为开始从端口0到端口1转换做准备
						mainMixerPlayable.DisconnectInput(1);
						transitionTargetPlayable.Destroy();
						transitionTargetPlayable = AnimationClipPlayable.Create(graph, currentTransitionToAnimation.data.animClip);
						transitionTargetPlayable.SetTime(0);
						graph.Connect(transitionTargetPlayable, 0, mainMixerPlayable, 1);
						mainMixerPlayable.SetInputWeight(1, 0);
						transitionTargetPlayable.SetSpeed(currentTransitionToAnimation.data.multiplier);
						transitionTargetPlayable.Play();

						//Debug.Log($"{currentPlayingAnimation.data.animName} => {currentTransitionToAnimation.data.animName}");
					}
				}
			}
			//当前有执行的转换
			else
			{
				//转换刚开始时记录一下当前开始转换的时间
				if (transitionStartTime == 0)
				{
					transitionStartTime = Time.time;
				}
				float weight = Mathf.Clamp01((Time.time - transitionStartTime) / transitionTime);
				mainMixerPlayable.SetInputWeight(0, 1 - weight);
				mainMixerPlayable.SetInputWeight(1, weight);
				//转换完成
				if (weight == 1)
				{
					//交换端口0和端口1的anim
					mainMixerPlayable.DisconnectInput(0);
					mainMixerPlayable.DisconnectInput(1);
					AnimationClipPlayable temp = currentPlayingPlayable;
					currentPlayingPlayable = transitionTargetPlayable;
					transitionTargetPlayable = temp;
					graph.Connect(currentPlayingPlayable, 0, mainMixerPlayable, 0);
					graph.Connect(transitionTargetPlayable, 0, mainMixerPlayable, 1);
					mainMixerPlayable.SetInputWeight(0, 1);
					mainMixerPlayable.SetInputWeight(1, 0);
					transitionTargetPlayable.SetTime(0);
					transitionTargetPlayable.Pause();

					//转换完成后记录
					hasTransition = false;
					transitionStartTime = 0;
					//将当前转换项设置为当前播放项
					animationRuntimeStackLog.Push(currentPlayingAnimation);
					currentPlayingAnimation = currentTransitionToAnimation;
					currentTransitionToAnimation = null;
				}
			}
		}

		/// <summary>
		/// 提交一个分层动画转换请求 等待处理
		/// </summary>
		/// <param name="layerName">AvatarMask</param>
		public void RequestLayerTransition(string animationName, string layerName, float layerWeight = 1)
		{
			//在处理退出时如果有请求进来则优先处理请求
			if (exitLayerAnimation)
			{
				exitLayerAnimation = false;
				layerExitTransitionStartTime = 0;
				layerMixerPlayable.SetInputWeight(1, 1);
			}
			layerAnimationWeight = layerWeight;
			AnimationPlayerDataSO requestItem = GetAnimationData(animationName);
			AnimationLayerMaskData requestMask = GetAnimationLayerMaskData(layerName);

			if (requestItem == null)
			{
				Debug.LogWarning($"the \"{animationName}\" AnimationPlayerData is not exist!");
				return;
			}
			if (requestMask == null)
			{
				Debug.LogWarning($"the \"{layerName}\" AnimationLayerMaskData is not exist!");
				return;
			}

			//设定动画的层级和权重
			layerMixerPlayable.SetLayerMaskFromAvatarMask(1, requestMask.mask);
			SetCurrentLayerAnimationWeight(layerAnimationWeight);

			//如果是在同一时间请求 那么currentLayerChecker应该与randomChecker相等 因为在同一时间内randomChecker不会改变
			bool isSameTime = (currentLayerChecker == randomChecker);
			currentLayerChecker = randomChecker;

			//判断此请求是否可以入转换队列 首先判断队列是否为空 如果为空则直接将请求入队
			if (animationLayerTransitionQueue.isEmpty())
			{
				//首次请求 直接入队
				if (!hasLayerAnimation)
				{
					animationLayerTransitionQueue.Enqueue(requestItem);
					enterLayerAnimation = true;
					hasLayerAnimation = true;
				}
				else
				{
					//如果当前有转换 那么判断的对象应该是当前转换项 如果与当前转换项相同那么这个请求就忽略 不用重复转换了
					if (hasLayerTransition)
					{
						if (requestItem.animName != currentLayerTransitionToAnimation.data.animName)
						{
							animationLayerTransitionQueue.Enqueue(requestItem);
						}
					}
					//如果没有 判断的对象才是当前播放项
					else
					{
						if (currentLayerPlayingAnimation != null && requestItem.animName != currentLayerPlayingAnimation.data.animName)
						{
							animationLayerTransitionQueue.Enqueue(requestItem);
						}
					}
				}
			}
			//如果队列不为空
			else
			{
				//查看队尾元素
				var qtail = animationLayerTransitionQueue.PeekTail();
				//首先要确定是不是在同一时间请求的 如果是就直接都入队 不考虑优先级但要考虑重复问题
				if (isSameTime)
				{
					if (requestItem.animName != qtail.animName)
					{
						animationLayerTransitionQueue.Enqueue(requestItem);
					}
				}
				//如果不是同一时间请求的就继续进行判断
				else
				{
					//与队尾请求比较 如果两个请求相等则忽略当前请求
					if (qtail.animName == requestItem.animName)
					{
						return;
					}
					//如果两个请求不相等且当前请求的优先级大于队尾请求的优先级 把队尾优先级小于或等于当前请求的项移除 直到队尾项的优先级大于当前请求或队列为空
					while (requestItem.animPriority >= qtail.animPriority)
					{
						animationLayerTransitionQueue.PopTail();
						if (animationLayerTransitionQueue.isEmpty()) break;
						qtail = animationLayerTransitionQueue.PeekTail();
					}
					//一切顺利 将当前请求入队
					animationLayerTransitionQueue.Enqueue(requestItem);
				}
			}
		}

		/// <summary>
		/// 处理分层转换请求
		/// </summary>
		public void ProcessLayerTransitionRequest()
		{
			//单独处理首次请求
			if (enterLayerAnimation)
			{
				currentLayerTransitionToAnimation = new AnimationRuntimeInfo(animationLayerTransitionQueue.Dequeue(), Time.time);
				layerTransitionTargetPlayable.Destroy();
				layerTransitionTargetPlayable = AnimationClipPlayable.Create(graph, currentLayerTransitionToAnimation.data.animClip);
				layerTransitionTargetPlayable.SetTime(0);
				graph.Connect(layerTransitionTargetPlayable, 0, subMixerPlayable, 1);
				subMixerPlayable.SetInputWeight(1, 0);
				layerTransitionTargetPlayable.Play();
				layerTransitionTargetPlayable.SetSpeed(currentLayerTransitionToAnimation.data.multiplier);
				enterLayerAnimation = false;
				hasLayerTransition = true;
			}
			//处理退出层级动画
			if (exitLayerAnimation)
			{
				if (layerExitTransitionStartTime == 0)
				{
					layerExitTransitionStartTime = Time.time;
				}
				float weight = Mathf.Clamp01((Time.time - layerExitTransitionStartTime) / transitionTime) * layerAnimationWeight;
				layerMixerPlayable.SetInputWeight(1, layerAnimationWeight - weight);
				if (weight == layerAnimationWeight)
				{
					subMixerPlayable.SetInputWeight(0, 0);
					subMixerPlayable.SetInputWeight(1, 0);
					layerMixerPlayable.SetInputWeight(1, 1);
					currentLayerPlayingAnimation = null;
					currentLayerTransitionToAnimation = null;
					animationLayerTransitionQueue.Clear();
					layerExitTransitionStartTime = 0;
					hasLayerAnimation = false;
					exitLayerAnimation = false;
				}
			}
			//如果当前没有正在执行层级之间的转换
			if (!hasLayerTransition)
			{
				//请求队列不为空
				if (!animationLayerTransitionQueue.isEmpty())
				{
					if (currentLayerPlayingAnimation.data.canAbort || currentLayerPlayingAnimation.finishPlaying)
					{
						//在下一帧执行转换 并获取到当前转换项
						hasLayerTransition = true;
						currentLayerTransitionToAnimation = new AnimationRuntimeInfo(animationLayerTransitionQueue.Dequeue(), Time.time);

						//将目标anim与端口1绑定 为开始从端口0到端口1转换做准备
						subMixerPlayable.DisconnectInput(1);
						layerTransitionTargetPlayable.Destroy();
						layerTransitionTargetPlayable = AnimationClipPlayable.Create(graph, currentLayerTransitionToAnimation.data.animClip);
						layerTransitionTargetPlayable.SetTime(0);
						graph.Connect(layerTransitionTargetPlayable, 0, subMixerPlayable, 1);
						subMixerPlayable.SetInputWeight(1, 0);
						layerTransitionTargetPlayable.Play();
						layerTransitionTargetPlayable.SetSpeed(currentLayerTransitionToAnimation.data.multiplier);
						//Debug.Log($"{currentPlayingAnimation.data.animName} => {currentTransitionToAnimation.data.animName}");
					}
				}
				//如果请求为空
				else
				{
					//当前播放动画完成且不是循环动画
					if (currentLayerPlayingAnimation != null)
					{
						if (currentLayerPlayingAnimation.data.animPlayType != AnimationPlayType.Loop && currentLayerPlayingAnimation.finishPlaying)
						{
							ExitLayerAnimation();
						}
					}
				}
			}
			else
			{
				//转换刚开始时记录一下当前开始转换的时间
				if (layerTransitionStartTime == 0)
				{
					layerTransitionStartTime = Time.time;
				}
				float weight = Mathf.Clamp01((Time.time - layerTransitionStartTime) / transitionTime) * layerAnimationWeight;
				subMixerPlayable.SetInputWeight(0, layerAnimationWeight - weight);
				subMixerPlayable.SetInputWeight(1, weight);
				//转换完成
				if (weight == layerAnimationWeight)
				{
					//交换端口0和端口1的anim
					subMixerPlayable.DisconnectInput(0);
					subMixerPlayable.DisconnectInput(1);
					AnimationClipPlayable temp = layerCurrentPlayingPlayable;
					layerCurrentPlayingPlayable = layerTransitionTargetPlayable;
					layerTransitionTargetPlayable = temp;
					graph.Connect(layerCurrentPlayingPlayable, 0, subMixerPlayable, 0);
					graph.Connect(layerTransitionTargetPlayable, 0, subMixerPlayable, 1);
					subMixerPlayable.SetInputWeight(0, 1);
					subMixerPlayable.SetInputWeight(1, 0);
					layerTransitionTargetPlayable.SetTime(0);
					layerTransitionTargetPlayable.Pause();

					//转换完成后记录
					hasLayerTransition = false;
					layerTransitionStartTime = 0;
					//将当前转换项设置为当前播放项
					if (currentLayerPlayingAnimation != null)
					{
						animationLayerRuntimeStackLog.Push(currentLayerPlayingAnimation);
					}
					currentLayerPlayingAnimation = currentLayerTransitionToAnimation;
					currentLayerTransitionToAnimation = null;
				}
			}
		}

		public void Stop()
		{
			graph.Stop();
		}

		public void Play()
		{
			graph.Play();
		}

		/// <summary>
		/// 退出当前图层动画
		/// </summary>
		public void ExitLayerAnimation()
		{
			exitLayerAnimation = true;
		}

		/// <summary>
		/// 返回当前正在播放的动画的名称
		/// </summary>
		/// <returns></returns>
		public string GetCurrentPlayingAnimationName()
		{
			return currentPlayingAnimation.data.animName;
		}

		/// <summary>
		/// 返回当前正在播放的动画的名称
		/// </summary>
		/// <returns></returns>
		public string GetCurrentPlayingLayerAnimationName()
		{
			return currentLayerPlayingAnimation?.data.animName;
		}

		public float GetCurrentPlayingAnimationPercentage()
		{
			return currentPlayingAnimation.PlayPercentage();
		}

		/// <summary>
		/// 设置层动画的权重
		/// </summary>
		/// <param name="weight"></param>
		public void SetCurrentLayerAnimationWeight(float weight)
		{
			layerMixerPlayable.SetInputWeight(1, Mathf.Clamp01(weight));
		}

		/// <summary>
		/// 通过名称寻找特定的动画数据
		/// </summary>
		/// <param name="animationName">动画数据的名称</param>
		/// <returns></returns>
		/// <summary>
		private AnimationPlayerDataSO GetAnimationData(string animationName)
		{
			int count = animationDataContainer.datas.Count;
			for (int i = 0; i < count; i++)
			{
				if (animationDataContainer.datas[i].animName == animationName)
				{
					return animationDataContainer.datas[i];
				}
			}
			return null;
		}

		/// <summary>
		/// 通过名称寻找特定的动画层遮罩数据
		/// </summary>
		/// <param name="maskName"></param>
		/// <returns></returns>
		private AnimationLayerMaskData GetAnimationLayerMaskData(string maskName)
		{
			int count = animationDataContainer.layerMasks.Count;
			for (int i = 0; i < count; i++)
			{
				if (animationDataContainer.layerMasks[i].maskName == maskName)
				{
					return animationDataContainer.layerMasks[i];
				}
			}
			return null;
		}

		/// <summary>
		/// 动画运行时信息类
		/// </summary>
		public class AnimationRuntimeInfo
		{
			public AnimationPlayerDataSO data;
			public bool finishPlaying = false;//第一次播放完就为TRUE
			public double beginTime;//开始时间

			public AnimationRuntimeInfo(AnimationPlayerDataSO data, double beginTime)
			{
				this.data = data;
				this.beginTime = beginTime;
			}

			public bool CheckFinished()
			{
				if ((float)(Time.time - beginTime) * data.multiplier >= data.animClip.length + data.offset)
				{
					finishPlaying = true;
				}
				return finishPlaying;
			}

			public float PlayPercentage()
			{
				return Mathf.Clamp01(((float)(Time.time - beginTime) * data.multiplier) / (data.animClip.length + data.offset));
			}
		}
	}
}