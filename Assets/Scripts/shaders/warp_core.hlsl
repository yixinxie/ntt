//#include "UnityCG.cginc"

struct Distorted_vertex
{
	float3 positionWS;
	float4 positionCS;
	float4 o_positionWS;
};
Distorted_vertex warp_vertex(float3 positionOS, float4 _WarpParams)
{
	Distorted_vertex output;
	// distort begins
	/*float4 undistorted_wspos = mul(unity_ObjectToWorld, float4(positionOS, 1.0));
	float4 undistorted_cspos = mul(UNITY_MATRIX_VP, undistorted_wspos);*/
	VertexPositionInputs posnInputs = GetVertexPositionInputs(positionOS);
	float4 undistorted_cspos = posnInputs.positionCS;
	float4 undistorted_wspos = float4(posnInputs.positionWS, 1);

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
	return output;
}