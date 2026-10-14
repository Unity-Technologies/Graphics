using System.Collections.Generic;
using NUnit.Framework;
using Unity.GraphAuthoring.Editor.ProviderSystem;

namespace UnityEditor.ShaderGraph.ProviderSystem.Tests
{
    [TestFixture]
    class ReflectedFunctionTests
    {
        readonly string kTestPath = "Assets/generated-ReflectionTest.hlsl";
        Dictionary<string, ShaderFunction> lookup = new();


        // Each function with the provider key it's registered under; null uses the signature.
        (ShaderFunction function, string providerKey)[] functionsToTest = new[]
        {
            (new ShaderFunction("BaseCase", new[] { "Namespace1" , "Namespace2" }, null, new ShaderType("float"), "return 1;"), (string)null),

            (new ShaderFunction("InAndOut", null,
                new IShaderField[] {
                    new ShaderField("Field", true, true, new ShaderType("float3")),
                },
                new ShaderType("void"),
                "Field.xyz = float3(1,1,1);"), null),

            (new ShaderFunction("ProviderName", null, null, new ShaderType("float"), "return 1;"), "UniqueProviderName"),
        };

        [OneTimeSetUp]
        public void Setup()
        {
            ShaderStringBuilder sb = new();
            foreach (var (func, providerKey) in functionsToTest)
            {
                lookup.Add(providerKey ?? ShaderObjectUtils.QualifySignature(func), func);
                var code = ShaderObjectUtils.GenerateCode(func);
                if (providerKey != null)
                    code = code.Replace("UNITY_EXPORT_REFLECTION", $"/// <funchints>\n///\t <sg:ProviderKey>{providerKey}</sg:ProviderKey>\n/// </funchints>\nUNITY_EXPORT_REFLECTION");
                sb.AppendLine(code);
            }
            sb.AppendLine(k_ExternalParameterSource);
            FileUtilities.WriteToDisk(kTestPath, sb.ToString());

            AssetDatabase.ImportAsset(kTestPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        }

        [OneTimeTearDown]
        public void Teardown()
        {
            AssetDatabase.DeleteAsset(kTestPath);
        }

        const string k_ExternalParameterSource =
            "struct ExternalStruct { float x; };\n" +
            "/// <paramhints name=\"value\">\n///\t <sg:External>ExternalNamespace</sg:External>\n/// </paramhints>\n" +
            "UNITY_EXPORT_REFLECTION float ExternalParameter(ExternalStruct value) { return value.x; }";

        // The key a keyless function is stored under must match the one it's looked up by on reload.
        [Test]
        public void ExternalParameter_KeepsItsProviderAcrossReload()
        {
            Assert.IsTrue(ProviderLibrary.TryGetInstance(out var lib), "Provider library must be available in the main editor.");
            var entries = new List<IProvider<IShaderFunction>>(lib.ProvidersByVersion<IShaderFunction>("float ExternalParameter(in ExternalStruct)"));
            Assert.AreEqual(1, entries.Count);

            var provider = entries[0];
            provider.Reload();
            Assert.IsTrue(provider.IsValid);
            Assert.AreEqual("ExternalNamespace::ExternalStruct", ShaderObjectUtils.QualifyName(new List<IShaderField>(provider.Definition.Parameters)[0].ShaderType));
        }

        [Test]
        public void ReflectionTest()
        {
            Assert.IsTrue(ProviderLibrary.TryGetInstance(out var lib), "Provider library must be available in the main editor.");
            foreach (var (key, expected) in lookup)
            {
                var entries = new List<IProvider<IShaderFunction>>(lib.ProvidersByVersion<IShaderFunction>(key));
                Assert.AreEqual(1, entries.Count,
                    $"Expected exactly one registration for key '{key}'.");

                Assert.IsTrue(TestUtils.CompareFunction(expected, entries[0].Definition, ignoreBodyWhitespace: true),
                    $"Function found with key {key} does not match test case.");
                TestUtils.AssertNodeSetup(entries[0]);
            }
        }
    }
}
