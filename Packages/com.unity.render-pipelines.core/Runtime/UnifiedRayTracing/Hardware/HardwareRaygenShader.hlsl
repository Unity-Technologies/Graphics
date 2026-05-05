#pragma max_recursion_depth 1

[shader("raygeneration")]
void MainRayGenShader()
{
    UnifiedRT::DispatchInfo dispatchInfo;
    dispatchInfo.dispatchThreadID = DispatchRaysIndex();
    dispatchInfo.dispatchDimensionsInThreads = DispatchRaysDimensions();
    dispatchInfo.localThreadIndex = 0;
    dispatchInfo.globalThreadIndex = DispatchRaysIndex().x + DispatchRaysIndex().y * DispatchRaysDimensions().x + DispatchRaysIndex().z * (DispatchRaysDimensions().x * DispatchRaysDimensions().y);

    UNIFIED_RT_RAYGEN_FUNC(dispatchInfo);
}

// miss shader needs to be always defined
[shader("miss")]
void MissShader(inout UNIFIED_RT_PAYLOAD payload : SV_RayPayload)
{
#ifdef UNIFIED_RT_MISS_FUNC
    UnifiedRT::HitContext hitContext = (UnifiedRT::HitContext)0;
    UNIFIED_RT_MISS_FUNC(hitContext, payload);
#endif
}

namespace UnifiedRT
{
    struct AttributeData
    {
        float2 barycentrics;
        #ifdef UNIFIED_RT_ADDITIONAL_INTERSECTION_ATTRIBS
            UNIFIED_RT_ADDITIONAL_INTERSECTION_ATTRIBS additionalAttribs;
        #endif
    };
}

#ifdef UNIFIED_RT_CLOSESTHIT_FUNC
[shader("closesthit")]
void ClosestHitShader(inout UNIFIED_RT_PAYLOAD payload : SV_RayPayload, UnifiedRT::AttributeData attribs : SV_IntersectionAttributes)
{
    UnifiedRT::HitContext hitContext;
    hitContext.barycentrics = attribs.barycentrics;

    #ifdef UNIFIED_RT_ADDITIONAL_INTERSECTION_ATTRIBS
        UNIFIED_RT_CLOSESTHIT_FUNC(hitContext, payload, attribs.additionalAttribs);
    #else
        UNIFIED_RT_CLOSESTHIT_FUNC(hitContext, payload);
    #endif
}
#endif

#ifdef UNIFIED_RT_ANYHIT_FUNC
[shader("anyhit")]
void AnyHitShader(inout UNIFIED_RT_PAYLOAD payload : SV_RayPayload, UnifiedRT::AttributeData attribs : SV_IntersectionAttributes)
{
    UnifiedRT::HitContext hitContext;
    hitContext.barycentrics = attribs.barycentrics;

    uint res = UNIFIED_RT_ANYHIT_FUNC(hitContext, payload);

    if (res == UnifiedRT::kIgnoreHit)
        IgnoreHit();

    if (res == UnifiedRT::kAcceptHitAndEndSearch)
        AcceptHitAndEndSearch();

    // UnifiedRT::kAcceptHit: As specified in DXR, simply exiting means the hit is accepted.
}
#endif

#ifdef UNIFIED_RT_INTERSECTION_FUNC
[shader("intersection")]
void IntersectionShader()
{
    UnifiedRT::HitContext hitContext;
    #ifdef UNIFIED_RT_ADDITIONAL_INTERSECTION_ATTRIBS
        float hitT;
        float2 hitUvAttributes;
        UNIFIED_RT_ADDITIONAL_INTERSECTION_ATTRIBS hitAttribs;
        if (UNIFIED_RT_INTERSECTION_FUNC(hitContext, hitT, hitUvAttributes, hitAttribs))
        {
            UnifiedRT::AttributeData attribs;
            attribs.barycentrics = hitUvAttributes;
            attribs.additionalAttribs = hitAttribs;
            ReportHit(hitT, 0, attribs);
        }
    #else
        float hitT;
        float2 hitUvAttributes;
        if (UNIFIED_RT_INTERSECTION_FUNC(hitContext, hitT, hitUvAttributes))
        {
            UnifiedRT::AttributeData attribs;
            attribs.barycentrics = hitUvAttributes;
            ReportHit(hitT, 0, attribs);
        }
    #endif

}
#endif
