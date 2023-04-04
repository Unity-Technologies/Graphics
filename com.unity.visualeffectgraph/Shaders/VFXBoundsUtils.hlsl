#if !VFX_COMPUTE_BOUNDS
#define VFX_COMPUTE_BOUNDS 0
#endif

#define VFX_USE_BOUNDS_UTILS (VFX_COMPUTE_BOUNDS || VFX_COMPUTE_AABBS)

#if VFX_USE_BOUNDS_UTILS

float4x4 GetElementToVFX(VFXAttributes attributes, float3 size3)
{
    return GetElementToVFXMatrix(
           attributes.axisX,
           attributes.axisY,
           attributes.axisZ,
           float3(attributes.angleX, attributes.angleY, attributes.angleZ),
           float3(attributes.pivotX, attributes.pivotY, attributes.pivotZ),
           size3,
           attributes.position);
}

float4x4 GetElementToLocal(VFXAttributes attributes, float3 size3, float4x4 worldToLocal)
{
    float4x4 elementToVFX = GetElementToVFXMatrix(
           attributes.axisX,
           attributes.axisY,
           attributes.axisZ,
           float3(attributes.angleX, attributes.angleY, attributes.angleZ),
           float3(attributes.pivotX, attributes.pivotY, attributes.pivotZ),
           size3,
           attributes.position);

    #if VFX_WORLD_SPACE
        float4x4 elementToLocal = mul(worldToLocal, elementToVFX);
    #else
        float4x4 elementToLocal = elementToVFX;
    #endif

    return elementToLocal;
}

float ComputeBoundingRadius3DFromMatrix(float4x4 mat, out float3 center)
{
    center = mat._m03_m13_m23;
    float xAxisSqrLength = dot(mat._m00_m10_m20, mat._m00_m10_m20);
    float yAxisSqrLength = dot(mat._m01_m11_m21, mat._m01_m11_m21);
    float zAxisSqrLength = dot(mat._m02_m12_m22, mat._m02_m12_m22);
    float radius = 0.5f * sqrt(xAxisSqrLength + yAxisSqrLength + zAxisSqrLength);
    return radius;
}

//Bounding sphere of a flat element in the plane xy
float ComputeBoundingRadius2DFromMatrix(float4x4 mat, out float3 center)
{
    center = mat._m03_m13_m23;
    float xAxisSqrLength = dot(mat._m00_m10_m20, mat._m00_m10_m20);
    float yAxisSqrLength = dot(mat._m01_m11_m21, mat._m01_m11_m21);
    float radius = 0.5f * sqrt(xAxisSqrLength + yAxisSqrLength);
    return radius;
}

float GetBoundingRadius3D_Local(VFXAttributes attributes, float3 size3, float4x4 worldToLocal, out float3 localPos)
{
    float4x4 elementToLocal = GetElementToLocal(attributes, size3, worldToLocal);
    return ComputeBoundingRadius3DFromMatrix(elementToLocal, localPos);
}

float GetBoundingRadius2D_VFX(VFXAttributes attributes, float3 size3, out float3 vfxPos)
{
    float4x4 elementToVFX = GetElementToVFX(attributes, size3);
    return ComputeBoundingRadius2DFromMatrix(elementToVFX, vfxPos);
}

float3 GetTightBoundingExtents_VFX(VFXAttributes attributes, float3 size3, out float3 vfxPos)
{
    float4x4 mat = GetElementToVFX(attributes, size3);
    vfxPos = mat._m03_m13_m23;
    float3 extents;
    extents.x  = max(abs(mat._m00), abs(mat._m01));
    extents.y  = max(abs(mat._m10), abs(mat._m11));
    extents.z  = max(abs(mat._m20), abs(mat._m21));

    return extents;
}

#if VFX_COMPUTE_BOUNDS
RWStructuredBuffer<uint> boundsBuffer; // 3 uint for min, 3 uint for max

groupshared uint sMinPositionsX[NB_THREADS_PER_GROUP];
groupshared uint sMinPositionsY[NB_THREADS_PER_GROUP];
groupshared uint sMinPositionsZ[NB_THREADS_PER_GROUP];
groupshared uint sMaxPositionsX[NB_THREADS_PER_GROUP];
groupshared uint sMaxPositionsY[NB_THREADS_PER_GROUP];
groupshared uint sMaxPositionsZ[NB_THREADS_PER_GROUP];


//http://stereopsis.com/radix.html
uint FloatFlip(uint f)
{
    uint mask = -int(f >> 31) | 0x80000000;
    return f ^ mask;
}

uint IFloatFlip(uint f)
{
    uint mask = ((f >> 31) - 1) | 0x80000000;
    return f ^ mask;
}

void InitReduction(VFXAttributes attributes, float3 size3, uint tid, float4x4 worldToLocal)
{
    if (attributes.alive)
    {
        float3 localPos;
        //Bounds are computed in local space to avoid slow and overly conservative AABB transform later
        float radius = GetBoundingRadius3D_Local(attributes, size3, worldToLocal, localPos);
        sMinPositionsX[tid] = FloatFlip(asuint(localPos.x - radius));
        sMinPositionsY[tid] = FloatFlip(asuint(localPos.y - radius));
        sMinPositionsZ[tid] = FloatFlip(asuint(localPos.z - radius));
        sMaxPositionsX[tid] = FloatFlip(asuint(localPos.x + radius));
        sMaxPositionsY[tid] = FloatFlip(asuint(localPos.y + radius));
        sMaxPositionsZ[tid] = FloatFlip(asuint(localPos.z + radius));

    }
    else
    {
        sMinPositionsX[tid] = 0xffffffff;
        sMinPositionsY[tid] = 0xffffffff;
        sMinPositionsZ[tid] = 0xffffffff;
        sMaxPositionsX[tid] = 0u;
        sMaxPositionsY[tid] = 0u;
        sMaxPositionsZ[tid] = 0u;
    }
}

void PerformBoundsReduction(uint id, uint tid, uint instanceIndex, uint nbMax)
{
    if(id >= nbMax)
    {
        sMinPositionsX[tid] = 0xffffffff;
        sMinPositionsY[tid] = 0xffffffff;
        sMinPositionsZ[tid] = 0xffffffff;
        sMaxPositionsX[tid] = 0u;
        sMaxPositionsY[tid] = 0u;
        sMaxPositionsZ[tid] = 0u;
    }
    GroupMemoryBarrierWithGroupSync();
    for (uint s = NB_THREADS_PER_GROUP / 2; s > 0; s >>= 1) {
        if (tid < s)
        {
            sMinPositionsX[tid] = min(sMinPositionsX[tid], sMinPositionsX[tid + s]);
            sMinPositionsY[tid] = min(sMinPositionsY[tid], sMinPositionsY[tid + s]);
            sMinPositionsZ[tid] = min(sMinPositionsZ[tid], sMinPositionsZ[tid + s]);
            sMaxPositionsX[tid] = max(sMaxPositionsX[tid], sMaxPositionsX[tid + s]);
            sMaxPositionsY[tid] = max(sMaxPositionsY[tid], sMaxPositionsY[tid + s]);
            sMaxPositionsZ[tid] = max(sMaxPositionsZ[tid], sMaxPositionsZ[tid + s]);
        }

        GroupMemoryBarrierWithGroupSync();
    }
    if (tid == 0)
    {
        uint boundsBufferBaseIndex = instanceIndex * 6;
        InterlockedMin(boundsBuffer[boundsBufferBaseIndex + 0], sMinPositionsX[tid]);
        InterlockedMin(boundsBuffer[boundsBufferBaseIndex + 1], sMinPositionsY[tid]);
        InterlockedMin(boundsBuffer[boundsBufferBaseIndex + 2], sMinPositionsZ[tid]);
        InterlockedMax(boundsBuffer[boundsBufferBaseIndex + 3], sMaxPositionsX[tid]);
        InterlockedMax(boundsBuffer[boundsBufferBaseIndex + 4], sMaxPositionsY[tid]);
        InterlockedMax(boundsBuffer[boundsBufferBaseIndex + 5], sMaxPositionsZ[tid]);
    }
}

#endif

#if VFX_COMPUTE_AABBS
struct AABB
{
    float3 boxMin;
    float3 boxMax;
};

RWStructuredBuffer<AABB> aabbBuffer;

void FillAabbBuffer(VFXAttributes attributes, float3 size3, uint index, uint instanceIndex, int decimationFactor)
{
    AABB aabb;
    int aabbIndex = VFX_AABB_COUNT * instanceIndex + index / decimationFactor;
    int remainder = index % decimationFactor;

    if(remainder == 0)
    {
        if (attributes.alive)
        {
            float3 vfxPos;
            #if VFX_FACE_RAY
                float radius = GetBoundingRadius2D_VFX(attributes, size3, vfxPos);
                aabb.boxMin = vfxPos - float3(radius, radius, radius);
                aabb.boxMax = vfxPos + float3(radius, radius, radius);
            #else
                float3 extents = GetTightBoundingExtents_VFX(attributes, size3, vfxPos);
                aabb.boxMin = vfxPos - float3(extents.x, extents.y, extents.z);
                aabb.boxMax = vfxPos + float3(extents.x, extents.y, extents.z);
            #endif

            aabbBuffer[aabbIndex] = aabb;
        }
        else
        {
            aabb.boxMin = float3(VFX_NAN, VFX_NAN, VFX_NAN);
            aabb.boxMax = float3(VFX_NAN, VFX_NAN, VFX_NAN);
            aabbBuffer[aabbIndex] = aabb;
        }
    }
}
#endif
#endif
