#include "UnityCG.cginc"
//#include "AutoLight.cginc"
//#include "Lighting.cginc"
//#include "PBRLib.cginc"
//#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

// Register our programmable stage functions
#pragma vertex Vertex
#pragma fragment Fragment
#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
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

float4 _WarpParams;

struct Attributes {
    float3 positionOS : POSITION; // Position in object space
    float2 uv : TEXCOORD0; // Material texture UVs
    float3 normal : NORMAL;
    float4 tangent: TANGENT;
};

          
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
    float4 shadowCoords: TEXCOORD7;
};
float4 Fragment(Interpolators i) : SV_TARGET
{
    return 0.0;
}

Interpolators Vertex(Attributes v) {

    Interpolators o;
    o.uv = v.uv;
    float4 wpos = mul(unity_ObjectToWorld, float4(v.positionOS, 1.0));

    float4 undistorted_cspos = mul(UNITY_MATRIX_VP, wpos);
    //float3 from_cam = wpos.xyz - _WorldSpaceCameraPos;
    //float3 from_cam_dir = normalize(from_cam);
    //float mag = distance(tocam, 0.0);
    float3 cam_forward = mul((float3x3)unity_CameraToWorld, float3(0, 0, 1));
    float3 distort_origin = _WorldSpaceCameraPos + cam_forward * _WarpParams.z;
    float3 distort_dir = distort_origin - wpos.xyz;
    float distort_dist = distance(distort_dir, 0.0);
    distort_dir /= distort_dist;
    //wpos.xyz = distort_origin + distort_dir * distort_dist * (1 - distance(undistorted_cspos.xy, float2(0.5, 0.5)));
    wpos.xyz = wpos.xyz + distort_dir * pow(distance(undistorted_cspos.y, _WarpParams.y), 2) * _WarpParams.w;

    o.worldPos = wpos.xyz;
    o.positionCS = mul(UNITY_MATRIX_VP, wpos);

    //o.positionCS = mul(UNITY_MATRIX_MVP, float4(v.positionOS, 1.0));
    //o.positionCS = UnityObjectToClipPos(v.positionOS);

    // Normal mapping parameters
    o.tangent = normalize(mul(unity_ObjectToWorld, v.tangent).xyz);
    o.normal = normalize(UnityObjectToWorldNormal(v.normal));
    o.bitangent = normalize(cross(o.normal, o.tangent.xyz));

    o.tangentLocal = v.tangent;
    o.bitangentLocal = normalize(cross(v.normal, o.tangentLocal));
     
    //VertexPositionInputs positions = GetVertexPositionInputs(v.positionOS.xyz);
    //o.shadowCoords = TransformWorldToShadowCoord(v.positionOS.xyz);
    return o;
}
