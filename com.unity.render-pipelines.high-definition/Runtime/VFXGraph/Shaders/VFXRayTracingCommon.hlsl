#ifndef VFX_RAY_TRACING_COMMON_HLSL
#define VFX_RAY_TRACING_COMMON_HLSL

// Object <-> primtive matrices
float4x4 ObjectToPrimitive(VFXAttributes attributes, float3 size3)
{
    return GetVFXToElementMatrix(attributes.axisX, attributes.axisY, attributes.axisZ,
        float3(attributes.angleX, attributes.angleY, attributes.angleZ),
        float3(attributes.pivotX, attributes.pivotY, attributes.pivotZ),
        size3, attributes.position);
}

float4x4 PrimtiveToObject(VFXAttributes attributes, float3 size3)
{
    return GetElementToVFXMatrix(attributes.axisX, attributes.axisY, attributes.axisZ,
        float3(attributes.angleX,attributes.angleY,attributes.angleZ),
        float3(attributes.pivotX,attributes.pivotY,attributes.pivotZ),
        size3, attributes.position);
}

#if defined(VFX_PRIMITIVE_QUAD)
    #define RAY_TRACING_QUAD_PRIMTIIVE
    // structure that holds all we need for the intersection shader
    struct RayTracingProceduralData
    {
        float4x4 objectToPrimitive;
        float4x4 primitiveToObject;
        float3 normal;
        float3 position;
        VFXAttributes attributes;
        float3 size;
    };

    RayTracingProceduralData BuildRayTracingProceduralData(VFXAttributes attributes, float3 size3)
    {
        RayTracingProceduralData rtPrData;
        rtPrData.objectToPrimitive = ObjectToPrimitive(attributes, size3);
        rtPrData.primitiveToObject = PrimtiveToObject(attributes, size3);
        rtPrData.position =  rtPrData.primitiveToObject._m03_m13_m23;
        rtPrData.normal = -rtPrData.primitiveToObject._m02_m12_m22;
        rtPrData.attributes = attributes;
        rtPrData.size = size3;
        return rtPrData;
    }
#endif

#if defined(VFX_PRIMITIVE_TRIANGLE)
    #define RAY_TRACING_TRIANGLE_PRIMTIIVE

    // structure that holds all we need for the intersection shader
    struct RayTracingProceduralData
    {
        float4x4 objectToPrimitive;
        float4x4 primitiveToObject;
        float3 position;
        float3 normal;
        float2 p0;
        float2 p1;
        float2 p2;
        VFXAttributes attributes;
        float3 size;
    };

    RayTracingProceduralData BuildRayTracingProceduralData(VFXAttributes attributes, float3 size3)
    {
        RayTracingProceduralData rtPrData;
        rtPrData.objectToPrimitive = ObjectToPrimitive(attributes, size3);
        rtPrData.primitiveToObject = PrimtiveToObject(attributes, size3);
        rtPrData.position =  rtPrData.primitiveToObject._m03_m13_m23;
        rtPrData.normal = -rtPrData.primitiveToObject._m02_m12_m22;
        rtPrData.attributes = attributes;
        rtPrData.size = size3;

        // Triangle coordinates
        const float2 kOffsets[] = {
                float2(-0.5f,   -0.288675129413604736328125f),
                float2(0.0f,    0.57735025882720947265625f),
                float2(0.5f,    -0.288675129413604736328125f),
            };

        const float kUVScale = 0.866025388240814208984375f;

        // Evaluate the three points of the triangle
        rtPrData.p0 = (kOffsets[0] * kUVScale) + 0.5f;
        rtPrData.p1 = (kOffsets[1] * kUVScale) + 0.5f;
        rtPrData.p2 = (kOffsets[2] * kUVScale) + 0.5f;
        return rtPrData;
    }
#endif

#if defined(VFX_PRIMITIVE_OCTAGON)
    #define RAY_TRACING_OCTAGON_PRIMTIIVE
#endif

#endif // VFX_RAY_TRACING_COMMON_HLSL
