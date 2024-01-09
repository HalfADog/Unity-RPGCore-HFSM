using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace RPGCore.Animation
{
	public class AnimationPlayerManager : MonoBehaviour
	{
		//����ͼ���ò���

		private PlayableGraph graph;
		private AnimationPlayableOutput output;
		private AnimationLayerMixerPlayable layerMixerPlayable;
		private AnimationMixerPlayable mainMixerPlayable;
		private AnimationMixerPlayable subMixerPlayable;
		private AnimationClipPlayable currentPlayingPlayable;
		private AnimationClipPlayable transitionTargetPlayable;
		private AnimationClipPlayable layerCurrentPlayingPlayable;
		private AnimationClipPlayable layerTransitionTargetPlayable;

		//��ͨת��

		private bool hasTransition;
		private float transitionStartTime;

		//��ת��

		private bool hasLayerTransition;
		private float layerTransitionStartTime;
		private float layerExitTransitionStartTime;
		private bool hasLayerAnimation;
		private bool enterLayerAnimation;
		private bool exitLayerAnimation;
		private float layerAnimationWeight;

		//��¼ջ ��¼���Ź��Ķ�����Ϣ

		private static readonly int runtimeStackLogMaxLength = 8;
		private RingStack<AnimationRuntimeInfo> animationRuntimeStackLog = new RingStack<AnimationRuntimeInfo>(runtimeStackLogMaxLength);
		private RingStack<AnimationRuntimeInfo> animationLayerRuntimeStackLog = new RingStack<AnimationRuntimeInfo>(runtimeStackLogMaxLength);

		//ת���������

		private static readonly int transitionQueueMaxLength = 8;
		private RingQueueEx<AnimationPlayerDataSO> animationTransitionQueue = new RingQueueEx<AnimationPlayerDataSO>(transitionQueueMaxLength);
		private RingQueueEx<AnimationPlayerDataSO> animationLayerTransitionQueue = new RingQueueEx<AnimationPlayerDataSO>(transitionQueueMaxLength);

		//��ǰ���ڲ��ŵ���

		private AnimationRuntimeInfo currentPlayingAnimation;
		private AnimationRuntimeInfo currentLayerPlayingAnimation;

		/// <summary>
		/// ���ص�ǰ���ڲ��ŵĶ����Ƿ���ɲ���
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
		/// ���ص�ǰ���ڲ��ŵĲ㼶�����Ƿ���ɲ��� 0δ��� 1��� -1�㼶����δ����
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

		//��ǰҪת��������

		private AnimationRuntimeInfo currentTransitionToAnimation;
		private AnimationRuntimeInfo currentLayerTransitionToAnimation;

		//����� �����ж��Ƿ�Ϊͬһʱ�������

		private int randomChecker;
		private int currentChecker = -1;
		private int currentLayerChecker = -1;

		//Ҫ�õ������ж�������
		public AnimationPlayerDataContainerSO animationDataContainer;

		//ת��ʱ��
		[Range(0.1f, 2f)]
		public float transitionTime = 0.1f;

		public void Awake()
		{
			InitializePlayableGraph();
		}

		private void Update()
		{
			//������������ �����ж�ͬһʱ�̵�ת������
			randomChecker = Random.Range(1, 100000);
			//���µ�ǰ���Ŷ�������Ϣ
			currentPlayingAnimation.CheckFinished();
			//���µ�ǰ���ŵĲ㼶��������Ϣ
			if (hasLayerAnimation && currentLayerPlayingAnimation != null)
			{
				currentLayerPlayingAnimation.CheckFinished();
			}
			//����ת��
			ProcessTransitionRequest();
			//����㼶ת��
			if (hasLayerAnimation)
			{
				ProcessLayerTransitionRequest();
			}
			//Debug.Log(CurrentFinishPlaying);
		}

		/// <summary>
		/// ��ʼ��
		/// </summary>
		private void InitializePlayableGraph()
		{
			//����
			graph = PlayableGraph.Create(name + " Animation Player");
			output = AnimationPlayableOutput.Create(graph, "Output", GetComponent<Animator>());
			layerMixerPlayable = AnimationLayerMixerPlayable.Create(graph, 2);
			mainMixerPlayable = AnimationMixerPlayable.Create(graph, 2);
			subMixerPlayable = AnimationMixerPlayable.Create(graph, 2);
			currentPlayingPlayable = AnimationClipPlayable.Create(graph, animationDataContainer.defaultAnimation.animClip);
			transitionTargetPlayable = AnimationClipPlayable.Create(graph, null);
			layerCurrentPlayingPlayable = AnimationClipPlayable.Create(graph, null);
			layerTransitionTargetPlayable = AnimationClipPlayable.Create(graph, null);
			//����
			graph.Connect(currentPlayingPlayable, 0, mainMixerPlayable, 0);
			graph.Connect(transitionTargetPlayable, 0, mainMixerPlayable, 1);
			graph.Connect(layerCurrentPlayingPlayable, 0, subMixerPlayable, 0);
			graph.Connect(layerTransitionTargetPlayable, 0, subMixerPlayable, 1);
			graph.Connect(mainMixerPlayable, 0, layerMixerPlayable, 0);
			graph.Connect(subMixerPlayable, 0, layerMixerPlayable, 1);
			//��ʼ��
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
			//����
			graph.Play();
		}

		/// <summary>
		/// �ύһ������ת������ �ȴ�����
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

			//�������ͬһʱ������ ��ôcurrentCheckerӦ����randomChecker���
			//��Ϊ��ͬһʱ����randomChecker����ı�
			bool isSameTime = (currentChecker == randomChecker);
			currentChecker = randomChecker;
			//�жϴ������Ƿ������ת������
			//�����ж϶����Ƿ�Ϊ��
			//���Ϊ����ֱ�ӽ��������
			if (animationTransitionQueue.isEmpty())
			{
				//�����ǰ��ת�� ��ô�жϵĶ���Ӧ���ǵ�ǰת����
				//����뵱ǰת������ͬ��ô�������ͺ��� �����ظ�ת����
				if (hasTransition)
				{
					if (requestItem.animName != currentTransitionToAnimation.data.animName)
					{
						animationTransitionQueue.Enqueue(requestItem);
					}
				}
				//���û�� �жϵĶ�����ǵ�ǰ������
				else
				{
					if (requestItem.animName != currentPlayingAnimation.data.animName)
					{
						animationTransitionQueue.Enqueue(requestItem);
					}
				}
			}
			//������в�Ϊ��
			else
			{
				//�鿴��βԪ��
				var qtail = animationTransitionQueue.PeekTail();
				//����Ҫȷ���ǲ�����ͬһʱ������� ����Ǿ�ֱ�Ӷ���� ���������ȼ���Ҫ�����ظ�����
				if (isSameTime)
				{
					if (requestItem.animName != qtail.animName)
					{
						animationTransitionQueue.Enqueue(requestItem);
					}
				}
				//�������ͬһʱ������ľͼ��������ж�
				else
				{
					//���β����Ƚ�
					//������������������Ե�ǰ����
					if (qtail.animName == requestItem.animName)
					{
						return;
					}
					//���������������ҵ�ǰ��������ȼ����ڶ�β��������ȼ�
					//�Ѷ�β���ȼ�С�ڻ���ڵ�ǰ��������Ƴ�
					//ֱ����β������ȼ����ڵ�ǰ��������Ϊ��
					while (requestItem.animPriority >= qtail.animPriority)
					{
						//����������ȼ����
						if (requestItem.animPriority == qtail.animPriority && qtail.canAbort)
						{
							//�Ҷ�β��ֻ�ܱ��������ȼ���������������
							if (qtail.abortType == AnimationAbortType.OnlyHigherPriority)
							{
								break;
							}
						}
						animationTransitionQueue.PopTail();
						if (animationTransitionQueue.isEmpty()) break;
						qtail = animationTransitionQueue.PeekTail();
					}
					//һ��˳�� ����ǰ�������
					animationTransitionQueue.Enqueue(requestItem);
				}
			}
		}

		/// <summary>
		/// ����ת������
		/// </summary>
		private void ProcessTransitionRequest()
		{
			//�жϵ�ǰ�Ƿ�����ִ��ת��
			//�����ǰû��ִ��ת��
			if (!hasTransition)
			{
				//�ж�ת����������Ƿ�Ϊ��
				//�����Ϊ�� ˵����ǰ��ת������
				if (!animationTransitionQueue.isEmpty())
				{
					//�жϵ�ǰ�������ܷ񱻴��ִ��ת��
					//���������ϻ򲥷����ͬ������ִ��ת��
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
						//����һִ֡��ת�� ����ȡ����ǰת����
						hasTransition = true;
						currentTransitionToAnimation = new AnimationRuntimeInfo(animationTransitionQueue.Dequeue(), Time.time);

						//��Ŀ��anim��˿�1�� Ϊ��ʼ�Ӷ˿�0���˿�1ת����׼��
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
			//��ǰ��ִ�е�ת��
			else
			{
				//ת���տ�ʼʱ��¼һ�µ�ǰ��ʼת����ʱ��
				if (transitionStartTime == 0)
				{
					transitionStartTime = Time.time;
				}
				float weight = Mathf.Clamp01((Time.time - transitionStartTime) / transitionTime);
				mainMixerPlayable.SetInputWeight(0, 1 - weight);
				mainMixerPlayable.SetInputWeight(1, weight);
				//ת�����
				if (weight == 1)
				{
					//�����˿�0�Ͷ˿�1��anim
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

					//ת����ɺ��¼
					hasTransition = false;
					transitionStartTime = 0;
					//����ǰת��������Ϊ��ǰ������
					animationRuntimeStackLog.Push(currentPlayingAnimation);
					currentPlayingAnimation = currentTransitionToAnimation;
					currentTransitionToAnimation = null;
				}
			}
		}

		/// <summary>
		/// �ύһ���ֲ㶯��ת������ �ȴ�����
		/// </summary>
		/// <param name="layerName">AvatarMask</param>
		public void RequestLayerTransition(string animationName, string layerName, float layerWeight = 1)
		{
			//�ڴ����˳�ʱ�����������������ȴ�������
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

			//�趨�����Ĳ㼶��Ȩ��
			layerMixerPlayable.SetLayerMaskFromAvatarMask(1, requestMask.mask);
			SetCurrentLayerAnimationWeight(layerAnimationWeight);

			//�������ͬһʱ������ ��ôcurrentLayerCheckerӦ����randomChecker��� ��Ϊ��ͬһʱ����randomChecker����ı�
			bool isSameTime = (currentLayerChecker == randomChecker);
			currentLayerChecker = randomChecker;

			//�жϴ������Ƿ������ת������ �����ж϶����Ƿ�Ϊ�� ���Ϊ����ֱ�ӽ��������
			if (animationLayerTransitionQueue.isEmpty())
			{
				//�״����� ֱ�����
				if (!hasLayerAnimation)
				{
					animationLayerTransitionQueue.Enqueue(requestItem);
					enterLayerAnimation = true;
					hasLayerAnimation = true;
				}
				else
				{
					//�����ǰ��ת�� ��ô�жϵĶ���Ӧ���ǵ�ǰת���� ����뵱ǰת������ͬ��ô�������ͺ��� �����ظ�ת����
					if (hasLayerTransition)
					{
						if (requestItem.animName != currentLayerTransitionToAnimation.data.animName)
						{
							animationLayerTransitionQueue.Enqueue(requestItem);
						}
					}
					//���û�� �жϵĶ�����ǵ�ǰ������
					else
					{
						if (currentLayerPlayingAnimation != null && requestItem.animName != currentLayerPlayingAnimation.data.animName)
						{
							animationLayerTransitionQueue.Enqueue(requestItem);
						}
					}
				}
			}
			//������в�Ϊ��
			else
			{
				//�鿴��βԪ��
				var qtail = animationLayerTransitionQueue.PeekTail();
				//����Ҫȷ���ǲ�����ͬһʱ������� ����Ǿ�ֱ�Ӷ���� ���������ȼ���Ҫ�����ظ�����
				if (isSameTime)
				{
					if (requestItem.animName != qtail.animName)
					{
						animationLayerTransitionQueue.Enqueue(requestItem);
					}
				}
				//�������ͬһʱ������ľͼ��������ж�
				else
				{
					//���β����Ƚ� ������������������Ե�ǰ����
					if (qtail.animName == requestItem.animName)
					{
						return;
					}
					//���������������ҵ�ǰ��������ȼ����ڶ�β��������ȼ� �Ѷ�β���ȼ�С�ڻ���ڵ�ǰ��������Ƴ� ֱ����β������ȼ����ڵ�ǰ��������Ϊ��
					while (requestItem.animPriority >= qtail.animPriority)
					{
						animationLayerTransitionQueue.PopTail();
						if (animationLayerTransitionQueue.isEmpty()) break;
						qtail = animationLayerTransitionQueue.PeekTail();
					}
					//һ��˳�� ����ǰ�������
					animationLayerTransitionQueue.Enqueue(requestItem);
				}
			}
		}

		/// <summary>
		/// ����ֲ�ת������
		/// </summary>
		public void ProcessLayerTransitionRequest()
		{
			//���������״�����
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
			//�����˳��㼶����
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
			//�����ǰû������ִ�в㼶֮���ת��
			if (!hasLayerTransition)
			{
				//������в�Ϊ��
				if (!animationLayerTransitionQueue.isEmpty())
				{
					if (currentLayerPlayingAnimation.data.canAbort || currentLayerPlayingAnimation.finishPlaying)
					{
						//����һִ֡��ת�� ����ȡ����ǰת����
						hasLayerTransition = true;
						currentLayerTransitionToAnimation = new AnimationRuntimeInfo(animationLayerTransitionQueue.Dequeue(), Time.time);

						//��Ŀ��anim��˿�1�� Ϊ��ʼ�Ӷ˿�0���˿�1ת����׼��
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
				//�������Ϊ��
				else
				{
					//��ǰ���Ŷ�������Ҳ���ѭ������
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
				//ת���տ�ʼʱ��¼һ�µ�ǰ��ʼת����ʱ��
				if (layerTransitionStartTime == 0)
				{
					layerTransitionStartTime = Time.time;
				}
				float weight = Mathf.Clamp01((Time.time - layerTransitionStartTime) / transitionTime) * layerAnimationWeight;
				subMixerPlayable.SetInputWeight(0, layerAnimationWeight - weight);
				subMixerPlayable.SetInputWeight(1, weight);
				//ת�����
				if (weight == layerAnimationWeight)
				{
					//�����˿�0�Ͷ˿�1��anim
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

					//ת����ɺ��¼
					hasLayerTransition = false;
					layerTransitionStartTime = 0;
					//����ǰת��������Ϊ��ǰ������
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
		/// �˳���ǰͼ�㶯��
		/// </summary>
		public void ExitLayerAnimation()
		{
			exitLayerAnimation = true;
		}

		/// <summary>
		/// ���ص�ǰ���ڲ��ŵĶ���������
		/// </summary>
		/// <returns></returns>
		public string GetCurrentPlayingAnimationName()
		{
			return currentPlayingAnimation.data.animName;
		}

		/// <summary>
		/// ���ص�ǰ���ڲ��ŵĶ���������
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
		/// ���ò㶯����Ȩ��
		/// </summary>
		/// <param name="weight"></param>
		public void SetCurrentLayerAnimationWeight(float weight)
		{
			layerMixerPlayable.SetInputWeight(1, Mathf.Clamp01(weight));
		}

		/// <summary>
		/// ͨ������Ѱ���ض��Ķ�������
		/// </summary>
		/// <param name="animationName">�������ݵ�����</param>
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
		/// ͨ������Ѱ���ض��Ķ�������������
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
		/// ��������ʱ��Ϣ��
		/// </summary>
		public class AnimationRuntimeInfo
		{
			public AnimationPlayerDataSO data;
			public bool finishPlaying = false;//��һ�β������ΪTRUE
			public double beginTime;//��ʼʱ��

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