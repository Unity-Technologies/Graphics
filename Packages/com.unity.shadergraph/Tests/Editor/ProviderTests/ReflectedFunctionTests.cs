using System.Collections.Generic;
using NUnit.Framework;
using Unity.GraphAuthoring.Editor.ProviderSystem;
using Unity.GraphAuthoring.Editor.ProviderSystem.Hints;

namespace UnityEditor.ShaderGraph.ProviderSystem.Tests
{
    [TestFixture]
    class ReflectedFunctionTests
    {
        readonly string kTestPath = "Assets/generated-ReflectionTest.hlsl";
        Dictionary<string, ShaderFunction> lookup = new();


        ShaderFunction[] functionsToTest = new[]
        {
            new ShaderFunction("BaseCase", new[] { "Namespace1" , "Namespace2" }, null, new ShaderType("float"), "return 1;", null),

            new ShaderFunction("InAndOut", null,
                new IShaderField[] {
                    new ShaderField("Field", true, true, new ShaderType("float3"), null),
                },
                new ShaderType("void"),
                "Field.xyz = float3(1,1,1);", null),

            // Function with an explicit provider key hint.
            new ShaderFunction("ProviderName", null, null, new ShaderType("float"), "return 1;",
                new IStrongHint[] {
                    new ProviderKey("UniqueProviderName"),
                }),
        };

        [OneTimeSetUp]
        public void Setup()
        {
            ShaderStringBuilder sb = new();
            foreach (var func in functionsToTest)
            {
                lookup.Add(ShaderObjectUtils.EvaluateProviderKey(func), func);
                sb.AppendLine(ShaderObjectUtils.GenerateCode(func));
            }
            FileUtilities.WriteToDisk(kTestPath, sb.ToString());

            AssetDatabase.ImportAsset(kTestPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        }

        [OneTimeTearDown]
        public void Teardown()
        {
            AssetDatabase.DeleteAsset(kTestPath);
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

                Assert.IsTrue(TestUtils.CompareFunction(expected, expected),
                    $"Function found with key {key} does not match test case.");
                TestUtils.AssertNodeSetup(entries[0]);
            }
        }
    }
}
