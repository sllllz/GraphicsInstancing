using UnityEngine;
using System;
using System.Collections.Generic;
// ReSharper disable All

namespace Extend.GraphicsInstancing
{
	public class GraphicsAnimatorMesh : GraphicsMesh
	{
		struct Event
		{
			public string animationName;
			public string eventArg;
		}
		private bool m_pause = false;
		public bool Pause
		{
			get => m_pause;
			set
			{
				if (m_pause = value)
					return;
				m_pause = value;
			}
		}
		private float m_speed = 1f;
		public float Speed
		{
			get => m_speed;
			set
			{
				if (m_speed == value)
					return;
				m_speed = value;
			}
		}
		private float m_curFrame = 0;
		private float CurFrame
		{
			get => m_curFrame;
			set
			{
				if (m_curFrame == value)
					return;
				m_curFrame = value;
				m_group.SetFrameIndex(this, m_curAni.AnimationIndex + value);
			}
		}
		private float m_preFrame = 0;
		private float PreFrame
		{
			get => m_preFrame;
			set
			{
				if (m_preFrame == value)
					return;
				m_preFrame = value;
				if (value == 0)
				{
					m_group.SetPreFrameIndex(this, m_curAni.AnimationIndex + CurFrame);
				}
				else
				{
					m_group.SetPreFrameIndex(this, m_preAni.AnimationIndex + value);
				}
			}
		}
		private float m_aniTransition = 0;
		private float AniTransition
		{
			get => m_aniTransition;
			set
			{
				if (m_aniTransition == value)
					return;
				m_aniTransition = value;
				m_group.SetTransition(this, value);
			}
		}

		public override int Index
		{
			get => m_index;
			set
			{
				m_index = value;
				m_group.SetMatrix(this, m_pos, m_rotation, m_scale);
				// int offset = m_curAni != null ? m_curAni.AnimationIndex : 1;
				m_group.SetFrameIndex(this, m_curAni.AnimationIndex + CurFrame);
			}
		}

		private AnimationInfo m_curAni;
		private AnimationInfo m_preAni;
		private float m_transitionTime;
		private float m_crossFadeTime;
		private bool[] m_events = new bool[5];
		private int m_eventNum = 0;
		private int m_defaultAnimHash;
		private int m_currentAnimHash;
		protected new GraphicsAnimatorMeshGroup m_group;
		public Action<string> OnAnimationCompleteAction;
		public Action<string, string> OnAnimationEventAction;
		private bool m_triggerAction = false;
		private List<Event> triggerEvents = new List<Event>();

		public GraphicsAnimatorMesh(GraphicsMeshGroup group, Vector3 pos, Vector3 rotation, Vector3 scale) : base(group, pos, rotation, scale)
		{
			m_group = (GraphicsAnimatorMeshGroup)group;
			m_defaultAnimHash = m_group.AnimatorInfo.DefaultAnimHash;
		}

		public override void Init()
		{
			Play(m_defaultAnimHash, 0);
			Show();
		}

		public void Play(string aniName, float crossFadeTime = 0.2f, bool isForce = false) => Play(Animator.StringToHash(aniName), crossFadeTime, isForce);

		public void Play(int aniHash, float crossFadeTime = 0.2f, bool isForce = false)
		{
			if (m_currentAnimHash == aniHash && !isForce) return;
			var animationInfos = m_group.AnimatorInfo.AnimationInfos;
			for (int i = 0; i < animationInfos.Length; i++)
			{
				if (animationInfos[i].NameHash == aniHash)
				{
					Pause = false;
					if (crossFadeTime < 0.001f)
					{
						m_curAni = animationInfos[i];
						CurFrame = 0;
						m_preAni = null;
					}
					else
					{
						m_preAni = animationInfos[i];
						PreFrame = 0;
						AniTransition = 0;
						m_transitionTime = Time.time;
						m_crossFadeTime = crossFadeTime;
					}
					m_eventNum = animationInfos[i].Events.Length;
					for (int j = 0; j < m_eventNum; j++)
						m_events[j] = false;
					m_currentAnimHash = aniHash;
					break;
				}
			}
		}

		public float GetClipTime(string aniName) => GetClipTime(Animator.StringToHash(aniName));
		public float GetClipTime(int aniHash)
		{
			var animationInfos = m_group.AnimatorInfo.AnimationInfos;
			foreach (var animationInfo in animationInfos)
			{
				if (animationInfo.NameHash == aniHash)
				{
					return animationInfo.TotalFrame / animationInfo.Fps;
				}
			}
			return 0;
		}

		public override void Update(float time)
		{
			if (Pause || m_curAni == null) return;
			float frame = CurFrame + Speed * time * m_curAni.Fps;
			float preframe = 0;
			int totalFrame = m_curAni.TotalFrame - 1;
			float pretotalFrame = 0;
			CurFrame = frame;
			if (m_preAni != null)
			{
				if (Time.time > m_transitionTime + m_crossFadeTime)
				{
					m_curAni = m_preAni;
					CurFrame = PreFrame;
					m_preAni = null;
					AniTransition = 0;
					PreFrame = 0;
					frame = CurFrame;
					totalFrame = m_curAni.TotalFrame - 1;
					return;
				}
				else
				{
					PreFrame += Speed * time * m_preAni.Fps;
					preframe = PreFrame;
					pretotalFrame = m_preAni.TotalFrame - 1;

					AniTransition = (Time.time - m_transitionTime) / m_crossFadeTime;
				}
			}
			switch (m_curAni.Mode)
			{
				case WrapMode.Loop:
					{
						if (frame < 0f)
						{
							CurFrame = frame + totalFrame;
						}
						else if (frame > totalFrame)
						{
							CurFrame = frame - (totalFrame);
							if (m_preAni == null)
							{
								OnAnimationLoopComplete();
							}
						}
						break;
					}
				case WrapMode.PingPong:
					{
						if (frame < 0f)
						{
							Speed = Mathf.Abs(Speed);
							CurFrame = Mathf.Abs(frame);
						}
						else if (frame > totalFrame)
						{
							Speed = -Mathf.Abs(Speed);
							CurFrame = 2 * totalFrame - frame;
						}
						break;
					}
				case WrapMode.Default:
				case WrapMode.Once:
					{
						if (frame > totalFrame)
						{
							if (m_preAni == null)
							{
								OnAnimationComplete(m_curAni.Name);
							}
							CurFrame = totalFrame;
						}
						break;
					}
			}
			if (m_preAni != null)
			{
				switch (m_preAni.Mode)
				{
					case WrapMode.Loop:
						{
							if (preframe < 0f)
							{
								PreFrame = preframe + pretotalFrame;
							}
							else if (preframe > pretotalFrame)
							{
								PreFrame = preframe - pretotalFrame;
								//OnAnimationLoopComplete();
							}
							break;
						}
					case WrapMode.PingPong:
						{
							if (preframe < 0f)
							{
								Speed = Mathf.Abs(Speed);
								PreFrame = Mathf.Abs(preframe);
							}
							else if (preframe > pretotalFrame)
							{
								Speed = -Mathf.Abs(Speed);
								PreFrame = 2 * pretotalFrame - preframe;
							}
							break;
						}
					case WrapMode.Default:
					case WrapMode.Once:
						{
							//if (preframe < 0f || preframe > pretotalFrame)
							//{
							//	OnAnimationComplete(m_preAni.Name);
							//}
							break;
						}
				}
			}
			UpdateAnimationEvent();
		}

		private void UpdateAnimationEvent()
		{
			if (m_curAni == null)
				return;
			if (m_eventNum == 0)
				return;

			float time = CurFrame / m_curAni.Fps;
			for (int i = 0; i < m_eventNum; i++)
			{
				if (!m_events[i] && m_curAni.Events[i].Time > time)
				{
					m_events[i] = true;
					CacheAnimationEvent(m_curAni.Name, m_curAni.Events[i].StringParameter);
				}
			}
		}

		private void OnAnimationLoopComplete()
		{
			for (int i = 0; i < m_eventNum; i++)
			{
				m_events[i] = false;
			}
		}

		private void OnAnimationComplete(string animationName)
		{
			//m_curAni = null;
			//CurFrame = 0;
			//Pause = true;
			if (m_currentAnimHash != m_defaultAnimHash)
			{
				//别的动画渐变到默认动画耗时0.2s
				Play(m_defaultAnimHash);
			}
			else
			{
				Play(m_defaultAnimHash, 0f);
			}
			OnAnimationCompleteAction?.Invoke(animationName);
		}

		private void CacheAnimationEvent(string animationName, string eventArg)
		{
			if (OnAnimationEventAction == null) return;
			if (!m_triggerAction)
				m_group.AddTriggerEventMesh(this);

			m_triggerAction = true;
			triggerEvents.Add(new Event
			{
				animationName = animationName,
				eventArg = eventArg,
			});
		}

		public void TriggerAnimationEvent()
		{
			m_triggerAction = false;
			foreach (var e in triggerEvents)
			{
				if (!Visible) break;
				OnAnimationEventAction?.Invoke(e.animationName, e.eventArg);
			}
			triggerEvents.Clear();
		}
	}
}