using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Assertions;
using System.Collections.Generic;

namespace Extend.GraphicsInstancing
{
	public class GraphicsAnimatorMeshGroup : GraphicsMeshGroup
	{
		private static readonly int BoneTexProp = Shader.PropertyToID("_boneTexture");
		private static readonly int FrameIndexProp = Shader.PropertyToID("_frameIndex");
		private static readonly int PreFrameIndexProp = Shader.PropertyToID("_preFrameIndex");
		private static readonly int TransitionProp = Shader.PropertyToID("_transitionProgress");
		private static readonly int TextureUVProp = Shader.PropertyToID("_textureUV");
		private static readonly int BlockWidthUVProp = Shader.PropertyToID("_blockWidthUV");
		private static readonly int BlockHeightUVProp = Shader.PropertyToID("_blockHeightUV");
		private static readonly int BlockCountProp = Shader.PropertyToID("_blockCount");
		private MaterialPropertyBlock m_block;
		private float[] m_frameIndexes = new float[100];
		private float[] m_preFrameIndexes = new float[100];
		private float[] m_transitions = new float[100];
		private bool m_blockDirty = false;
		private List<GraphicsAnimatorMesh> m_triggerEventMeshes = new List<GraphicsAnimatorMesh>(10);
		public GraphicsAnimatorInfo AnimatorInfo;

		public GraphicsAnimatorMeshGroup(GraphicsMeshConfig.Config conf, GraphicsMeshInfo meshInfo): base(conf, meshInfo)
		{
			AnimatorInfo = m_meshInfo.AnimatorInfo;
			m_block = new MaterialPropertyBlock();
			var animTextures = new Texture2D[AnimatorInfo.AnimationTextures.Length];
			for (int i = 0; i != AnimatorInfo.AnimationTextures.Length; ++i)
			{
				animTextures[i] = AnimatorInfo.AnimationTextures[i].GetTexture();
			}

			m_meshInfo.Material.SetTexture(BoneTexProp, animTextures[0]);
			m_meshInfo.Material.SetFloat(TextureUVProp, 1.0f / animTextures[0].width);
			Vector2 blockUV = AnimatorInfo.GetBlockUV(0);
			m_meshInfo.Material.SetFloat(BlockWidthUVProp, blockUV.x);
			m_meshInfo.Material.SetFloat(BlockHeightUVProp, blockUV.y);
			m_meshInfo.Material.SetFloat(BlockCountProp, AnimatorInfo.GetBlockCount(0));
		}

		public override GraphicsMesh CreateGraphicMesh(Vector3 pos, Vector3 rotation, Vector3 scale)
		{
			Assert.IsTrue(CanCreate(), m_meshInfo.Mesh.name + " count over 1024");
			m_createCount++;
			var mesh = new GraphicsAnimatorMesh(this, pos, rotation, scale);
			mesh.Init();
			return mesh;
		}

		public void SetFrameIndex(GraphicsAnimatorMesh mesh, float frame)
		{
			if(!mesh.Visible) return;
			m_frameIndexes[mesh.Index] = frame;
			m_blockDirty = true;
		}

		protected override void OnAddMesh()
		{
			if(m_frameIndexes.Length < m_updateMeshes.Count)
			{
				int length = m_matrixes.Length + 100 > BUFFER_MAX_SIZE ? BUFFER_MAX_SIZE : m_matrixes.Length + 100;
				var temp = new float[length];
				var temp2 = new float[length];
				var temp3 = new float[length];
				for (int i = 0; i < m_frameIndexes.Length; i++)
				{
					temp[i] = m_frameIndexes[i];
					temp2[i] = m_preFrameIndexes[i];
					temp3[i] = m_transitions[i];
				}
				m_frameIndexes = temp;
				m_preFrameIndexes = temp2;
				m_transitions = temp3;
				m_block = new MaterialPropertyBlock();
			}
		}

		public void SetPreFrameIndex(GraphicsMesh mesh, float preFrame)
		{
			if(!mesh.Visible) return;
			m_preFrameIndexes[mesh.Index] = preFrame;
			m_blockDirty = true;
		}

		public void SetTransition(GraphicsMesh mesh, float transition)
		{
			if(!mesh.Visible) return;
			m_transitions[mesh.Index] = transition;
			m_blockDirty = true;
		}

		public void AddTriggerEventMesh(GraphicsAnimatorMesh mesh)
		{
			m_triggerEventMeshes.Add(mesh);
		}

		protected override void Draw(float time)
		{
			if( !m_meshInfo.Material ) {
				return;
			}
			foreach (var mesh in m_updateMeshes)
			{
				mesh.Update(time);
			}
			if(m_triggerEventMeshes.Count > 0)
			{
				foreach (var mesh in m_triggerEventMeshes)
				{
					mesh.TriggerAnimationEvent();
				}
				m_triggerEventMeshes.Clear();
			}
			if(m_blockDirty)
			{
				m_block.SetFloatArray(FrameIndexProp, m_frameIndexes);
				m_block.SetFloatArray(PreFrameIndexProp, m_preFrameIndexes);
				m_block.SetFloatArray(TransitionProp, m_transitions);
				m_blockDirty = false;
			}

			Graphics.DrawMeshInstanced(m_meshInfo.Mesh, 0, m_meshInfo.Material, m_matrixes, m_updateMeshes.Count, m_block);
		}
	}
}