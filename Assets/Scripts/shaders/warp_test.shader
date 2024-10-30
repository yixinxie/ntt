Shader "asm/urp_lit_warp" {
    Properties{
       _MainTex("Albedo Map", 2D) = "white" {}
        _NormalMap("Normal Map", 2D) = "bump" {}
        _MetalnessMap("Metalness Map", 2D) = "black" {}
        _RoughnessMap("Roughness Map", 2D) = "black" {}
        _OcclusionMap("Occlusion Map", 2D) = "white" {}

        _AlbedoColor("Albedo Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _FresnelColor("Fresnel Color (F0)", Color) = (1.0, 1.0, 1.0, 1.0)

        _Roughness("Roughness", Range(0,1)) = 0
        _Metalness("Metalness", Range(0,1)) = 0
        _Anisotropy("Anisotropy", Range(0,1)) = 0
        _WarpParams("Warp Parameters", Vector) = (1.0, 1.0, 1.0, 1.0)
    }
    // Subshaders allow for different behaviour and options for different pipelines and platforms
    SubShader
    {
            // These tags are shared by all passes in this sub shader
        Tags{"RenderPipeline" = "UniversalPipeline"}

        // Shaders can have several passes which are used to render different data about the material
        // Each pass has it's own vertex and fragment function and shader variant keywords
        Pass 
        {
            Name "WarpForwardLit" // For debugging
            Tags{"LightMode" = "UniversalForward"} // Pass specific tags. 
            // "UniversalForward" tells Unity this is the main lighting pass of this shader

            HLSLPROGRAM // Begin HLSL code
            // Register our programmable stage functions
            #pragma shader_feature _NORMALMAP
            #pragma shader_feature _ALPHATEST_ON
            #pragma shader_feature _ALPHAPREMULTIPLY_ON
            #pragma shader_feature _EMISSION
            //#pragma shader_feature _METALLICSPECGLOSSMAP
            //#pragma shader_feature _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            //#pragma shader_feature _OCCLUSIONMAP

            //#pragma shader_feature _SPECULARHIGHLIGHTS_OFF
            //#pragma shader_feature _ENVIRONMENTREFLECTIONS_OFF
            ////#pragma shader_feature _SPECULAR_SETUP
            //#pragma vertex vert
            //#pragma fragment frag
            
            // URP Keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE

            // Unity defined keywords
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fog

            // Some added includes, required to use the Lighting functions
            #include "warp_forward.hlsl"
           
            ENDHLSL
        }
        Pass
        {

            // The shadow caster pass, which draws to shadow maps
            Name "ShadowCaster"
            Tags{"LightMode" = "ShadowCaster"}

            ColorMask 0 // No color output, only depth

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment

            #include "warp_shadercaster.hlsl"
            ENDHLSL


        }
    }
}

// MIT License

// Copyright (c) 2023 Ned