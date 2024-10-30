Shader "asm/asm_urp_lit" {
    // Properties are options set per material, exposed by the material inspector
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
    }
        // Subshaders allow for different behaviour and options for different pipelines and platforms
            SubShader{
            // These tags are shared by all passes in this sub shader
            Tags{"RenderPipeline" = "UniversalPipeline"}

            // Shaders can have several passes which are used to render different data about the material
            // Each pass has it's own vertex and fragment function and shader variant keywords
            Pass {
                Name "ForwardLit" // For debugging
                Tags{"LightMode" = "UniversalForward"} // Pass specific tags. 
            // "UniversalForward" tells Unity this is the main lighting pass of this shader

            HLSLPROGRAM // Begin HLSL code
            // Register our programmable stage functions
            #pragma vertex Vertex
            #pragma fragment Fragment

            //#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "UnityCG.cginc"
        #include "AutoLight.cginc"
        #include "Lighting.cginc"
        #include "PBRLib.cginc"
            sampler2D _MainTex;
            sampler2D _NormalMap;
            sampler2D _MetalnessMap;
            sampler2D _RoughnessMap;
            sampler2D _OcclusionMap;

            float3 _AlbedoColor;
            float3 _FresnelColor;

            float _Roughness;
            float _Metalness;
            float _Anisotropy;

            // This attributes struct receives data about the mesh we're currently rendering
            // Data is automatically placed in fields according to their semantic
            struct Attributes {
                float3 positionOS : POSITION; // Position in object space
                float2 uv : TEXCOORD0; // Material texture UVs
                float3 normal : NORMAL;
                float4 tangent: TANGENT;
            };

            // This struct is output by the vertex function and input to the fragment function.
            // Note that fields will be transformed by the intermediary rasterization stage
            struct Interpolators {
                // This value should contain the position in clip space (which is similar to a position on screen)
                // when output from the vertex function. It will be transformed into pixel position of the current
                // fragment on the screen when read from the fragment function
                float4 positionCS : SV_POSITION;

                // The following variables will retain their values from the vertex stage, except the
                // rasterizer will interpolate them between vertices
                float2 uv : TEXCOORD0; // Material texture UVs

                float3 normal : TEXCOORD1;
                float3 tangent: TEXCOORD2;
                float3 bitangent: TEXCOORD3;
                float3 worldPos : TEXCOORD4;

                float3 tangentLocal: TEXCOORD5;
                float3 bitangentLocal: TEXCOORD6;
            };

            // The vertex function. This runs for each vertex on the mesh.
            // It must output the position on the screen each vertex should appear at,
            // as well as any data the fragment function will need
            Interpolators Vertex(Attributes v) {

                Interpolators o;
                o.uv = v.uv;
                o.positionCS = UnityObjectToClipPos(v.positionOS);
                o.worldPos = mul(unity_ObjectToWorld, v.positionOS);

                // Normal mapping parameters
                o.tangent = normalize(mul(unity_ObjectToWorld, v.tangent).xyz);
                o.normal = normalize(UnityObjectToWorldNormal(v.normal));
                o.bitangent = normalize(cross(o.normal, o.tangent.xyz));

                o.tangentLocal = v.tangent;
                o.bitangentLocal = normalize(cross(v.normal, o.tangentLocal));
                return o;

                //Interpolators output;
                //output.normal = float3(0.0, 0.0, 0.0);
                //output.tangent = float3(0.0, 0.0, 0.0);
                //output.bitangent = float3(0.0, 0.0, 0.0);
                //output.worldPos = float3(0.0, 0.0, 0.0);
                //output.tangentLocal = float3(0.0, 0.0, 0.0);
                //output.bitangentLocal = float3(0.0, 0.0, 0.0);
                //// These helper functions, found in URP/ShaderLib/ShaderVariablesFunctions.hlsl
                //// transform object space values into world and clip space
                //VertexPositionInputs posnInputs = GetVertexPositionInputs(input.positionOS);

                //// Pass position and orientation data to the fragment function
                //output.positionCS = posnInputs.positionCS;
                //output.uv = input.uv;

                //return output;
            }

            // The fragment function. This runs once per fragment, which you can think of as a pixel on the screen
            // It must output the final color of this pixel
            float4 Fragment(Interpolators i) : SV_TARGET {
                if (_WorldSpaceLightPos0.w == 1)
                return float4(0.0, 0.0, 0.0, 0.0);

            // Just for mapping the 2d texture onto a sphere
            float2 uv = i.uv;

            // VECTORS

            // Assuming this pass goes only for directional lights
            float3 lightVec = normalize(_WorldSpaceLightPos0.xyz);

            float3 viewVec = normalize(_WorldSpaceCameraPos.xyz - i.worldPos);
            float3 halfVec = normalize(lightVec + viewVec);

            // Calculate the tangent matrix if normal mapping is applied
            float3x3 tangentMatrix = transpose(float3x3(i.tangent, i.bitangent, i.normal));
            float3 normal = mul(tangentMatrix, tex2D(_NormalMap, uv).xyz * 2 - 1);

            float3 reflectVec = -reflect(viewVec, normal);

            // DOT PRODUCTS
            float NdotL = max(dot(i.normal, lightVec), 0.0);
            float NdotH = max(dot(i.normal, halfVec), 0.0);
            float HdotV = max(dot(halfVec, viewVec), 0.0);
            float NdotV = max(dot(i.normal, viewVec), 0.0);
            float HdotT = dot(halfVec, i.tangentLocal);
            float HdotB = dot(halfVec, i.bitangentLocal);

            // TEXTURE SAMPLES
            float3 albedo = sRGB2Lin(tex2D(_MainTex, uv));

            // PBR PARAMETERS

            // This assumes that the maximum param is right if both are supplied (range and map)
            float roughness = saturate(max(_Roughness + EPS, tex2D(_RoughnessMap, uv)).r);
            float metalness = saturate(max(_Metalness + EPS, tex2D(_MetalnessMap, uv)).r);
            float occlusion = saturate(tex2D(_OcclusionMap, uv).r);

            float3 F0 = lerp(float3(0.04, 0.04, 0.04), _FresnelColor * albedo, metalness);

            float D = trowbridgeReitzNDF(NdotH, roughness);
            D = trowbridgeReitzAnisotropicNDF(NdotH, roughness, _Anisotropy, HdotT, HdotB);
            float3 F = fresnel(F0, NdotV, roughness);
            float G = schlickBeckmannGAF(NdotV, roughness) * schlickBeckmannGAF(NdotL, roughness);

            // DIRECT LIGHTING

            // Normals from normal map
            float lambertDirect = max(dot(normal, lightVec), 0.0);

            float3 directRadiance = _LightColor0.rgb * occlusion;

            // INDIRECT LIGHTING
            float3 diffuseIrradiance = sRGB2Lin(UNITY_SAMPLE_TEXCUBE_LOD(unity_SpecCube0, normal, UNITY_SPECCUBE_LOD_STEPS).rgb) * occlusion;
            float3 specularIrradiance = sRGB2Lin(UNITY_SAMPLE_TEXCUBE_LOD(unity_SpecCube0, reflectVec, roughness * UNITY_SPECCUBE_LOD_STEPS).rgb) * occlusion;

            // DIFFUSE COMPONENT
            float3 diffuseDirectTerm = lambertDiffuse(albedo) * (1 - F) * (1 - metalness) * _AlbedoColor;

            // SPECULAR COMPONENT
            float3 specularDirectTerm = G * D * F / (4 * NdotV * NdotL + EPS);

            // DIRECT BRDF OUTPUT
            float3 brdfDirectOutput = (diffuseDirectTerm + specularDirectTerm) * lambertDirect * directRadiance;

            // Add constant ambient (to boost the lighting, only temporary)
            float3 ambientDiffuse = diffuseIrradiance * lambertDiffuse(albedo) * (1 - F) * (1 - metalness);

            // For now the ambient specular looks quite okay, but it isn't physically correct
            // TODO: try importance sampling the NDF from the environment map (just for testing & performance measuring)
            // TODO: implement the split-sum approximation (UE4 paper)
            float3 ambientSpecular = specularIrradiance * F;

            return float4(gammaCorrection(brdfDirectOutput + ambientDiffuse + ambientSpecular), 1.0);
            //return float4(ambientSpecular, 1.0);
            //return float4(1.0, 1.0, 1.0, 1.0);
            }
            ENDHLSL
        }
        }
}

// MIT License

// Copyright (c) 2023 Ned