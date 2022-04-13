#ifdef DEBUG_DISPLAY // Guard define here to be compliant with how shader graph generate code for include

#ifndef UNITY_DEBUG_DISPLAY_MATERIAL_INCLUDED
#define UNITY_DEBUG_DISPLAY_MATERIAL_INCLUDED

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/DebugDisplay.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Builtin/BuiltinData.cs.hlsl"

bool GetMaterialDebugColor(inout float4 color
#ifndef VFX_VARYING_PS_INPUTS
    , const FragInputs input
#endif
    , const BuiltinData builtinData
    , const PositionInputs posInput
    , const SurfaceData surfaceData
    , const BSDFData bsdfData)
{
    // Reminder: _DebugViewMaterialArray[i]
    //   i==0 -> the size used in the buffer
    //   i>0  -> the index used (0 value means nothing)
    // The index stored in this buffer could either be
    //   - a gBufferIndex (always stored in _DebugViewMaterialArray[1] as only one supported)
    //   - a property index which is different for each kind of material even if reflecting the same thing (see MaterialSharedProperty)
    bool found = false;
    int bufferSize = _DebugViewMaterialArray[0].x;
    if (bufferSize != 0)
    {
        bool needLinearToSRGB = false;
        float3 result = float3(1.0, 0.0, 1.0);

        // Loop through the whole buffer
        // Works because GetSurfaceDataDebug will do nothing if the index is not a known one
        for (int index = 1; index <= bufferSize; index++)
        {
            int indexMaterialProperty = _DebugViewMaterialArray[index].x;

            // skip if not really in use
            if (indexMaterialProperty != 0)
            {
                found = true;

                GetPropertiesDataDebug(indexMaterialProperty, result, needLinearToSRGB);
#ifndef VFX_VARYING_PS_INPUTS
                GetVaryingsDataDebug(indexMaterialProperty, input, result, needLinearToSRGB);
#endif
                GetBuiltinDataDebug(indexMaterialProperty, builtinData, posInput, result, needLinearToSRGB);
                GetSurfaceDataDebug(indexMaterialProperty, surfaceData, result, needLinearToSRGB);
                GetBSDFDataDebug(indexMaterialProperty, bsdfData, result, needLinearToSRGB);
            }
        }

        // TEMP!
        // For now, the final blit in the backbuffer performs an sRGB write
        // So in the meantime we apply the inverse transform to linear data to compensate, unless we output to AOVs.
        if (!needLinearToSRGB && _DebugAOVOutput == 0)
            result = SRGBToLinear(max(0, result));

        color = float4(result, 1.0);
    }
    return found;
}

#endif // UNITY_DEBUG_DISPLAY_MATERIAL_INCLUDED

#endif // DEBUG_DISPLAY
