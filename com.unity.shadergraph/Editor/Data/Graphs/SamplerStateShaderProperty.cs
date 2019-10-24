using System.Collections.Generic;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    class SamplerStateShaderProperty : AbstractShaderProperty<TextureSamplerState>, UnityEngine.ISerializationCallbackReceiver
    {
        public SamplerStateShaderProperty()
        {
            displayName = "SamplerState";
            value = new TextureSamplerState();
        }

        public override PropertyType propertyType => PropertyType.SamplerState;

        internal override bool isExposable => false;
        internal override bool isRenamable => false;

        internal static string GetSystemSamplerName(TextureSamplerState.FilterMode filterMode, TextureSamplerState.WrapMode wrapMode)
            => $"{PropertyType.SamplerState.ToConcreteShaderValueType().ToShaderString()}_{filterMode}_{wrapMode}";

        internal override string GetPropertyAsArgumentString()
        {
            return $"SAMPLER({referenceName})";
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
                hidden = hidden,
                overrideReferenceName = overrideReferenceName,
                value = value
            };
        }

        internal string GetSamplerPropertyDeclarationString(HashSet<string> systemSamplerNames)
        {
            if (overrideReferenceName == null)
            {
                var systemSamplerName = GetSystemSamplerName(value.filter, value.wrap);
                systemSamplerNames.Add(systemSamplerName);
                return $"#define {referenceName} {systemSamplerName}";
            }
            else
                return $"{PropertyType.SamplerState.FormatDeclarationString(ConcretePrecision.Float, referenceName)};";
        }

        void UnityEngine.ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            // Clears out overrideReferenceName because previously they are set to the system sampler name which could be common between several different
            // sampler state properties, but we want them to be separate for subgraph function inputs. See PropertyCollector.cs.
            // Only do this if the property is user created, because SubGraph creates hidden SamplerState properties to pair with texture inputs.
            if (!hidden)
                overrideReferenceName = null;
        }

        void UnityEngine.ISerializationCallbackReceiver.OnBeforeSerialize()
        {
        }
    }
}
