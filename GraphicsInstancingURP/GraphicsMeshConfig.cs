using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;

namespace Extend.GraphicsInstancing
{
	public class GraphicsMeshConfig : ScriptableObject
	{
		[Serializable]
		public class Config
		{
			public string Name;

			public GraphicsMeshInfo GraphicsMesh;

			public void Dispose()
			{
			}
		}

		public const string FILE_PATH = "Config/GraphicsMeshConfig";

		[SerializeField, FormerlySerializedAs("configurations")]
		private Config[] m_configs;

		public Config[] Configs => m_configs;

		private Dictionary<string, Config> hashedConfigurations;

		private static GraphicsMeshConfig m_configAsset;

		private void OnEnable() {
			if( m_configs == null ) {
				m_configs = new[] { new Config() };
			}
		}

		public GraphicsMeshConfig ConvertData() {
			hashedConfigurations = new Dictionary<string, Config>(m_configs.Length);
			foreach( var configuration in m_configs ) {
				hashedConfigurations.Add(configuration.Name, configuration);
			}

			return this;
		}

		public Config GetOne(string configName) {
			if( !hashedConfigurations.TryGetValue(configName, out var configuration) ) {
				Debug.LogError($"No GraphicsMesh config named : {configName}");
				return null;
			}
			if(configuration.GraphicsMesh == null)
			{
				Debug.LogError($"No GraphicsMesh config Asset named : {configName}");
				return null;
			}

			return configuration;
		}

		public void Unload()
		{
			m_configAsset = null;
		}

		public static GraphicsMeshConfig Load() {
		#if UNITY_EDITOR
			m_configAsset = Resources.Load<GraphicsMeshConfig>(FILE_PATH);
			Assert.IsTrue(m_configAsset != null);
		#endif
			return m_configAsset.ConvertData();
		}
	}
}