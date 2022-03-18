using UnityEditor.ShaderGraph;

namespace UnityEditor.Rendering.Foundry
{
    abstract class FoundryTestSubTarget : SubTarget<FoundryTestTarget>
    {
        static readonly GUID kSourceCodeGuid = new GUID("6b97d8beba532e244ba7c65fbbc048c2");  // FoundryTestSubTarget.cs

        public override void Setup(ref TargetSetupContext context)
        {
            context.AddAssetDependency(kSourceCodeGuid, AssetCollection.Flags.SourceDependency);
        }

        protected SubShaderDescriptor PostProcessSubShader(SubShaderDescriptor subShaderDescriptor)
        {
            return subShaderDescriptor;
        }
    }
}
