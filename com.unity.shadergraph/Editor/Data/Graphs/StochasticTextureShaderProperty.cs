using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    [BlackboardInputInfo(61)]
    class StochasticTextureShaderProperty : AbstractShaderProperty<SerializableStochasticTexture>
    {
        public StochasticTextureShaderProperty()
        {
            displayName = "StochasticTexture";
            value = new SerializableStochasticTexture();
        }

        public override PropertyType propertyType => PropertyType.StochasticTexture;

        internal override bool isExposable => true;         // the textures are exposable at least..
        internal override bool isRenamable => true;

        internal override void GetPropertyReferenceNames(List<string> result)
        {
            result.Add(referenceName);
        }

        internal override void GetPropertyDisplayNames(List<string> result)
        {
            result.Add(displayName);
        }

        // this is used for properties exposed to the Material in the shaderlab Properties{} block
        internal override void AppendPropertyBlockStrings(ShaderStringBuilder builder)
        {
            builder.AppendLine($"[HideInInspector]{referenceName}(\"{displayName}\", 2D) = \"white\" {{}}");
            builder.AppendLine($"[HideInInspector][NoScaleOffset]{referenceName}_invT(\"{displayName}\", 2D) = \"white\" {{}}");
            builder.AppendLine($"[HideInInspector]{referenceName}_compressionScalers(\"{displayName}\", Vector) = (1,1,1,1)");
            builder.AppendLine($"[HideInInspector]{referenceName}_colorSpaceOrigin(\"{displayName}\", Vector) = (1,1,1,1)");
            builder.AppendLine($"[HideInInspector]{referenceName}_colorSpaceVector1(\"{displayName}\", Vector) = (1,1,1,1)");
            builder.AppendLine($"[HideInInspector]{referenceName}_colorSpaceVector2(\"{displayName}\", Vector) = (1,1,1,1)");
            builder.AppendLine($"[HideInInspector]{referenceName}_colorSpaceVector3(\"{displayName}\", Vector) = (1,1,1,1)");
        }

        internal override string GetPropertyBlockString()
        {
            // this should not be called, as it is replaced by the Append*PropertyBlockStrings function above
            throw new NotSupportedException();
        }

        internal override bool AllowHLSLDeclaration(HLSLDeclaration decl) => false;     // disable UI, nothing to choose

        internal override void ForeachHLSLProperty(Action<HLSLProperty> action)
        {
            // TODO: not sure if we can set up stochastic textures globally or per-instance? not easily at least...
            action(new HLSLProperty(HLSLType._Texture2D, referenceName, HLSLDeclaration.Global));
            action(new HLSLProperty(HLSLType._SamplerState, "sampler" + referenceName, HLSLDeclaration.Global));
            action(new HLSLProperty(HLSLType._Texture2D, referenceName + "_invT", HLSLDeclaration.Global));
            action(new HLSLProperty(HLSLType._SamplerState, "sampler" + referenceName + "_invT", HLSLDeclaration.Global));

            action(new HLSLProperty(HLSLType._float4, referenceName + "_TexelSize", HLSLDeclaration.UnityPerMaterial));
            action(new HLSLProperty(HLSLType._float4, referenceName + "_ST", HLSLDeclaration.UnityPerMaterial));
            action(new HLSLProperty(HLSLType._float4, referenceName + "_invT_TexelSize", HLSLDeclaration.UnityPerMaterial));
            action(new HLSLProperty(HLSLType._float4, referenceName + "_compressionScalers", HLSLDeclaration.UnityPerMaterial));
            action(new HLSLProperty(HLSLType._float4, referenceName + "_colorSpaceOrigin", HLSLDeclaration.UnityPerMaterial));
            action(new HLSLProperty(HLSLType._float4, referenceName + "_colorSpaceVector1", HLSLDeclaration.UnityPerMaterial));
            action(new HLSLProperty(HLSLType._float4, referenceName + "_colorSpaceVector2", HLSLDeclaration.UnityPerMaterial));
            action(new HLSLProperty(HLSLType._float4, referenceName + "_colorSpaceVector3", HLSLDeclaration.UnityPerMaterial));
        }

        // argument string used to pass this property to a subgraph
        internal override string GetPropertyAsArgumentString(string precisionString)
        {
            return "UnityStochasticTexture2D " + referenceName;
        }

        internal override string GetHLSLVariableName(bool isSubgraphProperty, GenerationMode mode)
        {
            if (isSubgraphProperty)
                return referenceName;
            else
            {
                return $"UnityBuildStochasticTexture2DStruct({referenceName})";
            }
        }

        // if a blackboard property is deleted, or copy/pasted, all node instances of it are replaced with this:
        internal override AbstractMaterialNode ToConcreteNode()
        {
            return null;    // return null to indicate there is NO concrete form of a VT property
        }

        internal override PreviewProperty GetPreviewMaterialProperty()
        {
            return new PreviewProperty(propertyType)
            {
                name = referenceName,
                stochasticProperty = this
            };
        }

        internal override ShaderInput Copy()
        {
            var st = new StochasticTextureShaderProperty
            {
                displayName = displayName,
                value = new SerializableStochasticTexture(),
            };

            // duplicate layer data, but reset reference names (they should be unique)
//             for (int layer = 0; layer < value.layers.Count; layer++)
//             {
//                 var guid = Guid.NewGuid();
//                 vt.value.layers.Add(new SerializableVirtualTextureLayer(value.layers[layer]));
//             }

            return st;
        }

        internal void AddTextureInfo(List<PropertyCollector.TextureInfo> infos)
        {
/*
            for (int layer = 0; layer < value.layers.Count; layer++)
            {
                string layerRefName = value.layers[layer].layerRefName;
                var layerTexture = value.layers[layer].layerTexture;
                var texture = layerTexture != null ? layerTexture.texture : null;

                var textureInfo = new PropertyCollector.TextureInfo
                {
                    name = layerRefName,
                    textureId = texture != null ? texture.GetInstanceID() : 0,
                    dimension = texture != null ? texture.dimension : UnityEngine.Rendering.TextureDimension.Any,
                    modifiable = true
                };
                infos.Add(textureInfo);
            }
*/
        }

        // internal override bool isAlwaysExposed => true;
        internal override bool isCustomSlotAllowed => false;

        public override void OnAfterDeserialize(string json)
        {
        }
    }
}
