// warp pbr shadow multilights entities
Shader "asm/warp_pbr_shadow" {
    Properties
    { 
        [Header(Surface options)]
        [MainTexture] _ColorMap("Color", 2D) = "white" {}
        [MainColor] _ColorTint("Tint", Color) = (1, 1, 1, 1)
        _Cutoff("Alpha cutout threshold", Range(0, 1)) = 0.5
        [NoScaleOffset][Normal] _NormalMap("Normal", 2D) = "bump" {}
        _NormalStrength("Normal strength", Range(0, 1)) = 1
        [NoScaleOffset] _MetalnessMask("Metalness mask", 2D) = "white" {}
        _Metalness("Metalness strength", Range(0, 1)) = 0
        [Toggle(_SPECULAR_SETUP)] _SpecularSetupToggle("Use specular workflow", Float) = 0
        [NoScaleOffset] _SpecularMap("Specular map", 2D) = "white" {}
        _SpecularTint("Specular tint", Color) = (1, 1, 1, 1)
        [NoScaleOffset] _SmoothnessMask("Smoothness mask", 2D) = "white" {}
        _Smoothness("Smoothness multiplier", Range(0, 1)) = 0.5
        [NoScaleOffset] _EmissionMap("Emission map", 2D) = "white" {}
        [HDR] _EmissionTint("Emission tint", Color) = (0, 0, 0, 0)
        [NoScaleOffset] _ParallaxMap("Height/displacement map", 2D) = "white" {}
        _ParallaxStrength("Parallax strength", Range(0, 1)) = 0.005
        [NoScaleOffset] _ClearCoatMask("Clear coat mask", 2D) = "white" {}
        _ClearCoatStrength("Clear coat strength", Range(0, 1)) = 0
        [NoScaleOffset] _ClearCoatSmoothnessMask("Clear coat smoothness mask", 2D) = "white" {}
        _ClearCoatSmoothness("Clear coat smoothness", Range(0, 1)) = 0

        _WarpParams("Warp Parameters", Vector) = (0.0, 0.0, 1000.0, 0.01)

        _ASMLight0("ASMLight0", Vector) = (0.0, 0.0, 0.0, 0.0)
        _ASMLight1("ASMLight1", Vector) = (0.0, 0.0, 0.0, 0.0)
        _ASMLight2("ASMLight2", Vector) = (0.0, 0.0, 0.0, 0.0)
        _ASMLight3("ASMLight3", Vector) = (0.0, 0.0, 0.0, 0.0)

        [HideInInspector] _Cull("Cull mode", Float) = 2 // 2 is "Back"
        [HideInInspector] _SourceBlend("Source blend", Float) = 0
        [HideInInspector] _DestBlend("Destination blend", Float) = 0
        [HideInInspector] _ZWrite("ZWrite", Float) = 0
        [HideInInspector] _SurfaceType("Surface type", Float) = 0
        [HideInInspector] _BlendType("Blend type", Float) = 0
         _FaceRenderingMode("Face rendering type", Float) = 0
    }

    SubShader
    {
        Tags {"RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}

        Pass 
        {
            Name "WarpPBRShadow"
            Tags{"LightMode" = "UniversalForward"}

            //Blend[_SourceBlend][_DestBlend]
            //ZWrite[_ZWrite]
            //Cull[_Cull]

            HLSLPROGRAM


            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma target 4.5
            #pragma multi_compile _ DOTS_INSTANCING_ON
            #pragma multi_compile_instancing
            #define _NORMALMAP
            #define _CLEARCOATMAP
            //#pragma multi_compile_instancing
            //#pragma enable_cbuffer
            #pragma shader_feature_local _ALPHA_CUTOUT
            #pragma shader_feature_local _DOUBLE_SIDED_NORMALS
            #pragma shader_feature_local_fragment _SPECULAR_SETUP
            #pragma shader_feature_local_fragment _ALPHAPREMULTIPLY_ON
            
#if UNITY_VERSION >= 202120
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
#else
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
#endif
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
#if UNITY_VERSION >= 202120
            #pragma multi_compile_fragment _ DEBUG_DISPLAY
#endif


            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ParallaxMapping.hlsl"

            struct Attributes {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            #if UNITY_ANY_INSTANCING_ENABLED
                uint instanceID : INSTANCEID_SEMANTIC;
            #endif
            };


            struct Interpolators {
                float4 positionCS : SV_POSITION;

                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float4 tangentWS : TEXCOORD3;
                float4 o_positionWS: TEXCOORD4;
            #if UNITY_ANY_INSTANCING_ENABLED
                uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _ColorMap_ST;
                float4 _ColorTint;
                float _Cutoff;
                float _NormalStrength;
                float _Metalness;
                float3 _SpecularTint;
                float _Smoothness;
                float3 _EmissionTint;
                float _ParallaxStrength;
                float _ClearCoatStrength;
                float _ClearCoatSmoothness;

                float4 _WarpParams;
                float4 _ASMLight0;
                float4 _ASMLight1;
                float4 _ASMLight2;
                float4 _ASMLight3;
            CBUFFER_END

            #ifdef UNITY_DOTS_INSTANCING_ENABLED

            UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)

                UNITY_DOTS_INSTANCED_PROP(float4, _ColorMap_ST)
                UNITY_DOTS_INSTANCED_PROP(float4, _ColorTint)
                UNITY_DOTS_INSTANCED_PROP(float, _Cutoff)
                UNITY_DOTS_INSTANCED_PROP(float, _NormalStrength)
                UNITY_DOTS_INSTANCED_PROP(float, _Metalness)
                UNITY_DOTS_INSTANCED_PROP(float3, _SpecularTint)
                UNITY_DOTS_INSTANCED_PROP(float, _Smoothness)
                UNITY_DOTS_INSTANCED_PROP(float3, _EmissionTint)
                UNITY_DOTS_INSTANCED_PROP(float, _ParallaxStrength)
                UNITY_DOTS_INSTANCED_PROP(float, _ClearCoatStrength)
                UNITY_DOTS_INSTANCED_PROP(float, _ClearCoatSmoothness)

                UNITY_DOTS_INSTANCED_PROP(float4, _WarpParams)

                UNITY_DOTS_INSTANCED_PROP(float4, _ASMLight0)
                UNITY_DOTS_INSTANCED_PROP(float4, _ASMLight1)
                UNITY_DOTS_INSTANCED_PROP(float4, _ASMLight2)
                UNITY_DOTS_INSTANCED_PROP(float4, _ASMLight3)
            UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)

           
            #define _ColorMap_ST UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _ColorMap_ST)
            #define _ColorTint UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _ColorTint)
            #define _Cutoff UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _Cutoff)
            #define _NormalStrength UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _NormalStrength)
            #define _Metalness UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _Metalness)
            #define _SpecularTint UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float3, _SpecularTint)
            #define _Smoothness UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _Smoothness)
            #define _EmissionTint UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float3, _EmissionTint)
            #define _ParallaxStrength UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _ParallaxStrength)
            #define _ClearCoatStrength UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _ClearCoatStrength)
            #define _ClearCoatSmoothness UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _ClearCoatSmoothness)

            #define _WarpParams UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _WarpParams)

            #define _ASMLight0 UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _ASMLight0)
            #define _ASMLight1 UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _ASMLight1)
            #define _ASMLight2 UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _ASMLight2)
            #define _ASMLight3 UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _ASMLight3)
            #endif

            TEXTURE2D(_ColorMap); SAMPLER(sampler_ColorMap);
            
            TEXTURE2D(_NormalMap); SAMPLER(sampler_NormalMap);
            TEXTURE2D(_MetalnessMask); SAMPLER(sampler_MetalnessMask);
            TEXTURE2D(_SpecularMap); SAMPLER(sampler_SpecularMap);
            TEXTURE2D(_SmoothnessMask); SAMPLER(sampler_SmoothnessMask);
            TEXTURE2D(_EmissionMap); SAMPLER(sampler_EmissionMap);
            TEXTURE2D(_ParallaxMap); SAMPLER(sampler_ParallaxMap);
            TEXTURE2D(_ClearCoatMask); SAMPLER(sampler_ClearCoatMask);
            TEXTURE2D(_ClearCoatSmoothnessMask); SAMPLER(sampler_ClearCoatSmoothnessMask);

            #include "warp_pbr_shadow_forward.hlsl"
//#include "warp_core.hlsl"
            //Interpolators Vertex(Attributes input) {
            //    Interpolators output;

            //    UNITY_SETUP_INSTANCE_ID(input);
            //    UNITY_TRANSFER_INSTANCE_ID(input, output);

            //    Distorted_vertex dv = warp_vertex(input.positionOS.xyz, _WarpParams);

            //    output.positionWS = dv.positionWS;
            //    output.positionCS = dv.positionCS;
            //    output.o_positionWS = dv.o_positionWS;

            //    VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

            //    //VertexPositionInputs posnInputs = GetVertexPositionInputs(input.positionOS);
            //    //output.positionCS = posnInputs.positionCS;
            //    //output.positionWS = posnInputs.positionWS;
            //   
            //    //output.uv = TRANSFORM_TEX(input.uv, _ColorMap);
            //    output.uv = input.uv;
            //    output.normalWS = normInputs.normalWS;
            //    output.tangentWS = float4(normInputs.tangentWS, input.tangentOS.w);

            //    return output;
            //}
            //float4 Fragment(Interpolators input) :SV_TARGET
            //{
            //    UNITY_SETUP_INSTANCE_ID(input);
            //    return float4(1,1,1,1);
            //}
            ENDHLSL
        }
         
        Pass {
            Name "ShadowCaster"
            Tags{"LightMode" = "ShadowCaster"}

            ColorMask 0
            Cull[_Cull] 

            HLSLPROGRAM
            #pragma target 4.5
            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }
    }

    //CustomEditor "NedPBRInspector"
}