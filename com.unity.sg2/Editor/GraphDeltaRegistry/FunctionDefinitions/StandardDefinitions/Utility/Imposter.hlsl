#ifndef SHADERGRAPHIMPOSTER
#define SHADERGRAPHIMPOSTER

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Sampling/Sampling.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ParallaxMapping.hlsl"

struct Ray
{
    float3 Origin;
    float3 Direction;
};

float3 CoordToHemi(float2 coord)
{
    coord = float2(coord.x + coord.y, coord.x - coord.y) * 0.5;
    float3 dir = float3(coord.x, 1 - dot(float2(1, 1), abs(coord.xy)), coord.y);
    return dir;
}

float3 CoordToOcta(float2 coord)
{
    float3 dir = float3(coord.x, 1 - dot(1, abs(coord)), coord.y);
    if (dir.y < 0)
    {
        float2 flip = dir.xz >= 0 ? float2(1, 1) : float2(-1, -1);
        dir.xz = (1 - abs(dir.zx)) * flip;
    }
    return dir;
}

float3 GridToVector(float2 coord, bool IsHemi)
{
    float3 dir;
    if (IsHemi)
    {
        dir = CoordToHemi(coord);
    }
    else
    {
        dir = CoordToOcta(coord);
    }
    return dir;
}

//for hemisphere
float2 VectorToHemi(float3 dir)
{
    dir.xz /= dot(1.0, abs(dir));
    return float2(dir.x + dir.z, dir.x - dir.z);
}

float2 VectorToOcta(float3 dir)
{
    dir.xz /= dot(1, abs(dir));
    if (dir.y <= 0)
    {
        dir.xz = (1 - abs(dir.zx)) * (dir.xz >= 0 ? 1.0 : -1.0);
    }
    return dir.xz;
}

float4 CalculateWeights(float2 uv)
{
    uv = frac(uv);

    float2 oneMinusUV = 1 - uv.xy;

    float4 weights;
    //frame 0
    weights.x = min(oneMinusUV.x, oneMinusUV.y);
    //frame 1
    weights.y = abs(dot(uv, float2(1.0, -1.0)));
    //frame 2
    weights.z = min(uv.x, uv.y);
    //mask
    weights.w = saturate(ceil(uv.x - uv.y));

    return weights;
}

float3 FrameTransformLocal(float3 BillboardPosToCam, float3 normal, out float3 worldX, out float3 worldZ)
{
    float3 upVector = float3 (0, 1, 0);
    worldX = normalize(cross(upVector, normal));
    worldZ = normalize(cross(worldX, normal));

    BillboardPosToCam *= -1.0;

    float3x3 worldToLocal = float3x3(worldX, worldZ, normal);
    float3 localRay = normalize(mul(worldToLocal, BillboardPosToCam));
    return localRay;
}

float2 PlaneIntersectionUV(float3 planeNormal, float3 planeX, float3 planeZ, float3 center, float2 UVscale, Ray ray)
{
    float normalDotOrigin = dot(planeNormal, -ray.Origin);//(p0 - l0) . n
    float normalDotRay = dot(planeNormal, ray.Direction);//l.n
    float planeDistance = normalDotOrigin / normalDotRay;//distance >0 then intersecting

    //intersect = rayDir * distance + rayPos
    float3 intersection = ((ray.Direction * planeDistance) + ray.Origin) - center;

    float dx = dot(planeX, intersection);
    float dz = dot(planeZ, intersection);

    float2 uv = float2(0, 0);

    if (planeDistance > 0)
    {
        uv = -float2(dx, dz);
    }
    else
    {
        uv = float2(0, 0);
    }

    uv /= UVscale;
    uv += float2(0.5, 0.5);
    return uv;
}



float3 CalculateBillboardProjection(float3 objectSpaceCameraDir, float2 uv)
{
    float3 x = normalize(cross(objectSpaceCameraDir, float3(0.0, 1.0, 0.0)));
    float3 z = normalize(cross(x, objectSpaceCameraDir));

    uv = uv * 2.0 - 1.0;

    float3 newX = x * uv.x;
    float3 newZ = z * uv.y;

    float3 result = newX + newZ;
    return result;
}

//Calculate Vertex postion and UVs
void ImposterUV(in float3 inPos, in float4 inUV, in float imposterFrames, in float3 imposterOffset, in float imposterSize, in float imposterFrameClip,
    in bool isHemi, in float parallax, in int heightMapChannel, in SamplerState ss, in Texture2D heightMap, in float4 mapTexelSize,
    out float3 outPos, out float4 outWeights, out float2 outUV0, out float2 outUV1, out float2 outUV2, out float4 outUVGrid)
{
    float framesMinusOne = imposterFrames - 1;

    //camera pos in object space
    float3 objectSpaceCameraPos = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos.xyz, 1)).xyz - imposterOffset;
    float3 objectSpaceCameraDir = normalize(objectSpaceCameraPos.xyz);

    //get uv in a single frame
    float2 UVscaled = inUV.xy * (1.0 / imposterFrames);
    float2 size = imposterSize.xx * 1.33;

    float3 BillboardPos = CalculateBillboardProjection(objectSpaceCameraDir, inUV.xy);
    BillboardPos *= 0.67;

    //camera to projection vector
    float3 rayDirLocal = BillboardPos - objectSpaceCameraPos;

    //BillboardPos position to camera ray
    float3 BillboardPosToCam = normalize(objectSpaceCameraPos - BillboardPos);

    Ray ray;
    ray.Origin = objectSpaceCameraPos;
    ray.Direction = rayDirLocal;

    //set up virtual grid
    float2 grid;
    if (isHemi) {
        objectSpaceCameraDir.y = max(0.001, objectSpaceCameraDir.y);
        grid = VectorToHemi(objectSpaceCameraDir);
    }
    else {
        grid = VectorToOcta(objectSpaceCameraDir);
    }

    grid = saturate(grid * 0.5 + 0.5); //scale to 0 to 1 
    grid *= framesMinusOne;//multiply framesMinusOne to cover the texture

    float2 gridFrac = frac(grid);
    float2 gridFloor = floor(grid);

    float4 weights = CalculateWeights(gridFrac);

    //set up for octahedron:
    //1.find the nearest 3 frames
    //2.base on the grid find the direction intersect with the octahedron
    //3.construct the face/plane for that direction
    //4.base on the plane and find the virtual uv coord

    //get the 3 nearest frames
    float2 frame0 = gridFloor;
    float2 frame1 = gridFloor + lerp(float2(0, 1), float2(1, 0), weights.w);
    float2 frame2 = gridFloor + float2(1, 1);

    //convert frame coord to octahedron ray
    float3 frame0ray = normalize(GridToVector(float2(frame0 / framesMinusOne * 2 - 1), isHemi));
    float3 frame1ray = normalize(GridToVector(float2(frame1 / framesMinusOne * 2 - 1), isHemi));
    float3 frame2ray = normalize(GridToVector(float2(frame2 / framesMinusOne * 2 - 1), isHemi));

    float3 center = float3(0, 0, 0);

    float3 plane0x;
    float3 plane0normal = frame0ray;
    float3 plane0z;
    float3 frame0local = FrameTransformLocal(BillboardPosToCam, frame0ray, plane0x, plane0z);

    float2 vUv0 = PlaneIntersectionUV(plane0normal, plane0x, plane0z, center, size, ray);
    vUv0 /= imposterFrames;

    float3 plane1x;
    float3 plane1normal = frame1ray;
    float3 plane1z;
    float3 frame1local = FrameTransformLocal(BillboardPosToCam, frame1ray, plane1x, plane1z);

    float2 vUv1 = PlaneIntersectionUV(plane1normal, plane1x, plane1z, center, size, ray);
    vUv1 /= imposterFrames;

    float3 plane2x;
    float3 plane2normal = frame2ray;
    float3 plane2z;
    float3 frame2local = FrameTransformLocal(BillboardPosToCam, frame2ray, plane2x, plane2z);

    float2 vUv2 = PlaneIntersectionUV(plane2normal, plane2x, plane2z, center, size, ray);
    vUv2 /= imposterFrames;

    frame0local.xy /= imposterFrames;
    frame1local.xy /= imposterFrames;
    frame2local.xy /= imposterFrames;

    float2 gridSnap = floor(grid) / imposterFrames.xx;

    float2 frame0grid = gridSnap;
    float2 frame1grid = gridSnap + (lerp(float2(0, 1), float2(1, 0), weights.w) / imposterFrames.xx);
    float2 frame2grid = gridSnap + (float2(1, 1) / imposterFrames.xx);

    float2 vp0uv = frame0grid + vUv0.xy;
    float2 vp1uv = frame1grid + vUv1.xy;
    float2 vp2uv = frame2grid + vUv2.xy;

    //vert pos
    outPos = BillboardPos + imposterOffset;

    //frame size ->2048/12 = 170.6
    float frameSize = mapTexelSize.z / imposterFrames;
    //actual texture size used -> 170*12 = 2040
    float actualTextureSize = floor(frameSize) * imposterFrames;
    //the  scalar -> 2048/2040 = 0.99609375
    float scalar = mapTexelSize.z / actualTextureSize;

    vp0uv *= scalar;
    vp0uv *= scalar;
    vp0uv *= scalar;

    if (parallax != 0) {

        float h0 = SAMPLE_TEXTURE2D(heightMap, ss, vp0uv)[heightMapChannel];
        float h1 = SAMPLE_TEXTURE2D(heightMap, ss, vp1uv)[heightMapChannel];
        float h2 = SAMPLE_TEXTURE2D(heightMap, ss, vp2uv)[heightMapChannel];

        vp0uv += (0.5 - h0) * frame0local.xy * parallax;
        vp1uv += (0.5 - h1) * frame1local.xy * parallax;
        vp2uv += (0.5 - h2) * frame2local.xy * parallax;
    }
    //clip out neighboring frames 
    float2 gridSize = 1.0 / imposterFrames.xx;
    float2 bleeds = mapTexelSize.xy * imposterFrameClip;
    vp0uv = clamp(vp0uv, frame0grid - bleeds, frame0grid + gridSize + bleeds);
    vp1uv = clamp(vp1uv, frame1grid - bleeds, frame1grid + gridSize + bleeds);
    vp2uv = clamp(vp2uv, frame2grid - bleeds, frame2grid + gridSize + bleeds);

    //surface
    outUV0 = vp0uv;
    outUV1 = vp1uv;
    outUV2 = vp2uv;
    outWeights = weights;
    outUVGrid.xy = UVscaled;
    outUVGrid.zw = grid;
}

void ImposterUV_oneFrame(in float3 inPos, in float4 inUV, in float imposterFrames, in float3 imposterOffset, in float imposterSize, in float imposterFrameClip,
    in bool isHemi, in float parallax, in int heightMapChannel, in SamplerState ss, in Texture2D heightMap, in float4 mapTexelSize,
    out float3 outPos, out float4 outWeights, out float2 outUV0, out float4 outUVGrid)
{
    float framesMinusOne = imposterFrames - 1;

    //camera pos in object space
    float3 objectSpaceCameraPos = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos.xyz, 1)).xyz - imposterOffset;
    float3 objectSpaceCameraDir = normalize(objectSpaceCameraPos.xyz);

    //get uv in a single frame
    float2 UVscaled = inUV.xy * (1.0 / imposterFrames);
    float2 size = imposterSize.xx * 1.33;

    float3 BillboardPos = CalculateBillboardProjection(objectSpaceCameraDir, inUV.xy);
    BillboardPos *= 0.67;

    //camera to projection vector
    float3 rayDirLocal = BillboardPos - objectSpaceCameraPos;

    //BillboardPos position to camera ray
    float3 BillboardPosToCam = normalize(objectSpaceCameraPos - BillboardPos);

    Ray ray;
    ray.Origin = objectSpaceCameraPos;
    ray.Direction = rayDirLocal;

    //set up virtual grid
    float2 grid;
    if (isHemi) {
        objectSpaceCameraDir.y = max(0.001, objectSpaceCameraDir.y);
        grid = VectorToHemi(objectSpaceCameraDir);
    }
    else {
        grid = VectorToOcta(objectSpaceCameraDir);
    }

    grid = saturate(grid * 0.5 + 0.5); //scale to 0 to 1 
    grid *= framesMinusOne;//multiply framesMinusOne to cover the texture

    float2 gridFrac = frac(grid);
    float2 gridFloor = floor(grid);

    //convert frame coord to octahedron ray
    float3 frame0ray = normalize(GridToVector(float2(gridFloor / framesMinusOne * 2 - 1), isHemi));

    float3 center = float3(0, 0, 0);

    float3 plane0x;
    float3 plane0normal = frame0ray;
    float3 plane0z;
    float3 frame0local = FrameTransformLocal(BillboardPosToCam, frame0ray, plane0x, plane0z);

    float2 vUv0 = PlaneIntersectionUV(plane0normal, plane0x, plane0z, center, size, ray);
    vUv0 /= imposterFrames;

    frame0local.xy /= imposterFrames;

    float2 gridSnap = floor(grid) / imposterFrames.xx;
    float2 frame0grid = gridSnap;
    float2 vp0uv = frame0grid + vUv0.xy;

    //vert pos
    outPos = BillboardPos + imposterOffset;

    //frame size ->2048/12 = 170.6
    float frameSize = mapTexelSize.z / imposterFrames;
    //actual texture size used -> 170*12 = 2040
    float actualTextureSize = floor(frameSize) * imposterFrames;
    //the  scalar -> 2048/2040 = 0.99609375
    float scalar = mapTexelSize.z / actualTextureSize;

    vp0uv *= scalar;

    if (parallax != 0) {

        float h0 = SAMPLE_TEXTURE2D(heightMap, ss, vp0uv)[heightMapChannel];
        vp0uv += (0.5 - h0) * frame0local.xy * parallax;
    }
    //clip out neighboring frames 
    float2 gridSize = 1.0 / imposterFrames.xx;
    float2 bleeds = mapTexelSize.xy * imposterFrameClip;
    vp0uv = clamp(vp0uv, frame0grid - bleeds, frame0grid + gridSize + bleeds);

    //surface
    outUV0 = vp0uv;
    outUVGrid.xy = UVscaled;
    outUVGrid.zw = grid;

}
//Sample from UVs and blend bases on the weights
void ImposterSample(in texture2D Texture, in float4 weights, in float4 inUVGrid,
    in float2 inUV0, in float2 inUV1, in float2 inUV2, in SamplerState ss, out float4 outColor)
{
    outColor = SAMPLE_TEXTURE2D_GRAD(Texture, ss, inUV0, ddx(inUVGrid.xy), ddy(inUVGrid.xy)) * weights.x
        + SAMPLE_TEXTURE2D_GRAD(Texture, ss, inUV1, ddx(inUVGrid.xy), ddy(inUVGrid.xy)) * weights.y
        + SAMPLE_TEXTURE2D_GRAD(Texture, ss, inUV2, ddx(inUVGrid.xy), ddy(inUVGrid.xy)) * weights.z;
}

void ImposterSample_oneFrame(in texture2D Texture, in float4 inUVGrid,
    in float2 inUV0, in SamplerState ss, out float4 outColor)
{
    outColor = SAMPLE_TEXTURE2D_GRAD(Texture, ss, inUV0, ddx(inUVGrid.xy), ddy(inUVGrid.xy));
}

#endif 
