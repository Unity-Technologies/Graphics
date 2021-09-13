using System;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [BlackboardInputInfo(80)]
    class SamplerStateShaderProperty : AbstractShaderProperty<TextureSamplerState>
    {
        public SamplerStateShaderProperty()
        {
            displayName = "SamplerState";
            value = new TextureSamplerState();
        }

        public override PropertyType propertyType => PropertyType.SamplerState;

        // Sampler States cannot be exposed on a Material
        internal override bool isExposable => false;

        // subgraph Sampler States can be renamed
        // just the actual properties they create will always have fixed names
        internal override bool isRenamable => true;

        internal override bool isReferenceRenamable => false;

        // this is the fixed naming scheme for actual samplerstates properties
        string propertyReferenceName => value.defaultPropertyName;
        public override string referenceNameForEditing => propertyReferenceName;

        internal override bool AllowHLSLDeclaration(HLSLDeclaration decl) => false; // disable UI, nothing to choose

        internal override void ForeachHLSLProperty(Action<HLSLProperty> action)
        {
            action(new HLSLProperty(HLSLType._SamplerState, propertyReferenceName, HLSLDeclaration.Global));
        }

        internal override string GetPropertyAsArgumentString()
        {
            return $"UnitySamplerState {referenceName}";
        }

        internal override string GetHLSLVariableName(bool isSubgraphProperty)
        {
            if (isSubgraphProperty)
                return referenceName;
            else
                return $"UnityBuildSamplerStateStruct({propertyReferenceName})";
        }

        internal override AbstractMaterialNode ToConcreteNode()
        {
            return new SamplerStateNode()
            {
                filter = value.filter,
                wrap = value.wrap
            };
        }

        internal override PreviewProperty GetPreviewMaterialProperty()
        {
            return default(PreviewProperty);
        }

        internal override ShaderInput Copy()
        {
            return new SamplerStateShaderProperty()
            {
                displayName = displayName,
                value = value,
            };
        }

        public override int latestVersion => 1;
        public override void OnAfterDeserialize(string json)
        {
            if (sgVersion == 0)
            {
                // we no longer require forced reference names on sampler state properties
                // as we enforce custom property naming by simply not using the reference name
                // this allows us to use the real reference name for subgraph parameters
                // however we must clear out the old reference name first (as it was always hard-coded)
                // this will fallback to the default ref name
                overrideReferenceName = null;
                var unused = referenceName;
                ChangeVersion(1);
            }
        }
    }
}
