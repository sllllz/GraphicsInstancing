using UnityEngine;
using System;

namespace Extend.GraphicsInstancing
{
	[Serializable]
	public class AnimationEvent
	{
		public string Function;
		public string StringParameter;
		public float Time;
	}

	[Serializable]
	public class AnimationTexture
	{
		public int Width;
		public int Height;
		public byte[] TexBytes;
		private Texture2D m_texture;
		public Texture2D GetTexture()
		{
			if(m_texture == null && TexBytes != null)
			{
				m_texture = new Texture2D(Width, Height, TextureFormat.RGBAHalf, false);
				m_texture.LoadRawTextureData(TexBytes);
				m_texture.filterMode = FilterMode.Point;
				m_texture.minimumMipmapLevel = 0;
				m_texture.Apply();
				// TexBytes = null;
			}

			return m_texture;
		}
	}

	[Serializable]
	public class AnimationInfo
	{
		public string Name;
		public int NameHash;
		public int TotalFrame;
		public int Fps;
		public int AnimationIndex;
		public int TextureIndex;
		public WrapMode Mode;
		public AnimationEvent[] Events;
	}

	public class GraphicsMeshInfo : ScriptableObject
	{
		public Mesh Mesh;
		public Material Material;
		public GraphicsAnimatorInfo AnimatorInfo;
	}
}
