// warp unlit entities
Shader "asm/warp_unlit"
{
    Properties
    {
        [MainTexture] _BaseMap ("BaseMap", 2D) = "white" {}
        _Color("Color", Color) = (1, 1, 1, 1)
        _WarpParams("Warp Parameters", Vector) = (0.0, 0.0, 1000.0, 0.01)
        //_CBrightness("cbrightness", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5
            #pragma multi_compile _ DOTS_INSTANCING_ON
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
            #include "warp_core.hlsl"
            //#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
             
            
            struct Attributes
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
#if UNITY_ANY_INSTANCING_ENABLED
                uint instanceID : INSTANCEID_SEMANTIC;
#endif
            };


            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float4 o_positionWS: TEXCOORD2;
#if UNITY_ANY_INSTANCING_ENABLED
                uint instanceID : CUSTOM_INSTANCE_ID;
#endif
                //UNITY_FOG_COORDS(1)
            };
            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _WarpParams;
            CBUFFER_END
                 
#ifdef UNITY_DOTS_INSTANCING_ENABLED
            UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
                UNITY_DOTS_INSTANCED_PROP(float4, _Color)
            UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)
#define _Color UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _Color)
//#define _Color UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _WarpParams)
#endif

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            Varyings vert (Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                Distorted_vertex dv = warp_vertex(v.vertex.xyz, _WarpParams);
                
                o.positionWS = dv.positionWS;
                o.positionCS = dv.positionCS;
                o.o_positionWS = dv.o_positionWS;

                //o.vertex = TransformObjectToHClip(v.vertex.xyz);
                //o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.uv = v.uv;
                return o;
            }
            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                //uint rawMetadataValue = UNITY_DOTS_INSTANCED_METADATA_NAME(float4, _Color);
                //float4 c0 = UNITY_ACCESS_DOTS_INSTANCED_PROP(float4, _Color);
                //half4 col = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * c0;
                half4 col = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _Color;

                return col;
            }
            ENDHLSL
        }
            //Pass{
            //Name "ShadowCaster"
            //Tags{"LightMode" = "ShadowCaster"}

            //ColorMask 0
            //Cull[_Cull]

            //HLSLPROGRAM
            //#pragma target 4.5
            //#pragma multi_compile_instancing
            //#pragma multi_compile _ DOTS_INSTANCING_ON
            //#pragma vertex ShadowPassVertex
            //#pragma fragment ShadowPassFragment

            //#include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            //#include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            //ENDHLSL
            //}

    }
            
}
//https://discussions.unity.com/t/trying-to-render-a-batchrenderergroup-or-entities-graphics-batch-with-wrong-cbuffer-setup/1525054
//https://discussions.unity.com/t/texture-array-shader-missing-dots_instancing_on-variant/909146/12