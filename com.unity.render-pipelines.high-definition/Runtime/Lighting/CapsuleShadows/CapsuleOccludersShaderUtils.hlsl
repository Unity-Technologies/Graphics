// This is just a grabbag of stuff for now before we actually start, hacking together some functions that we'll need but we don't have a place for yet. 



// This is a shell for the scalarized loop through the sphere occluders.

void SphereOccludersLoop()
{
    uint sphereCount, sphereStart;

#ifndef LIGHTLOOP_DISABLE_TILE_AND_CLUSTER
    GetCountAndStart(posInput, LIGHTCATEGORY_PUNCTUAL, sphereStart, sphereCount);
#else   // LIGHTLOOP_DISABLE_TILE_AND_CLUSTER
    sphereCount = _PunctualLightCount;
    sphereStart = 0;
#endif

    bool fastPath = false;
#if SCALARIZE_LIGHT_LOOP
    uint sphereStartLane0;
    fastPath = IsFastPath(sphereStart, sphereStartLane0);

    if (fastPath)
    {
        sphereStart = sphereStartLane0;
    }
#endif

    // Scalarized loop. All spheres that are in a tile/cluster touched by any pixel in the wave are loaded (scalar load), only the one relevant to current thread/pixel are processed.
    // For clarity, the following code will follow the convention: variables starting with s_ are meant to be wave uniform (meant for scalar register),
    // v_ are variables that might have different value for each thread in the wave (meant for vector registers).
    // This will perform more loads than it is supposed to, however, the benefits should offset the downside, especially given that light data accessed should be largely coherent.
    // Note that the above is valid only if wave intriniscs are supported.
    uint v_sphereListOffset = 0;
    uint v_lightIdx = sphereStart;

    while (v_sphereListOffset < sphereCount)
    {
        v_lightIdx = FetchIndex(sphereStart, v_sphereListOffset);
        uint s_lightIdx = ScalarizeElementIndex(v_lightIdx, fastPath);
        if (s_lightIdx == -1)
            break;

        LightData s_lightData = FetchLight(s_lightIdx);

        // If current scalar and vector light index match, we process the light. The v_sphereListOffset for current thread is increased.
        // Note that the following should really be ==, however, since helper lanes are not considered by WaveActiveMin, such helper lanes could
        // end up with a unique v_lightIdx value that is smaller than s_lightIdx hence being stuck in a loop. All the active lanes will not have this problem.
        if (s_lightIdx >= v_lightIdx)
        {
            v_sphereListOffset++;
            if (IsMatchingLightLayer(s_lightData.lightLayers, builtinData.renderingLayers))
            {
                DirectLighting lighting = EvaluateBSDF_Punctual(context, V, posInput, preLightData, s_lightData, bsdfData, builtinData);
                AccumulateDirectLighting(lighting, aggregateLighting);
            }
        }
    }
}
