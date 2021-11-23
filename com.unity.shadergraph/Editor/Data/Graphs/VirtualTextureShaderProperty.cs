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
    [BlackboardInputInfo(60)]
    class VirtualTextureShaderProperty : AbstractShaderProperty<SerializableVirtualTexture>
    {
        public VirtualTextureShaderProperty()
        {
            displayName = "VirtualTexture";
            value = new SerializableVirtualTexture();

            // add at least one layer
            value.layers = new List<SerializableVirtualTextureLayer>();
            value.layers.Add(new SerializableVirtualTextureLayer("Layer0", new SerializableTexture()));
            value.layers.Add(new SerializableVirtualTextureLayer("Layer1", new SerializableTexture()));
        }

        public override PropertyType propertyType => PropertyType.VirtualTexture;

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
            else
            {
                // For procedural VT, we only need to expose a single property, indicating the referenceName and the number of layers

                // Adds a property as:
                //   [ProceduralTextureStack.MyStack(1)] [NoScaleOffset] MyStack("Procedural Virtual Texture", 2D) = "white" {}
                // or:
                //   [GlobalProceduralTextureStack.MyStack(2)] [NoScaleOffset] MyStack("Procedural Virtual Texture", 2D) = "white" {}
                string prefixString = value.shaderDeclaration == HLSLDeclaration.UnityPerMaterial
                    ? "ProceduralTextureStack"
                    : "GlobalProceduralTextureStack";

                int numLayers = value.layers.Count;
                builder.AppendLine($"{hideTagString}[{prefixString}.{referenceName}({numLayers})][NoScaleOffset]{referenceName}(\"{"Procedural Virtual Texture"}\", 2D) = \"white\" {{}}");
            }
        }

        internal override string GetPropertyBlockString()
        {
            // this should not be called, as it is replaced by the Append*PropertyBlockStrings function above
            throw new NotSupportedException();
        }

        internal override bool AllowHLSLDeclaration(HLSLDeclaration decl) => false; // disable UI, nothing to choose

        internal override void ForeachHLSLProperty(Action<HLSLProperty> action)
        {
            int numLayers = value.layers.Count;
            if (numLayers > 0)
            {
                HLSLDeclaration decl = (value.procedural) ? value.shaderDeclaration : HLSLDeclaration.UnityPerMaterial;

                action(new HLSLProperty(HLSLType._CUSTOM, referenceName + "_CBDecl", decl, concretePrecision)
                {
                    customDeclaration = (ssb) =>
                    {
                        ssb.TryAppendIndentation();
                        ssb.Append("DECLARE_STACK_CB(");
                        ssb.Append(referenceName);
                        ssb.Append(");");
                        ssb.AppendNewLine();
                    }
                });

                if (!value.procedural)
                {
                    //declare regular texture properties (for fallback case)
                    for (int i = 0; i < numLayers; i++)
                    {
                        string layerRefName = value.layers[i].layerRefName;
                        action(new HLSLProperty(HLSLType._Texture2D, layerRefName, HLSLDeclaration.Global));
                        action(new HLSLProperty(HLSLType._SamplerState, "sampler" + layerRefName, HLSLDeclaration.Global));
                    }
                }

                Action<ShaderStringBuilder> customDecl = (builder) =>
                {
                    // declare texture stack
                    builder.TryAppendIndentation();
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
                    builder.Append(");");
                    builder.AppendNewLine();

                    // declare the actual virtual texture property "variable" as a macro define to the BuildVTProperties function
                    builder.TryAppendIndentation();
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
                };

                action(new HLSLProperty(HLSLType._CUSTOM, referenceName + "_Global", HLSLDeclaration.Global, concretePrecision)
                {
                    customDeclaration = customDecl
                });
            }
        }

        // argument string used to pass this property to a subgraph
        internal override string GetPropertyAsArgumentString(string precisionString)
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
            var vt = new VirtualTextureShaderProperty
            {
                displayName = displayName,
                value = new SerializableVirtualTexture(),
            };

            // duplicate layer data, but reset reference names (they should be unique)
            for (int layer = 0; layer < value.layers.Count; layer++)
            {
                var guid = Guid.NewGuid();
                vt.value.layers.Add(new SerializableVirtualTextureLayer(value.layers[layer]));
            }

            return vt;
        }

        internal void AddTextureInfo(List<PropertyCollector.TextureInfo> infos)
        {
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
        }

        internal override bool isAlwaysExposed => true;
        internal override bool isCustomSlotAllowed => false;

        public override void OnAfterDeserialize(string json)
        {
            // VT shader properties must be exposed so they can be picked up by the native-side VT system
            generatePropertyBlock = true;
        }
    }
}
