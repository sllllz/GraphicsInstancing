using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace Extend.GraphicsInstancing
{
	public class GraphicsInstancingService : MonoBehaviour
	{
		private readonly Dictionary<string, GraphicsMeshGroup> m_groups = new Dictionary<string, GraphicsMeshGroup>(10);
		private readonly Dictionary<string, GraphicsMeshGroup> m_addGroups = new Dictionary<string, GraphicsMeshGroup>(10);
		private readonly List<string> m_removeGroups = new List<string>(10);
		private GraphicsMeshConfig m_config;
		private static GraphicsInstancingService _instance;
		public static GraphicsInstancingService Instance{
			get
			{
				if(_instance == null)
				{
					GameObject temp = new GameObject("Service");
					_instance = temp.AddComponent<GraphicsInstancingService>();
				}
				return _instance;
			}
		}
		private void Awake() {
			Initialize();
		}

		public void Initialize(){
			m_config = GraphicsMeshConfig.Load();
		}

		private void OnDestroy() {
			foreach (var item in m_addGroups.Values)
			{
				item.Dispose();
			}
			foreach (var item in m_groups.Values)
			{
				item.Dispose();
			}
			m_config.Unload();
		}

		private void Update() {
			if(m_addGroups.Count > 0)
			{
				foreach (var item in m_addGroups)
				{
					m_groups.Add(item.Key, item.Value);
				}
				m_addGroups.Clear();
			}
			if(m_removeGroups.Count > 0)
			{
				foreach (var item in m_removeGroups)
				{
					m_groups[item].Dispose();
					m_groups.Remove(item);
				}
				m_removeGroups.Clear();
			}

			if(m_groups.Count > 0)
			{
				foreach (var group in m_groups.Values)
				{
					group.Update(Time.deltaTime);
				}
			}
		}

		public int InstancingCount => m_groups.Count;

		public GraphicsMesh CreateMesh(string name, Vector3 pos, Vector3 rotation, Vector3 scale)
		{
			var conf = m_config.GetOne(name);
			if(conf == null)
				return null;
			
			if(!m_groups.TryGetValue(name, out var group))
			{
				if(m_removeGroups.Contains(name))
					m_removeGroups.Remove(name);
				if(!m_addGroups.TryGetValue(name, out group))
				{
					var meshInfo = conf.GraphicsMesh;
					if(meshInfo.AnimatorInfo != null)
					{
						group = new GraphicsAnimatorMeshGroup(conf, meshInfo);
					}
					else
					{
						group = new GraphicsMeshGroup(conf, meshInfo);
					}

					m_addGroups.Add(name, group);
				}
			}

			return group.CreateGraphicMesh(pos, rotation, scale);
		}

		public void Dump() {
			Debug.Log("GraphicsInstancing Count " + InstancingCount.ToString());
		}

		public void RemoveGroup(GraphicsMeshGroup group)
		{
			foreach (var item in m_groups)
			{
				if(item.Value == group)
				{
					m_removeGroups.Add(item.Key);
				}
			}
		}
	}
}