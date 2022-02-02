using UnityEditor.ShaderGraph;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    static class HDStructs
    {
        public static StructDescriptor AttributesMesh = new StructDescriptor()
        {
            name = "AttributesMesh",
            packFields = false,
            fields = new FieldDescriptor[]
            {
                HDStructFields.AttributesMesh.positionOS,
                HDStructFields.AttributesMesh.normalOS,
                HDStructFields.AttributesMesh.tangentOS,
                HDStructFields.AttributesMesh.uv0,
                HDStructFields.AttributesMesh.uv1,
                HDStructFields.AttributesMesh.uv2,
                HDStructFields.AttributesMesh.uv3,
                HDStructFields.AttributesMesh.color,
                HDStructFields.AttributesMesh.instanceID,
                HDStructFields.AttributesMesh.weights,
                HDStructFields.AttributesMesh.indices,
                HDStructFields.AttributesMesh.vertexID,
            }
        };

        public static StructDescriptor VaryingsMeshToPS = new StructDescriptor()
        {
            name = "VaryingsMeshToPS",
            packFields = true,
            populateWithCustomInterpolators = true,
            fields = new FieldDescriptor[]
            {
                HDStructFields.VaryingsMeshToPS.positionCS,
                HDStructFields.VaryingsMeshToPS.positionRWS,
                HDStructFields.VaryingsMeshToPS.positionPredisplacementRWS,
                HDStructFields.VaryingsMeshToPS.normalWS,
                HDStructFields.VaryingsMeshToPS.tangentWS,
                HDStructFields.VaryingsMeshToPS.texCoord0,
                HDStructFields.VaryingsMeshToPS.texCoord1,
                HDStructFields.VaryingsMeshToPS.texCoord2,
                HDStructFields.VaryingsMeshToPS.texCoord3,
                HDStructFields.VaryingsMeshToPS.color,
                HDStructFields.VaryingsMeshToPS.instanceID,
            }
        };

        public static StructDescriptor VaryingsMeshToDS = new StructDescriptor()
        {
            name = "VaryingsMeshToDS",
            packFields = true,
            populateWithCustomInterpolators = true,
            fields = new FieldDescriptor[]
            {
                HDStructFields.VaryingsMeshToDS.positionRWS,
                HDStructFields.VaryingsMeshToDS.positionPredisplacementRWS,
                HDStructFields.VaryingsMeshToDS.tessellationFactor,
                HDStructFields.VaryingsMeshToDS.normalWS,
                HDStructFields.VaryingsMeshToDS.tangentWS,
                HDStructFields.VaryingsMeshToDS.texCoord0,
                HDStructFields.VaryingsMeshToDS.texCoord1,
                HDStructFields.VaryingsMeshToDS.texCoord2,
                HDStructFields.VaryingsMeshToDS.texCoord3,
                HDStructFields.VaryingsMeshToDS.color,
                HDStructFields.VaryingsMeshToDS.instanceID,
            }
        };
    }
}
