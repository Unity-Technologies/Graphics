// Various shadow sampling logic.
// Again two versions, one for dynamic resource indexing, one for static resource access.

// ------------------------------------------------------------------
//  PCF Filtering helpers
// ------------------------------------------------------------------

// Assuming a isoceles right angled triangle of height "triangleHeight" (as drawn below).
// This function return the area of the triangle above the first texel.
//
// |\      <-- 45 degree slop isosceles right angled triangle
// | \
// ----    <-- length of this side is "triangleHeight"
// _ _ _ _ <-- texels
real SampleShadow_GetTriangleTexelArea(real triangleHeight)
{
	return triangleHeight - 0.5;
}

// Assuming a isoceles triangle of 1.5 texels height and 3 texels wide lying on 4 texels.
// This function return the area of the triangle above each of those texels.
//    |    <-- offset from -0.5 to 0.5, 0 meaning triangle is exactly in the center
//   / \   <-- 45 degree slop isosceles triangle (ie tent projected in 2D)
//  /   \
// _ _ _ _ <-- texels
// X Y Z W <-- result indices (in computedArea.xyzw and computedAreaUncut.xyzw)
void SampleShadow_GetTexelAreas_Tent_3x3(real offset, out real4 computedArea, out real4 computedAreaUncut)
{
	// Compute the exterior areas
	real offset01SquaredHalved = (offset + 0.5) * (offset + 0.5) * 0.5;
	computedAreaUncut.x = computedArea.x = offset01SquaredHalved - offset;
	computedAreaUncut.w = computedArea.w = offset01SquaredHalved;

	// Compute the middle areas
	// For Y : We find the area in Y of as if the left section of the isoceles triangle would
	// intersect the axis between Y and Z (ie where offset = 0).
	computedAreaUncut.y = SampleShadow_GetTriangleTexelArea(1.5 - offset);
	// This area is superior to the one we are looking for if (offset < 0) thus we need to
	// subtract the area of the triangle defined by (0,1.5-offset), (0,1.5+offset), (-offset,1.5).
	real clampedOffsetLeft = min(offset,0);
	real areaOfSmallLeftTriangle = clampedOffsetLeft * clampedOffsetLeft;
	computedArea.y = computedAreaUncut.y - areaOfSmallLeftTriangle;

	// We do the same for the Z but with the right part of the isoceles triangle
	computedAreaUncut.z = SampleShadow_GetTriangleTexelArea(1.5 + offset);
	real clampedOffsetRight = max(offset,0);
	real areaOfSmallRightTriangle = clampedOffsetRight * clampedOffsetRight;
	computedArea.z = computedAreaUncut.z - areaOfSmallRightTriangle;
}

// Assuming a isoceles triangle of 1.5 texels height and 3 texels wide lying on 4 texels.
// This function return the weight of each texels area relative to the full triangle area.
void SampleShadow_GetTexelWeights_Tent_3x3(real offset, out real4 computedWeight)
{
	real4 dummy;
	SampleShadow_GetTexelAreas_Tent_3x3(offset, computedWeight, dummy);
	computedWeight *= 0.44444;//0.44 == 1/(the triangle area)
}

// Assuming a isoceles triangle of 2.5 texel height and 5 texels wide lying on 6 texels.
// This function return the weight of each texels area relative to the full triangle area.
//  /       \
// _ _ _ _ _ _ <-- texels
// 0 1 2 3 4 5 <-- computed area indices (in texelsWeights[])
void SampleShadow_GetTexelWeights_Tent_5x5(real offset, out real3 texelsWeightsA, out real3 texelsWeightsB)
{
	// See _UnityInternalGetAreaPerTexel_3TexelTriangleFilter for details.
	real4 computedArea_From3texelTriangle;
	real4 computedAreaUncut_From3texelTriangle;
	SampleShadow_GetTexelAreas_Tent_3x3(offset, computedArea_From3texelTriangle, computedAreaUncut_From3texelTriangle);

	// Triangle slope is 45 degree thus we can almost reuse the result of the 3 texel wide computation.
	// the 5 texel wide triangle can be seen as the 3 texel wide one but shifted up by one unit/texel.
	// 0.16 is 1/(the triangle area)
	texelsWeightsA.x = 0.16 * (computedArea_From3texelTriangle.x);
	texelsWeightsA.y = 0.16 * (computedAreaUncut_From3texelTriangle.y);
	texelsWeightsA.z = 0.16 * (computedArea_From3texelTriangle.y + 1);
	texelsWeightsB.x = 0.16 * (computedArea_From3texelTriangle.z + 1);
	texelsWeightsB.y = 0.16 * (computedAreaUncut_From3texelTriangle.z);
	texelsWeightsB.z = 0.16 * (computedArea_From3texelTriangle.w);
}

// Assuming a isoceles triangle of 3.5 texel height and 7 texels wide lying on 8 texels.
// This function return the weight of each texels area relative to the full triangle area.
//  /           \
// _ _ _ _ _ _ _ _ <-- texels
// 0 1 2 3 4 5 6 7 <-- computed area indices (in texelsWeights[])
void SampleShadow_GetTexelWeights_Tent_7x7(real offset, out real4 texelsWeightsA, out real4 texelsWeightsB)
{
	// See _UnityInternalGetAreaPerTexel_3TexelTriangleFilter for details.
	real4 computedArea_From3texelTriangle;
	real4 computedAreaUncut_From3texelTriangle;
	SampleShadow_GetTexelAreas_Tent_3x3(offset, computedArea_From3texelTriangle, computedAreaUncut_From3texelTriangle);

	// Triangle slope is 45 degree thus we can almost reuse the result of the 3 texel wide computation.
	// the 7 texel wide triangle can be seen as the 3 texel wide one but shifted up by two unit/texel.
	// 0.081632 is 1/(the triangle area)
	texelsWeightsA.x = 0.081632 * (computedArea_From3texelTriangle.x);
	texelsWeightsA.y = 0.081632 * (computedAreaUncut_From3texelTriangle.y);
	texelsWeightsA.z = 0.081632 * (computedAreaUncut_From3texelTriangle.y + 1);
	texelsWeightsA.w = 0.081632 * (computedArea_From3texelTriangle.y + 2);
	texelsWeightsB.x = 0.081632 * (computedArea_From3texelTriangle.z + 2);
	texelsWeightsB.y = 0.081632 * (computedAreaUncut_From3texelTriangle.z + 1);
	texelsWeightsB.z = 0.081632 * (computedAreaUncut_From3texelTriangle.z);
	texelsWeightsB.w = 0.081632 * (computedArea_From3texelTriangle.w);
}

// 3x3 Tent filter (45 degree sloped triangles in U and V)
void SampleShadow_ComputeSamples_Tent_3x3(real4 shadowMapTexture_TexelSize, real2 coord, out real fetchesWeights[4], out real2 fetchesUV[4])
{
	// tent base is 3x3 base thus covering from 9 to 12 texels, thus we need 4 bilinear PCF fetches
	real2 tentCenterInTexelSpace = coord.xy * shadowMapTexture_TexelSize.zw;
	real2 centerOfFetchesInTexelSpace = floor(tentCenterInTexelSpace + 0.5);
	real2 offsetFromTentCenterToCenterOfFetches = tentCenterInTexelSpace - centerOfFetchesInTexelSpace;

	// find the weight of each texel based
	real4 texelsWeightsU, texelsWeightsV;
	SampleShadow_GetTexelWeights_Tent_3x3(offsetFromTentCenterToCenterOfFetches.x, texelsWeightsU);
	SampleShadow_GetTexelWeights_Tent_3x3(offsetFromTentCenterToCenterOfFetches.y, texelsWeightsV);

	// each fetch will cover a group of 2x2 texels, the weight of each group is the sum of the weights of the texels
	real2 fetchesWeightsU = texelsWeightsU.xz + texelsWeightsU.yw;
	real2 fetchesWeightsV = texelsWeightsV.xz + texelsWeightsV.yw;

	// move the PCF bilinear fetches to respect texels weights
	real2 fetchesOffsetsU = texelsWeightsU.yw / fetchesWeightsU.xy + real2(-1.5,0.5);
	real2 fetchesOffsetsV = texelsWeightsV.yw / fetchesWeightsV.xy + real2(-1.5,0.5);
	fetchesOffsetsU *= shadowMapTexture_TexelSize.xx;
	fetchesOffsetsV *= shadowMapTexture_TexelSize.yy;

	real2 bilinearFetchOrigin = centerOfFetchesInTexelSpace * shadowMapTexture_TexelSize.xy;
	fetchesUV[0] = bilinearFetchOrigin + real2(fetchesOffsetsU.x, fetchesOffsetsV.x);
	fetchesUV[1] = bilinearFetchOrigin + real2(fetchesOffsetsU.y, fetchesOffsetsV.x);
	fetchesUV[2] = bilinearFetchOrigin + real2(fetchesOffsetsU.x, fetchesOffsetsV.y);
	fetchesUV[3] = bilinearFetchOrigin + real2(fetchesOffsetsU.y, fetchesOffsetsV.y);

	fetchesWeights[0] = fetchesWeightsU.x * fetchesWeightsV.x;
	fetchesWeights[1] = fetchesWeightsU.y * fetchesWeightsV.x;
	fetchesWeights[2] = fetchesWeightsU.x * fetchesWeightsV.y;
	fetchesWeights[3] = fetchesWeightsU.y * fetchesWeightsV.y;
}

// 5x5 Tent filter (45 degree sloped triangles in U and V)
void SampleShadow_ComputeSamples_Tent_5x5(real4 shadowMapTexture_TexelSize, real2 coord, out real fetchesWeights[9], out real2 fetchesUV[9])
{
	// tent base is 5x5 base thus covering from 25 to 36 texels, thus we need 9 bilinear PCF fetches
	real2 tentCenterInTexelSpace = coord.xy * shadowMapTexture_TexelSize.zw;
	real2 centerOfFetchesInTexelSpace = floor(tentCenterInTexelSpace + 0.5);
	real2 offsetFromTentCenterToCenterOfFetches = tentCenterInTexelSpace - centerOfFetchesInTexelSpace;

	// find the weight of each texel based on the area of a 45 degree slop tent above each of them.
	real3 texelsWeightsU_A, texelsWeightsU_B;
	real3 texelsWeightsV_A, texelsWeightsV_B;
	SampleShadow_GetTexelWeights_Tent_5x5(offsetFromTentCenterToCenterOfFetches.x, texelsWeightsU_A, texelsWeightsU_B);
	SampleShadow_GetTexelWeights_Tent_5x5(offsetFromTentCenterToCenterOfFetches.y, texelsWeightsV_A, texelsWeightsV_B);

	// each fetch will cover a group of 2x2 texels, the weight of each group is the sum of the weights of the texels
	real3 fetchesWeightsU = real3(texelsWeightsU_A.xz, texelsWeightsU_B.y) + real3(texelsWeightsU_A.y, texelsWeightsU_B.xz);
	real3 fetchesWeightsV = real3(texelsWeightsV_A.xz, texelsWeightsV_B.y) + real3(texelsWeightsV_A.y, texelsWeightsV_B.xz);

	// move the PCF bilinear fetches to respect texels weights
	real3 fetchesOffsetsU = real3(texelsWeightsU_A.y, texelsWeightsU_B.xz) / fetchesWeightsU.xyz + real3(-2.5,-0.5,1.5);
	real3 fetchesOffsetsV = real3(texelsWeightsV_A.y, texelsWeightsV_B.xz) / fetchesWeightsV.xyz + real3(-2.5,-0.5,1.5);
	fetchesOffsetsU *= shadowMapTexture_TexelSize.xxx;
	fetchesOffsetsV *= shadowMapTexture_TexelSize.yyy;

	real2 bilinearFetchOrigin = centerOfFetchesInTexelSpace * shadowMapTexture_TexelSize.xy;
	fetchesUV[0] = bilinearFetchOrigin + real2(fetchesOffsetsU.x, fetchesOffsetsV.x);
	fetchesUV[1] = bilinearFetchOrigin + real2(fetchesOffsetsU.y, fetchesOffsetsV.x);
	fetchesUV[2] = bilinearFetchOrigin + real2(fetchesOffsetsU.z, fetchesOffsetsV.x);
	fetchesUV[3] = bilinearFetchOrigin + real2(fetchesOffsetsU.x, fetchesOffsetsV.y);
	fetchesUV[4] = bilinearFetchOrigin + real2(fetchesOffsetsU.y, fetchesOffsetsV.y);
	fetchesUV[5] = bilinearFetchOrigin + real2(fetchesOffsetsU.z, fetchesOffsetsV.y);
	fetchesUV[6] = bilinearFetchOrigin + real2(fetchesOffsetsU.x, fetchesOffsetsV.z);
	fetchesUV[7] = bilinearFetchOrigin + real2(fetchesOffsetsU.y, fetchesOffsetsV.z);
	fetchesUV[8] = bilinearFetchOrigin + real2(fetchesOffsetsU.z, fetchesOffsetsV.z);

	fetchesWeights[0] = fetchesWeightsU.x * fetchesWeightsV.x;
	fetchesWeights[1] = fetchesWeightsU.y * fetchesWeightsV.x;
	fetchesWeights[2] = fetchesWeightsU.z * fetchesWeightsV.x;
	fetchesWeights[3] = fetchesWeightsU.x * fetchesWeightsV.y;
	fetchesWeights[4] = fetchesWeightsU.y * fetchesWeightsV.y;
	fetchesWeights[5] = fetchesWeightsU.z * fetchesWeightsV.y;
	fetchesWeights[6] = fetchesWeightsU.x * fetchesWeightsV.z;
	fetchesWeights[7] = fetchesWeightsU.y * fetchesWeightsV.z;
	fetchesWeights[8] = fetchesWeightsU.z * fetchesWeightsV.z;
}

// 7x7 Tent filter (45 degree sloped triangles in U and V)
void SampleShadow_ComputeSamples_Tent_7x7(real4 shadowMapTexture_TexelSize, real2 coord, out real fetchesWeights[16], out real2 fetchesUV[16])
{
	// tent base is 7x7 base thus covering from 49 to 64 texels, thus we need 16 bilinear PCF fetches
	real2 tentCenterInTexelSpace = coord.xy * shadowMapTexture_TexelSize.zw;
	real2 centerOfFetchesInTexelSpace = floor(tentCenterInTexelSpace + 0.5);
	real2 offsetFromTentCenterToCenterOfFetches = tentCenterInTexelSpace - centerOfFetchesInTexelSpace;

	// find the weight of each texel based on the area of a 45 degree slop tent above each of them.
	real4 texelsWeightsU_A, texelsWeightsU_B;
	real4 texelsWeightsV_A, texelsWeightsV_B;
	SampleShadow_GetTexelWeights_Tent_7x7(offsetFromTentCenterToCenterOfFetches.x, texelsWeightsU_A, texelsWeightsU_B);
	SampleShadow_GetTexelWeights_Tent_7x7(offsetFromTentCenterToCenterOfFetches.y, texelsWeightsV_A, texelsWeightsV_B);

	// each fetch will cover a group of 2x2 texels, the weight of each group is the sum of the weights of the texels
	real4 fetchesWeightsU = real4(texelsWeightsU_A.xz, texelsWeightsU_B.xz) + real4(texelsWeightsU_A.yw, texelsWeightsU_B.yw);
	real4 fetchesWeightsV = real4(texelsWeightsV_A.xz, texelsWeightsV_B.xz) + real4(texelsWeightsV_A.yw, texelsWeightsV_B.yw);

	// move the PCF bilinear fetches to respect texels weights
	real4 fetchesOffsetsU = real4(texelsWeightsU_A.yw, texelsWeightsU_B.yw) / fetchesWeightsU.xyzw + real4(-3.5,-1.5,0.5,2.5);
	real4 fetchesOffsetsV = real4(texelsWeightsV_A.yw, texelsWeightsV_B.yw) / fetchesWeightsV.xyzw + real4(-3.5,-1.5,0.5,2.5);
	fetchesOffsetsU *= shadowMapTexture_TexelSize.xxxx;
	fetchesOffsetsV *= shadowMapTexture_TexelSize.yyyy;

	real2 bilinearFetchOrigin = centerOfFetchesInTexelSpace * shadowMapTexture_TexelSize.xy;
	fetchesUV[0]  = bilinearFetchOrigin + real2(fetchesOffsetsU.x, fetchesOffsetsV.x);
	fetchesUV[1]  = bilinearFetchOrigin + real2(fetchesOffsetsU.y, fetchesOffsetsV.x);
	fetchesUV[2]  = bilinearFetchOrigin + real2(fetchesOffsetsU.z, fetchesOffsetsV.x);
	fetchesUV[3]  = bilinearFetchOrigin + real2(fetchesOffsetsU.w, fetchesOffsetsV.x);
	fetchesUV[4]  = bilinearFetchOrigin + real2(fetchesOffsetsU.x, fetchesOffsetsV.y);
	fetchesUV[5]  = bilinearFetchOrigin + real2(fetchesOffsetsU.y, fetchesOffsetsV.y);
	fetchesUV[6]  = bilinearFetchOrigin + real2(fetchesOffsetsU.z, fetchesOffsetsV.y);
	fetchesUV[7]  = bilinearFetchOrigin + real2(fetchesOffsetsU.w, fetchesOffsetsV.y);
	fetchesUV[8]  = bilinearFetchOrigin + real2(fetchesOffsetsU.x, fetchesOffsetsV.z);
	fetchesUV[9]  = bilinearFetchOrigin + real2(fetchesOffsetsU.y, fetchesOffsetsV.z);
	fetchesUV[10] = bilinearFetchOrigin + real2(fetchesOffsetsU.z, fetchesOffsetsV.z);
	fetchesUV[11] = bilinearFetchOrigin + real2(fetchesOffsetsU.w, fetchesOffsetsV.z);
	fetchesUV[12] = bilinearFetchOrigin + real2(fetchesOffsetsU.x, fetchesOffsetsV.w);
	fetchesUV[13] = bilinearFetchOrigin + real2(fetchesOffsetsU.y, fetchesOffsetsV.w);
	fetchesUV[14] = bilinearFetchOrigin + real2(fetchesOffsetsU.z, fetchesOffsetsV.w);
	fetchesUV[15] = bilinearFetchOrigin + real2(fetchesOffsetsU.w, fetchesOffsetsV.w);

	fetchesWeights[0]  = fetchesWeightsU.x * fetchesWeightsV.x;
	fetchesWeights[1]  = fetchesWeightsU.y * fetchesWeightsV.x;
	fetchesWeights[2]  = fetchesWeightsU.z * fetchesWeightsV.x;
	fetchesWeights[3]  = fetchesWeightsU.w * fetchesWeightsV.x;
	fetchesWeights[4]  = fetchesWeightsU.x * fetchesWeightsV.y;
	fetchesWeights[5]  = fetchesWeightsU.y * fetchesWeightsV.y;
	fetchesWeights[6]  = fetchesWeightsU.z * fetchesWeightsV.y;
	fetchesWeights[7]  = fetchesWeightsU.w * fetchesWeightsV.y;
	fetchesWeights[8]  = fetchesWeightsU.x * fetchesWeightsV.z;
	fetchesWeights[9]  = fetchesWeightsU.y * fetchesWeightsV.z;
	fetchesWeights[10] = fetchesWeightsU.z * fetchesWeightsV.z;
	fetchesWeights[11] = fetchesWeightsU.w * fetchesWeightsV.z;
	fetchesWeights[12] = fetchesWeightsU.x * fetchesWeightsV.w;
	fetchesWeights[13] = fetchesWeightsU.y * fetchesWeightsV.w;
	fetchesWeights[14] = fetchesWeightsU.z * fetchesWeightsV.w;
	fetchesWeights[15] = fetchesWeightsU.w * fetchesWeightsV.w;
}

// ------------------------------------------------------------------
//  PCF Filtering methods
// ------------------------------------------------------------------

//
//					1 tap PCF sampling
//
real SampleShadow_PCF_1tap( ShadowContext shadowContext, inout uint payloadOffset, real3 tcs, real bias, uint slice, uint texIdx, uint sampIdx )
{
	real depthBias = asfloat( shadowContext.payloads[payloadOffset].x );
	payloadOffset++;

	// add the depth bias
	tcs.z += depthBias;
	// sample the texture
	return SampleCompShadow_T2DA( shadowContext, texIdx, sampIdx, tcs, slice ).x;
}

real SampleShadow_PCF_1tap( ShadowContext shadowContext, inout uint payloadOffset, real3 tcs, real bias, uint slice, Texture2DArray tex, SamplerComparisonState compSamp )
{
	real depthBias = asfloat( shadowContext.payloads[payloadOffset].x );
	payloadOffset++;

	// add the depth bias
	tcs.z += depthBias;
	// sample the texture
	return SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, tcs, slice );
}

//
//					3x3 tent PCF sampling (4 taps)
//
real SampleShadow_PCF_Tent_3x3( ShadowContext shadowContext, inout uint payloadOffset, real4 texelSizeRcp, real3 coord, uint slice, uint texIdx, uint sampIdx )
{
	real2 params     = asfloat( shadowContext.payloads[payloadOffset].xy );
	real  depthBias  = params.x;
	payloadOffset++;

	// TODO move this to shadow data to avoid the rcp?
	real4 shadowMapTexture_TexelSize = real4(texelSizeRcp.xy, rcp(texelSizeRcp.xy));

	// add the depth bias
	coord.z += depthBias;

	real shadow = 0.0;
	real fetchesWeights[4];
	real2 fetchesUV[4];

	SampleShadow_ComputeSamples_Tent_3x3(shadowMapTexture_TexelSize, coord.xy, fetchesWeights, fetchesUV);
	[loop] for (int i = 0; i < 4; i++)
	{
		shadow += fetchesWeights[i] * SampleCompShadow_T2DA( shadowContext, texIdx, sampIdx, real3( fetchesUV[i].xy,  coord.z ), slice ).x;
	}
	return shadow;
}

real SampleShadow_PCF_Tent_3x3(ShadowContext shadowContext, inout uint payloadOffset, real4 texelSizeRcp, real3 coord, uint slice, Texture2DArray tex, SamplerComparisonState compSamp )
{
	real2 params     = asfloat( shadowContext.payloads[payloadOffset].xy );
	real  depthBias  = params.x;
	payloadOffset++;

	// TODO move this to shadow data to avoid the rcp?
	real4 shadowMapTexture_TexelSize = real4(texelSizeRcp.xy, rcp(texelSizeRcp.xy));

	// add the depth bias
	coord.z += depthBias;

	real shadow = 0.0;
	real fetchesWeights[4];
	real2 fetchesUV[4];

	SampleShadow_ComputeSamples_Tent_3x3(shadowMapTexture_TexelSize, coord.xy, fetchesWeights, fetchesUV);
	for (int i = 0; i < 4; i++)
	{
		shadow += fetchesWeights[i] * SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, real3( fetchesUV[i].xy,  coord.z ), slice ).x;
	}
	return shadow;
}

//
//					5x5 tent PCF sampling (9 taps)
//
real SampleShadow_PCF_Tent_5x5( ShadowContext shadowContext, inout uint payloadOffset, real4 texelSizeRcp, real3 coord, uint slice, uint texIdx, uint sampIdx )
{
	real2 params     = asfloat( shadowContext.payloads[payloadOffset].xy );
	real  depthBias  = params.x;
	payloadOffset++;

	// TODO move this to shadow data to avoid the rcp?
	real4 shadowMapTexture_TexelSize = real4(texelSizeRcp.xy, rcp(texelSizeRcp.xy));

	// add the depth bias
	coord.z += depthBias;

	real shadow = 0.0;
	real fetchesWeights[9];
	real2 fetchesUV[9];

	SampleShadow_ComputeSamples_Tent_5x5(shadowMapTexture_TexelSize, coord.xy, fetchesWeights, fetchesUV);
	[loop] for (int i = 0; i < 9; i++)
	{
		shadow += fetchesWeights[i] * SampleCompShadow_T2DA( shadowContext, texIdx, sampIdx, real3( fetchesUV[i].xy,  coord.z ), slice ).x;
	}
	return shadow;
}

real SampleShadow_PCF_Tent_5x5(ShadowContext shadowContext, inout uint payloadOffset, real4 texelSizeRcp, real3 coord, uint slice, Texture2DArray tex, SamplerComparisonState compSamp )
{
	real2 params     = asfloat( shadowContext.payloads[payloadOffset].xy );
	real  depthBias  = params.x;
	payloadOffset++;

	// TODO move this to shadow data to avoid the rcp
	real4 shadowMapTexture_TexelSize = real4(texelSizeRcp.xy, rcp(texelSizeRcp.xy));

	// add the depth bias
	coord.z += depthBias;

	real shadow = 0.0;
	real fetchesWeights[9];
	real2 fetchesUV[9];

	SampleShadow_ComputeSamples_Tent_5x5(shadowMapTexture_TexelSize, coord.xy, fetchesWeights, fetchesUV);

	for( int i = 0; i < 9; i++ )
	{
		shadow += fetchesWeights[i] * SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, real3( fetchesUV[i].xy,  coord.z ), slice ).x;
	}

	return shadow;
}

//
//					7x7 tent PCF sampling (16 taps)
//
real SampleShadow_PCF_Tent_7x7( ShadowContext shadowContext, inout uint payloadOffset, real4 texelSizeRcp, real3 coord, uint slice, uint texIdx, uint sampIdx )
{
	real2 params     = asfloat( shadowContext.payloads[payloadOffset].xy );
	real  depthBias  = params.x;
	payloadOffset++;

	// TODO move this to shadow data to avoid the rcp
	real4 shadowMapTexture_TexelSize = real4(texelSizeRcp.xy, rcp(texelSizeRcp.xy));

	// add the depth bias
	coord.z += depthBias;

	real shadow = 0.0;
	real fetchesWeights[16];
	real2 fetchesUV[16];

	SampleShadow_ComputeSamples_Tent_7x7(shadowMapTexture_TexelSize, coord.xy, fetchesWeights, fetchesUV);
	[loop] for (int i = 0; i < 16; i++)
	{
		shadow += fetchesWeights[i] * SampleCompShadow_T2DA( shadowContext, texIdx, sampIdx, real3( fetchesUV[i].xy,  coord.z ), slice ).x;
	}

	return shadow;
}

real SampleShadow_PCF_Tent_7x7(ShadowContext shadowContext, inout uint payloadOffset, real4 texelSizeRcp, real3 coord, uint slice, Texture2DArray tex, SamplerComparisonState compSamp )
{
	real2 params     = asfloat( shadowContext.payloads[payloadOffset].xy );
	real  depthBias  = params.x;
	payloadOffset++;

	// TODO move this to shadow data to avoid the rcp
	real4 shadowMapTexture_TexelSize = real4(texelSizeRcp.xy, rcp(texelSizeRcp.xy));

	// add the depth bias
	coord.z += depthBias;

	real shadow = 0.0;
	real fetchesWeights[16];
	real2 fetchesUV[16];

	SampleShadow_ComputeSamples_Tent_7x7(shadowMapTexture_TexelSize, coord.xy, fetchesWeights, fetchesUV);

#if SHADOW_OPTIMIZE_REGISTER_USAGE == 1
	int i;
	[loop]
	for( i = 0; i < 1; i++ )
	{
		shadow += fetchesWeights[ 0] * SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, real3( fetchesUV[ 0].xy, coord.z ), slice ).x;
		shadow += fetchesWeights[ 1] * SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, real3( fetchesUV[ 1].xy, coord.z ), slice ).x;
		shadow += fetchesWeights[ 2] * SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, real3( fetchesUV[ 2].xy, coord.z ), slice ).x;
		shadow += fetchesWeights[ 3] * SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, real3( fetchesUV[ 3].xy, coord.z ), slice ).x;
	}
	[loop]
	for( i = 0; i < 1; i++ )
	{
		shadow += fetchesWeights[ 4] * SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, real3( fetchesUV[ 4].xy, coord.z ), slice ).x;
		shadow += fetchesWeights[ 5] * SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, real3( fetchesUV[ 5].xy, coord.z ), slice ).x;
		shadow += fetchesWeights[ 6] * SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, real3( fetchesUV[ 6].xy, coord.z ), slice ).x;
		shadow += fetchesWeights[ 7] * SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, real3( fetchesUV[ 7].xy, coord.z ), slice ).x;
	}
	[loop]
	for( i = 0; i < 1; i++ )
	{
		shadow += fetchesWeights[ 8] * SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, real3( fetchesUV[ 8].xy, coord.z ), slice ).x;
		shadow += fetchesWeights[ 9] * SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, real3( fetchesUV[ 9].xy, coord.z ), slice ).x;
		shadow += fetchesWeights[10] * SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, real3( fetchesUV[10].xy, coord.z ), slice ).x;
		shadow += fetchesWeights[11] * SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, real3( fetchesUV[11].xy, coord.z ), slice ).x;
	}
	[loop]
	for( i = 0; i < 1; i++ )
	{
		shadow += fetchesWeights[12] * SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, real3( fetchesUV[12].xy, coord.z ), slice ).x;
		shadow += fetchesWeights[13] * SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, real3( fetchesUV[13].xy, coord.z ), slice ).x;
		shadow += fetchesWeights[14] * SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, real3( fetchesUV[14].xy, coord.z ), slice ).x;
		shadow += fetchesWeights[15] * SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, real3( fetchesUV[15].xy, coord.z ), slice ).x;
	}
#else
	for( int i = 0; i < 16; i++ )
	{
		shadow += fetchesWeights[i] * SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, real3( fetchesUV[i].xy, coord.z ), slice ).x;
	}
#endif
	return shadow;
}

//
//					9 tap adaptive PCF sampling
//
real SampleShadow_PCF_9tap_Adaptive( ShadowContext shadowContext, inout uint payloadOffset, real4 texelSizeRcp, real3 tcs, real bias, uint slice, uint texIdx, uint sampIdx )
{
	real2 params     = asfloat( shadowContext.payloads[payloadOffset].xy );
	real  depthBias  = params.x;
	real  filterSize = params.y;
	payloadOffset++;

	texelSizeRcp *= filterSize;

	// add the depth bias
	tcs.z += depthBias;

	// Terms0 are weights for the individual samples, the other terms are offsets in texel space
	real4 vShadow3x3PCFTerms0 = real4( 20.0 / 267.0, 33.0 / 267.0, 55.0 / 267.0, 0.0 );
	real4 vShadow3x3PCFTerms1 = real4( texelSizeRcp.x,  texelSizeRcp.y, -texelSizeRcp.x, -texelSizeRcp.y );
	real4 vShadow3x3PCFTerms2 = real4( texelSizeRcp.x,  texelSizeRcp.y, 0.0, 0.0 );
	real4 vShadow3x3PCFTerms3 = real4(-texelSizeRcp.x, -texelSizeRcp.y, 0.0, 0.0 );

	real4 v20Taps;
	v20Taps.x = SampleCompShadow_T2DA( shadowContext, texIdx, sampIdx, real3( tcs.xy + vShadow3x3PCFTerms1.xy, tcs.z ), slice ).x; //  1  1
	v20Taps.y = SampleCompShadow_T2DA( shadowContext, texIdx, sampIdx, real3( tcs.xy + vShadow3x3PCFTerms1.zy, tcs.z ), slice ).x; // -1  1
	v20Taps.z = SampleCompShadow_T2DA( shadowContext, texIdx, sampIdx, real3( tcs.xy + vShadow3x3PCFTerms1.xw, tcs.z ), slice ).x; //  1 -1
	v20Taps.w = SampleCompShadow_T2DA( shadowContext, texIdx, sampIdx, real3( tcs.xy + vShadow3x3PCFTerms1.zw, tcs.z ), slice ).x; // -1 -1
	real flSum = dot( v20Taps.xyzw, real4( 0.25, 0.25, 0.25, 0.25 ) );
	// fully in light or shadow? -> bail
	if( ( flSum == 0.0 ) || ( flSum == 1.0 ) )
		return flSum;

	// we're in a transition area, do 5 more taps
	flSum *= vShadow3x3PCFTerms0.x * 4.0;

	real4 v33Taps;
	v33Taps.x = SampleCompShadow_T2DA( shadowContext, texIdx, sampIdx, real3( tcs.xy + vShadow3x3PCFTerms2.xz, tcs.z ), slice ).x; //  1  0
	v33Taps.y = SampleCompShadow_T2DA( shadowContext, texIdx, sampIdx, real3( tcs.xy + vShadow3x3PCFTerms3.xz, tcs.z ), slice ).x; // -1  0
	v33Taps.z = SampleCompShadow_T2DA( shadowContext, texIdx, sampIdx, real3( tcs.xy + vShadow3x3PCFTerms3.zy, tcs.z ), slice ).x; //  0 -1
	v33Taps.w = SampleCompShadow_T2DA( shadowContext, texIdx, sampIdx, real3( tcs.xy + vShadow3x3PCFTerms2.zy, tcs.z ), slice ).x; //  0  1
	flSum += dot( v33Taps.xyzw, vShadow3x3PCFTerms0.yyyy );

	flSum += SampleCompShadow_T2DA( shadowContext, texIdx, sampIdx, tcs, slice ).x * vShadow3x3PCFTerms0.z;

	return flSum;
}

real SampleShadow_PCF_9tap_Adaptive(ShadowContext shadowContext, inout uint payloadOffset, real4 texelSizeRcp, real3 tcs, real bias, uint slice, Texture2DArray tex, SamplerComparisonState compSamp )
{
	real2 params     = asfloat( shadowContext.payloads[payloadOffset].xy );
	real  depthBias  = params.x;
	real  filterSize = params.y;
	payloadOffset++;

	texelSizeRcp *= filterSize;

	// add the depth bias
	tcs.z += depthBias;

	// Terms0 are weights for the individual samples, the other terms are offsets in texel space
	real4 vShadow3x3PCFTerms0 = real4(20.0 / 267.0, 33.0 / 267.0, 55.0 / 267.0, 0.0);
	real4 vShadow3x3PCFTerms1 = real4( texelSizeRcp.x,  texelSizeRcp.y, -texelSizeRcp.x, -texelSizeRcp.y);
	real4 vShadow3x3PCFTerms2 = real4( texelSizeRcp.x,  texelSizeRcp.y, 0.0, 0.0);
	real4 vShadow3x3PCFTerms3 = real4(-texelSizeRcp.x, -texelSizeRcp.y, 0.0, 0.0);

	real4 v20Taps;
	v20Taps.x = SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, real3( tcs.xy + vShadow3x3PCFTerms1.xy, tcs.z ), slice ).x; //  1  1
	v20Taps.y = SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, real3( tcs.xy + vShadow3x3PCFTerms1.zy, tcs.z ), slice ).x; // -1  1
	v20Taps.z = SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, real3( tcs.xy + vShadow3x3PCFTerms1.xw, tcs.z ), slice ).x; //  1 -1
	v20Taps.w = SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, real3( tcs.xy + vShadow3x3PCFTerms1.zw, tcs.z ), slice ).x; // -1 -1
	real flSum = dot( v20Taps.xyzw, real4( 0.25, 0.25, 0.25, 0.25 ) );
	// fully in light or shadow? -> bail
	if( ( flSum == 0.0 ) || ( flSum == 1.0 ) )
		return flSum;

	// we're in a transition area, do 5 more taps
	flSum *= vShadow3x3PCFTerms0.x * 4.0;

	real4 v33Taps;
	v33Taps.x = SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, real3( tcs.xy + vShadow3x3PCFTerms2.xz, tcs.z ), slice ).x; //  1  0
	v33Taps.y = SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, real3( tcs.xy + vShadow3x3PCFTerms3.xz, tcs.z ), slice ).x; // -1  0
	v33Taps.z = SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, real3( tcs.xy + vShadow3x3PCFTerms3.zy, tcs.z ), slice ).x; //  0 -1
	v33Taps.w = SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, real3( tcs.xy + vShadow3x3PCFTerms2.zy, tcs.z ), slice ).x; //  0  1
	flSum += dot( v33Taps.xyzw, vShadow3x3PCFTerms0.yyyy );

	flSum += SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, tcs, slice ).x * vShadow3x3PCFTerms0.z;

	return flSum;
}

#include "ShadowMoments.hlsl"

//
//					1 tap VSM sampling
//
real SampleShadow_VSM_1tap( ShadowContext shadowContext, inout uint payloadOffset, real3 tcs, uint slice, uint texIdx, uint sampIdx )
{
#if UNITY_REVERSED_Z
	real  depth		 = 1.0 - tcs.z;
#else
	real  depth		 = tcs.z;
#endif
	real2 params		 = asfloat( shadowContext.payloads[payloadOffset].xy );
	real  lightLeakBias = params.x;
	real  varianceBias  = params.y;
	payloadOffset++;

	real2 moments = SampleShadow_T2DA( shadowContext, texIdx, sampIdx, tcs.xy, slice ).xy;

	return ShadowMoments_ChebyshevsInequality( moments, depth, varianceBias, lightLeakBias );
}

real SampleShadow_VSM_1tap(ShadowContext shadowContext, inout uint payloadOffset, real3 tcs, uint slice, Texture2DArray tex, SamplerState samp )
{
#if UNITY_REVERSED_Z
	real  depth		 = 1.0 - tcs.z;
#else
	real  depth		 = tcs.z;
#endif
	real2 params		 = asfloat( shadowContext.payloads[payloadOffset].xy );
	real  lightLeakBias = params.x;
	real  varianceBias  = params.y;
	payloadOffset++;

	real2 moments = SAMPLE_TEXTURE2D_ARRAY_LOD( tex, samp, tcs.xy, slice, 0.0 ).xy;

	return ShadowMoments_ChebyshevsInequality( moments, depth, varianceBias, lightLeakBias );
}

//
//					1 tap EVSM sampling
//
real SampleShadow_EVSM_1tap( ShadowContext shadowContext, inout uint payloadOffset, real3 tcs, uint slice, uint texIdx, uint sampIdx, bool fourMoments )
{
#if UNITY_REVERSED_Z
	real  depth		 = 1.0 - tcs.z;
#else
	real  depth		 = tcs.z;
#endif
	real4 params		 = asfloat( shadowContext.payloads[payloadOffset] );
	real  lightLeakBias = params.x;
	real  varianceBias	 = params.y;
	real2 evsmExponents = params.zw;
	payloadOffset++;

	real2 warpedDepth = ShadowMoments_WarpDepth( depth, evsmExponents );

	real4 moments = SampleShadow_T2DA( shadowContext, texIdx, sampIdx, tcs.xy, slice );

	// Derivate of warping at depth
	real2 depthScale  = evsmExponents * warpedDepth;
	real2 minVariance = depthScale * depthScale * varianceBias;

	[branch]
	if( fourMoments )
	{
		real posContrib = ShadowMoments_ChebyshevsInequality( moments.xz, warpedDepth.x, minVariance.x, lightLeakBias );
		real negContrib = ShadowMoments_ChebyshevsInequality( moments.yw, warpedDepth.y, minVariance.y, lightLeakBias );
		return min( posContrib, negContrib );
	}
	else
	{
		return ShadowMoments_ChebyshevsInequality( moments.xy, warpedDepth.x, minVariance.x, lightLeakBias );
	}
}

real SampleShadow_EVSM_1tap( ShadowContext shadowContext, inout uint payloadOffset, real3 tcs, uint slice, Texture2DArray tex, SamplerState samp, bool fourMoments )
{
#if UNITY_REVERSED_Z
	real  depth		 = 1.0 - tcs.z;
#else
	real  depth		 = tcs.z;
#endif
	real4 params		 = asfloat( shadowContext.payloads[payloadOffset] );
	real  lightLeakBias = params.x;
	real  varianceBias  = params.y;
	real2 evsmExponents = params.zw;
	payloadOffset++;

	real2 warpedDepth = ShadowMoments_WarpDepth( depth, evsmExponents );

	real4 moments = SAMPLE_TEXTURE2D_ARRAY_LOD( tex, samp, tcs.xy, slice, 0.0 );

	// Derivate of warping at depth
	real2 depthScale  = evsmExponents * warpedDepth;
	real2 minVariance = depthScale * depthScale * varianceBias;

	[branch]
	if( fourMoments )
	{
		real posContrib = ShadowMoments_ChebyshevsInequality( moments.xz, warpedDepth.x, minVariance.x, lightLeakBias );
		real negContrib = ShadowMoments_ChebyshevsInequality( moments.yw, warpedDepth.y, minVariance.y, lightLeakBias );
		return min( posContrib, negContrib );
	}
	else
	{
		return ShadowMoments_ChebyshevsInequality( moments.xy, warpedDepth.x, minVariance.x, lightLeakBias );
	}
}


//
//					1 tap MSM sampling
//
real SampleShadow_MSM_1tap( ShadowContext shadowContext, inout uint payloadOffset, real3 tcs, uint slice, uint texIdx, uint sampIdx, bool useHamburger )
{
	real4 params        = asfloat( shadowContext.payloads[payloadOffset] );
	real  lightLeakBias = params.x;
	real  momentBias    = params.y;
	real  depthBias	 = params.z;
	real  bpp16		 = params.w;
#if UNITY_REVERSED_Z
	real  depth         = (1.0 - tcs.z) - depthBias;
#else
	real  depth         = tcs.z + depthBias;
#endif
	payloadOffset++;

	real4 moments = SampleShadow_T2DA( shadowContext, texIdx, sampIdx, tcs.xy, slice );
	if( bpp16 != 0.0 )
		moments = ShadowMoments_Decode16MSM( moments );

	real3 z;
	real4 b;
	ShadowMoments_SolveMSM( moments, depth, momentBias, z, b );

	if( useHamburger )
		return ShadowMoments_SolveDelta3MSM( z, b.xy, lightLeakBias );
	else
		return (z[1] < 0.0 || z[2] > 1.0) ? ShadowMoments_SolveDelta4MSM( z, b, lightLeakBias ) : ShadowMoments_SolveDelta3MSM( z, b.xy, lightLeakBias );
}

real SampleShadow_MSM_1tap( ShadowContext shadowContext, inout uint payloadOffset, real3 tcs, uint slice, Texture2DArray tex, SamplerState samp, bool useHamburger )
{
	real4 params        = asfloat( shadowContext.payloads[payloadOffset] );
	real  lightLeakBias = params.x;
	real  momentBias    = params.y;
	real  depthBias	 = params.z;
	real  bpp16		 = params.w;
#if UNITY_REVERSED_Z
	real  depth         = (1.0 - tcs.z) - depthBias;
#else
	real  depth         = tcs.z + depthBias;
#endif
	payloadOffset++;

	real4 moments = SAMPLE_TEXTURE2D_ARRAY_LOD( tex, samp, tcs.xy, slice, 0.0 );
	if( bpp16 != 0.0 )
		moments = ShadowMoments_Decode16MSM( moments );

	real3 z;
	real4 b;
	ShadowMoments_SolveMSM( moments, depth, momentBias, z, b );

	if( useHamburger )
		return ShadowMoments_SolveDelta3MSM( z, b.xy, lightLeakBias );
	else
		return (z[1] < 0.0 || z[2] > 1.0) ? ShadowMoments_SolveDelta4MSM( z, b, lightLeakBias ) : ShadowMoments_SolveDelta3MSM( z, b.xy, lightLeakBias );
}

//-----------------------------------------------------------------------------------------------------
// helper function to dispatch a specific shadow algorithm
real SampleShadow_SelectAlgorithm( ShadowContext shadowContext, ShadowData shadowData, inout uint payloadOffset, real3 posTC, real depthBias, uint slice, uint algorithm, uint texIdx, uint sampIdx )
{
	[branch]
	switch( algorithm )
	{
	case GPUSHADOWALGORITHM_PCF_1TAP		: return SampleShadow_PCF_1tap( shadowContext, payloadOffset, posTC, depthBias, slice, texIdx, sampIdx );
	case GPUSHADOWALGORITHM_PCF_9TAP		: return SampleShadow_PCF_9tap_Adaptive( shadowContext, payloadOffset, shadowData.texelSizeRcp, posTC, depthBias, slice, texIdx, sampIdx );
	case GPUSHADOWALGORITHM_PCF_TENT_3X3	: return SampleShadow_PCF_Tent_3x3( shadowContext, payloadOffset, shadowData.texelSizeRcp, posTC, slice, texIdx, sampIdx );
	case GPUSHADOWALGORITHM_PCF_TENT_5X5	: return SampleShadow_PCF_Tent_5x5( shadowContext, payloadOffset, shadowData.texelSizeRcp, posTC, slice, texIdx, sampIdx );
	case GPUSHADOWALGORITHM_PCF_TENT_7X7	: return SampleShadow_PCF_Tent_7x7( shadowContext, payloadOffset, shadowData.texelSizeRcp, posTC, slice, texIdx, sampIdx );
	case GPUSHADOWALGORITHM_VSM				: return SampleShadow_VSM_1tap(  shadowContext, payloadOffset, posTC, slice, texIdx, sampIdx );
	case GPUSHADOWALGORITHM_EVSM_2			: return SampleShadow_EVSM_1tap( shadowContext, payloadOffset, posTC, slice, texIdx, sampIdx, false );
	case GPUSHADOWALGORITHM_EVSM_4			: return SampleShadow_EVSM_1tap( shadowContext, payloadOffset, posTC, slice, texIdx, sampIdx, true );
	case GPUSHADOWALGORITHM_MSM_HAM			: return SampleShadow_MSM_1tap(  shadowContext, payloadOffset, posTC, slice, texIdx, sampIdx, true );
	case GPUSHADOWALGORITHM_MSM_HAUS		: return SampleShadow_MSM_1tap(  shadowContext, payloadOffset, posTC, slice, texIdx, sampIdx, false );
	default: return 1.0;
	}
}

real SampleShadow_SelectAlgorithm( ShadowContext shadowContext, ShadowData shadowData, inout uint payloadOffset, real3 posTC, real depthBias, uint slice, uint algorithm, Texture2DArray tex, SamplerComparisonState compSamp )
{
	[branch]
	switch( algorithm )
	{
	case GPUSHADOWALGORITHM_PCF_1TAP		: return SampleShadow_PCF_1tap( shadowContext, payloadOffset, posTC, depthBias, slice, tex, compSamp );
	case GPUSHADOWALGORITHM_PCF_9TAP		: return SampleShadow_PCF_9tap_Adaptive( shadowContext, payloadOffset, shadowData.texelSizeRcp, posTC, depthBias, slice, tex, compSamp );
	case GPUSHADOWALGORITHM_PCF_TENT_3X3	: return SampleShadow_PCF_Tent_3x3( shadowContext, payloadOffset, shadowData.texelSizeRcp, posTC, slice, tex, compSamp );
	case GPUSHADOWALGORITHM_PCF_TENT_5X5	: return SampleShadow_PCF_Tent_5x5( shadowContext, payloadOffset, shadowData.texelSizeRcp, posTC, slice, tex, compSamp );
	case GPUSHADOWALGORITHM_PCF_TENT_7X7	: return SampleShadow_PCF_Tent_7x7( shadowContext, payloadOffset, shadowData.texelSizeRcp, posTC, slice, tex, compSamp );

	default: return 1.0;
	}
}

real SampleShadow_SelectAlgorithm( ShadowContext shadowContext, ShadowData shadowData, inout uint payloadOffset, real3 posTC, real depthBias, uint slice, uint algorithm, Texture2DArray tex, SamplerState samp )
{
	[branch]
	switch( algorithm )
	{
	case GPUSHADOWALGORITHM_VSM				: return SampleShadow_VSM_1tap(  shadowContext, payloadOffset, posTC, slice, tex, samp );
	case GPUSHADOWALGORITHM_EVSM_2			: return SampleShadow_EVSM_1tap( shadowContext, payloadOffset, posTC, slice, tex, samp, false );
	case GPUSHADOWALGORITHM_EVSM_4			: return SampleShadow_EVSM_1tap( shadowContext, payloadOffset, posTC, slice, tex, samp, true );
	case GPUSHADOWALGORITHM_MSM_HAM			: return SampleShadow_MSM_1tap(  shadowContext, payloadOffset, posTC, slice, tex, samp, true );
	case GPUSHADOWALGORITHM_MSM_HAUS		: return SampleShadow_MSM_1tap(  shadowContext, payloadOffset, posTC, slice, tex, samp, false );
	default: return 1.0;
	}
}
