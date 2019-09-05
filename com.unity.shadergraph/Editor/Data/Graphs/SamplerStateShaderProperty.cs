using System.Collections.Generic;

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
        
        public override bool isExposable => false;
        public override bool isRenamable => false;

        public static string GetSystemSamplerName(TextureSamplerState.FilterMode filterMode, TextureSamplerState.WrapMode wrapMode)
            => $"{PropertyType.SamplerState.ToConcreteShaderValueType().ToShaderString()}_{filterMode}_{wrapMode}";

        public override string GetPropertyAsArgumentString()
        {
            return $"SAMPLER({referenceName})";
        }
        
        public override AbstractMaterialNode ToConcreteNode()
        {
            return new SamplerStateNode() 
            {
                filter = value.filter,
                wrap = value.wrap
            };
        }

        public override PreviewProperty GetPreviewMaterialProperty()
        {
            return default(PreviewProperty);
        }

        public override ShaderInput Copy()
        {
            return new SamplerStateShaderProperty()
            {
                displayName = displayName,
                hidden = hidden,
                overrideReferenceName = overrideReferenceName,
                value = value
            };
        }

        public string GetSamplerPropertyDeclarationString(HashSet<string> systemSamplerNames)
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

        public static void GenerateSystemSamplerNames(ShaderStringBuilder sb, HashSet<string> systemSamplerNames)
        {
            foreach (var systemSamplerName in systemSamplerNames)
                sb.AppendLine($"{PropertyType.SamplerState.FormatDeclarationString(ConcretePrecision.Float, systemSamplerName)};");
        }

        // Clears out overrideReferenceName because previously they are set to the system sampler name which could be common between several different
        // sampler state properties, but we want them to be separate for subgraph function inputs. See PropertyCollector.cs.
        void UnityEngine.ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            overrideReferenceName = null;
        }

        void UnityEngine.ISerializationCallbackReceiver.OnBeforeSerialize()
        {
        }
    }
}
