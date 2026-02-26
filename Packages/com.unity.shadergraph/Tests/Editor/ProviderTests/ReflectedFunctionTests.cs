using System.Collections.Generic;
using NUnit.Framework;

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

            new ShaderFunction("ProviderName", null, null, new ShaderType("float"), "return 1;",
                new Dictionary<string ,string>() {
                    { Hints.Func.kProviderKey,"UniqueProviderName" },
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
            int count = 0;
            HashSet<string> keysNotFound = new(lookup.Keys);
            foreach(var provider in ProviderLibrary.Instance.AllProvidersByType<IShaderFunction>())
            {
                if (lookup.TryGetValue(provider.ProviderKey, out var actual))
                {
                    count++;
                    keysNotFound.Remove(provider.ProviderKey);
                    var expected = lookup[provider.ProviderKey];
                    Assert.IsTrue(TestUtils.CompareFunction(actual, expected), $"Function found with key {provider.ProviderKey} does not match test case.");
                    TestUtils.AssertNodeSetup(provider);
                }
            }
            if (keysNotFound.Count > 0)
            {
                System.Text.StringBuilder sb = new();
                foreach (var key in keysNotFound)
                    sb.Append($"{key}, ");

                Assert.Fail($"Expected keys could not be found: {sb.ToString()}");
            }
        }
    }
}
