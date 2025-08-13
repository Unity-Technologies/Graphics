using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.Tools.Tests
{
    class MaterialUpgraderRegistryTests
    {
        class TestPipeline : RenderPipelineAsset
        {
            protected override RenderPipeline CreatePipeline()
            {
                throw new System.NotImplementedException();
            }
        }

        public class MaterialUpgraderTest1000 : MaterialUpgrader
        {
            public override int priority => 1000;

            public MaterialUpgraderTest1000(string oldShaderName, string newShaderName)
            {
                if (oldShaderName == null)
                    throw new ArgumentNullException("oldShaderName");

                RenameShader(oldShaderName, newShaderName, null);
            }
        }

        public class MaterialUpgraderTest2000 : MaterialUpgrader
        {
            public override int priority => 2000;

            public MaterialUpgraderTest2000(string oldShaderName, string newShaderName)
            {
                if (oldShaderName == null)
                    throw new ArgumentNullException("oldShaderName");

                RenameShader(oldShaderName, newShaderName, null);
            }
        }

        public class MaterialUpgraderTest0000 : MaterialUpgrader
        {
            public MaterialUpgraderTest0000(string oldShaderName, string newShaderName)
            {
                if (oldShaderName == null)
                    throw new ArgumentNullException("oldShaderName");

                RenameShader(oldShaderName, newShaderName, null);
            }
        }

        public class MaterialUpgraderTest5000 : MaterialUpgrader
        {
            public override int priority => 5000;

            public MaterialUpgraderTest5000(string oldShaderName, string newShaderName)
            {
                if (oldShaderName == null)
                    throw new ArgumentNullException("oldShaderName");

                RenameShader(oldShaderName, newShaderName, null);
            }
        }

        [SupportedOnRenderPipeline(typeof(TestPipeline))]
        private class MaterialUpgraderProvider1000 : IMaterialUpgradersProvider
        {
            public int priority => 1000;

            public IEnumerable<MaterialUpgrader> GetUpgraders()
            {
                yield return new MaterialUpgraderTest1000("SomePath", "1000");
            }
        }

        [SupportedOnRenderPipeline(typeof(TestPipeline))]
        private class MaterialUpgraderProvider5000 : IMaterialUpgradersProvider
        {
            public int priority => 5000;

            public IEnumerable<MaterialUpgrader> GetUpgraders()
            {
                yield return new MaterialUpgraderTest5000("SomePath1", "5000");
                yield return new MaterialUpgraderTest5000("A", "5000");
            }
        }

        [SupportedOnRenderPipeline(typeof(TestPipeline))]
        private class MaterialUpgraderProvider0000 : IMaterialUpgradersProvider
        {
            public IEnumerable<MaterialUpgrader> GetUpgraders()
            {
                yield return new MaterialUpgraderTest2000("SomePath", "2000");
                yield return new MaterialUpgraderTest1000("SomePath1", "1000");
                yield return new MaterialUpgraderTest0000("Z", "0000");
            }
        }

        [SupportedOnRenderPipeline(typeof(TestPipeline))]
        private class MaterialUpgraderProvider2000 : IMaterialUpgradersProvider
        {
            public IEnumerable<MaterialUpgrader> GetUpgraders()
            {
                yield return new MaterialUpgraderTest5000("SomePath", "5000");
                yield return new MaterialUpgraderTest2000("SomePath1", "2000");
            }
        }

        [Test]
        public void MaterialUpgraders_AreSortedCorrectly()
        {
            var expected = new List<(Type type, string oldShader, string newShader, int priority)>
            {
                (typeof(MaterialUpgraderTest5000), "A", "5000", 5000),
                (typeof(MaterialUpgraderTest5000), "SomePath", "5000", 5000),
                (typeof(MaterialUpgraderTest2000), "SomePath", "2000", 2000),
                (typeof(MaterialUpgraderTest1000), "SomePath", "1000", 1000),                
                (typeof(MaterialUpgraderTest5000), "SomePath1", "5000", 5000),
                (typeof(MaterialUpgraderTest2000), "SomePath1", "2000", 2000),
                (typeof(MaterialUpgraderTest1000), "SomePath1", "1000", 1000),
                (typeof(MaterialUpgraderTest0000), "Z", "0000", 0),
            };

            var actual = MaterialUpgraderRegistry.instance.GetMaterialUpgradersForPipeline(typeof(TestPipeline));

            Assert.AreEqual(expected.Count, actual.Count, "Mismatch in number of upgraders returned");

            for (int i = 0; i < expected.Count; i++)
            {
                var expectedItem = expected[i];
                var actualItem = actual[i];

                Assert.AreEqual(expectedItem.type, actualItem.GetType(), $"Type mismatch at index {i}");
                Assert.AreEqual(expectedItem.oldShader, actualItem.OldShaderPath, $"Old shader mismatch at index {i}");
                Assert.AreEqual(expectedItem.newShader, actualItem.NewShaderPath, $"New shader mismatch at index {i}");
                Assert.AreEqual(expectedItem.priority, actualItem.priority, $"Priority mismatch at index {i}");
            }
        }
    }

}
