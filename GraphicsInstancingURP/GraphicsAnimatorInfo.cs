using UnityEngine;
using System;

namespace Extend.GraphicsInstancing
{
	public class GraphicsAnimatorInfo : ScriptableObject
	{
		public int BlockWidth;
		public int BlockHeight;
		public int DefaultAnimHash;
		public AnimationInfo[] AnimationInfos;
		public AnimationTexture[] AnimationTextures;

		public float GetBlockCount(int textureIndex)
		{
			if(AnimationTextures.Length > textureIndex)
			{
				return (float)AnimationTextures[textureIndex].Width / BlockWidth;
			}
			return 0;
		}
		public Vector2 GetBlockUV(int textureIndex)
		{
			Vector2 uv = Vector2.zero;
			if(AnimationTextures.Length > textureIndex)
			{
				uv.x = (float)BlockWidth / AnimationTextures[textureIndex].Width;
				uv.y = (float)BlockHeight / AnimationTextures[textureIndex].Height;
			}
			return uv;
		}
	}
}