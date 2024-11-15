// not compatible with entities graphics
Shader "asm_mr/urp_lit_warp" {
    Properties{
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
        [HideInInspector] _FaceRenderingMode("Face rendering type", Float) = 0
    }

        SubShader{
            Tags {"RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}

            Pass {
                Name "WarpForwardLit"
                Tags{"LightMode" = "UniversalForward"}

                Blend[_SourceBlend][_DestBlend]
                ZWrite[_ZWrite]
                Cull[_Cull]

                HLSLPROGRAM

                #define _NORMALMAP
                #define _CLEARCOATMAP
                //#pragma multi_compile_instancing
            #pragma enable_cbuffer
                #pragma shader_feature_local _ALPHA_CUTOUT
                #pragma shader_feature_local _DOUBLE_SIDED_NORMALS
                #pragma shader_feature_local_fragment _SPECULAR_SETUP
                #pragma shader_feature_local_fragment _ALPHAPREMULTIPLY_ON
                #pragma target 4.5
                #pragma multi_compile _ DOTS_INSTANCING_ON
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

                #pragma vertex Vertex
                #pragma fragment Fragment

                #include "warp_forward.hlsl"
                ENDHLSL
            }
            Pass{
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
            /*Pass {
                Name "ShadowCaster"
                Tags{"LightMode" = "ShadowCaster"}

                ColorMask 0
                Cull[_Cull]

                HLSLPROGRAM

                #pragma target 4.5
                #pragma multi_compile _ DOTS_INSTANCING_ON
                #pragma vertex Vertex
                #pragma fragment Fragment
                #pragma shader_feature_local _ALPHA_CUTOUT
                #pragma shader_feature_local _DOUBLE_SIDED_NORMALS

                #include "../warp_shadowcaster.hlsl"
                ENDHLSL
            }*/
        }

    //CustomEditor "NedPBRInspector"
}