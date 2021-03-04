#ifndef TERRAIN_ENGINE_INCLUDED
#define TERRAIN_ENGINE_INCLUDED

// Terrain engine shader helpers

CBUFFER_START(UnityTerrain)
    // grass
    fixed4 _WavingTint;
    float4 _WaveAndDistance;    // wind speed, wave size, wind amount, max sqr distance
    float4 _CameraPosition;     // .xyz = camera position, .w = 1 / (max sqr distance)
    float3 _CameraRight, _CameraUp;

    // trees
    fixed4 _TreeInstanceColor;
    float4 _TreeInstanceScale;
    float4x4 _TerrainEngineBendTree;
    float4 _SquashPlaneNormal;
    float _SquashAmount;

    // billboards
    float3 _TreeBillboardCameraRight;
    float4 _TreeBillboardCameraUp;
    float4 _TreeBillboardCameraFront;
    float4 _TreeBillboardCameraPos;
    float4 _TreeBillboardDistances; // x = max distance ^ 2
CBUFFER_END


// ---- Vertex input structures

struct appdata_tree {
    float4 vertex : POSITION;       // position
    float4 tangent : TANGENT;       // directional AO
    float3 normal : NORMAL;         // normal
    fixed4 color : COLOR;           // .w = bend factor
    float4 texcoord : TEXCOORD0;    // UV
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct appdata_tree_billboard {
    float4 vertex : POSITION;
    fixed4 color : COLOR;           // Color
    float4 texcoord : TEXCOORD0;    // UV Coordinates
    float2 texcoord1 : TEXCOORD1;   // Billboard extrusion
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

// ---- Grass helpers

// Calculate a 4 fast sine-cosine pairs
// val:     the 4 input values - each must be in the range (0 to 1)
// s:       The sine of each of the 4 values
// c:       The cosine of each of the 4 values
void FastSinCos (float4 val, out float4 s, out float4 c) {
    val = val * 6.408849 - 3.1415927;
    // powers for taylor series
    float4 r5 = val * val;                  // wavevec ^ 2
    float4 r6 = r5 * r5;                        // wavevec ^ 4;
    float4 r7 = r6 * r5;                        // wavevec ^ 6;
    float4 r8 = r6 * r5;                        // wavevec ^ 8;

    float4 r1 = r5 * val;                   // wavevec ^ 3
    float4 r2 = r1 * r5;                        // wavevec ^ 5;
    float4 r3 = r2 * r5;                        // wavevec ^ 7;


    //Vectors for taylor's series expansion of sin and cos
    float4 sin7 = {1, -0.16161616, 0.0083333, -0.00019841};
    float4 cos8  = {-0.5, 0.041666666, -0.0013888889, 0.000024801587};

    // sin
    s =  val + r1 * sin7.y + r2 * sin7.z + r3 * sin7.w;

    // cos
    c = 1 + r5 * cos8.x + r6 * cos8.y + r7 * cos8.z + r8 * cos8.w;
}

fixed4 TerrainWaveGrass (inout float4 vertex, float waveAmount, fixed4 color)
{
    float4 _waveXSize = float4(0.012, 0.02, 0.06, 0.024) * _WaveAndDistance.y;
    float4 _waveZSize = float4 (0.006, .02, 0.02, 0.05) * _WaveAndDistance.y;
    float4 waveSpeed = float4 (0.3, .5, .4, 1.2) * 4;

    float4 _waveXmove = float4(0.012, 0.02, -0.06, 0.048) * 2;
    float4 _waveZmove = float4 (0.006, .02, -0.02, 0.1);

    float4 waves;
    waves = vertex.x * _waveXSize;
    waves += vertex.z * _waveZSize;

    // Add in time to model them over time
    waves += _WaveAndDistance.x * waveSpeed;

    float4 s, c;
    waves = frac (waves);
    FastSinCos (waves, s,c);

    s = s * s;

    s = s * s;

    float lighting = dot (s, normalize (float4 (1,1,.4,.2))) * .7;

    s = s * waveAmount;

    float3 waveMove = float3 (0,0,0);
    waveMove.x = dot (s, _waveXmove);
    waveMove.z = dot (s, _waveZmove);

    vertex.xz -= waveMove.xz * _WaveAndDistance.z;

    // apply color animation

    // fix for dx11/etc warning
    fixed3 waveColor = lerp (fixed3(0.5,0.5,0.5), _WavingTint.rgb, fixed3(lighting,lighting,lighting));

    // Fade the grass out before detail distance.
    // Saturate because Radeon HD drivers on OS X 10.4.10 don't saturate vertex colors properly.
    float3 offset = vertex.xyz - _CameraPosition.xyz;
    color.a = saturate (2 * (_WaveAndDistance.w - dot (offset, offset)) * _CameraPosition.w);

    return fixed4(2 * waveColor * color.rgb, color.a);
}

void TerrainBillboardGrass( inout float4 pos, float2 offset )
{
    float3 grasspos = pos.xyz - _CameraPosition.xyz;
    if (dot(grasspos, grasspos) > _WaveAndDistance.w)
        offset = 0.0;
    pos.xyz += offset.x * _CameraRight.xyz;
    pos.xyz += offset.y * _CameraUp.xyz;
}

// Grass: appdata_full usage
// color        - .xyz = color, .w = wave scale
// normal       - normal
// tangent.xy   - billboard extrusion
// texcoord     - UV coords
// texcoord1    - 2nd UV coords

void WavingGrassVert (inout appdata_full v)
{
    // MeshGrass v.color.a: 1 on top vertices, 0 on bottom vertices
    // _WaveAndDistance.z == 0 for MeshLit
    float waveAmount = v.color.a * _WaveAndDistance.z;

    v.color = TerrainWaveGrass (v.vertex, waveAmount, v.color);
}

void WavingGrassBillboardVert (inout appdata_full v)
{
    TerrainBillboardGrass (v.vertex, v.tangent.xy);
    // wave amount defined by the grass height
    float waveAmount = v.tangent.y;
    v.color = TerrainWaveGrass (v.vertex, waveAmount, v.color);
}


// ---- Tree helpers


inline float4 Squash(in float4 pos)
{
    // To squash the tree the vertex needs to be moved in the direction
    // of the squash plane. The plane is defined by the the:
    // plane point - point lying on the plane, defined in model space
    // plane normal - _SquashPlaneNormal.xyz

    // we're pushing squashed tree plane in direction of planeNormal by amount of _SquashPlaneNormal.w
    // this squashing has to match logic of tree billboards

    float3 planeNormal = _SquashPlaneNormal.xyz;

    // unoptimized version:
    //float3 planePoint = -planeNormal * _SquashPlaneNormal.w;
    //float3 projectedVertex = pos.xyz + dot(planeNormal, (planePoint - pos)) * planeNormal;

    // optimized version:
    float3 projectedVertex = pos.xyz - (dot(planeNormal.xyz, pos.xyz) + _SquashPlaneNormal.w) * planeNormal;

    pos = float4(lerp(projectedVertex, pos.xyz, _SquashAmount), 1);

    return pos;
}

void TerrainAnimateTree( inout float4 pos, float alpha )
{
    pos.xyz *= _TreeInstanceScale.xyz;
    float3 bent = mul(_TerrainEngineBendTree, float4(pos.xyz, 0.0)).xyz;
    pos.xyz = lerp( pos.xyz, bent, alpha );

    pos = Squash(pos);
}


// ---- Billboarded tree helpers


void TerrainBillboardTree( inout float4 pos, float2 offset, float offsetz )
{
    float3 treePos = pos.xyz - _TreeBillboardCameraPos.xyz;
    float treeDistanceSqr = dot(treePos, treePos);
    if( treeDistanceSqr > _TreeBillboardDistances.x )
        offset.xy = offsetz = 0.0;

    // positioning of billboard vertices horizontally
    pos.xyz += _TreeBillboardCameraRight.xyz * offset.x;

    // tree billboards can have non-uniform scale,
    // so when looking from above (or bellow) we must use
    // billboard width as billboard height

    // 1) non-compensating
    //pos.xyz += _TreeBillboardCameraUp.xyz * offset.y;

    // 2) correct compensating (?)
    //float alpha = _TreeBillboardCameraPos.w;
    //float a = offset.y;
    //float b = offsetz;
        // 2a) using elipse-radius formula
        ////float r = abs(a * b) / sqrt(sqr(a * sin(alpha)) + sqr(b * cos(alpha))) * sign(b);
        //float r = abs(a) * b / sqrt(sqr(a * sin(alpha)) + sqr(b * cos(alpha)));
        // 2b) sin-cos lerp
        //float r = b * sin(alpha) + a * cos(alpha);
    //pos.xyz += _TreeBillboardCameraUp.xyz * r;

    // 3) incorrect compensating (using lerp)
    // _TreeBillboardCameraPos.w contains ImposterRenderTexture::GetBillboardAngleFactor()
    //float billboardAngleFactor = _TreeBillboardCameraPos.w;
    //float r = lerp(offset.y, offsetz, billboardAngleFactor);
    //pos.xyz += _TreeBillboardCameraUp.xyz * r;

    // so now we take solution #3 and complicate it even further...
    //
    // case 49851: Flying trees
    // The problem was that tree billboard was fixed on it's center, which means
    // the root of the tree is not fixed and can float around. This can be quite visible
    // on slopes (checkout the case on fogbugz for screenshots).
    //
    // We're fixing this by fixing billboards to the root of the tree.
    // Note that root of the tree is not necessary the bottom of the tree -
    // there might be significant part of the tree bellow terrain.
    // This fixation mode doesn't work when looking from above/below, because
    // billboard is so close to the ground, so we offset it by certain distance
    // when viewing angle is bigger than certain treshold (40 deg at the moment)

    // _TreeBillboardCameraPos.w contains ImposterRenderTexture::billboardAngleFactor
    float billboardAngleFactor = _TreeBillboardCameraPos.w;
    // The following line performs two things:
    // 1) peform non-uniform scale, see "3) incorrect compensating (using lerp)" above
    // 2) blend between vertical and horizontal billboard mode
    float radius = lerp(offset.y, offsetz, billboardAngleFactor);

    // positioning of billboard vertices veritally
    pos.xyz += _TreeBillboardCameraUp.xyz * radius;

    // _TreeBillboardCameraUp.w contains ImposterRenderTexture::billboardOffsetFactor
    float billboardOffsetFactor = _TreeBillboardCameraUp.w;
    // Offsetting billboad from the ground, so it doesn't get clipped by ztest.
    // In theory we should use billboardCenterOffsetY instead of offset.x,
    // but we can't because offset.y is not the same for all 4 vertices, so
    // we use offset.x which is the same for all 4 vertices (except sign).
    // And it doesn't matter a lot how much we offset, we just need to offset
    // it by some distance
    pos.xyz += _TreeBillboardCameraFront.xyz * abs(offset.x) * billboardOffsetFactor;
}


// ---- Tree Creator

float4 _Wind;

// Expand billboard and modify normal + tangent to fit
inline void ExpandBillboard (in float4x4 mat, inout float4 pos, inout float3 normal, inout float4 tangent)
{
    // tangent.w = 0 if this is a billboard
    float isBillboard = 1.0f - abs(tangent.w);

    // billboard normal
    float3 norb = normalize(mul(float4(normal, 0), mat)).xyz;

    // billboard tangent
    float3 tanb = normalize(mul(float4(tangent.xyz, 0.0f), mat)).xyz;

    pos += mul(float4(normal.xy, 0, 0), mat) * isBillboard;
    normal = lerp(normal, norb, isBillboard);
    tangent = lerp(tangent, float4(tanb, -1.0f), isBillboard);
}

float4 SmoothCurve( float4 x ) {
    return x * x *( 3.0 - 2.0 * x );
}
float4 TriangleWave( float4 x ) {
    return abs( frac( x + 0.5 ) * 2.0 - 1.0 );
}
float4 SmoothTriangleWave( float4 x ) {
    return SmoothCurve( TriangleWave( x ) );
}

// Detail bending
inline float4 AnimateVertex(float4 pos, float3 normal, float4 animParams)
{
    // animParams stored in color
    // animParams.x = branch phase
    // animParams.y = edge flutter factor
    // animParams.z = primary factor
    // animParams.w = secondary factor

    float fDetailAmp = 0.1f;
    float fBranchAmp = 0.3f;

    // Phases (object, vertex, branch)
    float fObjPhase = dot(unity_ObjectToWorld._14_24_34, 1);
    float fBranchPhase = fObjPhase + animParams.x;

    float fVtxPhase = dot(pos.xyz, animParams.y + fBranchPhase);

    // x is used for edges; y is used for branches
    float2 vWavesIn = _Time.yy + float2(fVtxPhase, fBranchPhase );

    // 1.975, 0.793, 0.375, 0.193 are good frequencies
    float4 vWaves = (frac( vWavesIn.xxyy * float4(1.975, 0.793, 0.375, 0.193) ) * 2.0 - 1.0);

    vWaves = SmoothTriangleWave( vWaves );
    float2 vWavesSum = vWaves.xz + vWaves.yw;

    // Edge (xz) and branch bending (y)
    float3 bend = animParams.y * fDetailAmp * normal.xyz;
    bend.y = animParams.w * fBranchAmp;
    pos.xyz += ((vWavesSum.xyx * bend) + (_Wind.xyz * vWavesSum.y * animParams.w)) * _Wind.w;

    // Primary bending
    // Displace position
    pos.xyz += animParams.z * _Wind.xyz;

    return pos;
}

#endif
