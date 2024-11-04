Shader "Unlit/entitiesgraphics_test"
{
    Properties
    {
        [MainTexture] _BaseMap ("BaseMap", 2D) = "white" {}
        _Color("Color", Color) = (1, 1, 1, 1)
        _CBrightness("cbrightness", Color) = (1, 1, 1, 1)
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

            //#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
             
            //CBUFFER_START(UnityPerMaterial)
            //float4 _Color;
            //float4 _BaseMap_ST;
            //CBUFFER_END 

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
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;

#if UNITY_ANY_INSTANCING_ENABLED
                uint instanceID : CUSTOM_INSTANCE_ID;
#endif
                //UNITY_FOG_COORDS(1)
            };

#ifdef DOTS_INSTANCING_ON
            UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
                UNITY_DOTS_INSTANCED_PROP(float4, _Color)
                UNITY_DOTS_INSTANCED_PROP(float4, _CBrightness)
            UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)
#define _Color UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _Color)
#endif

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            //CBUFFER_START(UnityPerMaterial)
            //CBUFFER_END

            Varyings vert (Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                //o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.uv = v.uv;
                return o;
            }
            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                //uint rawMetadataValue = UNITY_DOTS_INSTANCED_METADATA_NAME(float4, _CBrightness);
                ///float4 c0 = UNITY_ACCESS_DOTS_INSTANCED_PROP(float4, _CBrightness);
                half4 col = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * float4(1,1,0,1);

                return col;
            }
            ENDHLSL
        }
    }
}

//https://discussions.unity.com/t/texture-array-shader-missing-dots_instancing_on-variant/909146/12