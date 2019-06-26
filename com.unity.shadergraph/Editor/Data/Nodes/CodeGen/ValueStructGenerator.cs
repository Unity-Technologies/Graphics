using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Unity.Mathematics;

namespace UnityEditor.ShaderGraph
{
    public static class MathUtils
    {
        public static float normalize(float v) => v == 0 ? float.PositiveInfinity : 1;
        public static float2 normalize(float2 v) => math.normalize(v);
        public static float3 normalize(float3 v) => math.normalize(v);
        public static float4 normalize(float4 v) => math.normalize(v);

        public static float reflect(float i, float n) => i - 2f * n * (i * n);
        public static float2 reflect(float2 i, float2 n) => math.reflect(i, n);
        public static float3 reflect(float3 i, float3 n) => math.reflect(i, n);
        public static float4 reflect(float4 i, float4 n) => math.reflect(i, n);

        public static float refract(float i, float n, float eta)
        {
            float ni = n * i;
            float k = 1.0f - eta * eta * (1.0f - ni * ni);
            return math.select(0.0f, eta * i - (eta * ni + math.sqrt(k)) * n, k >= 0);
        }

        public static float2 refract(float2 i, float2 n, float eta) => math.refract(i, n, eta);
        public static float3 refract(float3 i, float3 n, float eta) => math.refract(i, n, eta);
        public static float4 refract(float4 i, float4 n, float eta) => math.refract(i, n, eta);
    }

    public static class ValueStructGenerator
    {
        private static string ValueTypeName(int componentCount)
            => componentCount == 1 ? "Float" : $"Float{componentCount}";

        private static string HlslTypeName(int componentCount)
            => componentCount == 1 ? "float" : $"float{componentCount}";

        private static char SwizzleComponentName(int v)
            => "xyzw"[v];

        private static string SwizzleName(char[] swizzleNames, int componentCount)
            => String.Concat(Enumerable.Take(swizzleNames, componentCount).Reverse());

        private static void GenerateSwizzle(StringBuilder sb, int maxComponents)
        {
            var swizzleName = new char[4];
            for (int swizzleComponents = 1; swizzleComponents <= 4; ++swizzleComponents)
            {
                var typeName = ValueTypeName(swizzleComponents);
                for (int i = 0; i < (int)Math.Pow(maxComponents, swizzleComponents); ++i)
                {
                    swizzleName[0] = SwizzleComponentName(i % maxComponents);
                    swizzleName[1] = SwizzleComponentName((i / maxComponents) % maxComponents);
                    swizzleName[2] = SwizzleComponentName((i / maxComponents / maxComponents) % maxComponents);
                    swizzleName[3] = SwizzleComponentName((i / maxComponents / maxComponents / maxComponents) % maxComponents);
                    var swizzle = SwizzleName(swizzleName, swizzleComponents);
                    sb.Append($"\t\tpublic {typeName} {swizzle}\n");
                    sb.Append($"\t\t\t=> new {typeName}() {{ Value =");
                    if (maxComponents == 1)
                    {
                        // We don't have .x, .xx, .xxx, .xxxx on float.
                        sb.Append($"Value != null ? {(swizzleComponents != 1 ? "float" + swizzleComponents : "")}(Value.Value) : {typeName}.Null }};\n");
                    }
                    else
                    {
                        sb.Append($"Value?.{swizzle} }};\n");
                    }
                    sb.Append("\n");
                }
            }
        }

        private static void GenerateConstructor(StringBuilder sb, List<int> stack)
        {
            int components = stack.Sum();
            var typeName = ValueTypeName(components);
            var hlslTypeName = HlslTypeName(components);
            sb.Append($"\t\tpublic static {typeName} {typeName}(");
            for (int v = 0; v < stack.Count; ++v)
                sb.Append($"{ValueTypeName(stack[v])} v{v}{(v != stack.Count - 1 ? ", " : "")}");
            sb.Append(")\n");
            if (stack.Count == 1)
            {
                sb.Append("\t\t\t=> v0;\n\n");
            }
            else
            {
                sb.Append($"\t\t\t=> new {typeName}() {{ Value = ");
                for (int v = 0; v < stack.Count; ++v)
                    sb.Append($"{(v != 0 ? " && " : "")}v{v}.Value != null");
                sb.Append($" ? {hlslTypeName}(");
                for (int v = 0; v < stack.Count; ++v)
                    sb.Append($"{(v != 0 ? ", " : "")}v{v}.Value.Value");
                sb.Append($") : Hlsl.{typeName}.Null");
                sb.Append(" };\n\n");
            }
        }

        private static void GenerateConstructosRecurse(StringBuilder sb, int components, List<int> stack)
        {
            if (components == 0)
            {
                GenerateConstructor(sb, stack);
            }
            else
            {
                for (int i = components; i >= 1; --i)
                {
                    stack.Add(i);
                    GenerateConstructosRecurse(sb, components - i, stack);
                    stack.RemoveAt(stack.Count - 1);
                }
            }
        }

        private static void GenerateConstructors(StringBuilder sb)
        {
            var stack = new List<int>(4);
            for (int components = 1; components <= 4; ++components)
                GenerateConstructosRecurse(sb, components, stack);
        }

        private static string GetIntrinsicTypeName(int components, int fixToFloat1, int index)
        {
            return "Hlsl." + ((fixToFloat1 & (1 << index)) == 0 ? ValueTypeName(components) : "Float");
        }

        private static void Intrinsic1(StringBuilder sb, string func, int fixToFloat1 = 0, string evaluator = null)
        {
            if (evaluator == null)
                evaluator = $"x.Value != null ? math.{func}(x.Value.Value) : {{0}}.Null";
            for (int components = 1; components <= 4; ++components)
            {
                var returnType = GetIntrinsicTypeName(components, fixToFloat1, 0);
                var paramType0 = GetIntrinsicTypeName(components, fixToFloat1, 1);
                sb.Append($"\t\tpublic static {returnType} {func}({paramType0} x)\n");
                sb.Append($"\t\t\t=> new {returnType}() {{ Value = {String.Format(evaluator, returnType)} }};\n\n");
            }
        }

        private static void Intrinsic2(StringBuilder sb, string func, int fixToFloat1 = 0, string evaluator = null)
        {
            if (evaluator == null)
                evaluator = $"x.Value != null && y.Value != null ? math.{func}(x.Value.Value, y.Value.Value) : {{0}}.Null";
            for (int components = 1; components <= 4; ++components)
            {
                var returnType = GetIntrinsicTypeName(components, fixToFloat1, 0);
                var paramType0 = GetIntrinsicTypeName(components, fixToFloat1, 1);
                var paramType1 = GetIntrinsicTypeName(components, fixToFloat1, 2);
                sb.Append($"\t\tpublic static {returnType} {func}({paramType0} x, {paramType1} y)\n");
                sb.Append($"\t\t\t=> new {returnType}() {{ Value = {String.Format(evaluator, returnType)} }};\n\n");
            }
        }

        private static void Intrinsic3(StringBuilder sb, string func, int fixToFloat1 = 0, string evaluator = null)
        {
            if (evaluator == null)
                evaluator = $"x.Value != null && y.Value != null && z.Value != null ? math.{func}(x.Value.Value, y.Value.Value, z.Value.Value) : {{0}}.Null";
            for (int components = 1; components <= 4; ++components)
            {
                var returnType = GetIntrinsicTypeName(components, fixToFloat1, 0);
                var paramType0 = GetIntrinsicTypeName(components, fixToFloat1, 1);
                var paramType1 = GetIntrinsicTypeName(components, fixToFloat1, 2);
                var paramType2 = GetIntrinsicTypeName(components, fixToFloat1, 3);
                sb.Append($"\t\tpublic static {returnType} {func}({paramType0} x, {paramType1} y, {paramType2} z)\n");
                sb.Append($"\t\t\t=> new {returnType}() {{ Value = {String.Format(evaluator, returnType)} }};\n\n");
            }
        }

        [MenuItem("Tools/SG/GenerateCs")]
        public static void Generate()
        {
            string path = "Packages/com.unity.shadergraph/Editor/Data/Nodes/CodeGen/Values.gen.cs";

            using (var stream = new StreamWriter(path))
            {
                var sb = new StringBuilder();
                sb.Append("// Auto-generated by Tools/SG/GenerateCs menu. DO NOT hand edit.\n");
                sb.Append("using Unity.Mathematics;\n");
                sb.Append("using static Unity.Mathematics.math;\n");
                sb.Append("namespace UnityEditor.ShaderGraph.Hlsl\n");
                sb.Append("{\n");

                for (int i = 1; i <= 4; ++i)
                {
                    var typeName = ValueTypeName(i);
                    var hlslTypeName = HlslTypeName(i);
                    sb.Append($"\tpublic struct {typeName}\n");
                    sb.Append("\t{\n");
                    sb.Append($"\t\tpublic {hlslTypeName}? Value;\n");
                    sb.Append($"\t\tpublic static readonly {hlslTypeName}? Null = null;\n\n");

                    if (i == 1)
                    {
                        for (int j = 2; j <= 4; ++j)
                        {
                            var otherTypeName = ValueTypeName(j);
                            var otherHlslTypeName = HlslTypeName(j);
                            sb.Append($"\t\tpublic static implicit operator {otherTypeName}(Float x)\n");
                            sb.Append($"\t\t\t=> new {otherTypeName}() {{ Value = x.Value != null ? {otherHlslTypeName}(x.Value.Value) : {otherTypeName}.Null }};\n");
                            sb.Append("\n");
                        }
                    }

                    if (i == 4)
                    {
                        sb.Append("\t\tpublic string Trim(int components)\n");
                        sb.Append("\t\t{\n");
                        sb.Append("\t\t\tif (components == 1)\n");
                        sb.Append("\t\t\t\treturn \".x\";\n");
                        sb.Append("\t\t\telse if (components == 2)\n");
                        sb.Append("\t\t\t\treturn \".xy\";\n");
                        sb.Append("\t\t\telse if (components == 3)\n");
                        sb.Append("\t\t\t\treturn \".xyz\";\n");
                        sb.Append("\t\t\telse\n");
                        sb.Append("\t\t\t\treturn \".xyzw\";\n");
                        sb.Append("\t\t}\n\n");
                    }

                    sb.Append($"\t\tpublic static {typeName} operator-({typeName} v)\n");
                    sb.Append($"\t\t\t=> new {typeName}() {{ Value = v.Value != null ? -v.Value.Value : Null }};\n\n");

                    sb.Append($"\t\tpublic static implicit operator {typeName} (float v)\n");
                    sb.Append($"\t\t\t=> new {typeName}() {{ Value = {(i != 1 ? hlslTypeName : "")}(v) }};\n\n");

                    sb.Append($"\t\tpublic static implicit operator {typeName} (int v)\n");
                    sb.Append($"\t\t\t=> new {typeName}() {{ Value = {(i != 1 ? hlslTypeName : "")}(v) }};\n\n");

                    sb.Append($"\t\tpublic static implicit operator {typeName} (double v)\n");
                    sb.Append($"\t\t\t=> new {typeName}() {{ Value = {(i != 1 ? hlslTypeName : "")}((float)v) }};\n\n");

                    sb.Append($"\t\tpublic static {typeName} operator+({typeName} x, {typeName} y)\n");
                    sb.Append($"\t\t\t=> new {typeName}() {{ Value = x.Value != null && y.Value != null ? x.Value.Value + y.Value.Value : Null }};\n");
                    sb.Append("\n");
                    sb.Append($"\t\tpublic static {typeName} operator-({typeName} x, {typeName} y)\n");
                    sb.Append($"\t\t\t=> new {typeName}() {{ Value = x.Value != null && y.Value != null ? x.Value.Value - y.Value.Value : Null }};\n");
                    sb.Append("\n");
                    sb.Append($"\t\tpublic static {typeName} operator*({typeName} x, {typeName} y)\n");
                    sb.Append($"\t\t\t=> new {typeName}() {{ Value = x.Value != null && y.Value != null ? x.Value.Value * y.Value.Value : Null }};\n");
                    sb.Append("\n");
                    sb.Append($"\t\tpublic static {typeName} operator/({typeName} x, {typeName} y)\n");
                    sb.Append($"\t\t\t=> new {typeName}() {{ Value = x.Value != null && y.Value != null ? x.Value.Value / y.Value.Value : Null }};\n");
                    sb.Append("\n");

                    sb.Append("\t\t#region Swizzles\n\n");
                    GenerateSwizzle(sb, i);
                    sb.Append("\t\t#endregion\n");

                    sb.Append("\t}\n");
                    sb.Append("\n");
                }

                // intrinsics
                sb.Append("\tpublic static class Intrinsics\n");
                sb.Append("\t{\n");

                // Constructors
                GenerateConstructors(sb);

                Intrinsic1(sb, "abs");
                Intrinsic1(sb, "acos");
                Intrinsic1(sb, "asin");
                Intrinsic1(sb, "atan");
                Intrinsic2(sb, "atan2");
                Intrinsic1(sb, "ceil");
                Intrinsic3(sb, "clamp");
                Intrinsic1(sb, "cos");
                Intrinsic1(sb, "cosh");

                sb.Append("\t\tpublic static Float3 cross(Float3 x, Float3 y)\n");
                sb.Append("\t\t\t=> new Float3() { Value = x.Value != null && y.Value != null ? math.cross(x.Value.Value, y.Value.Value) : Hlsl.Float3.Null };\n\n");

                Intrinsic1(sb, "ddx", evaluator: "x.Value != null ? 0 : {0}.Null");
                Intrinsic1(sb, "ddy", evaluator: "x.Value != null ? 0 : {0}.Null");
                Intrinsic1(sb, "degrees");
                Intrinsic2(sb, "distance", fixToFloat1: 1);
                Intrinsic2(sb, "dot", fixToFloat1: 1);
                Intrinsic1(sb, "exp");
                Intrinsic1(sb, "exp2");
                Intrinsic1(sb, "floor");
                Intrinsic2(sb, "fmod");
                Intrinsic1(sb, "frac");
                Intrinsic1(sb, "fwidth", evaluator: "x.Value != null ? 0 : {0}.Null");
                Intrinsic1(sb, "length", fixToFloat1: 1);
                Intrinsic3(sb, "lerp");
                Intrinsic1(sb, "log");
                Intrinsic1(sb, "log10");
                Intrinsic1(sb, "log2");
                Intrinsic2(sb, "max");
                Intrinsic2(sb, "min");
                Intrinsic1(sb, "normalize", evaluator: "x.Value != null ? MathUtils.normalize(x.Value.Value) : {0}.Null");
                Intrinsic2(sb, "pow");
                Intrinsic1(sb, "radians");
                Intrinsic2(sb, "reflect", evaluator: "x.Value != null && y.Value != null ? MathUtils.reflect(x.Value.Value, y.Value.Value) : {0}.Null");
                Intrinsic3(sb, "refract", fixToFloat1: 8, evaluator: "x.Value != null && y.Value != null && z.Value != null ? MathUtils.refract(x.Value.Value, y.Value.Value, z.Value.Value) : {0}.Null");
                Intrinsic1(sb, "rcp");
                Intrinsic1(sb, "round");
                Intrinsic1(sb, "rsqrt");
                Intrinsic1(sb, "saturate");
                Intrinsic1(sb, "sign");
                Intrinsic1(sb, "sin");
                Intrinsic1(sb, "sinh");
                Intrinsic3(sb, "smoothstep");
                Intrinsic1(sb, "sqrt");
                Intrinsic2(sb, "step");
                Intrinsic1(sb, "tan");
                Intrinsic1(sb, "tanh");
                Intrinsic1(sb, "trunc");

                sb.Append("\t}\n");

                sb.Append("}\n");

                sb.Replace("\n", System.Environment.NewLine);
                sb.Replace("\t", "    ");
                stream.WriteLine(sb.ToString());
            }

            AssetDatabase.Refresh();
        }
    }
}
