#if SHADERPASS != SHADERPASS_GBUFFER_EMIT
#error SHADERPASS_is_not_correctly_define
#endif


#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VertMesh.hlsl"


CBUFFER_START(EmitGBufferProperty)
    float _CurrentMaterialID;
CBUFFER_END

uniform float2 _TileSize;
//uniform float2 _ScreenSize;
StructuredBuffer<MaterialRange> _MaterialRangeBuffer;
Texture2D<uint4> _VisibilityBuffer;
//Texture2D<float> _VisibilityDepth;
//TEXTURE2D_X(_VisibilityBuffer);
TEXTURE2D_X(_VisibilityDepth);


uniform float _MaterialDepth;
ByteAddressBuffer _MaterialBuffer;

StructuredBuffer<uint> _TempMaterialBuffer;

struct Attributes
{
    uint vertexID : SV_VertexID;
    DEFAULT_UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 texcoord : TEXCOORD0;
};

struct PixelParameters
{
    float4 position;
    half4 vertexColor;
    half3 worldNormal;
    half3 worldTangent;
    
};


FragInputs GetFragInputs(Varyings input, VertexAttribute attribute, float3x3 tangentToWorld)
{
    FragInputs output;
    ZERO_INITIALIZE(FragInputs, output);
    output.tangentToWorld = k_identity3x3;
    output.positionSS = input.positionCS; // input.positionCS is SV_Position

    //#ifdef VARYINGS_NEED_POSITION_WS
    //    output.positionRWS.xyz = input.interpolators0.xyz;
    //#endif

#ifdef VARYINGS_NEED_TANGENT_TO_WORLD
        //float4 tangentWS = float4(input.interpolators2.xyz, input.interpolators2.w > 0.0 ? 1.0 : -1.0); // must not be normalized (mikkts requirement)
        //output.tangentToWorld = BuildTangentToWorld(tangentWS, input.interpolators1.xyz);
    output.tangentToWorld = tangentToWorld;
#endif // VARYINGS_NEED_TANGENT_TO_WORLD

#ifdef VARYINGS_NEED_TEXCOORD0
    output.texCoord0.xy = attribute.texcoords[0];
    output.texcoordDDX0 = attribute.texcoordsDDX[0];
    output.texcoordDDY0 = attribute.texcoordsDDY[0];
#endif
#ifdef VARYINGS_NEED_TEXCOORD1
    output.texCoord1.xy = attribute.texcoords[1];
    output.texcoordDDX1 = attribute.texcoordsDDX[1];
    output.texcoordDDY1 = attribute.texcoordsDDY[1];
#endif
#ifdef VARYINGS_NEED_TEXCOORD2
    output.texCoord2.xy = attribute.texcoords[2];
    output.texcoordDDX2 = attribute.texcoordsDDX[2];
    output.texcoordDDY2 = attribute.texcoordsDDY[2];
#endif
#ifdef VARYINGS_NEED_TEXCOORD3
    output.texCoord3.xy = attribute.texcoords[3];
    output.texcoordDDX3 = attribute.texcoordsDDX[3];
    output.texcoordDDY3 = attribute.texcoordsDDY[3];
#endif
#ifdef VARYINGS_NEED_COLOR
    output.color = attribute.vertexColor;
#endif

    return output;
}


void GetSurfaceAndBuiltinDataFromMaterialBuffer(FragInputs input, 
    InstanceHeader instance,
    ClusterBuffer cluster,
    float3 V, 
    uint instancePageOffset,
    inout PositionInputs posInput, 
    out SurfaceData surfaceData, 
    out BuiltinData builtinData RAY_TRACING_OPTIONAL_PARAMETERS)
{
#ifdef _DOUBLESIDED_ON
    float3 doubleSidedConstants = _DoubleSidedConstants.xyz;
#else
    float3 doubleSidedConstants = float3(1.0, 1.0, 1.0);
#endif
    
    LayerTexCoord layerTexCoord;
    ZERO_INITIALIZE(LayerTexCoord, layerTexCoord);
    GetLayerTexCoord(input, layerTexCoord);

    //float2 ddxddy = _UVMappingMask.x * float2(input.texcoordDDX0.x, input.texcoordDDY0.x) +
    //                _UVMappingMask.y * float2(input.texcoordDDX1.x, input.texcoordDDY1.y) +
    //                _UVMappingMask.z * float2(input.texcoordDDX2.x, input.texcoordDDY2.y) +
    //                _UVMappingMask.w * float2(input.texcoordDDX3.x, input.texcoordDDY3.y);
    //ADD_IDX(layerTexCoord.base).ddxddy = ddxddy;

#if !defined(SHADER_STAGE_RAY_TRACING)
#ifdef LAYERED_LIT_SHADER
    float4 blendMasks = GetBlendMask(layerTexCoord, input.color);
    float depthOffset = ApplyPerPixelDisplacement(input, V, layerTexCoord, blendMasks);
#else
    float depthOffset = ApplyPerPixelDisplacement(input, V, layerTexCoord);
#endif
    #ifdef _DEPTHOFFSET_ON
    ApplyDepthOffsetPositionInput(V, depthOffset, GetViewForwardDir(), GetWorldToHClipMatrix(), posInput);
    #endif
#else
    float depthOffset = 0.0;
#endif
    
    // We perform the conversion to world of the normalTS outside of the GetSurfaceData
    // so it allow us to correctly deal with detail normal map and optimize the code for the layered shaders
    float3 normalTS;
    float3 bentNormalTS;
    float3 bentNormalWS;
#ifdef LAYERED_LIT_SHADER
    SurfaceData surfaceData0, surfaceData1, surfaceData2, surfaceData3;
    float3 normalTS0, normalTS1, normalTS2, normalTS3;
    float3 bentNormalTS0, bentNormalTS1, bentNormalTS2, bentNormalTS3;
    float alpha0 = GetSurfaceDataFromMaterialBuffer0(input, layerTexCoord, instance, cluster, surfaceData0, normalTS0, bentNormalTS0);
    float alpha1 = GetSurfaceDataFromMaterialBuffer1(input, layerTexCoord, instance, cluster, surfaceData1, normalTS1, bentNormalTS1);
    float alpha2 = GetSurfaceDataFromMaterialBuffer2(input, layerTexCoord, instance, cluster, surfaceData2, normalTS2, bentNormalTS2);
    float alpha3 = GetSurfaceDataFromMaterialBuffer3(input, layerTexCoord, instance, cluster, surfaceData3, normalTS3, bentNormalTS3);
    // Note: If per pixel displacement is enabled it mean we will fetch again the various heightmaps at the intersection location. Not sure the compiler can optimize.
    float weights[_MAX_LAYER];
    ComputeLayerWeights(input, layerTexCoord, float4(alpha0, alpha1, alpha2, alpha3), blendMasks, weights);
    // For layered shader, alpha of base color is used as either an opacity mask, a composition mask for inheritance parameters or a density mask.
    float alpha = PROP_BLEND_SCALAR(alpha, weights);

#if defined(_MAIN_LAYER_INFLUENCE_MODE)

    #ifdef _INFLUENCEMASK_MAP
    float influenceMask = GetInfluenceMask(layerTexCoord);
    #else
    float influenceMask = 1.0;
    #endif

    if (influenceMask > 0.0f)
    {
        surfaceData.baseColor = ComputeMainBaseColorInfluence(influenceMask, surfaceData0.baseColor, surfaceData1.baseColor, surfaceData2.baseColor, surfaceData3.baseColor, layerTexCoord, blendMasks.a, weights);
        normalTS = ComputeMainNormalInfluence(influenceMask, input, normalTS0, normalTS1, normalTS2, normalTS3, layerTexCoord, blendMasks.a, weights);
        bentNormalTS = ComputeMainNormalInfluence(influenceMask, input, bentNormalTS0, bentNormalTS1, bentNormalTS2, bentNormalTS3, layerTexCoord, blendMasks.a, weights);
    }
    else
#endif
    {
        surfaceData.baseColor = SURFACEDATA_BLEND_VECTOR3(surfaceData, baseColor, weights);
        normalTS = BlendLayeredVector3(normalTS0, normalTS1, normalTS2, normalTS3, weights);
        bentNormalTS = BlendLayeredVector3(bentNormalTS0, bentNormalTS1, bentNormalTS2, bentNormalTS3, weights);
    }

    surfaceData.perceptualSmoothness = SURFACEDATA_BLEND_SCALAR(surfaceData, perceptualSmoothness, weights);
    surfaceData.ambientOcclusion = SURFACEDATA_BLEND_SCALAR(surfaceData, ambientOcclusion, weights);
    surfaceData.metallic = SURFACEDATA_BLEND_SCALAR(surfaceData, metallic, weights);
    surfaceData.tangentWS = normalize(input.tangentToWorld[0].xyz); // The tangent is not normalize in tangentToWorld for mikkt. Tag: SURFACE_GRADIENT
    surfaceData.subsurfaceMask = SURFACEDATA_BLEND_SCALAR(surfaceData, subsurfaceMask, weights);
    surfaceData.thickness = SURFACEDATA_BLEND_SCALAR(surfaceData, thickness, weights);
    surfaceData.diffusionProfileHash = SURFACEDATA_BLEND_DIFFUSION_PROFILE(surfaceData, diffusionProfileHash, weights); // We don't need the hash as we only use it to compute the diffusion profile index

    // Layered shader support SSS and Transmission features
    surfaceData.materialFeatures = MATERIALFEATUREFLAGS_LIT_STANDARD;

#ifdef _MATERIAL_FEATURE_SUBSURFACE_SCATTERING
    surfaceData.materialFeatures |= MATERIALFEATUREFLAGS_LIT_SUBSURFACE_SCATTERING;
#endif
#ifdef _MATERIAL_FEATURE_TRANSMISSION
    surfaceData.materialFeatures |= MATERIALFEATUREFLAGS_LIT_TRANSMISSION;
#endif

    // Init other parameters
    surfaceData.anisotropy = 0.0;
    surfaceData.specularColor = float3(0.0, 0.0, 0.0);
    surfaceData.coatMask = 0.0;
    surfaceData.iridescenceThickness = 0.0;
    surfaceData.iridescenceMask = 0.0;

    // Transparency parameters
    // Use thickness from SSS
    surfaceData.ior = 1.0;
    surfaceData.transmittanceColor = float3(1.0, 1.0, 1.0);
    surfaceData.atDistance = 1000000.0;
    surfaceData.transmittanceMask = 0.0;
#else
    float alpha = GetSurfaceDataFromMaterialBuffer(input, layerTexCoord, instance, cluster, surfaceData, normalTS, bentNormalTS);
#endif

    GetNormalWS(input, normalTS, surfaceData.normalWS, doubleSidedConstants);
    surfaceData.geomNormalWS = input.tangentToWorld[2];
    surfaceData.specularOcclusion = 1.0; // This need to be init here to quiet the compiler in case of decal, but can be override later.

#if HAVE_DECALS
    if (_EnableDecals)
    {
        // Both uses and modifies 'surfaceData.normalWS'.
        DecalSurfaceData decalSurfaceData = GetDecalSurfaceData(posInput, input, alpha);
        ApplyDecalToSurfaceData(decalSurfaceData, input.tangentToWorld[2], surfaceData);
    }
#endif

        // Use bent normal to sample GI if available
#ifdef _BENTNORMALMAP
    //GetNormalWS(input, bentNormalTS, bentNormalWS, doubleSidedConstants);
    // Cloud todo    
    bentNormalWS = surfaceData.normalWS;
#else
    bentNormalWS = surfaceData.normalWS;
#endif
//    bentNormalWS.xyz = float3(1, 0, 0);
//surfaceData.baseColor=bentNormalWS;

    // This is use with anisotropic material
    surfaceData.tangentWS = Orthonormalize(surfaceData.tangentWS, surfaceData.normalWS);
    
#if defined(_ENABLE_GEOMETRIC_SPECULAR_AA) && !defined(SHADER_STAGE_RAY_TRACING)
    // Specular AA
    surfaceData.perceptualSmoothness = GeometricNormalFiltering(surfaceData.perceptualSmoothness, input.tangentToWorld[2], _SpecularAAScreenSpaceVariance, _SpecularAAThreshold);
#endif
    
    // Caution: surfaceData must be fully initialize before calling GetBuiltinData
#ifdef LAYERED_LIT_SHADER
    GetBuiltinDataForGPUDriven(input, V, posInput, surfaceData, alpha, bentNormalWS, depthOffset, layerTexCoord.base0, instancePageOffset, builtinData);
#else
    GetBuiltinDataForGPUDriven(input, V, posInput, surfaceData, alpha, bentNormalWS, depthOffset, layerTexCoord.base, instancePageOffset, builtinData);
#endif
    //surfaceData.baseColor =bentNormalWS;
//surfaceData.baseColor = builtinData.bakeDiffuseLighting;

    RAY_TRACING_OPTIONAL_ALPHA_TEST_PASS
}

Varyings Vert(Attributes inputMesh)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(inputMesh);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
#ifdef PROCEDURAL_INSTANCING_ON
//#if defined(SHADER_API_VULKAN)
    float remap[6] = {0, 2, 1, 3, 2, 0};
//#else
//    float remap[6] = {0, 1, 2, 3, 0, 2};
//#endif
    uint quadIndex = remap[inputMesh.vertexID];
    output.positionCS = GetTileVertexPosition(quadIndex, inputMesh.instanceID, _TileSize, _ScreenSize.xy);
    output.positionCS.y *= -1.0;
    //output.positionCS.z = asfloat(_TempMaterialBuffer[0]);
    uint materialID = asuint(_CurrentMaterialID);
    output.positionCS.z = asfloat(materialID);
    output.texcoord = GetTileTexCoord(quadIndex, inputMesh.instanceID, _TileSize, _ScreenSize.xy);
    
    uint2 xy = output.texcoord * _TileSize;
    uint id = (/*_TileSize.y -*/ xy.y) * _TileSize.x + xy.x;
    MaterialRange range = _MaterialRangeBuffer[inputMesh.instanceID];
    uint curMaterialID = _TempMaterialBuffer[1] & (0x00003FFF);
    curMaterialID = materialID & 0x00003FFF;
    if ((range.min > curMaterialID || curMaterialID > range.max))
    {
        output.positionCS.xy = asfloat(0xFFFFFFFF);
    }
#else
    output.positionCS = float4(0, 0, -1, 1);
    output.texcoord = float2(0, 0);
#endif
    return output;
}


void Frag(Varyings input, OUTPUT_GBUFFER( outGBuffer)/*, out float outputDepth : SV_Depth*/)
{
    //uint2 uv = input.texcoord.xy * 64 * _TileSize;
    uint2 uv = input.positionCS.xy;
    uint3 pixelValue = LOAD_TEXTURE2D(_VisibilityBuffer, uv).rgb;
    uint clusterID = GetClusterID(pixelValue.r);
    uint triangleID = GetTriangleID(pixelValue.g);
    uint vertexID = triangleID * 3; 
    VertexAttribute vertex0 = GetVertexAttribute(clusterID, vertexID);
    VertexAttribute vertex1 = GetVertexAttribute(clusterID, vertexID + 1);
    VertexAttribute vertex2 = GetVertexAttribute(clusterID, vertexID + 2);

    ClusterIDs id = _ClusterIDBuffer[clusterID];
    InstanceHeader instance = _InstanceHeaderBuffer[id.instanceID];
    GeometryInfo geometry = _GeometryBuffer[id.geometryID];
    ClusterPageHeader header = _ClusterPageHeaderBuffer[id.clusterID];
    ClusterBuffer cluster = GetClusterBuffer(header);

    float4x4 mvp = mul(_MatrixVP, instance.worldMatrix);
    float4 clipPos0 = mul(mvp, float4(vertex0.localPosition, 1.0));
    float4 clipPos1 = mul(mvp, float4(vertex1.localPosition, 1.0));
    float4 clipPos2 = mul(mvp, float4(vertex2.localPosition, 1.0));

    const float2 pixelClip = (input.positionCS.xy) * _ScreenSize.zw * float2(2, 2) + float2(-1, -1);
    Barycentrics barycentrics = CalculateTriangleBarycentrics(pixelClip, clipPos0, clipPos1, clipPos2);
    
    half4 vertexColor = (barycentrics.UVW.x * vertex0.vertexColor
        + barycentrics.UVW.y * vertex1.vertexColor
        + barycentrics.UVW.z * vertex2.vertexColor) * (1.0f / 255.0f);

    float3 TangentZ = normalize(barycentrics.UVW.x * vertex0.normal
        + barycentrics.UVW.y * vertex1.normal
        + barycentrics.UVW.z * vertex2.normal);
    //TangentZ = normalize(vertex2.normal);

    VertexAttribute pixelVertexAttribute;
    pixelVertexAttribute.vertexColor = vertexColor;
    pixelVertexAttribute.normal = TangentZ;

    float3x3 tangentToWorld = k_identity3x3;
    [unroll]
    for (uint i = 0; i != 8; ++i)
    {
        float2 texcoord10 = vertex1.texcoords[i] - vertex0.texcoords[i];
        float2 texcoord20 = vertex2.texcoords[i] - vertex0.texcoords[i];

        pixelVertexAttribute.texcoords[i] = vertex0.texcoords[i] + barycentrics.UVW.y * texcoord10 + barycentrics.UVW.z * texcoord20;
        pixelVertexAttribute.texcoordsDDX[i] = barycentrics.UVW_dx.y * texcoord10 + barycentrics.UVW_dx.z * texcoord20;
    	pixelVertexAttribute.texcoordsDDY[i] = barycentrics.UVW_dy.y * texcoord10 + barycentrics.UVW_dy.z * texcoord20;

#ifdef VARYINGS_NEED_TANGENT_TO_WORLD
        // Generate tangent frame for UV0
		if (i == 0)
		{
			// Implicit tangent space
			// Based on Christian Schl�ler's derivation: http://www.thetenthplanet.de/archives/1180
			// The technique derives a tangent space from the interpolated normal and (position,uv) deltas in two not necessarily orthogonal directions.
			// The described technique uses screen space derivatives as a way to obtain these direction deltas in a pixel shader,
			// but as we have the triangle vertices explicitly available using the local space corner deltas directly is faster and more convenient.

            float3 PointLocal10 = vertex1.localPosition - vertex0.localPosition;
            float3 PointLocal20 = vertex2.localPosition - vertex0.localPosition;

            bool TangentXValid = abs(texcoord10.x) + abs(texcoord20.x) > 1e-6;

            float3 TangentX;
            float3 TangentY;
			if (TangentXValid)
			{
                float3 Perp2 = cross(TangentZ, PointLocal20);
                float3 Perp1 = cross(PointLocal10, TangentZ);
                float3 TangentU = Perp2 * texcoord10.x + Perp1 * texcoord20.x;
                float3 TangentV = Perp2 * texcoord10.y + Perp1 * texcoord20.y;

				TangentX = normalize(TangentU);
				TangentY = cross(pixelVertexAttribute.normal, TangentX);

				//AttributeData.UnMirrored = dot(TangentV, TangentY) < 0.0f ? -1.0f : 1.0f;
				//TangentY *= AttributeData.UnMirrored;
                TangentY *= dot(TangentV, TangentY) < 0.0f ? -1.0f : 1.0f;
			}
			else
			{
                const float Sign = TangentZ.z >= 0 ? 1 : -1;
                const float a = -rcp(Sign + TangentZ.z);
                const float b = TangentZ.x * TangentZ.y * a;
	
				TangentX = float3(1 + Sign * a * pow(TangentZ.x, 2), Sign * b, -Sign * TangentZ.x);
				TangentY = float3(b,  Sign + a * pow(TangentZ.y, 2), -TangentZ.y);

				//AttributeData.UnMirrored = 1;
			}

            float3x3 TangentToLocal = float3x3(TangentX, TangentY, TangentZ);

			// Should be Pow2(InvScale) but that requires renormalization
            float3x3 LocalToWorld = (float3x3) instance.worldMatrix;
            //float3 InvScale = InstanceData.InvNonUniformScaleAndDeterminantSign.xyz;
			//LocalToWorld[0] *= InvScale.x;
			//LocalToWorld[1]*= InvScale.y;
			//LocalToWorld[2]*= InvScale.z;
			
            //tangentToWorld = mul(LocalToWorld, TangentToLocal);
            tangentToWorld = mul(TangentToLocal, LocalToWorld);
		}
#endif
    }

    if (geometry.copyUV0 > 0)
    {
        pixelVertexAttribute.texcoords[1] = pixelVertexAttribute.texcoords[0];
        pixelVertexAttribute.texcoordsDDX[1] = pixelVertexAttribute.texcoordsDDX[0];
        pixelVertexAttribute.texcoordsDDY[1] = pixelVertexAttribute.texcoordsDDY[0];
    }
        

    float4 tangent = normalize(barycentrics.UVW.x * vertex0.tangent
        + barycentrics.UVW.y * vertex1.tangent 
        + barycentrics.UVW.z * vertex2.tangent);

    pixelVertexAttribute.tangent = tangent;
    pixelVertexAttribute.localPosition = barycentrics.UVW.x * vertex0.localPosition
        + barycentrics.UVW.y * vertex1.localPosition 
        + barycentrics.UVW.z * vertex2.localPosition;

    float3 normalWS = mul(float4(pixelVertexAttribute.normal, 0.0f), instance.world2LocalMatrix).xyz;
    float4 tangentWS = mul(pixelVertexAttribute.tangent, instance.world2LocalMatrix);
    tangentWS = float4(tangentWS.xyz, tangentWS.w > 0.0 ? 1.0 : -1.0); // must not be normalized (mikkts requirement)
    tangentToWorld = BuildTangentToWorld(tangentWS, normalWS);

    FragInputs fragInput = GetFragInputs(input, pixelVertexAttribute, tangentToWorld);
    float3 positionWS = mul(instance.worldMatrix, float4(pixelVertexAttribute.localPosition, 1.0f)).xyz;
    fragInput.positionRWS = positionWS;
    // input.positionSS is SV_Position
    PositionInputs posInput = GetPositionInput(fragInput.positionSS.xy, 
        _ScreenSize.zw, 
        fragInput.positionSS.z, 
        fragInput.positionSS.w, 
        positionWS);

#ifdef VARYINGS_NEED_POSITION_WS
    float3 V = GetWorldSpaceNormalizeViewDir(fragInput.positionRWS);
#else
    // Unused
    float3 V = float3(1.0, 1.0, 1.0); // Avoid the division by 0
#endif

    SurfaceData surfaceData;
    BuiltinData builtinData;
    GetSurfaceAndBuiltinDataFromMaterialBuffer(fragInput, instance, cluster, V, instance.pageOffset, posInput, surfaceData, builtinData);
//builtinData.bakeDiffuseLighting = 1;
//builtinData.backBakeDiffuseLighting = 1;
// Debug to output the uv1
//surfaceData.baseColor = float3(fragInput.texCoord1.xyz);
//surfaceData.baseColor = vertex0.normal;
//surfaceData.baseColor = surfaceData.normalWS;
//surfaceData.baseColor = builtinData.bakeDiffuseLighting; 
    ENCODE_INTO_GBUFFER(surfaceData, builtinData, posInput.positionSS, outGBuffer);

    //float depth = LOAD_TEXTURE2D_X(_VisibilityDepth, uv).r;
    //outputDepth = depth;

    //outGBuffer0.rg = pixelVertexAttribute.texcoords[1];
    //outGBuffer0.rg = vertex0.texcoords[0];
    //outGBuffer3.rg = input.positionCS.xy;
}
