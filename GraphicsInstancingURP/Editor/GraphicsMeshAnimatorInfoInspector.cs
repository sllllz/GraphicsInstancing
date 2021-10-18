using UnityEngine;
using UnityEditor;

namespace Extend.GraphicsInstancing.Editor
{
	[CustomEditor(typeof(GraphicsAnimatorInfo))]
	class GraphicsMeshAnimatorInfoInspector : UnityEditor.Editor
	{
		Texture2D[] m_animTextures;
		private void OnEnable() {
			GraphicsAnimatorInfo info = (GraphicsAnimatorInfo)target;
			if(info != null)
			{
				m_animTextures = new Texture2D[info.AnimationTextures.Length];
				for (int i = 0; i != info.AnimationTextures.Length; ++i)
				{
					int width = info.AnimationTextures[i].Width;
					int height = info.AnimationTextures[i].Height;
					byte[] bytes = info.AnimationTextures[i].TexBytes;
					Texture2D texture = new Texture2D(width, height, TextureFormat.RGBAHalf, false);
					texture.LoadRawTextureData(bytes);
					texture.filterMode = FilterMode.Point;
					texture.name = info.name + "_Editor_" + i;
					texture.Apply();
					m_animTextures[i] = texture;
				}
			}
		}

		private void OnDisable(){
			if(m_animTextures != null)
			{
				foreach (var tex in m_animTextures)
				{
					Object.DestroyImmediate(tex);
				}
			}
		}

		public override void OnInspectorGUI() {
			GraphicsAnimatorInfo info = (GraphicsAnimatorInfo)target;

			EditorGUILayout.PropertyField(serializedObject.FindProperty("BlockWidth"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("BlockHeight"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("DefaultAnimHash"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("AnimationInfos"));

			if(m_animTextures != null)
			{
				for (int i = 0; i < m_animTextures.Length; i++)
				{
					var rect = EditorGUILayout.GetControlRect();
					rect = new Rect(rect.x, rect.y - 1, 100, 20);
					int texWidth = 80;
					var texRect = new Rect(EditorGUIUtility.currentViewWidth - texWidth - 5, rect.y, texWidth, texWidth);
					EditorGUI.LabelField(rect, "Tex " + i);
					rect.y += 20;
					EditorGUI.LabelField(rect, "Width " + info.AnimationTextures[i].Width);
					rect.y += 20;
					EditorGUI.LabelField(rect, "Height " + info.AnimationTextures[i].Height);
					EditorGUI.DrawPreviewTexture(texRect, m_animTextures[i], null, ScaleMode.ScaleToFit);
					GUILayout.Space(65);
				}
			}

			serializedObject.ApplyModifiedProperties();
		}
	}
}