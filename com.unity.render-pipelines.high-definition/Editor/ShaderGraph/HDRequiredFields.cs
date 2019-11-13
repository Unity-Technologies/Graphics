using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    static class HDRequiredFields
    {
        public static FieldDescriptor[] Meta = new FieldDescriptor[]
        {
            HDStructFields.AttributesMesh.normalOS,
            HDStructFields.AttributesMesh.tangentOS,
            HDStructFields.AttributesMesh.uv0,
            HDStructFields.AttributesMesh.uv1,
            HDStructFields.AttributesMesh.color,
            HDStructFields.AttributesMesh.uv2,
        };

        public static FieldDescriptor[] PositionRWS = new FieldDescriptor[]
        {
            HDStructFields.VaryingsMeshToPS.positionRWS,
        };

        public static FieldDescriptor[] LitMinimal = new FieldDescriptor[]
        {
            HDStructFields.FragInputs.tangentToWorld,
            HDStructFields.FragInputs.positionRWS,
            HDStructFields.FragInputs.texCoord1,
            HDStructFields.FragInputs.texCoord2,
        };

        public static FieldDescriptor[] LitFull = new FieldDescriptor[]
        {
            HDStructFields.AttributesMesh.normalOS,
            HDStructFields.AttributesMesh.tangentOS,
            HDStructFields.AttributesMesh.uv0,
            HDStructFields.AttributesMesh.uv1,
            HDStructFields.AttributesMesh.color,
            HDStructFields.AttributesMesh.uv2,
            HDStructFields.AttributesMesh.uv3,
            HDStructFields.FragInputs.tangentToWorld,
            HDStructFields.FragInputs.positionRWS,
            HDStructFields.FragInputs.texCoord1,
            HDStructFields.FragInputs.texCoord2,
            HDStructFields.FragInputs.texCoord3,
            HDStructFields.FragInputs.color,
        };

        public static FieldDescriptor[] DecalMesh = new FieldDescriptor[]
        {   
            HDStructFields.AttributesMesh.normalOS,
            HDStructFields.AttributesMesh.tangentOS,
            HDStructFields.AttributesMesh.uv0,
            HDStructFields.FragInputs.tangentToWorld,
            HDStructFields.FragInputs.positionRWS,
            HDStructFields.FragInputs.texCoord0,
        };
    }
}
