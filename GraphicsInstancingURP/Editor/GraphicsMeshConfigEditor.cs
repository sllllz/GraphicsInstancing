using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;


namespace Extend.GraphicsInstancing.Editor
{
	public class ConfigViewTreeItem : TreeViewItem {
		public GraphicsMeshConfig.Config Configuration { get; }

		public ConfigViewTreeItem(GraphicsMeshConfig.Config configuration) : base(configuration.GetHashCode()) {
			Configuration = configuration;
		}

		public override string displayName { get => Configuration?.Name; set {} }
	}

	public class ConfigViewDelegate : IListViewDelegate<ConfigViewTreeItem> {
		private readonly GraphicsMeshConfig configurationContext;
		private readonly SerializedObject serializedObject;
		private int selectedIndex = -1;

		public ConfigViewDelegate(GraphicsMeshConfig context) {
			configurationContext = context;
			serializedObject = new SerializedObject(context);
		}

		public MultiColumnHeader Header => new MultiColumnHeader(new MultiColumnHeaderState(new[] {
			new MultiColumnHeaderState.Column {headerContent = new GUIContent("Name"), width = 10},
			new MultiColumnHeaderState.Column {headerContent = new GUIContent("Graphics Mesh"), width = 20, canSort = false},
		}));

		public List<TreeViewItem> GetData() {
			return configurationContext.Configs.Select(configuration => new ConfigViewTreeItem(configuration)).Cast<TreeViewItem>().ToList();
		}

		public List<TreeViewItem> GetSortedData(int columnIndex, bool isAscending) {
			var items = GetData();
			items.Sort((a, b) => (isAscending ? -1 : 1) * string.Compare(a.displayName, b.displayName, StringComparison.Ordinal));
			return items;
		}

		private static readonly string[] columnIndexToFieldName = {"Name", "GraphicsMesh"};

		public void Draw(Rect rect, int columnIndex, ConfigViewTreeItem data, bool selected) {
			var index = Array.IndexOf(configurationContext.Configs, data.Configuration);
			if( selected ) {
				selectedIndex = index;
			}
			
			var configurations = serializedObject.FindProperty("m_configs");
			var element = configurations.GetArrayElementAtIndex(index);

			var fieldName = columnIndexToFieldName[columnIndex];
			var prop = element.FindPropertyRelative(fieldName);
			
			EditorGUI.BeginChangeCheck();
			EditorGUI.PropertyField(rect, prop, GUIContent.none);

			if( EditorGUI.EndChangeCheck() ) {
				serializedObject.ApplyModifiedProperties();
			}
		}

		public void OnItemClick(int id) {
		}

		public void OnContextClick() {
		}

		public void Add() {
			var configurations = serializedObject.FindProperty("m_configs");
			configurations.InsertArrayElementAtIndex(configurations.arraySize);
			serializedObject.ApplyModifiedProperties();
		}

		public void Remove() {
			var configurations = serializedObject.FindProperty("m_configs");
			if( selectedIndex >= 0 && selectedIndex < configurations.arraySize ) {
				configurations.DeleteArrayElementAtIndex(selectedIndex);
				serializedObject.ApplyModifiedProperties();
			}
			selectedIndex = -1;
		}

		public void Save() {
			EditorUtility.SetDirty(configurationContext);
			AssetDatabase.SaveAssets();
		}
	}

	public class GraphicsMeshConfigEditor : EditorWindow
	{
		private static ListView<ConfigViewTreeItem> listView;
		private ConfigViewDelegate _delegate;
		private bool refreshFlag;
		private bool dirtyFlag;
		private static GraphicsMeshConfigEditor window;
		public const string ConfigPath = "Assets/GraphicsInstancing/Resources/" + GraphicsMeshConfig.FILE_PATH + ".asset";

		[MenuItem("Window/GraphicsMeshInfoConfig")]
		static void MakeWindow()
		{
			if(window)
			{
				window.Close();
				window = null;
			}
			window = GetWindow<GraphicsMeshConfigEditor>();
			window.titleContent = new GUIContent("GraphicsMeshConfig");
			window.Show();
		}

		void OnEnable()
		{
			var config = AssetDatabase.LoadAssetAtPath<GraphicsMeshConfig>(ConfigPath);
			if( !config ) {
				config = CreateInstance<GraphicsMeshConfig>();
				AssetDatabase.CreateAsset(config, ConfigPath);
			}

			_delegate = new ConfigViewDelegate(config);
			listView = new ListView<ConfigViewTreeItem>(_delegate);
			listView.Refresh();
		}

		void OnGUI()
		{
			ButtonsGUI();
			var controlRect = EditorGUILayout.GetControlRect(
				GUILayout.ExpandHeight(true),
				GUILayout.ExpandWidth(true));
			if( refreshFlag ) {
				listView?.Refresh();
			}

			listView?.OnGUI(controlRect);
			if( dirtyFlag || refreshFlag ) {
				_delegate.Save();
			}

			dirtyFlag = false;
			refreshFlag = false;
		}

		private void ButtonsGUI() {
			GUILayout.BeginHorizontal();
			if( GUILayout.Button("Add") ) {
				_delegate.Add();
				refreshFlag = true;
			}

			if( GUILayout.Button("Remove") ) {
				_delegate.Remove();
				refreshFlag = true;
			}

			if( GUILayout.Button("Refresh") ) {
				refreshFlag = true;
			}
			
			if( GUILayout.Button("Save") ) {
				_delegate.Save();
			}

			GUILayout.EndHorizontal();
		}
	}
}