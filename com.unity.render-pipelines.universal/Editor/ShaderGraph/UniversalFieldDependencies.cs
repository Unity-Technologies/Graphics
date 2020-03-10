using UnityEditor.ShaderGraph;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    static class UniversalFieldDependencies
    {
        public static DependencyCollection UniversalDefault = new DependencyCollection()
        {
            { FieldDependencies.Default },
            new FieldDependency(UniversalStructFields.Varyings.stereoTargetEyeIndexAsRTArrayIdx,    StructFields.Attributes.instanceID ),
            new FieldDependency(UniversalStructFields.Varyings.stereoTargetEyeIndexAsBlendIdx0,     StructFields.Attributes.instanceID ),
        };
    }
}
