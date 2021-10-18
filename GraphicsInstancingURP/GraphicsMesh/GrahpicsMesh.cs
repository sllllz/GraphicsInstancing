using UnityEngine;

namespace Extend.GraphicsInstancing
{
	public class GraphicsMesh
	{
		public enum GraphicsMeshState {
			None = 0,
			Show,
			Hide,
			Remove,
		}
		protected Vector3 m_pos;
		public Vector3 Pos{
			get => m_pos;
			set {
				if(m_pos == value)
					return;
				m_pos = value;
				m_group.SetMatrix(this, m_pos, m_rotation, m_scale);
			}
		}
		protected Vector3 m_rotation;
		public Vector3 Rotation
		{
			get => m_rotation;
			set {
				if(m_rotation == value)
					return;
				m_rotation = value;
				m_group.SetMatrix(this, m_pos, m_rotation, m_scale);
			}
		}
		protected Vector3 m_scale = Vector3.one;
		public Vector3 Scale
		{
			get => m_scale;
			set {
				if(m_scale == value)
					return;
				m_scale = value;
				m_group.SetMatrix(this, m_pos, m_rotation, m_scale);
			}
		}
		protected int m_index = -1;
		public virtual int Index{
			get => m_index;
			set{
				m_index = value;
				m_group.SetMatrix(this, m_pos, m_rotation, m_scale);
			}
		}
		private GraphicsMeshState m_state = GraphicsMeshState.None;
		public GraphicsMeshState State{
			get => m_state;
			set {
				if(m_state == value || m_state == GraphicsMeshState.Remove)
					return;
				var oldState = m_state;
				m_state = value;
				switch(m_state)
				{
					case GraphicsMeshState.Show:
						m_group.ShowGraphicsMesh(this);
						break;
					case GraphicsMeshState.Hide:
						m_group.HideGraphicsMesh(this);
						break;
					case GraphicsMeshState.Remove:
						if(oldState == GraphicsMeshState.Show)
							m_group.HideGraphicsMesh(this);
						m_group.RemoveGraphicsMesh(this);
						break;
				}
			}
		}
		public bool Visible => State == GraphicsMeshState.Show;
		protected GraphicsMeshGroup m_group;

		public GraphicsMesh(GraphicsMeshGroup group, Vector3 pos, Vector3 rotation, Vector3 scale)
		{
			m_group = group;
			m_pos = pos;
			m_rotation = rotation;
			m_scale = scale;
		}

		public virtual void Init() {
			Show();
		}

		public void Destroy() { State = GraphicsMeshState.Remove; }
		public void Show() { State = GraphicsMeshState.Show; }
		public void Hide() { State = GraphicsMeshState.Hide; }

		public virtual void Update(float time) {}
	}
}