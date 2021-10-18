#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

// 顶点着色器的输入
struct Attributes
{
    float3 positionOS   : POSITION;
    float2 uv           : TEXCOORD0;
    half4 uv2           : TEXCOORD2;
    half4 color         : COLOR;
    half3 normal        : NORMAL;
    half3 tangent       : TANGENT;
    uint vertexId       : SV_VertexID;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

// 顶点着色器的输出
struct Varyings
{
    float4 vertex   : SV_POSITION;
    float2 uv       : TEXCOORD0;

    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

half4x4 loadMatFromTexture(uint frameIndex, uint boneIndex, float uv, float blockWidthUV, float blockHeightUV, int blockCount)
{
    float2 uvFrame;
    uint y = frameIndex / blockCount;
    uvFrame.x = (frameIndex - blockCount * y) * blockWidthUV;
    uvFrame.y = y * blockHeightUV + boneIndex * uv;
    half4 c1 = SAMPLE_TEXTURE2D_LOD(_boneTexture, sampler_boneTexture, uvFrame, 0);
    uvFrame.x = uvFrame.x + uv;
    half4 c2 = SAMPLE_TEXTURE2D_LOD(_boneTexture, sampler_boneTexture, uvFrame, 0);
    uvFrame.x = uvFrame.x + uv;
    half4 c3 = SAMPLE_TEXTURE2D_LOD(_boneTexture, sampler_boneTexture, uvFrame, 0);
    uvFrame.x = uvFrame.x + uv;
    half4 c4 = SAMPLE_TEXTURE2D_LOD(_boneTexture, sampler_boneTexture, uvFrame, 0);
    half4x4 m;
    m._11_21_31_41 = c1;
    m._12_22_32_42 = c2;
    m._13_23_33_43 = c3;
    m._14_24_34_44 = c4;
    return m;
}

half4 skinning(Attributes v)
{
    float4 pos = float4(v.positionOS, 1);

    half4 w = v.color;
    half4 bone = half4(v.uv2.x, v.uv2.y, v.uv2.z, v.uv2.w);

    float curFrame = UNITY_ACCESS_INSTANCED_PROP(Props, _frameIndex);
    float preAniFrame = UNITY_ACCESS_INSTANCED_PROP(Props, _preFrameIndex);
    float progress = UNITY_ACCESS_INSTANCED_PROP(Props, _transitionProgress);

    /*int preFrame = curFrame;
    int nextFrame = curFrame + 1.0f;*/

    half4x4 localToWorldMatrixPre = loadMatFromTexture(curFrame, bone.x, _textureUV, _blockWidthUV, _blockHeightUV, _blockCount) * w.x;
    localToWorldMatrixPre += loadMatFromTexture(curFrame, bone.y, _textureUV, _blockWidthUV, _blockHeightUV, _blockCount) * max(0, w.y);
    localToWorldMatrixPre += loadMatFromTexture(curFrame, bone.z, _textureUV, _blockWidthUV, _blockHeightUV, _blockCount) * max(0, w.z);
    localToWorldMatrixPre += loadMatFromTexture(curFrame, bone.w, _textureUV, _blockWidthUV, _blockHeightUV, _blockCount) * max(0, w.w);

    /*half4x4 localToWorldMatrixNext = loadMatFromTexture(nextFrame, bone.x, _textureUV, _blockWidthUV, _blockHeightUV, _blockCount) * w.x;
    localToWorldMatrixNext += loadMatFromTexture(nextFrame, bone.y, _textureUV, _blockWidthUV, _blockHeightUV, _blockCount) * max(0, w.y);
    localToWorldMatrixNext += loadMatFromTexture(nextFrame, bone.z, _textureUV, _blockWidthUV, _blockHeightUV, _blockCount) * max(0, w.z);
    localToWorldMatrixNext += loadMatFromTexture(nextFrame, bone.w, _textureUV, _blockWidthUV, _blockHeightUV, _blockCount) * max(0, w.w);*/

    half4 localPosPre = mul(pos, localToWorldMatrixPre);
    //half4 localPosNext = mul(pos, localToWorldMatrixNext);
    //half4 localPos = lerp(localPosPre, localPosNext, curFrame - preFrame);

    half4x4 localToWorldMatrixPreAni = loadMatFromTexture(preAniFrame, bone.x, _textureUV, _blockWidthUV, _blockHeightUV, _blockCount) * w.x;
    localToWorldMatrixPreAni += loadMatFromTexture(preAniFrame, bone.y, _textureUV, _blockWidthUV, _blockHeightUV, _blockCount) * max(0, w.y);
    localToWorldMatrixPreAni += loadMatFromTexture(preAniFrame, bone.z, _textureUV, _blockWidthUV, _blockHeightUV, _blockCount) * max(0, w.z);
    localToWorldMatrixPreAni += loadMatFromTexture(preAniFrame, bone.w, _textureUV, _blockWidthUV, _blockHeightUV, _blockCount) * max(0, w.w);
    half4 localPosPreAni = mul(pos, localToWorldMatrixPreAni);

    localPosPre = lerp(localPosPre, localPosPreAni, progress);


    half3 localNormPre = mul(v.normal.xyz, (float3x3)localToWorldMatrixPre);
    half3 localNormNext = mul(v.normal.xyz, (float3x3)localToWorldMatrixPreAni);
    v.normal = normalize(lerp(localNormPre, localNormNext, progress));
    half3 localTanPre = mul(v.tangent.xyz, (float3x3)localToWorldMatrixPre);
    half3 localTanNext = mul(v.tangent.xyz, (float3x3)localToWorldMatrixPreAni);
    v.tangent.xyz = normalize(lerp(localTanPre, localTanNext, progress));


    return localPosPre;
}

// 顶点着色器
Varyings skinningVert(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    half4 pos = skinning(input);

    VertexPositionInputs vertexInput = GetVertexPositionInputs(pos.xyz);
    output.vertex = vertexInput.positionCS;
    output.uv = TRANSFORM_TEX(input.uv, _MainTex);
    return output;
}

// 片段着色器
half4 skinningFrag(Varyings input) : SV_TARGET 
{    
    UNITY_SETUP_INSTANCE_ID(input);

    half4 mainTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
    return _Color * mainTex + _AddColor * mainTex.w;
}