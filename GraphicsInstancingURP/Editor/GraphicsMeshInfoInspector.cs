using UnityEngine;
using UnityEditor;

namespace Extend.GraphicsInstancing.Editor
{
	// [CustomEditor(typeof(GraphicsMeshInfo))]
	class GraphicsMeshInfoInspector : UnityEditor.Editor
	{
		public override void OnInspectorGUI() {
			var meshProp = serializedObject.FindProperty("Mesh");
			EditorGUILayout.PropertyField(meshProp);

			var materialProp = serializedObject.FindProperty("Material");
			EditorGUILayout.PropertyField(materialProp);

			var animatorInfoProp = serializedObject.FindProperty("AnimatorInfo");
			EditorGUILayout.PropertyField(animatorInfoProp);

			serializedObject.ApplyModifiedProperties();
		}
	}
}