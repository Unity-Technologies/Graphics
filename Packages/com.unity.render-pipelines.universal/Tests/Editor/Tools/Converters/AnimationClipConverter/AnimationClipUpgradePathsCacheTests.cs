using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace UnityEditor.Rendering.Universal.Tools
{
    [TestFixture]
    [Category("Graphics Tools")]
    class AnimationClipUpgradePathsCacheTests
    {
        public class DefaultBuiltInToURP : MaterialUpgrader
        {
            public DefaultBuiltInToURP(string oldShaderName, string newShaderName)
            {
                if (oldShaderName == null)
                    throw new ArgumentNullException(nameof(oldShaderName));

                RenameShader(oldShaderName, newShaderName, null);
                RenameFloat("builtIn01", "urp01");
                RenameFloat("builtIn03", "urp02");
                RenameColor("builtIn01", "urp01");
                RenameColor("builtIn03", "urp02");
                RenameTexture("builtIn01", "urp01");
                RenameTexture("builtIn03", "urp02");
            }
        }

        public class ConflictingUpgrader : MaterialUpgrader
        {
            public ConflictingUpgrader(string oldShaderName, string newShaderName)
            {
                RenameShader(oldShaderName, newShaderName, null);
                // Creates conflict - same source maps to different targets
                RenameFloat("conflictProp", "target01");
            }
        }

        public class ConflictingUpgrader01 : MaterialUpgrader
        {
            public ConflictingUpgrader01(string oldShaderName, string newShaderName)
            {
                RenameShader(oldShaderName, newShaderName, null);
                // Creates conflict - same source maps to different targets
                RenameFloat("conflictProp", "target02");
            }
        }

        public class AlternativeBuiltInToURP : MaterialUpgrader
        {
            public AlternativeBuiltInToURP(string oldShaderName, string newShaderName)
            {
                RenameShader(oldShaderName, newShaderName, null);
                RenameFloat("builtIn01", "urpAlternative01");
                RenameColor("colorProp", "urpColor");
                RenameTexture("textureProp", "urpTexture");
            }
        }

        private static IEnumerable s_ValidUpgradeTestCases()
        {
            // Test valid float property upgrade
            yield return new TestCaseData(
                new MaterialUpgrader[]
                {
                    new DefaultBuiltInToURP("DefaultBuiltIn", "DefaultURP"),
                },
                "DefaultURP",
                MaterialUpgrader.MaterialPropertyType.Float,
                "builtIn01"
            ).Returns((ShaderPropertyUsage.ValidForUpgrade, "urp01"))
            .SetName("ValidUpgrade_Float_BuiltIn01ToUrp01");

            // Test valid float property upgrade (second mapping)
            yield return new TestCaseData(
                new MaterialUpgrader[]
                {
                    new DefaultBuiltInToURP("DefaultBuiltIn", "DefaultURP"),
                },
                "DefaultURP",
                MaterialUpgrader.MaterialPropertyType.Float,
                "builtIn03"
            ).Returns((ShaderPropertyUsage.ValidForUpgrade, "urp02"))
            .SetName("ValidUpgrade_Float_BuiltIn03ToUrp02");

            // Test valid color property upgrade
            yield return new TestCaseData(
                new MaterialUpgrader[]
                {
                    new DefaultBuiltInToURP("DefaultBuiltIn", "DefaultURP"),
                },
                "DefaultURP",
                MaterialUpgrader.MaterialPropertyType.Color,
                "builtIn01"
            ).Returns((ShaderPropertyUsage.ValidForUpgrade, "urp01"))
            .SetName("ValidUpgrade_Color_BuiltIn01ToUrp01");

            // Test valid texture property upgrade
            yield return new TestCaseData(
                new MaterialUpgrader[]
                {
                    new DefaultBuiltInToURP("DefaultBuiltIn", "DefaultURP"),
                },
                "DefaultURP",
                MaterialUpgrader.MaterialPropertyType.Texture,
                "builtIn01"
            ).Returns((ShaderPropertyUsage.ValidForUpgrade, "urp01"))
            .SetName("ValidUpgrade_Texture_BuiltIn01ToUrp01");
        }

        private static IEnumerable s_NoMappingTestCases()
        {
            // Test property with no mapping
            yield return new TestCaseData(
                new MaterialUpgrader[]
                {
                    new DefaultBuiltInToURP("DefaultBuiltIn", "DefaultURP"),
                },
                "DefaultURP",
                MaterialUpgrader.MaterialPropertyType.Float,
                "nonExistentProperty"
            ).Returns((ShaderPropertyUsage.NoMapping, "nonExistentProperty"))
            .SetName("NoMapping_Float_NonExistentProperty");

            // Test color property with no mapping
            yield return new TestCaseData(
                new MaterialUpgrader[]
                {
                    new DefaultBuiltInToURP("DefaultBuiltIn", "DefaultURP"),
                },
                "DefaultURP",
                MaterialUpgrader.MaterialPropertyType.Color,
                "unmappedColorProp"
            ).Returns((ShaderPropertyUsage.NoMapping, "unmappedColorProp"))
            .SetName("NoMapping_Color_UnmappedProperty");
        }

        private static IEnumerable s_AlreadyUpgradedTestCases()
        {
            // Test property that's already upgraded (target property name)
            yield return new TestCaseData(
                new MaterialUpgrader[]
                {
                    new DefaultBuiltInToURP("DefaultBuiltIn", "DefaultURP"),
                },
                "DefaultURP",
                MaterialUpgrader.MaterialPropertyType.Float,
                "urp01"
            ).Returns((ShaderPropertyUsage.AlreadyUpgraded, "urp01"))
            .SetName("AlreadyUpgraded_Float_Urp01");

            // Test texture property that's already upgraded
            yield return new TestCaseData(
                new MaterialUpgrader[]
                {
                    new DefaultBuiltInToURP("DefaultBuiltIn", "DefaultURP"),
                },
                "DefaultURP",
                MaterialUpgrader.MaterialPropertyType.Texture,
                "urp02"
            ).Returns((ShaderPropertyUsage.AlreadyUpgraded, "urp02"))
            .SetName("AlreadyUpgraded_Texture_Urp02");
        }

        private static IEnumerable s_InvalidShaderTestCases()
        {
            // Test with non-existent shader
            yield return new TestCaseData(
                new MaterialUpgrader[]
                {
                    new DefaultBuiltInToURP("DefaultBuiltIn", "DefaultURP"),
                },
                "NonExistentShader",
                MaterialUpgrader.MaterialPropertyType.Float,
                "builtIn01"
            ).Returns((ShaderPropertyUsage.InvalidShader, "builtIn01"))
            .SetName("InvalidShader_NonExistentShader");

            // Test with null shader name
            yield return new TestCaseData(
                new MaterialUpgrader[]
                {
                    new DefaultBuiltInToURP("DefaultBuiltIn", "DefaultURP"),
                },
                null,
                MaterialUpgrader.MaterialPropertyType.Float,
                "builtIn01"
            ).Returns((ShaderPropertyUsage.InvalidShader, "builtIn01"))
            .SetName("InvalidShader_NullShaderName");

            // Test with empty shader name
            yield return new TestCaseData(
                new MaterialUpgrader[]
                {
                    new DefaultBuiltInToURP("DefaultBuiltIn", "DefaultURP"),
                },
                "",
                MaterialUpgrader.MaterialPropertyType.Float,
                "builtIn01"
            ).Returns((ShaderPropertyUsage.InvalidShader, "builtIn01"))
            .SetName("InvalidShader_EmptyShaderName");
        }

        private static IEnumerable s_MultipleUpgradePathsTestCases()
        {
            // Test conflicting upgrade paths
            yield return new TestCaseData(
                new MaterialUpgrader[]
                {
                    new ConflictingUpgrader("OldShader", "NewShader"),
                    new ConflictingUpgrader01("OldShader", "NewShader"),
                },
                "NewShader",
                MaterialUpgrader.MaterialPropertyType.Float,
                "conflictProp"
            ).Returns((ShaderPropertyUsage.MultipleUpgradePaths, "target02"))
            .SetName("MultipleUpgradePaths_ConflictingProperty");

            // Test multiple upgraders with conflicting mappings
            yield return new TestCaseData(
                new MaterialUpgrader[]
                {
                    new DefaultBuiltInToURP("DefaultBuiltIn", "DefaultURP"),
                    new AlternativeBuiltInToURP("AlternativeBuiltIn", "DefaultURP"),
                },
                "DefaultURP",
                MaterialUpgrader.MaterialPropertyType.Float,
                "builtIn01"
            ).Returns((ShaderPropertyUsage.MultipleUpgradePaths, "urpAlternative01"))
            .SetName("MultipleUpgradePaths_MultipleUpgradersConflict");
        }

        private static IEnumerable s_MultipleUpgradersTestCases()
        {
            // Test non-conflicting properties from different upgraders
            yield return new TestCaseData(
                new MaterialUpgrader[]
                {
                    new DefaultBuiltInToURP("DefaultBuiltIn", "DefaultURP"),
                    new AlternativeBuiltInToURP("AlternativeBuiltIn", "DefaultURP"),
                },
                "DefaultURP",
                MaterialUpgrader.MaterialPropertyType.Color,
                "colorProp"
            ).Returns((ShaderPropertyUsage.ValidForUpgrade, "urpColor"))
            .SetName("MultipleUpgraders_NonConflictingColorProperty");

            // Test property from first upgrader when multiple upgraders exist
            yield return new TestCaseData(
                new MaterialUpgrader[]
                {
                    new DefaultBuiltInToURP("DefaultBuiltIn", "DefaultURP"),
                    new AlternativeBuiltInToURP("AlternativeBuiltIn", "DefaultURP"),
                },
                "DefaultURP",
                MaterialUpgrader.MaterialPropertyType.Float,
                "builtIn03"
            ).Returns((ShaderPropertyUsage.ValidForUpgrade, "urp02"))
            .SetName("MultipleUpgraders_PropertyFromFirstUpgrader");
        }

        [Test, TestCaseSource(nameof(s_ValidUpgradeTestCases))]
        [TestCaseSource(nameof(s_NoMappingTestCases))]
        [TestCaseSource(nameof(s_AlreadyUpgradedTestCases))]
        [TestCaseSource(nameof(s_InvalidShaderTestCases))]
        [TestCaseSource(nameof(s_MultipleUpgradePathsTestCases))]
        [TestCaseSource(nameof(s_MultipleUpgradersTestCases))]
        public (ShaderPropertyUsage, string) DoTest(
            MaterialUpgrader[] upgraders,
            string shaderName,
            MaterialUpgrader.MaterialPropertyType propertyType,
            string property)
        {
            using var cache = new AnimationClipUpgradePathsCache(upgraders.ToList());

            ShaderPropertyUsage usage = cache.GetShaderPropertyUsage(
                shaderName,
                propertyType,
                property,
                out string newPropertyName);

            return (usage, newPropertyName);
        }

        [Test]
        public void EmptyUpgraderList_ReturnsInvalidShader()
        {
            using var cache = new AnimationClipUpgradePathsCache(new List<MaterialUpgrader>());

            var usage = cache.GetShaderPropertyUsage(
                "AnyShader",
                MaterialUpgrader.MaterialPropertyType.Float,
                "anyProperty",
                out string newPropertyName);

            Assert.AreEqual(ShaderPropertyUsage.InvalidShader, usage);
            Assert.AreEqual("anyProperty", newPropertyName);
        }

        [Test]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            var cache = new AnimationClipUpgradePathsCache(
                new List<MaterialUpgrader>
                {
                    new DefaultBuiltInToURP("DefaultBuiltIn", "DefaultURP")
                });

            cache.Dispose();
            Assert.DoesNotThrow(() => cache.Dispose());
        }

        [Test]
        public void DifferentPropertyTypes_HaveIndependentMappings()
        {
            using var cache = new AnimationClipUpgradePathsCache(
                new List<MaterialUpgrader>
                {
                    new DefaultBuiltInToURP("DefaultBuiltIn", "DefaultURP")
                });

            // Same property name, different types should have independent mappings
            var floatUsage = cache.GetShaderPropertyUsage(
                "DefaultURP",
                MaterialUpgrader.MaterialPropertyType.Float,
                "builtIn01",
                out string floatNewName);

            var colorUsage = cache.GetShaderPropertyUsage(
                "DefaultURP",
                MaterialUpgrader.MaterialPropertyType.Color,
                "builtIn01",
                out string colorNewName);

            var textureUsage = cache.GetShaderPropertyUsage(
                "DefaultURP",
                MaterialUpgrader.MaterialPropertyType.Texture,
                "builtIn01",
                out string textureNewName);

            Assert.AreEqual(ShaderPropertyUsage.ValidForUpgrade, floatUsage);
            Assert.AreEqual(ShaderPropertyUsage.ValidForUpgrade, colorUsage);
            Assert.AreEqual(ShaderPropertyUsage.ValidForUpgrade, textureUsage);
            Assert.AreEqual("urp01", floatNewName);
            Assert.AreEqual("urp01", colorNewName);
            Assert.AreEqual("urp01", textureNewName);
        }

        [Test]
        public void MultipleShaders_AreHandledIndependently()
        {
            using var cache = new AnimationClipUpgradePathsCache(
                new List<MaterialUpgrader>
                {
                    new DefaultBuiltInToURP("DefaultBuiltIn", "DefaultURP"),
                    new AlternativeBuiltInToURP("AlternativeBuiltIn", "AlternativeURP")
                });

            var usage1 = cache.GetShaderPropertyUsage(
                "DefaultURP",
                MaterialUpgrader.MaterialPropertyType.Color,
                "builtIn01",
                out string newName1);

            var usage2 = cache.GetShaderPropertyUsage(
                "AlternativeURP",
                MaterialUpgrader.MaterialPropertyType.Color,
                "colorProp",
                out string newName2);

            Assert.AreEqual(ShaderPropertyUsage.ValidForUpgrade, usage1);
            Assert.AreEqual(ShaderPropertyUsage.ValidForUpgrade, usage2);
            Assert.AreEqual("urp01", newName1);
            Assert.AreEqual("urpColor", newName2);
        }
    }
}
