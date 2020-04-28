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
            value.entries = new List<SerializableVirtualTextureLayer>();
            value.entries.Add(new SerializableVirtualTextureLayer("Layer0", new SerializableTexture()));
            value.entries.Add(new SerializableVirtualTextureLayer("Layer1", new SerializableTexture()));
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

        // this is used for properties exposed to the Material in the shaderlab Properties{} block
        internal override string GetPropertyBlockString()
        {
            // adds a properties in this format so: [TextureStack.MyStack(0)] [NoScaleOffset] Layer0("Layer0", 2D) = "white" {}
            string result = "";
            for (int layer= 0; layer < value.entries.Count; layer++)
            {
                string layerName = value.entries[layer].layerName;
                result += $"{hideTagString}[TextureStack.{referenceName}({layer})][NoScaleOffset]{layerName}(\"{layerName}\", 2D) = \"white\" {{}}{Environment.NewLine}";
            }
            return result;
        }

        internal override void AppendBatchablePropertyDeclarations(ShaderStringBuilder builder, string delimiter = ";")
        {
            int numLayers = value.entries.Count;
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
            int numLayers = value.entries.Count;
            if (numLayers > 0)
            {
                builder.Append("DECLARE_STACK");
                builder.Append((numLayers <= 1) ? "" : numLayers.ToString());
                builder.Append("(");
                builder.Append(referenceName);
                builder.Append(",");
                for (int i = 0; i < value.entries.Count; i++)
                {
                    if (i != 0) builder.Append(",");
                    builder.Append(value.entries[i].layerName);
                }
                builder.Append(")");
                builder.AppendLine(delimiter);      // TODO: don't like delimiter, pretty sure it's not necessary if we invert the defaults on GEtPropertyDeclaration / GetPropertyArgument string

                // declare the actual property "variable" as a macro define to the BuildVTProperties function
                builder.AppendLine("#define " + referenceName + " BuildVTProperties_" + referenceName + "()");
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
            return "VTPropertyParameters " + referenceName;
        }

        // if a blackboard property is deleted, all node instances of it are replaced with this:
        internal override AbstractMaterialNode ToConcreteNode()
        {
            // TODO:
            return null;
        }

        internal override PreviewProperty GetPreviewMaterialProperty()
        {
            return new PreviewProperty(propertyType)
            {
                name = referenceName,
                vtValues = value
            };
        }

        internal override ShaderInput Copy()
        {
            return new VirtualTextureShaderProperty
            {
                displayName = displayName,
                hidden = hidden,
                value = value,
                precision = precision
            };
        }

        internal void AddTextureInfo(List<PropertyCollector.TextureInfo> infos)
        {
            for (int layer = 0; layer < value.entries.Count; layer++)
            {
                string layerName = value.entries[layer].layerName;
                var layerTexture = value.entries[layer].layerTexture;

                var textureInfo = new PropertyCollector.TextureInfo
                {
                    name = layerName,
                    textureId = layerTexture != null ? layerTexture.texture.GetInstanceID() : 0,
                    modifiable = true
                };
                infos.Add(textureInfo);
            }
        }
    }
}
