#ifndef UNIVERSAL_UNLIT_INPUT_INCLUDED
#define UNIVERSAL_UNLIT_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

TEXTURE2D(_boneTexture);
float4 _boneTexture_ST;
SAMPLER(sampler_boneTexture);

UNITY_INSTANCING_BUFFER_START(Props)
    UNITY_DEFINE_INSTANCED_PROP(int, _frameIndex)
    UNITY_DEFINE_INSTANCED_PROP(int, _preFrameIndex)
    UNITY_DEFINE_INSTANCED_PROP(float, _transitionProgress)
UNITY_INSTANCING_BUFFER_END(Props)

TEXTURE2D(_MainTex);
float4 _MainTex_ST;
SAMPLER(sampler_MainTex);

CBUFFER_START(UnityPerMaterial)
    half4 _Color;
    half4 _AddColor;
    float _textureUV;
    float _blockWidthUV;
    float _blockHeightUV;
    int _blockCount;
CBUFFER_END

#endif