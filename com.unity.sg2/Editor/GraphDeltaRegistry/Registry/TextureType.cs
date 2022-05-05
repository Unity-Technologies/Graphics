using UnityEditor.ShaderFoundry;
using UnityEngine;


namespace UnityEditor.ShaderGraph.GraphDelta
{
    internal class SimpleSampleTexture2DNode : INodeDefinitionBuilder
    {
        public const string kTexture = "Input";
        public const string kUV = "UV";
        public const string kSampler = "SamplerStateOverride";
        public const string kOutput = "Output";

        public RegistryKey GetRegistryKey()
            => new() { Name = "SimpleSampleTexture2DNode", Version = 1 };

        public RegistryFlags GetRegistryFlags()
            => RegistryFlags.Func;

        public void BuildNode(NodeHandler node, Registry registry)
        {
            node.AddPort<BaseTextureType>(kTexture, true, registry);
            var uv = node.AddPort<GraphType>(kUV, true, registry).GetTypeField();
            node.AddPort<SamplerStateType>(kSampler, true, registry);
            var color = node.AddPort<GraphType>(kOutput, false, registry).GetTypeField();
            GraphTypeHelpers.InitGraphType(uv, GraphType.Length.Two);
            GraphTypeHelpers.InitGraphType(color);
        }

        public ShaderFunction GetShaderFunction(NodeHandler node, ShaderContainer container, Registry registry)
        {
            // Need to ignore the sampler if there is no user data on it or if it isn't connected, textures have one they are loaded w/already.
            var samplerPort = node.GetPort(kSampler);
            bool isConnected = samplerPort.GetConnectedPorts().Count() != 0;
            bool isInitialized = SamplerStateType.IsInitialized(samplerPort.GetTypeField());
            bool hasSampler = isConnected || isInitialized;

            var builder = new ShaderFunction.Builder(container, GetRegistryKey().Name + (hasSampler ? "_Smplr" : ""));
            var shaderType = registry.GetShaderType(node.GetPort(kTexture).GetTypeField(), container);



            // hasSampler = true; // can enable to force usage of sampler for testing.

            builder.AddInput(shaderType, kTexture);
            builder.AddInput(container._float2, kUV);
            if (hasSampler) // TODO: Should be possible (somewhere) to inform the interpreter that the variable generated for this port will not be used.
                builder.AddInput(container._UnitySamplerState, kSampler);
            builder.AddOutput(container._float4, kOutput);

            string body = $"{kOutput} = SAMPLE_TEXTURE2D({kTexture}.tex, {(hasSampler ? kSampler : kTexture)}.samplerstate, {kTexture}.GetTransformedUV({kUV}));";
            builder.AddLine(body);
            return builder.Build();
        }
    }

    internal class SimpleTextureNode : INodeDefinitionBuilder
    {
        public const string kInlineStatic = "InlineStatic";
        public const string kOutput = "Output";

        public RegistryKey GetRegistryKey()
            => new() { Name = "SimpleSampleTextureNode", Version = 1 };

        public RegistryFlags GetRegistryFlags()
            => RegistryFlags.Func;

        public void BuildNode(NodeHandler node, Registry registry)
        {
            var input = node.AddPort<BaseTextureType>(kInlineStatic, true, registry);
            input.GetTypeField().AddSubField("IsStatic", true); // TODO: should be handled by ui hints
            node.AddPort<BaseTextureType>(kOutput, false, registry);
        }

        public ShaderFunction GetShaderFunction(NodeHandler node, ShaderContainer container, Registry registry)
        {
            var shaderFunctionBuilder = new ShaderFunction.Builder(container, GetRegistryKey().Name);
            var field = node.GetPort(kInlineStatic).GetTypeField();

            //string name;
            //switch (BaseTextureType.GetTextureAsset(field))
            //{
            //    case Texture3D:
            //        name = $"{GetRegistryKey().Name}_3D";
            //        break;
            //    case Texture2DArray:
            //        name = $"{GetRegistryKey().Name}_2DArray";
            //        break;
            //    case Cubemap:
            //        name = $"{GetRegistryKey().Name}_Cube";
            //        break;
            //    case Texture2D:
            //    default:
            //        name = $"{GetRegistryKey().Name}_2D";
            //        break;
            //}

            var shaderType = registry.GetShaderType(field, container);

            shaderFunctionBuilder.AddInput(shaderType, kInlineStatic);
            shaderFunctionBuilder.AddOutput(shaderType, kOutput);
            shaderFunctionBuilder.AddLine($"{kOutput} = {kInlineStatic};");
            return shaderFunctionBuilder.Build();
        }
    }

    // TODO (Brett) This should probably be called TextureType not BaseTextureType
    public class BaseTextureType : ITypeDefinitionBuilder
    {
        public enum TextureType { Texture2D, Texture3D, CubeMap, Texture2DArray }

        public static RegistryKey kRegistryKey
            => new () { Name = "TextureType", Version = 1 };

        #region LocalNames
        public const string KAsset = "Asset";
        public const string kTextureType = "TextureType";
        #endregion

        public RegistryKey GetRegistryKey()
            => kRegistryKey;

        public RegistryFlags GetRegistryFlags()
            => RegistryFlags.Type;

        public static string GetUniqueUniformName(FieldHandler data)
            => data.ID.FullPath.Replace('.', '_') + "_Tex";

        private static string GetUniquePropertyName(FieldHandler data)
            => $"Property_{GetUniqueUniformName(data)}";

        public static Texture GetTextureAsset(FieldHandler data)
            => data.GetSubField<Internal.SerializableTexture>(KAsset).GetData().texture;

        public static void SetTextureAsset(FieldHandler data, Texture tex)
        {
            var stex = new Internal.SerializableTexture
            {
                texture = tex
            };

            data.GetSubField<Internal.SerializableTexture>(KAsset).SetData(stex);
        }

        public static void SetTextureType(FieldHandler data, TextureType type)
            => data.GetSubField<TextureType>(kTextureType).SetData(type);

        public static TextureType GetTextureType(FieldHandler data)
            => data.GetSubField<TextureType>(kTextureType).GetData();

        public void BuildType(FieldHandler field, Registry registry)
        {
            var stex = new Internal.SerializableTexture
            {
                texture = null
            };
            field.AddSubField(KAsset, stex);
            field.AddSubField(kTextureType, TextureType.Texture2D);
        }

        ShaderType ITypeDefinitionBuilder.GetShaderType(FieldHandler field, ShaderContainer container, Registry registry)
        {
            return GetTextureType(field) switch
            {
                TextureType.Texture3D => container._UnityTexture3D,
                TextureType.Texture2DArray => container._UnityTexture2DArray,
                TextureType.CubeMap => container._UnityTextureCube,
                _ => container._UnityTexture2D,
            };
        }

        public string GetInitializerList(FieldHandler field, Registry registry)
        {
            string name = GetUniqueUniformName(field);
            return $"In.{name}";
        }

        internal static StructField UniformPromotion(FieldHandler field, ShaderContainer container, Registry registry)
        {
            var name = GetUniqueUniformName(field);
            var fieldbuilder = new StructField.Builder(container, name, registry.GetShaderType(field, container));
            var attrBuilder = new ShaderAttribute.Builder(container, CommonShaderAttributes.Property);
            attrBuilder.Param("displayName", GetUniquePropertyName(field));
            attrBuilder.Param("defaultValue", "\"white\" {}");
            fieldbuilder.AddAttribute(attrBuilder.Build());

            return fieldbuilder.Build();
        }
    }

    internal class BaseTextureTypeAssignment : ICastDefinitionBuilder
    {
        public RegistryKey GetRegistryKey() =>
            new() { Name = "BaseTextureAssignment", Version = 1 };

        public RegistryFlags GetRegistryFlags() =>
            RegistryFlags.Cast;

        public (RegistryKey, RegistryKey) GetTypeConversionMapping() =>
            (BaseTextureType.kRegistryKey, BaseTextureType.kRegistryKey);

        public bool CanConvert(FieldHandler src, FieldHandler dst)
        {
            return BaseTextureType.GetTextureType(src) == BaseTextureType.GetTextureType(dst);
        }

        public ShaderFunction GetShaderCast(FieldHandler src, FieldHandler dst, ShaderContainer container, Registry registry)
        {
            var type = registry.GetShaderType(src, container);
            string castName = $"Cast{type.Name}_{type.Name}";
            var builder = new ShaderFunction.Builder(container, castName);
            builder.AddInput(type, "In");
            builder.AddOutput(type, "Out");
            builder.AddLine("Out = In;");
            return builder.Build();
        }
    }
}
