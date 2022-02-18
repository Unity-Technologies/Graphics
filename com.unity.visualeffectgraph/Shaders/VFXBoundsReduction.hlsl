#if VFX_COMPUTE_BOUNDS
RWBuffer<uint> boundsBuffer; // 3 uint for min, 3 uint for max

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
        float4x4 elementToVFX = GetElementToVFXMatrix(
            attributes.axisX,
            attributes.axisY,
            attributes.axisZ,
            float3(attributes.angleX, attributes.angleY, attributes.angleZ),
            float3(attributes.pivotX, attributes.pivotY, attributes.pivotZ),
            size3,
            attributes.position);

        #if VFX_WORLD_SPACE
        // The bounding box is always calculated in localSpace since the AABB in the graph is in local space.
        // That avoids unnecessary and overly conservation AABB changes of space.
        elementToVFX = mul(worldToLocal, elementToVFX);
        #endif

        float3 localPos = elementToVFX._m03_m13_m23;

        //Bounding sphere

        float xAxisSqrLength = dot(elementToVFX._m00_m10_m20, elementToVFX._m00_m10_m20);
        float yAxisSqrLength = dot(elementToVFX._m01_m11_m21, elementToVFX._m01_m11_m21);
        float zAxisSqrLength = dot(elementToVFX._m02_m12_m22, elementToVFX._m02_m12_m22);
        float radius = 0.5f * sqrt(xAxisSqrLength + yAxisSqrLength + zAxisSqrLength);

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

void PerformBoundsReduction(uint id, uint tid)
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
    [unroll((int)(log2(NB_THREADS_PER_GROUP) + 0.5f))]
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
        InterlockedMin(boundsBuffer[0], sMinPositionsX[tid]);
        InterlockedMin(boundsBuffer[1], sMinPositionsY[tid]);
        InterlockedMin(boundsBuffer[2], sMinPositionsZ[tid]);
        InterlockedMax(boundsBuffer[3], sMaxPositionsX[tid]);
        InterlockedMax(boundsBuffer[4], sMaxPositionsY[tid]);
        InterlockedMax(boundsBuffer[5], sMaxPositionsZ[tid]);
    }
}

#endif
