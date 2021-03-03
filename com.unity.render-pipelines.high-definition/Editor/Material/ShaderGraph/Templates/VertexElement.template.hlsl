$include("VertexElementCommon.template.hlsl")

AttributesElementInputs AttributesMeshToAttributesElementInputs(AttributesMesh input)
{
    AttributesElementInputs output;
    ZERO_INITIALIZE(AttributesElementInputs, output);

    output.positionOS = input.positionOS;
    output.normalOS   = input.normalOS;
    output.uv         = input.uv0;
    output.vertexID   = input.vertexID;
    output.instanceID = input.instanceID;

    return output;
}

AttributesElement VertElement(inout AttributesMesh input)
{
    AttributesElementInputs attributesElementInputs = AttributesMeshToAttributesElementInputs(input);

    // Invokes SRP agnostic VFX element evaluation.
    AttributesElement element = ElementDescriptionFunction(attributesElementInputs);

    // Copy element output to the input mesh.
    input.positionOS = element.position;
    input.normalOS   = element.normal;
    input.uv0        = element.uv;

    return element;
}

// Various helpers for property + varyings injections.
void ConfigureElementVaryings(AttributesElement element, inout VaryingsMeshToPS output)
{
    const Attributes attributes = element.attributes;
    $splice(VFXInterpolantsGeneration)
}

void ConfigureElementVertexProperties(AttributesElement element, inout GraphProperties properties)
{
    const Attributes attributes = element.attributes;
    $splice(VFXVertexPropertiesGeneration)
    $splice(VFXVertexPropertiesAssign)
}

void ConfigureElementPixelProperties(FragInputs fragInputs, inout GraphProperties properties)
{
    $splice(VFXPixelPropertiesAssign)
}

// Here we define some overrides of the core space transforms.
// VFX lets users work in two spaces: Local and World.
// Local means local to "Particle Space" (or Element) which in this case we treat similarly to Object Space.
// World means that position users define in their graph are placed in the absolute world.
// Becuase of these two spaces we must be careful how we transform into world space for current and previous frame.
#define TransformObjectToWorld TransformObjectToWorldVFX
float3 TransformObjectToWorldVFX(float3 positionOS)
{
    float3 positionWS = TransformPositionVFXToWorld(positionOS);

#ifdef VFX_WORLD_SPACE
    positionWS = GetCameraRelativePositionWS(positionOS);
#endif

    return positionWS;
}

#ifdef VFX_WORLD_SPACE
#define TransformPreviousObjectToWorld TransformPreviousObjectToWorldVFX
float3 TransformPreviousObjectToWorldVFX(float3 positionOS)
{
    return GetCameraRelativePositionWS(positionOS);
}
#endif

// Need to redefine GetVaryingsDataDebug since we omit FragInputs.hlsl and generate one procedurally.
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/MaterialDebug.cs.hlsl"

void GetVaryingsDataDebug(uint paramId, FragInputs input, inout float3 result, inout bool needLinearToSRGB)
{
    switch (paramId)
    {
    case DEBUGVIEWVARYING_TEXCOORD0:
        result = input.texCoord0.xyz;
        break;
    case DEBUGVIEWVARYING_TEXCOORD1:
        result = input.texCoord1.xyz;
        break;
    case DEBUGVIEWVARYING_TEXCOORD2:
        result = input.texCoord2.xyz;
        break;
    case DEBUGVIEWVARYING_TEXCOORD3:
        result = input.texCoord3.xyz;
        break;
    case DEBUGVIEWVARYING_VERTEX_TANGENT_WS:
        result = input.tangentToWorld[0].xyz * 0.5 + 0.5;
        break;
    case DEBUGVIEWVARYING_VERTEX_BITANGENT_WS:
        result = input.tangentToWorld[1].xyz * 0.5 + 0.5;
        break;
    case DEBUGVIEWVARYING_VERTEX_NORMAL_WS:
        result = IsNormalized(input.tangentToWorld[2].xyz) ?  input.tangentToWorld[2].xyz * 0.5 + 0.5 : float3(1.0, 0.0, 0.0);
        break;
    case DEBUGVIEWVARYING_VERTEX_COLOR:
        result = input.color.rgb; needLinearToSRGB = true;
        break;
    case DEBUGVIEWVARYING_VERTEX_COLOR_ALPHA:
        result = input.color.aaa;
        break;
    }
}
