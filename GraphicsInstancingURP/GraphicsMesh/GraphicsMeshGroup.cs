using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace Extend.GraphicsInstancing
{
	public class GraphicsMeshGroup
	{
		protected int m_createCount = 0;
		private GraphicsMeshConfig.Config config;
		protected GraphicsMeshInfo m_meshInfo;
		protected const int BUFFER_MAX_SIZE = 1023;
		protected Matrix4x4[] m_matrixes = new Matrix4x4[100];
		protected List<GraphicsMesh> m_updateMeshes = new List<GraphicsMesh>(10);

		public GraphicsMeshGroup(GraphicsMeshConfig.Config conf, GraphicsMeshInfo meshInfo)
		{
			config = conf;
			m_meshInfo = meshInfo;
			if( !m_meshInfo.Material ) {
				Debug.LogError($"Graphics mesh {m_meshInfo.name} material is empty!");
			}
		}

		public virtual GraphicsMesh CreateGraphicMesh(Vector3 pos, Vector3 rotation, Vector3 scale)
		{
			Assert.IsTrue(CanCreate(), m_meshInfo.Mesh.name + " count over 1024");
			m_createCount++;
			var mesh = new GraphicsMesh(this, pos, rotation, scale);
			mesh.Init();
			return mesh;
		}

		public void Dispose()
		{
			config.Dispose();
		}

		public void ShowGraphicsMesh(GraphicsMesh mesh)
		{
			m_updateMeshes.Add(mesh);
			OnAddMesh();
			mesh.Index = m_updateMeshes.Count - 1;
		}

		protected virtual void OnAddMesh()
		{
			
		}

		public void HideGraphicsMesh(GraphicsMesh mesh)
		{
			int count = m_updateMeshes.Count;
			int removeIndex = mesh.Index;
			var lastMesh = m_updateMeshes[count - 1];
			int lastIndex = lastMesh.Index;
			if(removeIndex != lastIndex)
			{
				lastMesh.Index = removeIndex;
				m_updateMeshes[removeIndex] = lastMesh;
				m_updateMeshes[lastIndex] = mesh;
			}
			m_updateMeshes.RemoveAt(count - 1);
		}

		public void RemoveGraphicsMesh(GraphicsMesh mesh)
		{
			m_createCount--;
		}

		public void SetMatrix(GraphicsMesh mesh, Vector3 pos, Vector3 rotation, Vector3 scale)
		{
			if(!mesh.Visible) return;
			if(m_matrixes.Length < m_updateMeshes.Count)
			{
				int length = m_matrixes.Length + 100 > BUFFER_MAX_SIZE ? BUFFER_MAX_SIZE : m_matrixes.Length + 100;
				var temp = new Matrix4x4[length];
				for (int i = 0; i < m_matrixes.Length; i++)
				{
					temp[i] = m_matrixes[i];
				}
				m_matrixes = temp;
			}
			m_matrixes[mesh.Index] = Matrix4x4.TRS(pos, Quaternion.Euler(rotation), scale);
		}

		public virtual void Update(float time)
		{
			Draw(time);
			if(m_createCount == 0)
			{
				GraphicsInstancingService.Instance.RemoveGroup(this);
			}
		}

		protected virtual void Draw(float time) {
			Graphics.DrawMeshInstanced(m_meshInfo.Mesh, 0, m_meshInfo.Material, m_matrixes, m_updateMeshes.Count);
		}

		protected bool CanCreate()
		{
			return m_createCount < BUFFER_MAX_SIZE;
		}
	}
}