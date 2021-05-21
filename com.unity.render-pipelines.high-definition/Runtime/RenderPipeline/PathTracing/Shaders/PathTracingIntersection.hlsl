#ifndef UNITY_PATH_TRACING_INTERSECTION_INCLUDED
#define UNITY_PATH_TRACING_INTERSECTION_INCLUDED

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingIntersection.hlsl"

// Structure that defines the current state of the intersection, for path tracing
struct PathIntersection
{
    // t as in: O + t*D = H (i.e. distance between O and H, if D is normalized)
    float t;
    // Resulting value (often color) of the ray
    float3 value;
    // Resulting alpha (camera rays only)
    float alpha;
    // Cone representation of the ray
    RayCone cone;
    // The remaining available depth for the current ray
    uint remainingDepth;
    // Pixel coordinate from which the initial ray was launched
    uint2 pixelCoord;
    // Max roughness encountered along the path
    float maxRoughness;
// SensorSDK - Begin
// Extra params for beam lighthing
    float3 beamDirection;
    float3 beamOrigin;

    float beamRadius;
    float beamDepth;

//Extra parameters to debug (to be removed
    float3 lightPosition;
    float3 lightDirection;
    float3 lightOutgoing;
    float lightIntensity;
    float lightAngleScale;
    float lightAngleOffset;
    float lightValue;
    float lightPDF;
    float3 color;
    float3 diffValue;
    float diffPdf;
    float3 specValue;
    float specPdf;

    float bsdfWeight0;
    float bsdfWeight1;
    float bsdfWeight2;
    float bsdfWeight3;

    //BSDFData
    uint materialFeatures;
    float3 diffuseColor;
    float3 fresnel0;
    float ambientOcclusion;
    float specularOcclusion;
    float3 normalWS;
    float perceptualRoughness;
    float coatMask;
    uint diffusionProfileIndex;
    float subsurfaceMask;
    float thickness;
    bool useThickObjectMode;
    float3 transmittance;
    float3 tangentWS;
    float3 bitangentWS;
    float roughnessT;
    float roughnessB;
    float anisotropy;
    float iridescenceThickness;
    float iridescenceMask;
    float coatRoughness;
    float3 geomNormalWS;
    float ior;
    float3 absorptionCoefficient;
    float transmittanceMask;

    float alpha2;
    float alphatreshold;

    int lightCount;
    float customRefractance;
//SensorSDK - End
};

#endif // UNITY_PATH_TRACING_INTERSECTION_INCLUDED
