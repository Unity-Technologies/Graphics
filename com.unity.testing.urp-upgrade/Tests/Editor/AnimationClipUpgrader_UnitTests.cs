using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityObject = UnityEngine.Object;
using AnimationClipProxy = UnityEditor.Rendering.AnimationClipUpgrader.AnimationClipProxy;
using IAnimationClip = UnityEditor.Rendering.AnimationClipUpgrader.IAnimationClip;
using IMaterial = UnityEditor.Rendering.UpgradeUtility.IMaterial;
using IRenderer = UnityEditor.Rendering.AnimationClipUpgrader.IRenderer;
using ClipPath = UnityEditor.Rendering.AnimationClipUpgrader.ClipPath;
using PrefabPath = UnityEditor.Rendering.AnimationClipUpgrader.PrefabPath;
using ScenePath = UnityEditor.Rendering.AnimationClipUpgrader.ScenePath;
using MaterialPropertyType = UnityEditor.Rendering.MaterialUpgrader.MaterialPropertyType;
using UID = UnityEditor.Rendering.UpgradeUtility.UID;
using static UnityEditor.Rendering.Tests.AnimationClipUpgraderTestUtility;
using static UnityEditor.Rendering.Tests.UpgraderTestUtility;

namespace UnityEditor.Rendering.Tests
{
    /// <summary>
    /// Utility to generate arguments for <see cref="AnimationClipUpgrader"/> using mock objects for parameterized tests.
    /// </summary>
    static class AnimationClipUpgraderTestUtility
    {
        internal static List<IMaterial> CreateMockMaterials(IEnumerable<(string ID, string ShaderName)> materials)
        {
            var result = new List<IMaterial>();
            foreach (var (id, shaderName) in materials)
            {
                var m = new Mock<IMaterial>();
                m.SetupGet(m => m.ID).Returns(id);
                m.SetupGet(m => m.ShaderName).Returns(shaderName);
                result.Add(m.Object);
            }
            return result;
        }
    }

    public class AnimationClipUpgrader_UnitTests
    {
        [TestCase("material._Color.r", "_Color", ShaderPropertyType.Color)]
        [TestCase("material._Color.g", "_Color", ShaderPropertyType.Color)]
        [TestCase("material._Color.b", "_Color", ShaderPropertyType.Color)]
        [TestCase("material._Color.a", "_Color", ShaderPropertyType.Color)]
        [TestCase("material._MainTex_ST.x", "_MainTex_ST", ShaderPropertyType.Float)]
        [TestCase("material._MainTex_ST.y", "_MainTex_ST", ShaderPropertyType.Float)]
        [TestCase("material._MainTex_ST.z", "_MainTex_ST", ShaderPropertyType.Float)]
        [TestCase("material._MainTex_ST.w", "_MainTex_ST", ShaderPropertyType.Float)]
        [TestCase("material._Cutoff", "_Cutoff", ShaderPropertyType.Float)]
        public void InferShaderProperty_ReturnsExpectedValue(
            string propertyName, string expectedPropertyName, ShaderPropertyType expectedType
        )
        {
            var binding = new EditorCurveBinding { propertyName = propertyName };

            var actual = AnimationClipUpgrader.InferShaderProperty(binding);

            Assert.That(actual, Is.EqualTo((expectedPropertyName, expectedType)));
        }

        static readonly TestCaseData[] k_ContainsAnimatedMaterialsTestData =
        {
            new TestCaseData(
                new EditorCurveBinding { type = typeof(MeshRenderer), propertyName = "material._MainTex_ST.x" }
            ).Returns(true)
                .SetName("MeshRenderer with animated material returns true"),
            new TestCaseData(
                new EditorCurveBinding { type = typeof(MeshRenderer), propertyName = "m_Enabled" }
            ).Returns(false)
                .SetName("MeshRenderer non-material property returns false"),
            new TestCaseData(
                new EditorCurveBinding { type = typeof(MeshFilter), propertyName = "m_Mesh" }
            ).Returns(false)
                .SetName("Non-MeshRenderer returns false")
        };

        [TestCaseSource(nameof(k_ContainsAnimatedMaterialsTestData))]
        public bool IsMaterialBinding_ReturnsExpectedValue(EditorCurveBinding binding) =>
            AnimationClipUpgrader.IsMaterialBinding(binding);

        [Test]
        public void IsMaterialBinding_WhenBindingHasInvalidValues_DoesNotThrow(
            [Values("", null)] string propertyName,
            [Values(typeof(MeshRenderer), null)] Type type
        )
        {
            var binding = new EditorCurveBinding { propertyName = propertyName, type = type };

            Assert.DoesNotThrow(() => AnimationClipUpgrader.IsMaterialBinding(binding));
        }

        [Test]
        public void GatherClipUsage_WhenNoDataForClip_DoesNotThrow()
        {
            var clip = new Mock<IAnimationClip>().Object;
            var clipData =
                new Dictionary<IAnimationClip, (ClipPath Path, EditorCurveBinding[] Bindings, SerializedShaderPropertyUsage Usage, IDictionary<EditorCurveBinding, string> PropertyRenames)>();

            Assert.DoesNotThrow(() =>
            {
                AnimationClipUpgrader.GatherClipUsage(
                    clip,
                    clipData,
                    renderersByPath: default,
                    allUpgradePathsToNewShaders: default,
                    upgradePathsUsedByMaterials: default
                );
            });
        }

        // test values
        const string k_UpgradableMaterialProp = "material._Color.r";
        const string k_ClipPath = "Assets/Clip.anim";
        const string k_RendererPath = "Path/To/Renderer";

        static readonly SerializedShaderPropertyUsage[] k_AllUsages =
            Enum.GetValues(typeof(SerializedShaderPropertyUsage)).Cast<SerializedShaderPropertyUsage>().ToArray();

        [Test]
        public void GatherClipUsage_WhenNoMaterialPropertyBindings_DoesNotModifyUsage(
            // TODO: use Values[] when SerializedShaderPropertyUsage is public
            [ValueSource(nameof(k_AllUsages))] object expectedUsage
        )
        {
            var clip = new Mock<IAnimationClip>().Object;
            var clipData = new Dictionary<IAnimationClip, (ClipPath Path, EditorCurveBinding[] Bindings, SerializedShaderPropertyUsage Usage, IDictionary<EditorCurveBinding, string> PropertyRenames)>
            {
                [clip] = (k_ClipPath, Array.Empty<EditorCurveBinding>(), (SerializedShaderPropertyUsage)expectedUsage, new Dictionary<EditorCurveBinding, string>())
            };

            AnimationClipUpgrader.GatherClipUsage(
                clip,
                clipData,
                renderersByPath: default,
                allUpgradePathsToNewShaders: default,
                upgradePathsUsedByMaterials: default
            );

            Assert.That(clipData[clip].Usage, Is.EqualTo(expectedUsage));
        }

        [Test]
        public void GatherClipUsage_WhenMaterialPropertyBindings_ButNoMatchingRenderer_DoesNotModifyUsage(
            // TODO: use Values[] when SerializedShaderPropertyUsage is public
            [ValueSource(nameof(k_AllUsages))] object expectedUsage
        )
        {
            var clip = new Mock<IAnimationClip>().Object;
            var bindings = new[] { new EditorCurveBinding { path = k_RendererPath, type = typeof(MeshRenderer), propertyName = k_UpgradableMaterialProp } };
            var clipData = new Dictionary<IAnimationClip, (ClipPath Path, EditorCurveBinding[] Bindings, SerializedShaderPropertyUsage Usage, IDictionary<EditorCurveBinding, string> PropertyRenames)>
            {
                [clip] = (k_ClipPath, bindings, (SerializedShaderPropertyUsage)expectedUsage, new Dictionary<EditorCurveBinding, string>())
            };
            var renderersByPath = new Dictionary<string, (IRenderer Renderer, List<IMaterial> Materials)>
            {
                [$"Different/{k_RendererPath}"] = (new Mock<IRenderer>().Object, new List<IMaterial>())
            };

            AnimationClipUpgrader.GatherClipUsage(
                clip,
                clipData,
                renderersByPath,
                allUpgradePathsToNewShaders: default,
                upgradePathsUsedByMaterials: default
            );

            Assert.That(clipData[clip].Usage, Is.EqualTo(expectedUsage));
        }

        [Test]
        public void GatherClipsUsageInDependentPrefabs_WhenNotUsed_ReturnsUnknown()
        {
            var clipDependents = new Dictionary<ClipPath, IReadOnlyCollection<PrefabPath>>
            {
                { k_ClipPath, Array.Empty<PrefabPath>() }
            };
            var assetDependencies = new Dictionary<PrefabPath, IReadOnlyCollection<ClipPath>>();
            var clip = new Mock<IAnimationClip>().Object;
            var bindings = new[] { new EditorCurveBinding { path = k_RendererPath, type = typeof(MeshRenderer), propertyName = k_UpgradableMaterialProp } };
            var clipData = new Dictionary<IAnimationClip, (ClipPath Path, EditorCurveBinding[] Bindings, SerializedShaderPropertyUsage Usage, IDictionary<EditorCurveBinding, string> PropertyRenames)>
            {
                [clip] = (k_ClipPath, bindings, SerializedShaderPropertyUsage.Unknown, new Dictionary<EditorCurveBinding, string>())
            };

            AnimationClipUpgrader.GatherClipsUsageInDependentPrefabs(
                clipDependents,
                assetDependencies,
                clipData,
                allUpgradePathsToNewShaders: default,
                upgradePathsUsedByMaterials: default
            );

            Assert.That(clipData[clip].Usage, Is.EqualTo(SerializedShaderPropertyUsage.Unknown));
        }

        [Test]
        public void GatherClipsUsageInDependentScenes_WhenNotUsed_ReturnsUnknown()
        {
            var clipDependents = new Dictionary<ClipPath, IReadOnlyCollection<ScenePath>>
            {
                { k_ClipPath, Array.Empty<ScenePath>() }
            };
            var assetDependencies = new Dictionary<ScenePath, IReadOnlyCollection<ClipPath>>();
            var clip = new Mock<IAnimationClip>().Object;
            var bindings = new[] { new EditorCurveBinding { path = k_RendererPath, type = typeof(MeshRenderer), propertyName = k_UpgradableMaterialProp } };
            var clipData = new Dictionary<IAnimationClip, (ClipPath Path, EditorCurveBinding[] Bindings, SerializedShaderPropertyUsage Usage, IDictionary<EditorCurveBinding, string> PropertyRenames)>
            {
                [clip] = (k_ClipPath, bindings, SerializedShaderPropertyUsage.Unknown, new Dictionary<EditorCurveBinding, string>())
            };

            AnimationClipUpgrader.GatherClipsUsageInDependentScenes(
                clipDependents,
                assetDependencies,
                clipData,
                allUpgradePathsToNewShaders: default,
                upgradePathsUsedByMaterials: default
            );

            Assert.That(clipData[clip].Usage, Is.EqualTo(SerializedShaderPropertyUsage.Unknown));
        }

        [Test]
        public void GatherClipUsage_WhenUsedByObjectWithNoMaterials_ReturnsUnknown()
        {
            var clip = new Mock<IAnimationClip>().Object;
            var bindings = new[] { new EditorCurveBinding { path = k_RendererPath, type = typeof(MeshRenderer), propertyName = k_UpgradableMaterialProp } };
            var clipData = new Dictionary<IAnimationClip, (ClipPath Path, EditorCurveBinding[] Bindings, SerializedShaderPropertyUsage Usage, IDictionary<EditorCurveBinding, string> PropertyRenames)>
            {
                [clip] = (k_ClipPath, bindings, SerializedShaderPropertyUsage.Unknown, new Dictionary<EditorCurveBinding, string>())
            };
            var renderersByPath = new Dictionary<string, (IRenderer Renderer, List<IMaterial> Materials)>
            {
                [k_RendererPath] = (new Mock<IRenderer>().Object, new List<IMaterial>())
            };

            AnimationClipUpgrader.GatherClipUsage(
                clip,
                clipData,
                renderersByPath,
                allUpgradePathsToNewShaders: default,
                upgradePathsUsedByMaterials: default
            );

            Assert.That(clipData[clip].Usage, Is.EqualTo(SerializedShaderPropertyUsage.Unknown));
        }

        static readonly TestCaseData[] k_UnknownUpgradePathTestCases =
        {
            new TestCaseData(
                new[] { ("ID1", "NewShader") },
                "material._Color.r",
                new[] { (From: "_Color", To: "_BaseColor") },
                new[]
                {
                    ("OldShader", "NewShader", new[] { (From: "_Color", To: "_BaseColor", Type: (int)MaterialPropertyType.Color) })
                }
            )
                .Returns(SerializedShaderPropertyUsage.UsedByUpgraded)
                .SetName("Single target material, upgraded, color property"),
            new TestCaseData(
                new[] { ("ID1", "NewShader") },
                "material._MainTex_ST.x",
                new[] { (From: "_MainTex_ST", To: "_BaseMap_ST_ST") },
                new[]
                {
                    ("OldShader", "NewShader", new[] { (From: "_MainTex_ST", To: "_BaseMap_ST_ST", Type: (int)MaterialPropertyType.Float) })
                }
            )
                .Returns(SerializedShaderPropertyUsage.UsedByUpgraded)
                .SetName("Single target material, upgraded, float property"),
            new TestCaseData(
                new[] { ("ID1", "NewShader") },
                "material._BaseColor.r",
                new[] { (From: "_BaseColor", To: "_BaseColor") },
                new[]
                {
                    ("OldShader", "NewShader", new[] { (From: "_Color", To: "_BaseColor", Type: (int)MaterialPropertyType.Color) })
                }
            )
                .Returns(SerializedShaderPropertyUsage.UsedByUpgraded)
                .SetName("Single target material, upgraded, color property already upgraded"),
            new TestCaseData(
                new[] { ("ID1", "NewShader") },
                "material._BaseMap_ST_ST.x",
                new[] { (From: "_BaseMap_ST_ST", To: "_BaseMap_ST_ST") },
                new[]
                {
                    ("OldShader", "NewShader", new[] { (From: "_MainTex_ST", To: "_BaseMap_ST_ST", Type: (int)MaterialPropertyType.Float) })
                }
            )
                .Returns(SerializedShaderPropertyUsage.UsedByUpgraded)
                .SetName("Single target material, upgraded, float property already upgraded"),
            new TestCaseData(
                new[] { ("ID1", "NewShader") },
                "material._Color.r",
                new[] { (From: "_Color", To: "_BaseColor1") },
                new[]
                {
                    ("OldShader1", "NewShader", new[] { (From: "_Color", To: "_BaseColor1", Type: (int)MaterialPropertyType.Color) }),
                    ("OldShader2", "NewShader", new[] { (From: "_Color", To: "_BaseColor2", Type: (int)MaterialPropertyType.Color) })
                }
            )
                .Returns(SerializedShaderPropertyUsage.UsedByUpgraded | SerializedShaderPropertyUsage.UsedByAmbiguouslyUpgraded)
                .SetName("Single target material, upgraded with multiple paths, color property"),
            new TestCaseData(
                new[] { ("ID1", "NewShader") },
                "material._MainTex_ST.x",
                new[] { (From: "_MainTex_ST", To: "_BaseMap_ST_ST1") },
                new[]
                {
                    ("OldShader1", "NewShader", new[] { (From: "_MainTex_ST", To: "_BaseMap_ST_ST1", Type: (int)MaterialPropertyType.Float) }),
                    ("OldShader2", "NewShader", new[] { (From: "_MainTex_ST", To: "_BaseMap_ST_ST2", Type: (int)MaterialPropertyType.Float) })
                }
            )
                .Returns(SerializedShaderPropertyUsage.UsedByUpgraded | SerializedShaderPropertyUsage.UsedByAmbiguouslyUpgraded)
                .SetName("Single target material, upgraded with multiple paths, float property"),
            new TestCaseData(
                new[] { ("ID1", "OldShader") },
                "material._Color.r",
                new[] { (From: "_Color", To: "_Color") },
                new[]
                {
                    ("OldShader", "NewShader", new[] { (From: "_Color", To: "_BaseColor", Type: (int)MaterialPropertyType.Color) })
                }
            )
                .Returns(SerializedShaderPropertyUsage.UsedByNonUpgraded)
                .SetName("Single target material, not upgraded, color property"),
            new TestCaseData(
                new[] { ("ID1", "OldShader") },
                "material._MainTex_ST.x",
                new[] { (From: "_MainTex_ST", To: "_MainTex_ST") },
                new[]
                {
                    ("OldShader", "NewShader", new[] { (From: "_MainTex_ST", To: "_BaseMap_ST_ST", Type: (int)MaterialPropertyType.Float) })
                }
            )
                .Returns(SerializedShaderPropertyUsage.UsedByNonUpgraded)
                .SetName("Single target material, not upgraded, float property"),
            new TestCaseData(
                new[] { ("ID1", "NewShader1"), ("ID2", "NewShader2") },
                "material._Color.r",
                new[] { (From: "_Color", To: "_BaseColor") },
                new[]
                {
                    ("OldShader", "NewShader1", new[] { (From: "_Color", To: "_BaseColor", Type: (int)MaterialPropertyType.Color) }),
                    ("OldShader", "NewShader2", new[] { (From: "_Color", To: "_BaseColor", Type: (int)MaterialPropertyType.Color) })
                }
            )
                .Returns(SerializedShaderPropertyUsage.UsedByUpgraded)
                .SetName("Two target materials, upgraded, same inference"),
            new TestCaseData(
                new[] { ("ID1", "NewShader1"), ("ID2", "NewShader2") },
                "material._Color.r",
                new[] { (From: "_Color", To: "_BaseColor2") },
                new[]
                {
                    ("OldShader", "NewShader1", new[] { (From: "_Color", To: "_BaseColor1", Type: (int)MaterialPropertyType.Color) }),
                    ("OldShader", "NewShader2", new[] { (From: "_Color", To: "_BaseColor2", Type: (int)MaterialPropertyType.Color) })
                }
            )
                .Returns(SerializedShaderPropertyUsage.UsedByUpgraded | SerializedShaderPropertyUsage.UsedByAmbiguouslyUpgraded)
                .SetName("Two target materials, upgraded, different inferences"),
        };

        [TestCaseSource(nameof(k_UnknownUpgradePathTestCases))]
        public object GatherClipUsage_WhenUpgradePathIsUnknown_ReturnsExpectedResult(
            (string ID, string ShaderName)[] upgradedMaterials, string bindingPropertyName,
            (string From, string To)[] expectedRenames,
            (string OldShader, string NewShader, (string From, string To, int Type)[] Renames)[] materialUpgraders
        )
        {
            var clip = new Mock<IAnimationClip>().Object;
            var bindings = new[] { new EditorCurveBinding { path = k_RendererPath, type = typeof(MeshRenderer), propertyName = bindingPropertyName } };
            var clipData = new Dictionary<IAnimationClip, (ClipPath Path, EditorCurveBinding[] Bindings, SerializedShaderPropertyUsage Usage, IDictionary<EditorCurveBinding, string> PropertyRenames)>
            {
                [clip] = (k_ClipPath, bindings, SerializedShaderPropertyUsage.Unknown, new Dictionary<EditorCurveBinding, string>())
            };
            var renderersByPath = new Dictionary<string, (IRenderer Renderer, List<IMaterial> Materials)>
            {
                [k_RendererPath] = (new Mock<IRenderer>().Object, CreateMockMaterials(upgradedMaterials))
            };
            var allUpgradePathsToNewShaders = CreateUpgradePathsToNewShaders(materialUpgraders);

            AnimationClipUpgrader.GatherClipUsage(
                clip,
                clipData,
                renderersByPath,
                allUpgradePathsToNewShaders,
                upgradePathsUsedByMaterials: default
            );

            var actualRenames = clipData.SelectMany(kv1 => kv1.Value.PropertyRenames.Select(kv2 => (AnimationClipUpgrader.InferShaderProperty(kv2.Key).Name, kv2.Value))).ToArray();
            Assert.That(actualRenames, Is.EqualTo(expectedRenames));

            return clipData[clip].Usage;
        }

        static readonly TestCaseData[] k_KnownUpgradePathTestCases =
        {
            new TestCaseData(
                new[] { ("ID1", "NewShader") }, "material._Color.r",
                new[] { (From: "_Color", To: "_BaseColor") },
                new[]
                {
                    ("OldShader", "NewShader", new[] { (From: "_Color", To: "_BaseColor", Type: (int)MaterialPropertyType.Color) })
                }
            )
                .Returns(SerializedShaderPropertyUsage.UsedByUpgraded)
                .SetName("Single target material, color property"),
            new TestCaseData(
                new[] { ("ID1", "NewShader") }, "material._MainTex_ST.x",
                new[] { (From: "_MainTex_ST", To: "_BaseMap_ST") },
                new[]
                {
                    ("OldShader", "NewShader", new[] { (From: "_MainTex_ST", To: "_BaseMap_ST", Type: (int)MaterialPropertyType.Float) })
                }
            )
                .Returns(SerializedShaderPropertyUsage.UsedByUpgraded)
                .SetName("Single target material, float property")
        };

        [TestCaseSource(nameof(k_KnownUpgradePathTestCases))]
        public object GatherClipUsage_WhenUpgradePathIsKnown_UsesKnownUpgradePath(
            (string ID, string ShaderName)[] upgradedMaterials, string bindingPropertyName,
            (string From, string To)[] expectedRenames,
            (string OldShader, string NewShader, (string From, string To, int Type)[] Renames)[] materialUpgraders
        )
        {
            var clip = new Mock<IAnimationClip>().Object;
            var bindings = new[] { new EditorCurveBinding { path = k_RendererPath, type = typeof(MeshRenderer), propertyName = bindingPropertyName } };
            var clipData = new Dictionary<IAnimationClip, (ClipPath Path, EditorCurveBinding[] Bindings, SerializedShaderPropertyUsage Usage, IDictionary<EditorCurveBinding, string> PropertyRenames)>
            {
                [clip] = (k_ClipPath, bindings, SerializedShaderPropertyUsage.Unknown, new Dictionary<EditorCurveBinding, string>())
            };
            var renderersByPath = new Dictionary<string, (IRenderer Renderer, List<IMaterial> Materials)>
            {
                [k_RendererPath] = (new Mock<IRenderer>().Object, CreateMockMaterials(upgradedMaterials))
            };
            var upgradePathsUsedByMaterials = new Dictionary<UID, MaterialUpgrader>
            {
                [renderersByPath[k_RendererPath].Materials[0].ID] = CreateMaterialUpgraders(materialUpgraders)[0]
            };

            AnimationClipUpgrader.GatherClipUsage(
                clip,
                clipData,
                renderersByPath,
                allUpgradePathsToNewShaders: default,
                upgradePathsUsedByMaterials: upgradePathsUsedByMaterials
            );

            var actualRenames = clipData.SelectMany(kv1 => kv1.Value.PropertyRenames.Select(kv2 => (AnimationClipUpgrader.InferShaderProperty(kv2.Key).Name, kv2.Value))).ToArray();
            Assert.That(actualRenames, Is.EqualTo(expectedRenames));

            return clipData[clip].Usage;
        }

        static readonly (string OldShader, string NewShader, (string From, string To, int Type)[])[] k_AllUpgrades =
        {
            ("OldShader1", "NewShader1", new[] { (From: "_Color", To: "_BaseColor1", Type: (int)MaterialPropertyType.Color) }),
            ("OldShader2", "NewShader2", new[] { (From: "_Color", To: "_BaseColor2", Type: (int)MaterialPropertyType.Color) })
        };
        static readonly (string OldShader, string NewShader, (string From, string To, int Type)[]) k_KnownUpgrade =
            ("OldShader1", "NewShader1", new[] { (From: "_Color", To: "_BaseColor1", Type: (int)MaterialPropertyType.Color) });

        static readonly TestCaseData[] k_OneKnownUpgradePathTestCases =
        {
            new TestCaseData(
                new[] { ("ID1", "NewShader1"), ("ID2", "NewShader2") }, "material._Color.r",
                new[] { (From: "_Color", To: "_BaseColor2") },
                k_AllUpgrades,
                new[]
                {
                    k_KnownUpgrade,
                    ("OldShader2", "NewShader2", new[] { (From: "_Color", To: "_BaseColor2", Type: (int)MaterialPropertyType.Color) })
                }
            )
                .Returns(SerializedShaderPropertyUsage.UsedByUpgraded | SerializedShaderPropertyUsage.UsedByAmbiguouslyUpgraded)
                .SetName("Two target materials, second is different known upgrade"),
            new TestCaseData(
                new[] { ("ID1", "NewShader1"), ("ID2", "OldShader1") }, "material._Color.r",
                new[] { (From: "_Color", To: "_Color") },
                k_AllUpgrades,
                new[] { k_KnownUpgrade }
            )
                .Returns(SerializedShaderPropertyUsage.UsedByUpgraded | SerializedShaderPropertyUsage.UsedByNonUpgraded)
                .SetName("Two target materials, second is non-upgraded material"),
            new TestCaseData(
                new[] { ("ID1", "NewShader1"), ("ID2", "NewShader2") }, "material._Color.r",
                new[] { (From: "_Color", To: "_BaseColor2") },
                k_AllUpgrades,
                new[] { k_KnownUpgrade }
            )
                .Returns(SerializedShaderPropertyUsage.UsedByUpgraded | SerializedShaderPropertyUsage.UsedByAmbiguouslyUpgraded)
                .SetName("Two target materials, second is inferred upgrade")
        };

        [TestCaseSource(nameof(k_OneKnownUpgradePathTestCases))]
        public object GatherClipUsage_WhenTwoMaterials_UpgradePathOnlyKnownForOne_ReturnsExpectedResult(
            (string ID, string ShaderName)[] upgradedMaterials, string bindingPropertyName,
            (string From, string To)[] expectedRenames,
            (string OldShader, string NewShader, (string From, string To, int Type)[] Renames)[] allUpgrades,
            (string OldShader, string NewShader, (string From, string To, int Type)[] Renames)[] knownUpgrades
        )
        {
            var clip = new Mock<IAnimationClip>().Object;
            var bindings = new[] { new EditorCurveBinding { path = k_RendererPath, type = typeof(MeshRenderer), propertyName = bindingPropertyName } };
            var clipData = new Dictionary<IAnimationClip, (ClipPath Path, EditorCurveBinding[] Bindings, SerializedShaderPropertyUsage Usage, IDictionary<EditorCurveBinding, string> PropertyRenames)>
            {
                [clip] = (k_ClipPath, bindings, SerializedShaderPropertyUsage.Unknown, new Dictionary<EditorCurveBinding, string>())
            };
            var renderersByPath = new Dictionary<string, (IRenderer Renderer, List<IMaterial> Materials)>
            {
                [k_RendererPath] = (new Mock<IRenderer>().Object, CreateMockMaterials(upgradedMaterials))
            };
            var allUpgradePathsToNewShaders = CreateUpgradePathsToNewShaders(allUpgrades);
            var upgradePathsUsedByMaterials = new Dictionary<UID, MaterialUpgrader>
            {
                [renderersByPath[k_RendererPath].Materials[0].ID] = CreateMaterialUpgraders(knownUpgrades)[0]
            };

            AnimationClipUpgrader.GatherClipUsage(
                clip,
                clipData,
                renderersByPath,
                allUpgradePathsToNewShaders: allUpgradePathsToNewShaders,
                upgradePathsUsedByMaterials: upgradePathsUsedByMaterials
            );

            var actualRenames = clipData.SelectMany(kv1 => kv1.Value.PropertyRenames.Select(kv2 => (AnimationClipUpgrader.InferShaderProperty(kv2.Key).Name, kv2.Value))).ToArray();
            Assert.That(actualRenames, Is.EqualTo(expectedRenames));

            return clipData[clip].Usage;
        }

        static readonly SerializedShaderPropertyUsage[] k_FilterTestCases =
        {
            SerializedShaderPropertyUsage.Unknown,
            SerializedShaderPropertyUsage.NoShaderProperties,
            SerializedShaderPropertyUsage.UsedByAmbiguouslyUpgraded,
            SerializedShaderPropertyUsage.UsedByNonUpgraded,
            SerializedShaderPropertyUsage.UsedByUpgraded | SerializedShaderPropertyUsage.UsedByNonUpgraded,
            ~SerializedShaderPropertyUsage.UsedByUpgraded
        };

        [Test]
        public void UpgradeClips_WhenClipUsageFiltered_DoesNotChangeClip(
            // TODO: use Values[] when SerializedShaderPropertyUsage is public
            [ValueSource(nameof(k_FilterTestCases))] object excludeFlags
        )
        {
            var bindings = new[] { new EditorCurveBinding { path = k_RendererPath, type = typeof(MeshRenderer), propertyName = "Old" } };
            var clip = new Mock<IAnimationClip>();
            var clipsToUpgrade = new Dictionary<IAnimationClip, (ClipPath Path, EditorCurveBinding[] Bindings, SerializedShaderPropertyUsage Usage, IDictionary<EditorCurveBinding, string> PropertyRenames)>
            {
                [clip.Object] = (default, default, default, new Dictionary<EditorCurveBinding, string> { { bindings[0], "New" } })
            };
            clip.Setup(c => c.GetCurveBindings()).Returns(bindings);
            clip.Setup(c => c.ReplaceBindings(It.IsAny<EditorCurveBinding[]>(), It.IsAny<EditorCurveBinding[]>()))
                .Callback((EditorCurveBinding[] b1, EditorCurveBinding[] b2) => bindings[0] = b2[0]);
            var expectedBindings = new[] { new EditorCurveBinding { path = k_RendererPath, type = typeof(MeshRenderer), propertyName = "Old" } };

            var upgraded = new HashSet<(IAnimationClip Clip, ClipPath Path, SerializedShaderPropertyUsage Usage)>();
            var notUpgraded = new HashSet<(IAnimationClip Clip, ClipPath Path, SerializedShaderPropertyUsage Usage)>();
            AnimationClipUpgrader.UpgradeClips(clipsToUpgrade, (SerializedShaderPropertyUsage)excludeFlags, upgraded, notUpgraded);

            Assert.That(clip.Object.GetCurveBindings().Single(), Is.EqualTo(expectedBindings[0]), "Did not find a single curve with expected bindings.");
        }

        [TestCase("material._Color.r", typeof(MeshRenderer), "material._Color.r", "_BaseColor", "material._BaseColor.r", TestName = "Known upgrade patches data")]
        [TestCase("material._Color.r", typeof(MeshRenderer), "material._OldProp.r", "_NewProp", "material._Color.r", TestName = "No known upgrade applies no change")]
        [TestCase("material._Color.r", typeof(MeshRenderer), "material._Colo.r", "_BaseColo", "material._Color.r", TestName = "Near match applies no change")]
        [TestCase("material._Color.r", typeof(MeshFilter), "material._Color.r", "_BaseColor", "material._Color.r", TestName = "Known upgrade but wrong target type applies no change")]
        public void UpgradeClips_AppliesExpectedChangesToClip(
            string oldPropertyName,
            Type bindingType,
            string fromPropertyName,
            string toPropertyName,
            string expectedPropertyName
        )
        {
            var bindings = new[] { new EditorCurveBinding { path = k_RendererPath, type = bindingType, propertyName = oldPropertyName } };
            var clip = new Mock<IAnimationClip>();
            var clipsToUpgrade = new Dictionary<IAnimationClip, (ClipPath Path, EditorCurveBinding[] Bindings, SerializedShaderPropertyUsage Usage, IDictionary<EditorCurveBinding, string> PropertyRenames)>
            {
                [clip.Object] = (default, default, SerializedShaderPropertyUsage.UsedByUpgraded, new Dictionary<EditorCurveBinding, string> { { EditorCurveBinding.FloatCurve(k_RendererPath, bindingType, fromPropertyName), toPropertyName } })
            };
            clip.Setup(c => c.GetCurveBindings()).Returns(bindings);
            clip.Setup(c => c.ReplaceBindings(It.IsAny<EditorCurveBinding[]>(), It.IsAny<EditorCurveBinding[]>()))
                .Callback((EditorCurveBinding[] b1, EditorCurveBinding[] b2) => bindings[0] = b2[0]);
            var expectedBindings = new[] { new EditorCurveBinding { path = k_RendererPath, type = bindingType, propertyName = expectedPropertyName } };

            var upgraded = new HashSet<(IAnimationClip Clip, ClipPath Path, SerializedShaderPropertyUsage Usage)>();
            var notUpgraded = new HashSet<(IAnimationClip Clip, ClipPath Path, SerializedShaderPropertyUsage Usage)>();
            AnimationClipUpgrader.UpgradeClips(clipsToUpgrade, default, upgraded, notUpgraded);

            var actualBinding = clip.Object.GetCurveBindings().Single();
            Assert.That(actualBinding, Is.EqualTo(expectedBindings[0]), "Did not find a single curve with expected bindings.");
        }

        [TestCase("material._Color.r", typeof(MeshRenderer), "material._Color.r", "_BaseColor", "material._BaseColor.r", TestName = "Known upgrade patches data")]
        [TestCase("material._Color.r", typeof(MeshRenderer), "material._OldProp.r", "_NewProp", "material._Color.r", TestName = "No known upgrade applies no change")]
        [TestCase("material._Color.r", typeof(MeshRenderer), "material._Colo.r", "_BaseColo", "material._Color.r", TestName = "Near match applies no change")]
        [TestCase("material._Color.r", typeof(MeshFilter), "material._Color.r", "_BaseColor", "material._Color.r", TestName = "Known upgrade but wrong target type applies no change")]
        public void UpgradeClips_AppliesExpectedChangesToProxyClip(
            string oldPropertyName,
            Type bindingType,
            string fromPropertyName,
            string toPropertyName,
            string expectedPropertyName
        )
        {
            var clip = new AnimationClipProxy { Clip = new AnimationClip() };
            try
            {
                var bindings = new[] { new EditorCurveBinding { path = k_RendererPath, type = bindingType, propertyName = oldPropertyName } };
                var curve = AnimationCurve.EaseInOut(1f, 2f, 3f, 4f);
                AnimationUtility.SetEditorCurve(clip.Clip, bindings[0], curve);
                var clipsToUpgrade = new Dictionary<IAnimationClip, (ClipPath Path, EditorCurveBinding[] Bindings, SerializedShaderPropertyUsage Usage, IDictionary<EditorCurveBinding, string> PropertyRenames)>
                {
                    [clip] = (default, default, SerializedShaderPropertyUsage.UsedByUpgraded, new Dictionary<EditorCurveBinding, string> { { EditorCurveBinding.FloatCurve(k_RendererPath, bindingType, fromPropertyName), toPropertyName } })
                };
                var expectedBindings = new[] { new EditorCurveBinding { path = k_RendererPath, type = bindingType, propertyName = expectedPropertyName } };

                var upgraded = new HashSet<(IAnimationClip Clip, ClipPath Path, SerializedShaderPropertyUsage Usage)>();
                var notUpgraded = new HashSet<(IAnimationClip Clip, ClipPath Path, SerializedShaderPropertyUsage Usage)>();
                AnimationClipUpgrader.UpgradeClips(clipsToUpgrade, default, upgraded, notUpgraded);

                var actualBinding = clip.GetCurveBindings().Single();
                Assert.That(actualBinding, Is.EqualTo(expectedBindings[0]), "Did not find a single curve with expected bindings.");
                var actualKeys = AnimationUtility.GetEditorCurve(clip.Clip, expectedBindings[0]).keys;
                Assert.That(actualKeys, Is.EqualTo(curve.keys), "Curve copied incorrectly");
            }
            finally
            {
                UnityObject.DestroyImmediate(clip.Clip);
            }
        }

        [Test]
        public void UpgradeClips_AppendsToResultCollectors()
        {
            var upgradedClip = new Mock<IAnimationClip>();
            var notUpgradedClip = new Mock<IAnimationClip>();
            var clipsToUpgrade = new Dictionary<IAnimationClip, (ClipPath Path, EditorCurveBinding[] Bindings, SerializedShaderPropertyUsage Usage, IDictionary<EditorCurveBinding, string> PropertyRenames)>
            {
                [upgradedClip.Object] = (default, default, SerializedShaderPropertyUsage.UsedByUpgraded, new Dictionary<EditorCurveBinding, string>()),
                [notUpgradedClip.Object] = (default, default, SerializedShaderPropertyUsage.UsedByNonUpgraded, new Dictionary<EditorCurveBinding, string>())
            };
            upgradedClip.Setup(c => c.GetCurveBindings()).Returns(Array.Empty<EditorCurveBinding>());
            upgradedClip.Setup(c => c.ReplaceBindings(It.IsAny<EditorCurveBinding[]>(), It.IsAny<EditorCurveBinding[]>()))
                .Callback((EditorCurveBinding[] b1, EditorCurveBinding[] b2) => { });
            var upgraded = new HashSet<(IAnimationClip Clip, ClipPath Path, SerializedShaderPropertyUsage Usage)>();
            var notUpgraded = new HashSet<(IAnimationClip Clip, ClipPath Path, SerializedShaderPropertyUsage Usage)>();

            AnimationClipUpgrader.UpgradeClips(clipsToUpgrade, ~SerializedShaderPropertyUsage.UsedByUpgraded, upgraded, notUpgraded);

            Assert.That(upgraded.Single().Clip, Is.EqualTo(upgradedClip.Object));
            Assert.That(notUpgraded.Single().Clip, Is.EqualTo(notUpgradedClip.Object));
        }
    }
}
