using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.ShaderGraph
{
    class PropertyCollector
    {
        public struct TextureInfo
        {
            public string name;
            public int textureId;
            public bool modifiable;
        }

        public readonly List<AbstractShaderProperty> properties = new List<AbstractShaderProperty>();

        public void AddShaderProperty(AbstractShaderProperty chunk)
        {
            if (properties.Any(x => x.referenceName == chunk.referenceName))
                return;
            properties.Add(chunk);
        }

        private const string s_UnityPerMaterialCbName = "UnityPerMaterial";

        private string GetPropertyCbName(AbstractShaderProperty property, GenerationMode generationMode)
        {
            var cbName = property.propertyType.IsBatchable() ? s_UnityPerMaterialCbName : string.Empty;

            //
            // Old behaviours that I don't know why we do them:

            // If the property is not exposed, put it to Global
            if (cbName == s_UnityPerMaterialCbName && !property.generatePropertyBlock)
                cbName = string.Empty;
            // If we are in preview, put all CB variables to UnityPerMaterial CB
            if (cbName != string.Empty && generationMode == GenerationMode.Preview)
                cbName = s_UnityPerMaterialCbName;

            return cbName;
        }

        public void GetPropertiesDeclaration(ShaderStringBuilder builder, GenerationMode mode, ConcretePrecision inheritedPrecision)
        {
            foreach (var prop in properties)
            {
                prop.ValidateConcretePrecision(inheritedPrecision);
            }

            var cbProps = new Dictionary<string, List<AbstractShaderProperty>>();
            foreach (var prop in properties)
            {
                var cbName = GetPropertyCbName(prop, mode);
                if (!cbProps.TryGetValue(cbName, out var vars))
                {
                    vars = new List<AbstractShaderProperty>();
                    cbProps.Add(cbName, vars);
                }
                vars.Add(prop);
            }

            foreach (var kvp in cbProps)
                kvp.Value.Sort((p1, p2) => p1.gpuInstanced.CompareTo(p2.gpuInstanced));

            // SamplerState properties are tricky:
            // - Unity only allows declaring SamplerState variable name of either sampler_{textureName} ("texture sampler") or SamplerState_{filterMode}_{wrapMode} ("system sampler").
            //   * That's why before the branch sg-texture-properties we have the referenceName of a SamplerStateShaderProperty set to the actual system sampler names.
            // - But with the existance of SubGraph functions we'll need unique SamplerState variable name for the function inputs.
            //   * That means if we have two SamplerState properties on the SubGraph blackboard of the same filterMode & wrapMode settings, it fails to compile because there are two
            //     identical function parameter names.
            // - So we'll have to use different names for each SamplerState property, which contradicts #1 (we could do special casing only for SubGraph function generation, but it needs
            //   changes to PropertyNode code generation, doable but more hacky).
            // - Instead, the branch sg-texture-properties changes the SamplerState property declaration to simply be:
            //       #define SamplerState_{referenceName} SamplerState{system sampler name}
            //   for all system sampler names (texture sampler names stay the same).
            //   And at the end collect all unique system sampler names and generate:
            //       SAMPLER(SamplerState{system sampler name});
            var systemSamplerNames = new HashSet<string>();
            foreach (var kvp in cbProps)
            {
                var cbName = kvp.Key;
                if (cbName != string.Empty)
                {
                    builder.AppendLine($"CBUFFER_START({cbName})");
                    builder.IncreaseIndent();
                }

                bool gpuInstancedBlock = false;

                foreach (var prop in kvp.Value)
                {
                    if (!gpuInstancedBlock && prop.gpuInstanced)
                    {
                        gpuInstancedBlock = true;
                        builder.AppendLine("#ifndef UNITY_DOTS_INSTANCING_ENABLED");
                        builder.IncreaseIndent();
                    }
                    else if (gpuInstancedBlock && !prop.gpuInstanced)
                    {
                        gpuInstancedBlock = false;
                        builder.AppendLine("#endif");
                        builder.DecreaseIndent();
                    }

                    if (prop is GradientShaderProperty gradientProperty)
                        builder.AppendLine(gradientProperty.GetGraidentPropertyDeclarationString());
                    else if (prop is SamplerStateShaderProperty samplerProperty)
                        builder.AppendLine(samplerProperty.GetSamplerPropertyDeclarationString(systemSamplerNames));
                    else
                        builder.AppendLine($"{prop.propertyType.FormatDeclarationString(prop.concretePrecision, prop.referenceName)};");
                }

                if (gpuInstancedBlock)
                {
                    builder.AppendLine("#endif");
                    builder.DecreaseIndent();
                }

                if (systemSamplerNames.Count > 0)
                {
                    UnityEngine.Debug.Assert(cbName == string.Empty);
                    SamplerStateShaderProperty.GenerateSystemSamplerNames(builder, systemSamplerNames);
                    systemSamplerNames.Clear();
                }

                if (cbName != string.Empty)
                {
                    builder.DecreaseIndent();
                    builder.AppendLine($"CBUFFER_END");
                }
            }
            builder.AppendNewLine();
        }

        public int GetDotsInstancingPropertiesCount(GenerationMode mode)
        {
            var batchAll = mode == GenerationMode.Preview;
            return properties.Where(n => (batchAll || (n.generatePropertyBlock && n.propertyType.IsBatchable())) && n.gpuInstanced).Count();
        }

        public string GetDotsInstancingPropertiesDeclaration(GenerationMode mode)
        {
            var builder = new ShaderStringBuilder();
            var batchAll = mode == GenerationMode.Preview;

            int instancedCount = GetDotsInstancingPropertiesCount(mode);

            if (instancedCount > 0)
            {
                builder.AppendLine("#if defined(UNITY_DOTS_INSTANCING_ENABLED)");
                builder.AppendLine("#define SHADER_GRAPH_GENERATED");
                builder.Append("#define DOTS_CUSTOM_ADDITIONAL_MATERIAL_VARS\t");

                int count = 0;
                foreach (var prop in properties.Where(n => batchAll || (n.generatePropertyBlock && n.propertyType.IsBatchable())))
                {
                    if (prop.gpuInstanced)
                    {
                        string varName = $"{prop.referenceName}_Array";
                        string sType = prop.concreteShaderValueType.ToShaderString(prop.concretePrecision);
                        builder.Append("UNITY_DEFINE_INSTANCED_PROP({0}, {1})", sType, varName);
                        if (count < instancedCount - 1)
                            builder.Append("\\");
                        builder.AppendLine("");
                        count++;
                    }
                }
                foreach (var prop in properties.Where(n => batchAll || (n.generatePropertyBlock && n.propertyType.IsBatchable())))
                {
                    if (prop.gpuInstanced)
                    {
                        string varName = $"{prop.referenceName}_Array";
                        builder.AppendLine("#define {0} UNITY_ACCESS_INSTANCED_PROP(unity_Builtins0, {1})", prop.referenceName, varName);
                    }
                }
            }
            builder.AppendLine("#endif");
            return builder.ToString();
        }

        public List<TextureInfo> GetConfiguredTexutres()
        {
            var result = new List<TextureInfo>();

            foreach (var prop in properties.OfType<TextureShaderProperty>())
            {
                if (prop.referenceName != null)
                {
                    var textureInfo = new TextureInfo
                    {
                        name = prop.referenceName,
                        textureId = prop.value.texture != null ? prop.value.texture.GetInstanceID() : 0,
                        modifiable = prop.modifiable
                    };
                    result.Add(textureInfo);
                }
            }

            foreach (var prop in properties.OfType<Texture2DArrayShaderProperty>())
            {
                if (prop.referenceName != null)
                {
                    var textureInfo = new TextureInfo
                    {
                        name = prop.referenceName,
                        textureId = prop.value.textureArray != null ? prop.value.textureArray.GetInstanceID() : 0,
                        modifiable = prop.modifiable
                    };
                    result.Add(textureInfo);
                }
            }

            foreach (var prop in properties.OfType<Texture3DShaderProperty>())
            {
                if (prop.referenceName != null)
                {
                    var textureInfo = new TextureInfo
                    {
                        name = prop.referenceName,
                        textureId = prop.value.texture != null ? prop.value.texture.GetInstanceID() : 0,
                        modifiable = prop.modifiable
                    };
                    result.Add(textureInfo);
                }
            }

            foreach (var prop in properties.OfType<CubemapShaderProperty>())
            {
                if (prop.referenceName != null)
                {
                    var textureInfo = new TextureInfo
                    {
                        name = prop.referenceName,
                        textureId = prop.value.cubemap != null ? prop.value.cubemap.GetInstanceID() : 0,
                        modifiable = prop.modifiable
                    };
                    result.Add(textureInfo);
                }
            }
            return result;
        }
    }
}
