
#ifndef MY_LIT_FORWARD_LIT_PASS_INCLUDED
#define MY_LIT_FORWARD_LIT_PASS_INCLUDED

#include "warp_common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ParallaxMapping.hlsl"
//CBUFFER_START(UnityPerMaterial)
//float4 _ColorMap_ST;
//float4 _ColorTint;
//float _Cutoff;
//float _NormalStrength;
//float _Metalness;
//float3 _SpecularTint;
//float _Smoothness;
//float3 _EmissionTint;
//float _ParallaxStrength;
//float _ClearCoatStrength;
//float _ClearCoatSmoothness;

float4 _WarpParams;
float4 _ASMLight0;
float4 _ASMLight1;
float4 _ASMLight2;
float4 _ASMLight3;
//CBUFFER_END

//UNITY_INSTANCING_BUFFER_START(Props)
//UNITY_DEFINE_INSTANCED_PROP(float4, _ASMLight0)
//UNITY_DEFINE_INSTANCED_PROP(float4, _ASMLight1)
//UNITY_DEFINE_INSTANCED_PROP(float4, _ASMLight2)
//UNITY_DEFINE_INSTANCED_PROP(float4, _ASMLight3)
//UNITY_INSTANCING_BUFFER_END(Props)




struct Attributes {
	float3 positionOS : POSITION;
	float3 normalOS : NORMAL;
	float4 tangentOS : TANGENT;
	float2 uv : TEXCOORD0;
	//UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Interpolators {
	float4 positionCS : SV_POSITION;

	float2 uv : TEXCOORD0;
	float3 positionWS : TEXCOORD1;
	float3 normalWS : TEXCOORD2;
	float4 tangentWS : TEXCOORD3;
	float4 o_positionWS: TEXCOORD4;
	//UNITY_VERTEX_INPUT_INSTANCE_ID
};
half3 additional_asmlights(InputData inputData, float4 asmlight, BRDFData brdfData, BRDFData brdfDataClearCoat, half clearCoatMask, bool specularHighlightsOff)
{
	float distance2light = distance(inputData.positionWS, asmlight.xyz);
	float intensity_after_falloff = 1 / distance2light * asmlight.w;
	float3 light_dir = normalize(asmlight.xyz - inputData.positionWS);
	return LightingPhysicallyBased(brdfData, brdfDataClearCoat, half3(intensity_after_falloff, intensity_after_falloff, intensity_after_falloff), light_dir, 1,
		inputData.normalWS, inputData.viewDirectionWS,
		clearCoatMask, specularHighlightsOff);
}
Interpolators Vertex(Attributes input) {
	Interpolators output;

	/*UNITY_SETUP_INSTANCE_ID(input);
	UNITY_TRANSFER_INSTANCE_ID(input, output);*/

	// Found in URP/ShaderLib/ShaderVariablesFunctions.hlsl
	VertexPositionInputs posnInputs = GetVertexPositionInputs(input.positionOS);
	VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);
	// distort begins

	float4 undistorted_wspos = mul(unity_ObjectToWorld, float4(input.positionOS, 1.0));

	float4 undistorted_cspos = posnInputs.positionCS;// mul(UNITY_MATRIX_VP, undistorted_wspos);

    float3 cam_forward = mul((float3x3)unity_CameraToWorld, float3(0, 0, 1));
    float3 distort_origin = _WorldSpaceCameraPos + cam_forward * _WarpParams.z;
    float3 distort_dir = distort_origin - undistorted_wspos.xyz;
    float distort_dist = distance(distort_dir, 0.0);
    distort_dir /= distort_dist;
    //wpos.xyz = distort_origin + distort_dir * distort_dist * (1 - distance(undistorted_cspos.xy, float2(0.5, 0.5)));
	float4 distorted_wspos = undistorted_wspos;
	distorted_wspos.xyz = distorted_wspos.xyz + distort_dir * pow(distance(undistorted_cspos.xy, _WarpParams.xy), 2) * _WarpParams.w;
    //o.worldPos = wpos.xyz;
	output.positionWS = distorted_wspos.xyz;
	output.positionCS = mul(UNITY_MATRIX_VP, distorted_wspos);
	output.o_positionWS = undistorted_wspos;
	// distort ends

	//output.positionCS = posnInputs.positionCS;
	output.uv = TRANSFORM_TEX(input.uv, _ColorMap);
	output.normalWS = normInputs.normalWS;
	output.tangentWS = float4(normInputs.tangentWS, input.tangentOS.w);
	//output.positionWS = posnInputs.positionWS;

	return output;
}
half4 UniversalFragmentPBR_lights(InputData inputData, SurfaceData surfaceData, float4 asmlight0, float4 asmlight1, float4 asmlight2, float4 asmlight3)
{
#if defined(_SPECULARHIGHLIGHTS_OFF)
    bool specularHighlightsOff = true;
#else
    bool specularHighlightsOff = false;
#endif
    BRDFData brdfData;

    // NOTE: can modify "surfaceData"...
    InitializeBRDFData(surfaceData, brdfData);

#if defined(DEBUG_DISPLAY)
    half4 debugColor;

    if (CanDebugOverrideOutputColor(inputData, surfaceData, brdfData, debugColor))
    {
        return debugColor;
    }
#endif

    // Clear-coat calculation...
    BRDFData brdfDataClearCoat = CreateClearCoatBRDFData(surfaceData, brdfData);
    half4 shadowMask = CalculateShadowMask(inputData);
    AmbientOcclusionFactor aoFactor = CreateAmbientOcclusionFactor(inputData, surfaceData);
    uint meshRenderingLayers = GetMeshRenderingLayer();
    Light mainLight = GetMainLight(inputData, shadowMask, aoFactor);

    // NOTE: We don't apply AO to the GI here because it's done in the lighting calculation below...
    MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI);

    LightingData lightingData = CreateLightingData(inputData, surfaceData);

    lightingData.giColor = GlobalIllumination(brdfData, brdfDataClearCoat, surfaceData.clearCoatMask,
        inputData.bakedGI, aoFactor.indirectAmbientOcclusion, inputData.positionWS,
        inputData.normalWS, inputData.viewDirectionWS, inputData.normalizedScreenSpaceUV);
#ifdef _LIGHT_LAYERS
    if (IsMatchingLightLayer(mainLight.layerMask, meshRenderingLayers))
#endif
    {
        lightingData.mainLightColor = LightingPhysicallyBased(brdfData, brdfDataClearCoat,
            mainLight,
            inputData.normalWS, inputData.viewDirectionWS,
            surfaceData.clearCoatMask, specularHighlightsOff);
    }

	//lightingData.additionalLightsColor += asmlight0.xyz;
	/*float distance2light = distance(inputData.positionWS, asmlight0.xyz);
	float intensity_after_falloff = 1 / distance2light * asmlight0.w;
	lightingData.additionalLightsColor += LightingPhysicallyBased(brdfData, brdfDataClearCoat, half3(intensity_after_falloff, intensity_after_falloff, intensity_after_falloff), asmlight0.xyz, 6,
		                inputData.normalWS, inputData.viewDirectionWS,
		                surfaceData.clearCoatMask, specularHighlightsOff);*/

	lightingData.additionalLightsColor += additional_asmlights(inputData, asmlight0, brdfData, brdfDataClearCoat, surfaceData.clearCoatMask, specularHighlightsOff);
	lightingData.additionalLightsColor += additional_asmlights(inputData, asmlight1, brdfData, brdfDataClearCoat, surfaceData.clearCoatMask, specularHighlightsOff);
	lightingData.additionalLightsColor += additional_asmlights(inputData, asmlight2, brdfData, brdfDataClearCoat, surfaceData.clearCoatMask, specularHighlightsOff);
	lightingData.additionalLightsColor += additional_asmlights(inputData, asmlight3, brdfData, brdfDataClearCoat, surfaceData.clearCoatMask, specularHighlightsOff);
//#if defined(_ADDITIONAL_LIGHTS)
//    uint pixelLightCount = GetAdditionalLightsCount();
//#if USE_FORWARD_PLUS
//    for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
//    {
//        FORWARD_PLUS_SUBTRACTIVE_LIGHT_CHECK
//
//            Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);
//
//#ifdef _LIGHT_LAYERS
//        if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
//#endif
//        {
//            lightingData.additionalLightsColor += LightingPhysicallyBased(brdfData, brdfDataClearCoat, light,
//                inputData.normalWS, inputData.viewDirectionWS,
//                surfaceData.clearCoatMask, specularHighlightsOff);
//        }
//    }
//#endif

//    LIGHT_LOOP_BEGIN(pixelLightCount)
//        Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);
//
//#ifdef _LIGHT_LAYERS
//    if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
//#endif
//    {
//        lightingData.additionalLightsColor += LightingPhysicallyBased(brdfData, brdfDataClearCoat, light,
//            inputData.normalWS, inputData.viewDirectionWS,
//            surfaceData.clearCoatMask, specularHighlightsOff);
//    }
//    LIGHT_LOOP_END
//#endif

//#if defined(_ADDITIONAL_LIGHTS_VERTEX)
//        lightingData.vertexLightingColor += inputData.vertexLighting * brdfData.diffuse;
//#endif

#if REAL_IS_HALF
    // Clamp any half.inf+ to HALF_MAX
    return min(CalculateFinalColor(lightingData, surfaceData.alpha), HALF_MAX);
#else
    return CalculateFinalColor(lightingData, surfaceData.alpha);
#endif
}
float4 Fragment(Interpolators input
#ifdef _DOUBLE_SIDED_NORMALS
	, FRONT_FACE_TYPE frontFace : FRONT_FACE_SEMANTIC
#endif
) : SV_TARGET{

	//UNITY_SETUP_INSTANCE_ID(input);

	float3 normalWS = input.normalWS;
#ifdef _DOUBLE_SIDED_NORMALS
	normalWS *= IS_FRONT_VFACE(frontFace, 1, -1);
#endif

	float3 positionWS = input.o_positionWS.xyz;
	float3 viewDirWS = GetWorldSpaceNormalizeViewDir(positionWS); // In ShaderVariablesFunctions.hlsl
	float3 viewDirTS = GetViewDirectionTangentSpace(input.tangentWS, normalWS, viewDirWS); // In ParallaxMapping.hlsl

	float2 uv = input.uv;
	uv += ParallaxMapping(TEXTURE2D_ARGS(_ParallaxMap, sampler_ParallaxMap), viewDirTS, _ParallaxStrength, uv);

	float4 colorSample = SAMPLE_TEXTURE2D(_ColorMap, sampler_ColorMap, uv) * _ColorTint;
	TestAlphaClip(colorSample);

	float3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv), _NormalStrength);
	float3x3 tangentToWorld = CreateTangentToWorld(normalWS, input.tangentWS.xyz, input.tangentWS.w);
	normalWS = normalize(TransformTangentToWorld(normalTS, tangentToWorld));

	InputData lightingInput = (InputData)0;
	lightingInput.positionWS = positionWS;
	lightingInput.normalWS = normalWS;
	lightingInput.viewDirectionWS = viewDirWS;
	lightingInput.shadowCoord = TransformWorldToShadowCoord(positionWS);
#if UNITY_VERSION >= 202120
	lightingInput.positionCS = input.positionCS;
	lightingInput.tangentToWorld = tangentToWorld;
#endif

	SurfaceData surfaceInput = (SurfaceData)0;
	surfaceInput.albedo = colorSample.rgb;

	surfaceInput.alpha = colorSample.a;

#ifdef _SPECULAR_SETUP
	surfaceInput.specular = SAMPLE_TEXTURE2D(_SpecularMap, sampler_SpecularMap, uv).rgb * _SpecularTint;
	surfaceInput.metallic = 0;
#else
	surfaceInput.specular = 1;
	surfaceInput.metallic = SAMPLE_TEXTURE2D(_MetalnessMask, sampler_MetalnessMask, uv).r * _Metalness;
#endif
	surfaceInput.smoothness = SAMPLE_TEXTURE2D(_SmoothnessMask, sampler_SmoothnessMask, uv).r * _Smoothness;
	surfaceInput.emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, uv).rgb * _EmissionTint;
	surfaceInput.clearCoatMask = SAMPLE_TEXTURE2D(_ClearCoatMask, sampler_ClearCoatMask, uv).r * _ClearCoatStrength;
	surfaceInput.clearCoatSmoothness = SAMPLE_TEXTURE2D(_ClearCoatSmoothnessMask, sampler_ClearCoatSmoothnessMask, uv).r * _ClearCoatSmoothness;
	surfaceInput.normalTS = normalTS;

	//return UniversalFragmentPBR_lights(lightingInput, surfaceInput, 
	//	UNITY_ACCESS_INSTANCED_PROP(Props, _ASMLight0),
	//	UNITY_ACCESS_INSTANCED_PROP(Props, _ASMLight1),
	//	UNITY_ACCESS_INSTANCED_PROP(Props, _ASMLight2),
	//	UNITY_ACCESS_INSTANCED_PROP(Props, _ASMLight3));
	return UniversalFragmentPBR_lights(lightingInput, surfaceInput, _ASMLight0, _ASMLight1, _ASMLight2, _ASMLight3);
}

#endif