using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Unity.GraphAuthoring.Editor.ProviderSystem;
using UnityEditor.ShaderApiReflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph.ProviderSystem.Tests
{
    // Locks in how hints shape provider nodes.
    [TestFixture]
    class ProviderNodeCharacterizationTests
    {
        const string kGoldenPath = "Packages/com.unity.shadergraph/Tests/Editor/ProviderTests/ProviderNodeCharacterization.golden.txt";
        const string kTempFolder = "Assets/ProviderNodeCharacterizationTemp";

        sealed class SourceProvider : IProvider<IShaderFunction>
        {
            public string ProviderKey { get; }
            public IShaderFunction Definition { get; }
            public IReadOnlyList<Message> Messages { get; }

            internal SourceProvider(string providerKey, ReflectedFunction source)
            {
                ProviderKey = providerKey;
                List<Message> messages = new();
                Definition = ShaderReflectionUtils.ToShaderFunction(source, default, null, messages, out _);
                Messages = messages;
            }

            public IProvider Clone() => this;
        }

        static Dictionary<string, string> H(params string[] keysAndValues)
        {
            Dictionary<string, string> hints = new();
            for (int i = 0; i < keysAndValues.Length; i += 2)
                hints[keysAndValues[i]] = keysAndValues[i + 1];
            return hints;
        }

        static (string declaration, Dictionary<string, string> hints) In(string name, string type, Dictionary<string, string> hints = null) => ($"{type} {name}", hints);
        static (string declaration, Dictionary<string, string> hints) Out(string name, string type, Dictionary<string, string> hints = null) => ($"out {type} {name}", hints);
        static (string declaration, Dictionary<string, string> hints) InOut(string name, string type, Dictionary<string, string> hints = null) => ($"inout {type} {name}", hints);

        // The function as reflected HLSL, with its hints as hint comments.
        static string F(string returnType, Dictionary<string, string> hints, params (string declaration, Dictionary<string, string> hints)[] parameters)
        {
            StringBuilder sb = new();
            AppendHints(sb, "funchints", hints);
            foreach (var (declaration, paramHints) in parameters)
                AppendHints(sb, $"paramhints name = \"{declaration.Split(' ').Last()}\"", paramHints);
            sb.Append("UNITY_EXPORT_REFLECTION ").Append(returnType).Append(" Func(").Append(string.Join(", ", parameters.Select(p => p.declaration))).Append(") {return 0;}\n");
            return sb.ToString();
        }

        static void AppendHints(StringBuilder sb, string tag, Dictionary<string, string> hints)
        {
            if (hints == null)
                return;
            sb.Append("///<").Append(tag).Append(">\n");
            foreach (var (key, value) in hints)
                sb.Append(value.Length == 0 ? $"///<{key} />\n" : $"///<{key}>{value}</{key}>\n");
            sb.Append("///</").Append(tag.Split(' ')[0]).Append(">\n");
        }

        static readonly (string name, string source)[] k_Cases =
        {
            ("Plain", F("float", null, In("a", "float"), Out("b", "float3"), InOut("c", "float2"))),
            ("Void", F("void", null, In("a", "float4"), Out("b", "float4"))),
            ("Presentation", F("float", H("sg:DisplayName", "Nice", "sg:Tooltip", "Tip", "sg:ReturnDisplayName", "Result", "sg:SearchTerms", "x, y", "sg:HelpURL", "https://example.com/page"),
                In("a", "float", H("sg:DisplayName", "Alpha", "summary", "Alpha tip")))),
            ("Return_InheritsLabel", F("float", H("Label", "From Label"))),
            ("Lifecycle_Deprecated", F("float", H("sg:Deprecated", "Use Other"))),
            ("Lifecycle_Obsolete", F("float", H("sg:Obsolete", "Gone"))),
            ("Precision", F("float", H("Precision", ""), In("a", "float3"), Out("b", "half"))),
            ("Dynamic", F("float", null, In("a", "float3", H("Dynamic", "")), In("b", "float", H("Dynamic", "")), Out("c", "float3", H("Dynamic", "")))),
            ("PrecisionAndDynamic", F("float", H("Precision", ""), In("a", "float3", H("Dynamic", "")), Out("c", "float3", H("Dynamic", "")))),
            ("Scalars", F("void", null, In("i", "int"), In("u", "uint"), In("f", "float"), In("h", "half"), In("b", "bool"))),
            ("Vectors", F("void", null, In("v2", "float2"), In("v3", "half3"), In("v4", "float4"))),
            ("Matrices", F("void", null, In("m2", "float2x2"), In("m3", "float3x3"), In("m4", "float4x4"))),
            ("Resources", F("void", null, In("t", "Texture2D"), In("ut", "UnityTexture2D"), In("s", "SamplerState"), In("ta", "Texture2DArray"), In("tc", "TextureCube"), In("t3", "Texture3D"))),
            ("Resources_Out", F("void", null, Out("t", "Texture2D"), Out("s", "SamplerState"))),
            ("External", F("void", null, In("a", "MyStruct", H("sg:External", "MyNamespace", "sg:Default", "0")), In("b", "OtherStruct"))),
            ("Defaults", F("void", null, In("f", "float", H("Default", "0.5")), In("v", "float3", H("Default", "1, 2, 3")), In("b", "bool", H("Default", "true")), In("i", "int", H("Default", "2")))),
            ("Static_Local", F("void", null, In("s", "float", H("sg:Static", "")), In("l", "float", H("sg:Local", "", "Default", "3")))),
            ("Range", F("void", null, In("r", "float", H("sg:Range", "-1, 2", "Default", "0.5")), In("s", "half", H("Slider", "4")))),
            ("Dropdown", F("void", null, In("d", "int", H("sg:Dropdown", "a, b, c", "Default", "1")))),
            ("Color", F("void", null, In("c3", "float3", H("sg:Color", "", "Default", "1, 0, 0")), In("c4", "half4", H("Color", "")))),
            ("Color_Static", F("void", null, In("c", "float4", H("sg:Static", "", "sg:Color", "")))),
            ("CustomBinding", F("void", null, In("a", "float4", H("sg:CustomBinding", "Bound")), InOut("b", "float3", H("sg:CustomBinding", "Both")))),
            ("Referables", F("void", null,
                In("uv", "float2", H("UV", "")), In("uv1", "float2", H("UV", "", "Default", "UV1")),
                In("pos", "float3", H("Position", "", "Default", "World")), In("posAbs", "float3", H("Position", "", "Default", "AbsoluteWorld")),
                In("n", "float3", H("Normal", "", "Default", "Tangent")), In("nAbs", "float3", H("Normal", "", "Default", "AbsoluteWorld")),
                In("t", "float3", H("Tangent", "")), In("bt", "float3", H("Bitangent", "")),
                In("vd", "float3", H("ViewDirection", "", "Default", "View")), In("vc", "float4", H("VertexColor", "")),
                In("sp", "float4", H("ScreenPosition", "", "Default", "Raw")))),
            ("Referable_Unknown", F("void", null, In("x", "float3", H("sg:Referable", "Foo")))),
            ("Linkage", F("void", null, In("enabled", "bool", H("sg:Linkage", "color")), In("color", "float3"), Out("result", "float3"))),
            ("Conflict", F("void", null, In("a", "float3", H("sg:Range", "0, 1", "sg:Color", "")))),
            ("Messages", F("float", H("sg:Version", " ", "sg:GroupKey", "g, x"), In("a", "float3", H("sg:Range", "0, 1")), Out("b", "float", H("sg:Default", "1")))),
        };

        static IEnumerable<string> CaseNames() => k_Cases.Select(c => c.name);

        static string CasePath(string caseName) => $"{kTempFolder}/{caseName}.hlsl";

        [OneTimeSetUp]
        public void WriteCases()
        {
            Directory.CreateDirectory(kTempFolder);
            foreach (var (name, source) in k_Cases)
                File.WriteAllText(CasePath(name), source);
            AssetDatabase.Refresh();
        }

        [OneTimeTearDown]
        public void DeleteCases()
        {
            AssetDatabase.DeleteAsset(kTempFolder);
        }

        static ReflectedFunction Reflect(string caseName)
        {
            var reflection = AssetDatabase.LoadAssetAtPath<ShaderInclude>(CasePath(caseName)).Reflection;
            return reflection.ReflectedFunctions.Single();
        }

        [TestCaseSource(nameof(CaseNames))]
        public void ProviderNodes_AreBuiltAsBefore(string caseName)
        {
            var golden = ReadGolden();
            Assert.IsTrue(golden.TryGetValue(caseName, out var expected), $"No golden snapshot for '{caseName}'. Regenerate the golden file.");
            Assert.AreEqual(expected, Snapshot(caseName));
        }

        // Writes to the temporary folder: this package is read from the package cache in its test project.
        [Test, Explicit("Regenerates the golden file from the current behavior.")]
        public void RegenerateGolden()
        {
            StringBuilder sb = new();
            foreach (var (name, _) in k_Cases)
                sb.Append("=== ").Append(name).Append('\n').Append(Snapshot(name)).Append('\n');
            var path = Path.Combine(Path.GetTempPath(), Path.GetFileName(kGoldenPath));
            File.WriteAllText(path, sb.ToString());
            Debug.Log("Golden written to " + path);
        }

        static Dictionary<string, string> ReadGolden()
        {
            Dictionary<string, string> sections = new();
            string name = null;
            StringBuilder body = new();
            foreach (var line in File.ReadAllText(Path.GetFullPath(kGoldenPath)).Replace("\r\n", "\n").Split('\n'))
            {
                if (line.StartsWith("=== "))
                {
                    if (name != null)
                        sections[name] = body.ToString().TrimEnd('\n');
                    name = line.Substring(4);
                    body.Clear();
                }
                else if (name != null)
                    body.Append(line).Append('\n');
            }
            if (name != null)
                sections[name] = body.ToString().TrimEnd('\n');
            return sections;
        }

        static readonly Regex k_PackageVersion = new("@[0-9]+\\.[0-9]+");
        static readonly Regex k_ObjectIds = new("\"m_ObjectId\": \"[0-9a-f]+\",?\\s*|[0-9a-f]{32}");

        static string SlotLine(MaterialSlot slot)
        {
            StringBuilder sb = new();
            sb.Append(slot.GetType().Name).Append(' ').Append(slot.id).Append(' ').Append(slot.slotType).Append(" \"").Append(slot.RawDisplayName()).Append('"');
            if (slot.hidden)
                sb.Append(" hidden");
            if (slot.hideConnector)
                sb.Append(" hideConnector");
            if (slot.HasCustomBinding)
                sb.Append(" binding=\"").Append(slot.CustomBinding).Append('"');
            if (slot.isInputSlot)
                sb.Append(" default=").Append(slot.GetDefaultValue(GenerationMode.ForReals));
            return k_ObjectIds.Replace(sb.ToString(), "<id>");
        }

        static string Snapshot(string caseName)
        {
            var graph = new GraphData();
            var node = new ProviderNode();
            graph.AddNode(node);
            node.InitializeFromProvider(new SourceProvider(caseName, Reflect(caseName)));

            StringBuilder sb = new();
            sb.Append("name ").Append(node.name).Append('\n');
            sb.Append("synonyms ").Append(string.Join(", ", node.synonyms ?? Array.Empty<string>())).Append('\n');
            sb.Append("precision ").Append(node.canSetPrecision).Append('\n');
            sb.Append("help ").Append(k_PackageVersion.Replace(node.documentationURL ?? "", "@<version>")).Append('\n');

            List<MaterialSlot> slots = new();
            node.GetSlots(slots);
            foreach (var slot in slots)
                sb.Append("slot ").Append(SlotLine(slot)).Append('\n');

            List<string> messages = new();
            foreach (var (message, severity) in node.Header.Messages ?? Enumerable.Empty<(string, MessageType)>())
                messages.Add($"function {severity}: {message}");
            foreach (var param in node.ParamHeaders?.Values ?? Enumerable.Empty<ParameterHeader>())
                foreach (var (message, severity) in param.Messages ?? Enumerable.Empty<(string, MessageType)>())
                    messages.Add($"{param.referenceName} {severity}: {message}");
            messages.Sort(StringComparer.Ordinal);
            foreach (var message in messages)
                sb.Append("message ").Append(message).Append('\n');

            var functions = new ShaderStringBuilder();
            functions.currentNode = node;
            node.GenerateNodeFunction(new FunctionRegistry(functions, new IncludeCollection(), true), GenerationMode.ForReals);
            var call = new ShaderStringBuilder();
            call.currentNode = node;
            node.GenerateNodeCode(call, GenerationMode.ForReals);
            sb.Append("functions\n").Append(k_ObjectIds.Replace(functions.ToString(), "<id>").Trim()).Append('\n');
            sb.Append("call\n").Append(k_ObjectIds.Replace(call.ToString(), "<id>").Trim()).Append('\n');
            return sb.ToString().TrimEnd('\n');
        }
    }
}
