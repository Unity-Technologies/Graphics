#ifndef CAPSULE_OCCLUDER_DATA_HLSL
#define CAPSULE_OCCLUDER_DATA_HLSL

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/CapsuleShadows/CapsuleOccluderData.cs.hlsl"

uint GetCasterType(CapsuleShadowCaster caster) { return caster.casterTypeAndLayerMask & 0xffU; }
uint GetLayerMask(CapsuleShadowCaster caster) { return caster.casterTypeAndLayerMask >> 8; }

CapsuleShadowVolume makeCapsuleShadowVolume(uint occluderIndex, uint casterIndex, uint casterType)
{
    CapsuleShadowVolume volume;
    volume.bits = (occluderIndex << 16) | (casterIndex << 8) | casterType;
    return volume;
}
uint GetOccluderIndex(CapsuleShadowVolume volume) { return volume.bits >> 16; }
uint GetCasterIndex(CapsuleShadowVolume volume) { return (volume.bits >> 8) & 0xffU; }
uint GetCasterType(CapsuleShadowVolume volume) { return volume.bits & 0xffU; }

#endif // ndef CAPSULE_OCCLUDER_DATA_HLSL
