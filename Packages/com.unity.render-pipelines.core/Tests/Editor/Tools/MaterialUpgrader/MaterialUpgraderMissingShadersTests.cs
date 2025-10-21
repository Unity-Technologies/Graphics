using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using static UnityEngine.Rendering.ReloadAttribute;

namespace UnityEditor.Rendering.Tools.Tests
{
    [TestFixture]
    [Category("Graphics Tools")]
    class MaterialUpgraderMissingShadersTests
    {
        [Test]
        public void TestMissingShaders()
        {
            StringBuilder sb = new StringBuilder();

            var pipelineTypes = TypeCache.GetTypesDerivedFrom<RenderPipelineAsset>();

            var testAssembly = typeof(MaterialUpgraderMissingShadersTests).Assembly;
            foreach (var type in pipelineTypes)
            {
                var allURPUpgraders = MaterialUpgrader.FetchAllUpgradersForPipeline(type);
                foreach (var upgrader in allURPUpgraders)
                {
                    var assembly = upgrader.GetType().Assembly;
                    if (testAssembly == assembly)
                        continue;

                    var src = Shader.Find(upgrader.OldShaderPath);
                    if (src == null)
                    {
                        sb.AppendLine($"Src Shader '{upgrader.OldShaderPath}' not found for upgrader {upgrader.GetType().Name}. This may indicate that the shader has been removed or renamed.");
                    }

                    var dst = Shader.Find(upgrader.NewShaderPath);
                    if (dst == null)
                    {
                        sb.AppendLine($"Dst Shader '{upgrader.NewShaderPath}' not found for upgrader {upgrader.GetType().Name}. This may indicate that the shader has been removed or renamed.");
                    }
                }
            }
            Assert.AreEqual(0, sb.Length, sb.ToString());
        }
    }

}
