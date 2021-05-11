#ifndef UNIVERSAL_BAKEDLIT_META_PASS_INCLUDED
#define UNIVERSAL_BAKEDLIT_META_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/Shaders/UnlitMetaPass.hlsl"

//LWRP -> Universal Backwards Compatibility
half4 LightweightFragmentMetaBakedLit(Varyings input) : SV_Target
{
    return LightweightFragmentMetaUnlit(input);
}

#endif
