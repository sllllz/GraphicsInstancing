Shader "Universal Render Pipeline/GraphicsSkinMesh"
{
    Properties
    {
        _Color("Color(RGB)",Color) = (1,1,1,1)
        _AddColor("AddColor",Color) = (0,0,0,0)
        _MainTex("MainTex", 2D) = "white"{}
        
        [HideInInspector] _textureUV("TextureUV", float) = 0
        [HideInInspector] _blockWidthUV("BlockWidthUV", float) = 0
        [HideInInspector] _blockHeightUV("BlockHeightUV", float) = 0
        [HideInInspector] _blockCount("BlockCount", int) = 0

        [PerRendererData] _boneTexture("BoneTexture", 2D) = "white"{}
		[PerRendererData] _frameIndex ("FrameIndex", int) = 0
		[PerRendererData] _preFrameIndex ("PreFrameIndex", int) = 0
		[PerRendererData] _transitionProgress ("TransitionProgress", float) = 0
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Opaque"
            "Queue"="Geometry+0"
        }
        
        Pass
        {
            Name "Pass"
            Blend One Zero, One Zero
            Cull Back
            ZTest LEqual
            ZWrite On
            HLSLPROGRAM

            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 3.5
            #pragma multi_compile_instancing
            #pragma vertex skinningVert
            #pragma fragment skinningFrag
            
            // Includes
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #include "GraphicsSkinMesh-URP-Input.hlsl"
            #include "GraphicsSkinMesh-URP-Pass.hlsl"
            
            ENDHLSL
        }
    }
    FallBack "Hidden/Shader Graph/FallbackError"
}