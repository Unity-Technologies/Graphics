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
    class VirtualTextureShaderProperty : AbstractShaderProperty<SerializableVirtualTexture>
    {
        public VirtualTextureShaderProperty()
        {
            displayName = "VirtualTexture";
            value = new SerializableVirtualTexture();

            // add at least one layer
            value.layers = new List<SerializableVirtualTextureLayer>();
            value.layers.Add(new SerializableVirtualTextureLayer("Layer0", "Layer0", new SerializableTexture()));
            value.layers.Add(new SerializableVirtualTextureLayer("Layer1", "Layer1", new SerializableTexture()));
        }

        public override PropertyType propertyType => PropertyType.VirtualTexture;

        // isBatchable should never be called of we override hasBatchable / hasNonBatchableProperties
        internal override bool isBatchable
        {
            get { throw new NotImplementedException(); }
        }

        internal override bool hasBatchableProperties => true;
        internal override bool hasNonBatchableProperties => true;

        internal override bool isExposable => true;         // the textures are exposable at least..
        internal override bool isRenamable => true;

        internal override void GetPropertyReferenceNames(List<string> result)
        {
            result.Add(referenceName);
            for (int layer = 0; layer < value.layers.Count; layer++)
            {
                result.Add(value.layers[layer].layerRefName);
            }
        }
        internal override void GetPropertyDisplayNames(List<string> result)
        {
            result.Add(displayName);
            for (int layer = 0; layer < value.layers.Count; layer++)
            {
                result.Add(value.layers[layer].layerName);
            }
        }

        // this is used for properties exposed to the Material in the shaderlab Properties{} block
        internal override void AppendPropertyBlockStrings(ShaderStringBuilder builder)
        {
            if (!value.procedural)
            {
                // adds properties in this format so: [TextureStack.MyStack(0)] [NoScaleOffset] Layer0("Layer0", 2D) = "white" {}
                for (int layer = 0; layer < value.layers.Count; layer++)
                {
                    string layerName = value.layers[layer].layerName;
                    string layerRefName = value.layers[layer].layerRefName;
                    builder.AppendLine($"{hideTagString}[TextureStack.{referenceName}({layer})][NoScaleOffset]{layerRefName}(\"{layerName}\", 2D) = \"white\" {{}}");
                }
            }
        }

        internal override string GetPropertyBlockString()
        {
            // this should not be called, as it is replaced by the Append*PropertyBlockStrings function above
            throw new NotSupportedException();
        }

        internal override void AppendBatchablePropertyDeclarations(ShaderStringBuilder builder, string delimiter = ";")
        {
            int numLayers = value.layers.Count;
            if (numLayers > 0)
            {
                builder.Append("DECLARE_STACK_CB(");
                builder.Append(referenceName);
                builder.Append(")");
                builder.AppendLine(delimiter);
            }
        }

        internal override void AppendNonBatchablePropertyDeclarations(ShaderStringBuilder builder, string delimiter = ";")
        {
            int numLayers = value.layers.Count;
            if (numLayers > 0)
            {
                if (!value.procedural)
                {
                    // declare regular texture properties (for fallback case)
                    for (int i = 0; i < value.layers.Count; i++)
                    {
                        string layerRefName = value.layers[i].layerRefName;
                        builder.AppendLine(
                            $"TEXTURE2D({layerRefName}); SAMPLER(sampler{layerRefName}); {concretePrecision.ToShaderString()}4 {layerRefName}_TexelSize;");
                    }
                }

                // declare texture stack
                builder.AppendIndentation();
                builder.Append("DECLARE_STACK");
                builder.Append((numLayers <= 1) ? "" : numLayers.ToString());
                builder.Append("(");
                builder.Append(referenceName);
                builder.Append(",");
                for (int i = 0; i < value.layers.Count; i++)
                {
                    if (i != 0) builder.Append(",");
                    builder.Append(value.layers[i].layerRefName);
                }
                builder.Append(")");
                builder.Append(delimiter);      // TODO: don't like delimiter, pretty sure it's not necessary if we invert the defaults on GEtPropertyDeclaration / GetPropertyArgument string
                builder.AppendNewLine();

                // declare the actual virtual texture property "variable" as a macro define to the BuildVTProperties function
                builder.AppendIndentation();
                builder.Append("#define ");
                builder.Append(referenceName);
                builder.Append(" AddTextureType(BuildVTProperties_");
                builder.Append(referenceName);
                builder.Append("()");
                for (int i = 0; i < value.layers.Count; i++)
                {
                    builder.Append(",");
                    builder.Append("TEXTURETYPE_");
                    builder.Append(value.layers[i].layerTextureType.ToString().ToUpper());
                }
                builder.Append(")");
                builder.AppendNewLine();
            }
        }

        internal override string GetPropertyDeclarationString(string delimiter = ";")
        {
            // this should not be called, as it is replaced by the Append*PropertyDeclarations functions above
            throw new NotSupportedException();
        }

        // argument string used to pass this property to a subgraph
        internal override string GetPropertyAsArgumentString()
        {
            return "VTPropertyWithTextureType " + referenceName;
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
                vtProperty = this
            };
        }

        internal override ShaderInput Copy()
        {
            var vt =  new VirtualTextureShaderProperty
            {
                displayName = displayName,
                hidden = hidden,
                value = new SerializableVirtualTexture(),
                precision = precision
            };

            // duplicate layer data, but reset reference names (they should be unique)
            for (int layer = 0; layer < value.layers.Count; layer++)
            {
                var guid = Guid.NewGuid();
                vt.value.layers.Add(
                    new SerializableVirtualTextureLayer(
                        value.layers[layer].layerName,
                        $"Layer_{GuidEncoder.Encode(guid)}",
                        value.layers[layer].layerTexture));
            }

            return vt;
        }

        internal void AddTextureInfo(List<PropertyCollector.TextureInfo> infos)
        {
            for (int layer = 0; layer < value.layers.Count; layer++)
            {
                string layerRefName = value.layers[layer].layerRefName;
                var layerTexture = value.layers[layer].layerTexture;

                var textureInfo = new PropertyCollector.TextureInfo
                {
                    name = layerRefName,
                    textureId = (layerTexture != null && layerTexture.texture != null) ? layerTexture.texture.GetInstanceID() : 0,
                    modifiable = true
                };
                infos.Add(textureInfo);
            }
        }
    }
}
