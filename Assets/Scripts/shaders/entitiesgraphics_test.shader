Shader "Unlit/entitiesgraphics_test"
{
    Properties
    {
        [MainTexture] _BaseMap ("BaseMap", 2D) = "white" {}
        _Color("Color", Vector) = (1, 1, 1, 1)
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
            // make fog work

            #pragma target 4.5
            #pragma multi_compile _ DOTS_INSTANCING_ON
            #pragma enable_cbuffer


            //#pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            //UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
            //UNITY_DOTS_INSTANCED_PROP(float4, _Color)
            //UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)

            CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            CBUFFER_END 

            struct Attributes
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                //UNITY_FOG_COORDS(1)
            };
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            CBUFFER_END

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);

                //o.vertex = UnityObjectToClipPos(v.vertex);
                //o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                //UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }
             
            half4 frag (Varyings  i) : SV_Target
            {
                // sample the texture
                half4 col = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);
                // apply fog
                //UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDHLSL
        }
    }
}
