

using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Registry.Types
{
    internal static class GradientTypeHelpers
    {
        public static Gradient GetToGradient(IFieldReader field)
        {
            field.GetField<GradientMode>(GradientType.kGradientMode, out var mode);
            field.GetField<int>(GradientType.kColorCount, out var colorCount);
            field.GetField<int>(GradientType.kAlphaCount, out var alphaCount);

            List<GradientColorKey> colors = new();
            List<GradientAlphaKey> alphas = new();
            for (int i = 0; i < colorCount; ++i)
            {
                field.GetField(GradientType.kColor(i), out GradientColorKey colorKey);
                colors.Add(colorKey);
            }
            for (int i = 0; i < alphaCount; ++i)
            {
                field.GetField(GradientType.kAlpha(i), out GradientAlphaKey alphaKey);
                alphas.Add(alphaKey);
            }

            var result = new Gradient();
            result.mode = mode;
            result.SetKeys(colors.ToArray(), alphas.ToArray());
            return result;
        }

        public static void SetFromGradient(IFieldWriter field, Gradient gradient)
        {
            field.SetField(GradientType.kGradientMode, gradient.mode);
            field.SetField(GradientType.kColorCount, gradient.colorKeys.Length);
            field.SetField(GradientType.kAlphaCount, gradient.alphaKeys.Length);

            for (int i = 0; i < 8 && i < gradient.colorKeys.Length; ++i)
                field.SetField(GradientType.kColor(i), gradient.colorKeys[i]);

            for (int i = 0; i < 8 && i < gradient.alphaKeys.Length; ++i)
                field.SetField(GradientType.kAlpha(i), gradient.alphaKeys[i]);
        }
    }

    /// <summary>
    /// Base 'GraphType' representing templated HLSL Types, eg. vector <float, 3>, matrix <float 4, 4>, int3, etc.
    /// </summary>
    internal class GradientType : Defs.ITypeDefinitionBuilder
    {
        public static RegistryKey kRegistryKey => new RegistryKey { Name = "GradientType", Version = 1 };
        public RegistryKey GetRegistryKey() => kRegistryKey;
        public RegistryFlags GetRegistryFlags() => RegistryFlags.Type;


        #region LocalNames
        // These types map directly from the Gradient class, maybe there is a more procedural way to go from simple classes -> CLDS Safe container fields?
        public const string kGradientMode = "GradientMode"; // GradientMode
        public const string kColorCount = "ColorCount";     // int
        public const string kAlphaCount = "AlphaCount";     // int
        public static string kColor(int i) => $"Color{i}";         // GradientColorKey
        public static string kAlpha(int i) => $"Alpha{i}";         // GardientAlphaKey
        #endregion

        public void BuildType(IFieldReader userData, IFieldWriter typeWriter, Registry registry)
        {
            typeWriter.SetField(kGradientMode, GradientMode.Blend);
            typeWriter.SetField(kColorCount, 2);
            typeWriter.SetField(kAlphaCount, 2);
            typeWriter.SetField(kColor(0), new GradientColorKey(Color.black, 0));
            typeWriter.SetField(kColor(1), new GradientColorKey(Color.white, 1));
            typeWriter.SetField(kAlpha(0), new GradientAlphaKey(255, 0));
            typeWriter.SetField(kAlpha(1), new GradientAlphaKey(255, 1));

            // TODO: Precision; the Gradient type we use in Functions.hlsl does not handle precision, despite surrounding shader code.
            // Ideally, we could just generate the complete Gradient Struct per precision type, instead of using the one from Functions.hlsl;
            // this would depend on ShaderFoundry reintroducing better support for structs. For now we'll use the built-in type.
        }

        private string ColorKeyToDecl(GradientColorKey key) => $"float4({key.color.r},{key.color.g},{key.color.b},{key.time})";
        private string AlphaKeyToDecl(GradientAlphaKey key) => $"float2({key.alpha},{key.time})";


        string Defs.ITypeDefinitionBuilder.GetInitializerList(IFieldReader data, Registry registry)
        {
            data.GetField<int>(kColorCount, out var colorCount);
            data.GetField<int>(kAlphaCount, out var alphaCount);
            data.GetField<GradientMode>(kGradientMode, out var gradientMode);

            string alpha = "";
            string color = "";
            for(int i = 0; i < 8; ++i)
            {
                var localColor =  "float4(0, 0, 0, 0)";
                var localAlpha = "float2(0, 0)";

                if (i < colorCount)
                {
                    data.GetField(kColor(i), out GradientColorKey colorKey);
                    localColor = ColorKeyToDecl(colorKey);
                }
                if (i < alphaCount)
                {
                    data.GetField(kAlpha(i), out GradientAlphaKey alphaKey);
                    localAlpha = AlphaKeyToDecl(alphaKey);
                }


                color += localColor + (i < 7 ? ", " : "");
                alpha += localAlpha + (i < 7 ? ", " : "");
            }

            return $"NewGradient({(int)gradientMode},{colorCount}, {alphaCount}, {color}, {alpha})";
        }

        ShaderFoundry.ShaderType Defs.ITypeDefinitionBuilder.GetShaderType(IFieldReader data, ShaderFoundry.ShaderContainer container, Registry registry)
        {
            var gradientBuilder = new ShaderFoundry.ShaderType.StructBuilder(container, "Gradient");
            gradientBuilder.DeclaredExternally();
            var gradientType = gradientBuilder.Build();
            return gradientType;
        }
    }

    internal class GradientTypeAssignment : Defs.ICastDefinitionBuilder
    {
        public RegistryKey GetRegistryKey() => new RegistryKey { Name = "GraphTypeAssignment", Version = 1 };
        public RegistryFlags GetRegistryFlags() => RegistryFlags.Cast;
        public (RegistryKey, RegistryKey) GetTypeConversionMapping() => (GraphType.kRegistryKey, GraphType.kRegistryKey);
        public bool CanConvert(IFieldReader src, IFieldReader dst)
        {
            src.GetField(GraphType.kLength, out GraphType.Length srcLen);
            src.GetField(GraphType.kHeight, out GraphType.Height srcHgt);

            dst.GetField(GraphType.kLength, out GraphType.Length dstLen);
            dst.GetField(GraphType.kHeight, out GraphType.Height dstHgt);

            return srcHgt == dstHgt || srcHgt == GraphType.Height.One && srcLen >= dstLen;
        }

        private static string MatrixCompNameFromIndex(int i, int d)
        {
            return $"_mm{ i / d }{ i % d }";
        }

        private static string VectorCompNameFromIndex(int i)
        {
            switch (i)
            {
                case 0: return "x";
                case 1: return "y";
                case 2: return "z";
                case 3: return "w";
                default: throw new Exception("Invalid vector index.");
            }
        }


        public ShaderFoundry.ShaderFunction GetShaderCast(IFieldReader src, IFieldReader dst, ShaderFoundry.ShaderContainer container, Registry registry)
        {
            // In this case, we can determine a casting operation purely from the built types. We don't actually need to analyze field data,
            // this is because it's all alreay been encapsulated in the previously built ShaderType.
            var srcType = registry.GetTypeBuilder(src.GetRegistryKey()).GetShaderType(src, container, registry);
            var dstType = registry.GetTypeBuilder(dst.GetRegistryKey()).GetShaderType(dst, container, registry);

            string castName = $"Cast{srcType.Name}_{dstType.Name}";
            var builder = new ShaderFoundry.ShaderFunction.Builder(container, castName);
            builder.AddInput(srcType, "In");
            builder.AddOutput(dstType, "Out");

            // CanConvert should prevent srcSize from being smaller than dstSize.
            var srcSize = srcType.IsVector ? srcType.VectorDimension : srcType.IsMatrix ? srcType.MatrixColumns * srcType.MatrixRows : 1;
            var dstSize = dstType.IsVector ? dstType.VectorDimension : dstType.IsMatrix ? dstType.MatrixColumns * dstType.MatrixRows : 1;

            string body = $"Out = {srcType.Name} {{ ";

            for (int i = 0; i < dstSize; ++i)
            {
                if (i < srcSize)
                {
                    if (dstType.IsMatrix) body += $"In.{MatrixCompNameFromIndex(i, dstType.MatrixColumns)}"; // are we row or column major?
                    if (dstType.IsVector) body += $"In.{VectorCompNameFromIndex(i)}";
                    if (dstType.IsScalar) body += $"In";
                }
                else body += "0";
                if (i != dstSize - 1) body += ", ";
            }
            body += " };";

            builder.AddLine(body);
            return builder.Build();
        }
    }
}




//public static string GetGradientValue(Gradient gradient, string delimiter = ";")
//{
//    string colorKeys = "";
//    for (int i = 0; i < 8; i++)
//    {
//        if (i < gradient.colorKeys.Length)
//            colorKeys += $"$precision4({NodeUtils.FloatToShaderValue(gradient.colorKeys[i].color.r)}, " +
//                $"{NodeUtils.FloatToShaderValue(gradient.colorKeys[i].color.g)}, " +
//                $"{NodeUtils.FloatToShaderValue(gradient.colorKeys[i].color.b)}, " +
//                $"{NodeUtils.FloatToShaderValue(gradient.colorKeys[i].time)})";
//        else
//            colorKeys += "$precision4(0, 0, 0, 0)";
//        if (i < 7)
//            colorKeys += ",";
//    }

//    string alphaKeys = "";
//    for (int i = 0; i < 8; i++)
//    {
//        if (i < gradient.alphaKeys.Length)
//            alphaKeys += $"$precision2({NodeUtils.FloatToShaderValue(gradient.alphaKeys[i].alpha)}, {NodeUtils.FloatToShaderValue(gradient.alphaKeys[i].time)})";
//        else
//            alphaKeys += "$precision2(0, 0)";
//        if (i < 7)
//            alphaKeys += ",";
//    }

//    return $"NewGradient({(int)gradient.mode}, {gradient.colorKeys.Length}, {gradient.alphaKeys.Length}, {colorKeys}, {alphaKeys}){delimiter}";
//}
