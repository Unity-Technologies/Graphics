using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityObject = UnityEngine.Object;
using IAssetPath = UnityEditor.Rendering.AnimationClipUpgrader.IAssetPath;
using ClipPath = UnityEditor.Rendering.AnimationClipUpgrader.ClipPath;
using PrefabPath = UnityEditor.Rendering.AnimationClipUpgrader.PrefabPath;
using ScenePath = UnityEditor.Rendering.AnimationClipUpgrader.ScenePath;

namespace UnityEditor.Rendering.Tests
{
    /// <summary>
    /// Configure relevant resources using default values on MonoScript importer for <see cref="TestResources"/>.
    /// </summary>
    class AnimationClipUpgrader_IntegrationTests
    {
        TestResources m_Resources;

        IReadOnlyDictionary<ClipPath, IReadOnlyCollection<PrefabPath>> m_ClipsToPrefabDependents;
        IReadOnlyDictionary<PrefabPath, IReadOnlyCollection<ClipPath>> m_PrefabsToClipDependencies;
        IReadOnlyDictionary<ClipPath, IReadOnlyCollection<ScenePath>> m_ClipsToSceneDependents;
        IReadOnlyDictionary<ScenePath, IReadOnlyCollection<ClipPath>> m_ScenesToClipDependencies;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            m_Resources = ScriptableObject.CreateInstance<TestResources>();

            // gather clip dependencies
            var allClips = typeof(TestResources).GetFields(BindingFlags.Instance | BindingFlags.Public)
                .Where(f => f.FieldType == typeof(AnimationClip))
                .Select(f => (ClipPath)(f.GetValue(m_Resources) as AnimationClip))
                .ToArray();
            var allPrefabs = typeof(TestResources).GetFields(BindingFlags.Instance | BindingFlags.Public)
                .Where(f => f.FieldType == typeof(GameObject))
                .Select(f => (PrefabPath)(f.GetValue(m_Resources) as GameObject))
                .ToArray();
            var allScenes = typeof(TestResources).GetFields(BindingFlags.Instance | BindingFlags.Public)
                .Where(f => f.FieldType == typeof(SceneAsset))
                .Select(f => (ScenePath)(f.GetValue(m_Resources) as SceneAsset))
                .ToArray();

            AnimationClipUpgrader.GetClipDependencyMappings(allClips, allPrefabs, out m_ClipsToPrefabDependents, out m_PrefabsToClipDependencies);
            AnimationClipUpgrader.GetClipDependencyMappings(allClips, allScenes, out m_ClipsToSceneDependents, out m_ScenesToClipDependencies);

            // sorting function to ease comparisons when actual/expected mismatch
            int CompareKeyValuePair((IAssetPath, IAssetPath) a, (IAssetPath, IAssetPath) b) =>
                string.Compare($"{a.Item1}{a.Item2}", $"{b.Item1}{b.Item2}", StringComparison.Ordinal);

            // validate clip/prefab dependencies
            GetExpectedPrefabDependencies(out var expectedClipToPrefabDependents, out var expectedPrefabsToClipDependencies);

            var actualClipToPrefabs = m_ClipsToPrefabDependents.SelectMany(kv => kv.Value.Select(v => (kv.Key, v))).ToList();
            var expectedClipToPrefabs = expectedClipToPrefabDependents.SelectMany(kv => kv.Value.Select(v => (kv.Key, v))).ToList();
            actualClipToPrefabs.Sort((a, b) => CompareKeyValuePair(a, b));
            expectedClipToPrefabs.Sort((a, b) => CompareKeyValuePair(a, b));
            Assume.That(actualClipToPrefabs, Is.EquivalentTo(expectedClipToPrefabs));

            var actualPrefabsToClips = m_PrefabsToClipDependencies.SelectMany(kv => kv.Value.Select(v => (kv.Key, v))).ToList();
            var expectedPrefabsToClips = expectedPrefabsToClipDependencies.SelectMany(kv => kv.Value.Select(v => (kv.Key, v))).ToList();
            actualPrefabsToClips.Sort((a, b) => CompareKeyValuePair(a, b));
            expectedPrefabsToClips.Sort((a, b) => CompareKeyValuePair(a, b));
            Assume.That(actualPrefabsToClips, Is.EquivalentTo(expectedPrefabsToClips));

            // validate clip/scene dependencies
            GetExpectedSceneDependencies(out var expectedClipToSceneDependents, out var expectedScenesToClipDependencies);

            var actualClipsToScenes = m_ClipsToSceneDependents.SelectMany(kv => kv.Value.Select(v => (kv.Key, v))).ToList();
            var expectedClipToScenes = expectedClipToSceneDependents.SelectMany(kv => kv.Value.Select(v => (kv.Key, v))).ToList();
            actualClipsToScenes.Sort((a, b) => CompareKeyValuePair(a, b));
            expectedClipToScenes.Sort((a, b) => CompareKeyValuePair(a, b));
            Assume.That(actualClipsToScenes, Is.EquivalentTo(expectedClipToScenes));

            var actualScenesToClips = m_ScenesToClipDependencies.SelectMany(kv => kv.Value.Select(v => (kv.Key, v))).ToList();
            var expectedScenesToClips = expectedScenesToClipDependencies.SelectMany(kv => kv.Value.Select(v => (kv.Key, v))).ToList();
            actualScenesToClips.Sort((a, b) => CompareKeyValuePair(a, b));
            expectedScenesToClips.Sort((a, b) => CompareKeyValuePair(a, b));
            Assume.That(actualScenesToClips, Is.EquivalentTo(expectedScenesToClips));
        }

        #region Expected Dependencies

        void GetExpectedPrefabDependencies(
            out Dictionary<ClipPath, PrefabPath[]> expectedClipDependents,
            out Dictionary<PrefabPath, ClipPath[]> expectedPrefabDependencies
        )
        {
            expectedClipDependents = new Dictionary<ClipPath, PrefabPath[]>
            {
                // Animation
                {
                    m_Resources.Clip_Animation_WithMaterialProperties_PotentiallyUpgradable,
                    new PrefabPath[]
                    {
                        m_Resources.Prefab_Animation_WithMaterialProperties_Upgradable,
                        m_Resources.Variant_Animation_WithMaterialProperties_Upgradable,
                        m_Resources.Prefab_Animation_WithMaterialProperties_NotUpgradable,
                        m_Resources.Variant_Animation_WithMaterialProperties_NotUpgradable,
                        m_Resources.Prefab_Animation_WithMaterialProperties_NoMaterials,
                        m_Resources.Prefab_Animation_WithMaterialProperties_NoRenderer
                    }
                }, {
                    m_Resources.Clip_Animation_WithMaterialProperties_OnlyUsedByUpgradable,
                    new PrefabPath[]
                    {
                        m_Resources.Prefab_Animation_WithMaterialProperties_Upgradable,
                        m_Resources.Variant_Animation_WithMaterialProperties_Upgradable
                    }
                }, {
                    m_Resources.Clip_Animation_WithMaterialProperties_OnlyUsedByNotUpgradable,
                    new PrefabPath[]
                    {
                        m_Resources.Prefab_Animation_WithMaterialProperties_NotUpgradable,
                        m_Resources.Variant_Animation_WithMaterialProperties_NotUpgradable
                    }
                }, {
                    m_Resources.Clip_Animation_WithMaterialProperties_UsedByUpgradableAndNotUpgradable,
                    new PrefabPath[]
                    {
                        m_Resources.Prefab_Animation_WithMaterialProperties_Upgradable,
                        m_Resources.Variant_Animation_WithMaterialProperties_Upgradable,
                        m_Resources.Prefab_Animation_WithMaterialProperties_NotUpgradable,
                        m_Resources.Variant_Animation_WithMaterialProperties_NotUpgradable
                    }
                }, {
                    m_Resources.Clip_Animation_WithoutMaterialProperties,
                    new PrefabPath[]
                    {
                        m_Resources.Prefab_Animation_WithoutMaterialProperties,
                        m_Resources.Variant_Animation_WithoutMaterialProperties
                    }
                },
                // Animator
                {
                    m_Resources.Clip_Animator_WithMaterialProperties_PotentiallyUpgradable,
                    new PrefabPath[]
                    {
                        m_Resources.Prefab_Animator_WithMaterialProperties_Upgradable,
                        m_Resources.Variant_Animator_WithMaterialProperties_Upgradable,
                        m_Resources.Prefab_Animator_WithMaterialProperties_NotUpgradable,
                        m_Resources.Variant_Animator_WithMaterialProperties_NotUpgradable,
                        m_Resources.Prefab_Animator_WithMaterialProperties_NoMaterials,
                        m_Resources.Prefab_Animator_WithMaterialProperties_NoRenderer
                    }
                }, {
                    m_Resources.Clip_Animator_WithMaterialProperties_OnlyUsedByUpgradable,
                    new PrefabPath[]
                    {
                        m_Resources.Prefab_Animator_WithMaterialProperties_Upgradable,
                        m_Resources.Variant_Animator_WithMaterialProperties_Upgradable
                    }
                }, {
                    m_Resources.Clip_Animator_WithMaterialProperties_OnlyUsedByNotUpgradable,
                    new PrefabPath[]
                    {
                        m_Resources.Prefab_Animator_WithMaterialProperties_NotUpgradable,
                        m_Resources.Variant_Animator_WithMaterialProperties_NotUpgradable
                    }
                }, {
                    m_Resources.Clip_Animator_WithMaterialProperties_UsedByUpgradableAndNotUpgradable,
                    new PrefabPath[]
                    {
                        m_Resources.Prefab_Animator_WithMaterialProperties_Upgradable,
                        m_Resources.Variant_Animator_WithMaterialProperties_Upgradable,
                        m_Resources.Prefab_Animator_WithMaterialProperties_NotUpgradable,
                        m_Resources.Variant_Animator_WithMaterialProperties_NotUpgradable
                    }
                }, {
                    m_Resources.Clip_Animator_WithoutMaterialProperties,
                    new PrefabPath[]
                    {
                        m_Resources.Prefab_Animator_WithoutMaterialProperties,
                        m_Resources.Variant_Animator_WithoutMaterialProperties
                    }
                },
                // PlayableDirector
                {
                    m_Resources.Clip_Timeline_WithMaterialProperties_PotentiallyUpgradable,
                    new PrefabPath[]
                    {
                        m_Resources.Prefab_Timeline_WithMaterialProperties_NoMaterials,
                        m_Resources.Prefab_Timeline_WithMaterialProperties_NoRenderer
                    }
                }, {
                    m_Resources.Clip_Timeline_WithMaterialProperties_OnlyUsedByUpgradable,
                    new PrefabPath[]
                    {
                        m_Resources.Prefab_Timeline_WithMaterialProperties_Upgradable,
                        m_Resources.Variant_Timeline_WithMaterialProperties_Upgradable
                    }
                }, {
                    m_Resources.Clip_Timeline_WithMaterialProperties_OnlyUsedByNotUpgradable,
                    new PrefabPath[]
                    {
                        m_Resources.Prefab_Timeline_WithMaterialProperties_NotUpgradable,
                        m_Resources.Variant_Timeline_WithMaterialProperties_NotUpgradable
                    }
                }, {
                    m_Resources.Clip_Timeline_WithMaterialProperties_UsedByUpgradableAndNotUpgradable,
                    new PrefabPath[]
                    {
                        m_Resources.Prefab_Timeline_WithMaterialProperties_Upgradable_AlsoUsedByNotUpgradable,
                        m_Resources.Prefab_Timeline_WithMaterialProperties_NotUpgradable_AlsoUsedByUpgradable,
                    }
                }, {
                    m_Resources.Clip_Timeline_WithoutMaterialProperties,
                    new PrefabPath[]
                    {
                        m_Resources.Prefab_Timeline_WithoutMaterialProperties,
                        m_Resources.Variant_Timeline_WithoutMaterialProperties
                    }
                }, {
                    m_Resources.Clip_Timeline_Standalone_WithMaterialProperties_Upgradable,
                    new PrefabPath[]
                    {
                        m_Resources.Prefab_Timeline_Standalone_WithMaterialProperties_Upgradable
                    }
                }
            };

            expectedPrefabDependencies = new Dictionary<PrefabPath, ClipPath[]>
            {
                // Animation
                {
                    m_Resources.Prefab_Animation_WithMaterialProperties_Upgradable,
                    new ClipPath[]
                    {
                        m_Resources.Clip_Animation_WithMaterialProperties_PotentiallyUpgradable,
                        m_Resources.Clip_Animation_WithMaterialProperties_OnlyUsedByUpgradable,
                        m_Resources.Clip_Animation_WithMaterialProperties_UsedByUpgradableAndNotUpgradable
                    }
                },
                {
                    m_Resources.Variant_Animation_WithMaterialProperties_Upgradable,
                    new ClipPath[]
                    {
                        m_Resources.Clip_Animation_WithMaterialProperties_PotentiallyUpgradable,
                        m_Resources.Clip_Animation_WithMaterialProperties_OnlyUsedByUpgradable,
                        m_Resources.Clip_Animation_WithMaterialProperties_UsedByUpgradableAndNotUpgradable
                    }
                } , {
                    m_Resources.Prefab_Animation_WithMaterialProperties_NotUpgradable,
                    new ClipPath[]
                    {
                        m_Resources.Clip_Animation_WithMaterialProperties_PotentiallyUpgradable,
                        m_Resources.Clip_Animation_WithMaterialProperties_OnlyUsedByNotUpgradable,
                        m_Resources.Clip_Animation_WithMaterialProperties_UsedByUpgradableAndNotUpgradable
                    }
                },
                {
                    m_Resources.Variant_Animation_WithMaterialProperties_NotUpgradable,
                    new ClipPath[]
                    {
                        m_Resources.Clip_Animation_WithMaterialProperties_PotentiallyUpgradable,
                        m_Resources.Clip_Animation_WithMaterialProperties_OnlyUsedByNotUpgradable,
                        m_Resources.Clip_Animation_WithMaterialProperties_UsedByUpgradableAndNotUpgradable
                    }
                }, {
                    m_Resources.Prefab_Animation_WithoutMaterialProperties,
                    new ClipPath[]
                    {
                        m_Resources.Clip_Animation_WithoutMaterialProperties
                    }
                },
                {
                    m_Resources.Variant_Animation_WithoutMaterialProperties,
                    new ClipPath[]
                    {
                        m_Resources.Clip_Animation_WithoutMaterialProperties
                    }
                },
                {
                    m_Resources.Prefab_Animation_WithMaterialProperties_NoMaterials,
                    new ClipPath[]
                    {
                        m_Resources.Clip_Animation_WithMaterialProperties_PotentiallyUpgradable
                    }
                },
                {
                    m_Resources.Prefab_Animation_WithMaterialProperties_NoRenderer,
                    new ClipPath[]
                    {
                        m_Resources.Clip_Animation_WithMaterialProperties_PotentiallyUpgradable
                    }
                },
                // Animator
                {
                    m_Resources.Prefab_Animator_WithMaterialProperties_Upgradable,
                    new ClipPath[]
                    {
                        m_Resources.Clip_Animator_WithMaterialProperties_PotentiallyUpgradable,
                        m_Resources.Clip_Animator_WithMaterialProperties_OnlyUsedByUpgradable,
                        m_Resources.Clip_Animator_WithMaterialProperties_UsedByUpgradableAndNotUpgradable
                    }
                },
                {
                    m_Resources.Variant_Animator_WithMaterialProperties_Upgradable,
                    new ClipPath[]
                    {
                        m_Resources.Clip_Animator_WithMaterialProperties_PotentiallyUpgradable,
                        m_Resources.Clip_Animator_WithMaterialProperties_OnlyUsedByUpgradable,
                        m_Resources.Clip_Animator_WithMaterialProperties_UsedByUpgradableAndNotUpgradable
                    }
                } , {
                    m_Resources.Prefab_Animator_WithMaterialProperties_NotUpgradable,
                    new ClipPath[]
                    {
                        m_Resources.Clip_Animator_WithMaterialProperties_PotentiallyUpgradable,
                        m_Resources.Clip_Animator_WithMaterialProperties_OnlyUsedByNotUpgradable,
                        m_Resources.Clip_Animator_WithMaterialProperties_UsedByUpgradableAndNotUpgradable
                    }
                },
                {
                    m_Resources.Variant_Animator_WithMaterialProperties_NotUpgradable,
                    new ClipPath[]
                    {
                        m_Resources.Clip_Animator_WithMaterialProperties_PotentiallyUpgradable,
                        m_Resources.Clip_Animator_WithMaterialProperties_OnlyUsedByNotUpgradable,
                        m_Resources.Clip_Animator_WithMaterialProperties_UsedByUpgradableAndNotUpgradable
                    }
                }, {
                    m_Resources.Prefab_Animator_WithoutMaterialProperties,
                    new ClipPath[]
                    {
                        m_Resources.Clip_Animator_WithoutMaterialProperties
                    }
                },
                {
                    m_Resources.Variant_Animator_WithoutMaterialProperties,
                    new ClipPath[]
                    {
                        m_Resources.Clip_Animator_WithoutMaterialProperties
                    }
                },
                {
                    m_Resources.Prefab_Animator_WithMaterialProperties_NoMaterials,
                    new ClipPath[]
                    {
                        m_Resources.Clip_Animator_WithMaterialProperties_PotentiallyUpgradable
                    }
                },
                {
                    m_Resources.Prefab_Animator_WithMaterialProperties_NoRenderer,
                    new ClipPath[]
                    {
                        m_Resources.Clip_Animator_WithMaterialProperties_PotentiallyUpgradable
                    }
                },
                // PlayableDirector
                {
                    m_Resources.Prefab_Timeline_WithMaterialProperties_Upgradable,
                    new ClipPath[]
                    {
                        m_Resources.Clip_Timeline_WithMaterialProperties_OnlyUsedByUpgradable
                    }
                },
                {
                    m_Resources.Variant_Timeline_WithMaterialProperties_Upgradable,
                    new ClipPath[]
                    {
                        m_Resources.Clip_Timeline_WithMaterialProperties_OnlyUsedByUpgradable
                    }
                } , {
                    m_Resources.Prefab_Timeline_WithMaterialProperties_NotUpgradable,
                    new ClipPath[]
                    {
                        m_Resources.Clip_Timeline_WithMaterialProperties_OnlyUsedByNotUpgradable
                    }
                },
                {
                    m_Resources.Variant_Timeline_WithMaterialProperties_NotUpgradable,
                    new ClipPath[]
                    {
                        m_Resources.Clip_Timeline_WithMaterialProperties_OnlyUsedByNotUpgradable
                    }
                }, {
                    m_Resources.Prefab_Timeline_WithoutMaterialProperties,
                    new ClipPath[]
                    {
                        m_Resources.Clip_Timeline_WithoutMaterialProperties
                    }
                },
                {
                    m_Resources.Variant_Timeline_WithoutMaterialProperties,
                    new ClipPath[]
                    {
                        m_Resources.Clip_Timeline_WithoutMaterialProperties
                    }
                },
                {
                    m_Resources.Prefab_Timeline_WithMaterialProperties_NoMaterials,
                    new ClipPath[]
                    {
                        m_Resources.Clip_Timeline_WithMaterialProperties_PotentiallyUpgradable
                    }
                },
                {
                    m_Resources.Prefab_Timeline_WithMaterialProperties_NoRenderer,
                    new ClipPath[]
                    {
                        m_Resources.Clip_Timeline_WithMaterialProperties_PotentiallyUpgradable
                    }
                },
                {
                    m_Resources.Prefab_Timeline_WithMaterialProperties_NotUpgradable_AlsoUsedByUpgradable,
                    new ClipPath[]
                    {
                        m_Resources.Clip_Timeline_WithMaterialProperties_UsedByUpgradableAndNotUpgradable
                    }
                },
                {
                    m_Resources.Prefab_Timeline_WithMaterialProperties_Upgradable_AlsoUsedByNotUpgradable,
                    new ClipPath[]
                    {
                        m_Resources.Clip_Timeline_WithMaterialProperties_UsedByUpgradableAndNotUpgradable
                    }
                },
                {
                    m_Resources.Prefab_Timeline_Standalone_WithMaterialProperties_Upgradable,
                    new ClipPath[]
                    {
                        m_Resources.Clip_Timeline_Standalone_WithMaterialProperties_Upgradable
                    }
                }
            };
        }

        void GetExpectedSceneDependencies(
            out Dictionary<ClipPath, ScenePath[]> expectedClipDependents,
            out Dictionary<ScenePath, ClipPath[]> expectedSceneDependencies
        )
        {
            expectedClipDependents = new Dictionary<ClipPath, ScenePath[]>
            {
                // Animation
                {
                    m_Resources.Clip_Animation_WithMaterialProperties_PotentiallyUpgradable,
                    new ScenePath[]
                    {
                        m_Resources.Scene_Animation_WithMaterialProperties_Upgradable
                    }
                },
                {
                    m_Resources.Clip_Animation_WithMaterialProperties_OnlyUsedByUpgradable,
                    new ScenePath[]
                    {
                        m_Resources.Scene_Animation_WithMaterialProperties_Upgradable
                    }
                },
                {
                    m_Resources.Clip_Animation_WithMaterialProperties_OnlyUsedByNotUpgradable,
                    new ScenePath[]
                    {
                    }
                },
                {
                    m_Resources.Clip_Animation_WithMaterialProperties_UsedByUpgradableAndNotUpgradable,
                    new ScenePath[]
                    {
                        m_Resources.Scene_Animation_WithMaterialProperties_Upgradable
                    }
                },
                {
                    m_Resources.Clip_Animation_WithoutMaterialProperties,
                    new ScenePath[]
                    {
                        m_Resources.Scene_Animation_WithoutMaterialProperties
                    }
                },
                // Animator
                {
                    m_Resources.Clip_Animator_WithMaterialProperties_PotentiallyUpgradable,
                    new ScenePath[]
                    {
                        m_Resources.Scene_Animator_WithMaterialProperties_Upgradable
                    }
                },
                {
                    m_Resources.Clip_Animator_WithMaterialProperties_OnlyUsedByUpgradable,
                    new ScenePath[]
                    {
                        m_Resources.Scene_Animator_WithMaterialProperties_Upgradable
                    }
                },
                {
                    m_Resources.Clip_Animator_WithMaterialProperties_OnlyUsedByNotUpgradable,
                    new ScenePath[]
                    {
                    }
                },
                {
                    m_Resources.Clip_Animator_WithMaterialProperties_UsedByUpgradableAndNotUpgradable,
                    new ScenePath[]
                    {
                        m_Resources.Scene_Animator_WithMaterialProperties_Upgradable
                    }
                },
                {
                    m_Resources.Clip_Animator_WithoutMaterialProperties,
                    new ScenePath[]
                    {
                        m_Resources.Scene_Animator_WithoutMaterialProperties
                    }
                },
                // PlayableDirector
                {
                    m_Resources.Clip_Timeline_WithMaterialProperties_PotentiallyUpgradable,
                    new ScenePath[]
                    {
                    }
                },
                {
                    m_Resources.Clip_Timeline_WithMaterialProperties_OnlyUsedByUpgradable,
                    new ScenePath[]
                    {
                        m_Resources.Scene_Timeline_WithMaterialProperties_Upgradable
                    }
                },
                {
                    m_Resources.Clip_Timeline_WithMaterialProperties_OnlyUsedByNotUpgradable,
                    new ScenePath[]
                    {
                    }
                },
                {
                    m_Resources.Clip_Timeline_WithMaterialProperties_UsedByUpgradableAndNotUpgradable,
                    new ScenePath[]
                    {
                    }
                },
                {
                    m_Resources.Clip_Timeline_WithoutMaterialProperties,
                    new ScenePath[]
                    {
                        m_Resources.Scene_Timeline_WithoutMaterialProperties
                    }
                }
            };

            expectedSceneDependencies = new Dictionary<ScenePath, ClipPath[]>
            {
                // Animation
                {
                    m_Resources.Scene_Animation_WithMaterialProperties_Upgradable,
                    new ClipPath[]
                    {
                        m_Resources.Clip_Animation_WithMaterialProperties_PotentiallyUpgradable,
                        m_Resources.Clip_Animation_WithMaterialProperties_OnlyUsedByUpgradable,
                        m_Resources.Clip_Animation_WithMaterialProperties_UsedByUpgradableAndNotUpgradable
                    }
                },
                {
                    m_Resources.Scene_Animation_WithoutMaterialProperties,
                    new ClipPath[]
                    {
                        m_Resources.Clip_Animation_WithoutMaterialProperties
                    }
                },
                // Animator
                {
                    m_Resources.Scene_Animator_WithMaterialProperties_Upgradable,
                    new ClipPath[]
                    {
                        m_Resources.Clip_Animator_WithMaterialProperties_PotentiallyUpgradable,
                        m_Resources.Clip_Animator_WithMaterialProperties_OnlyUsedByUpgradable,
                        m_Resources.Clip_Animator_WithMaterialProperties_UsedByUpgradableAndNotUpgradable
                    }
                },
                {
                    m_Resources.Scene_Animator_WithoutMaterialProperties,
                    new ClipPath[]
                    {
                        m_Resources.Clip_Animator_WithoutMaterialProperties
                    }
                },
                // PlayableDirector
                {
                    m_Resources.Scene_Timeline_WithMaterialProperties_Upgradable,
                    new ClipPath[]
                    {
                        m_Resources.Clip_Timeline_WithMaterialProperties_OnlyUsedByUpgradable
                    }
                },
                {
                    m_Resources.Scene_Timeline_WithoutMaterialProperties,
                    new ClipPath[]
                    {
                        m_Resources.Clip_Timeline_WithoutMaterialProperties
                    }
                }
            };
        }

        #endregion

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            if (m_Resources != null)
                ScriptableObject.DestroyImmediate(m_Resources);
        }

        [Test]
        public void GetAssetDataForClipsFiltered_RemovesClipsWithoutMaterialAnimation()
        {
            var animationClip = m_Resources.Clip_Animation_WithMaterialProperties_PotentiallyUpgradable;
            var animatorClip = m_Resources.Clip_Animator_WithMaterialProperties_PotentiallyUpgradable;
            var timelineClip = m_Resources.Clip_Timeline_WithMaterialProperties_PotentiallyUpgradable;

            var result = AnimationClipUpgrader.GetAssetDataForClipsFiltered(new ClipPath[]
            {
                m_Resources.Clip_Animation_WithMaterialProperties_PotentiallyUpgradable,
                m_Resources.Clip_Animation_WithoutMaterialProperties,
                m_Resources.Clip_Animator_WithMaterialProperties_PotentiallyUpgradable,
                m_Resources.Clip_Animator_WithoutMaterialProperties,
                m_Resources.Clip_Timeline_WithMaterialProperties_PotentiallyUpgradable,
                m_Resources.Clip_Timeline_WithoutMaterialProperties
            });
            var actualKeys = result.Keys.Select(k => k.Clip);

            Assert.That(actualKeys, Is.EquivalentTo(new[] { animationClip, animatorClip, timelineClip }));
        }

        [Test]
        public void GetAssetDataForClipsFiltered_WhenClipPathContainsMultipleClips_ReturnsMultipleClips()
        {
            var childAnimationClip = m_Resources.Clip_ChildAsset_Animation_WithMaterialProperties as AnimationClip;
            var childAnimatorClip = m_Resources.Clip_ChildAsset_Animator_WithMaterialProperties as AnimationClip;
            var parentPath = new ClipPath { Path = AssetDatabase.GetAssetPath(m_Resources.Clip_ParentAsset) };

            var result = AnimationClipUpgrader.GetAssetDataForClipsFiltered(new[] { parentPath });
            var actualKeys = result.Keys.Select(k => k.Clip);

            Assert.That(actualKeys, Is.EquivalentTo(new[] { childAnimationClip, childAnimatorClip }));
        }

        [TestCase(
            nameof(TestResources.Prefab_Animation_WithMaterialProperties_NoRenderer),
            nameof(TestResources.Clip_Animation_WithMaterialProperties_PotentiallyUpgradable),
            ExpectedResult = SerializedShaderPropertyUsage.Unknown,
            TestName = "Animation, no renderer = Unknown"
         )]
        [TestCase(
            nameof(TestResources.Prefab_Animation_WithMaterialProperties_NoMaterials),
            nameof(TestResources.Clip_Animation_WithMaterialProperties_PotentiallyUpgradable),
            ExpectedResult = SerializedShaderPropertyUsage.Unknown,
            TestName = "Animation, no materials = Unknown"
         )]
        [TestCase(
            nameof(TestResources.Prefab_Animator_WithMaterialProperties_NoRenderer),
            nameof(TestResources.Clip_Animator_WithMaterialProperties_PotentiallyUpgradable),
            ExpectedResult = SerializedShaderPropertyUsage.Unknown,
            TestName = "Animator, no renderer = Unknown"
         )]
        [TestCase(
            nameof(TestResources.Prefab_Animator_WithMaterialProperties_NoMaterials),
            nameof(TestResources.Clip_Animator_WithMaterialProperties_PotentiallyUpgradable),
            ExpectedResult = SerializedShaderPropertyUsage.Unknown,
            TestName = "Animator, no materials = Unknown"
         )]
        [TestCase(
            nameof(TestResources.Prefab_Timeline_WithMaterialProperties_NoRenderer),
            nameof(TestResources.Clip_Timeline_WithMaterialProperties_PotentiallyUpgradable),
            ExpectedResult = SerializedShaderPropertyUsage.Unknown,
            TestName = "Timeline, no renderer = Unknown"
         )]
        [TestCase(
            nameof(TestResources.Prefab_Timeline_WithMaterialProperties_NoMaterials),
            nameof(TestResources.Clip_Timeline_WithMaterialProperties_PotentiallyUpgradable),
            ExpectedResult = SerializedShaderPropertyUsage.Unknown,
            TestName = "Timeline, no materials = Unknown"
         )]
        [TestCase(
            nameof(TestResources.Prefab_Animator_WithMaterialProperties_Upgradable),
            nameof(TestResources.Clip_Animator_WithMaterialProperties_OnlyUsedByUpgradable),
            ExpectedResult = SerializedShaderPropertyUsage.UsedByNonUpgraded,
            TestName = "Animator, with materials = UsedByNonUpgraded"
         )]
        [TestCase(
            nameof(TestResources.Prefab_Animation_WithMaterialProperties_Upgradable),
            nameof(TestResources.Clip_Animation_WithMaterialProperties_OnlyUsedByUpgradable),
            ExpectedResult = SerializedShaderPropertyUsage.UsedByNonUpgraded,
            TestName = "Animation, with materials = UsedByNonUpgraded"
         )]
        [TestCase(
            nameof(TestResources.Prefab_Timeline_WithMaterialProperties_Upgradable),
            nameof(TestResources.Clip_Timeline_WithMaterialProperties_OnlyUsedByUpgradable),
            ExpectedResult = SerializedShaderPropertyUsage.UsedByNonUpgraded,
            TestName = "Timeline with materials using nested clip = UsedByNonUpgraded"
         )]
        [TestCase(
            nameof(TestResources.Prefab_Timeline_Standalone_WithMaterialProperties_Upgradable),
            nameof(TestResources.Clip_Timeline_Standalone_WithMaterialProperties_Upgradable),
            ExpectedResult = SerializedShaderPropertyUsage.UsedByNonUpgraded,
            TestName = "Timeline with materials using standalone clip = UsedByNonUpgraded"
         )]
        public SerializedShaderPropertyUsage GatherClipsUsageInDependentPrefabs_WhenUsingSpecifiedClipProvider_ReturnsExpectedUsage(
            string prefabName,
            string clipName
        )
        {
            var clipPath =
                (ClipPath)(typeof(TestResources).GetField(clipName, BindingFlags.Public | BindingFlags.Instance)?.GetValue(m_Resources) as AnimationClip);
            Assume.That(clipPath.Path, Is.Not.Null.And.Not.Empty);

            var prefabPath =
                (PrefabPath)(typeof(TestResources).GetField(prefabName, BindingFlags.Public | BindingFlags.Instance)?.GetValue(m_Resources) as GameObject);
            Assume.That(prefabPath.Path, Is.Not.Null.And.Not.Empty);
            var clipPaths = new[] { clipPath };
            AnimationClipUpgrader.GetClipDependencyMappings(clipPaths, new[] { prefabPath }, out var clipDependents, out var prefabDependencies);
            var materialUpgrader = new MaterialUpgrader();
            materialUpgrader.RenameShader(
                m_Resources.Material_Legacy_Upgradable.shader.name, m_Resources.Material_URP.shader.name
            );
            materialUpgrader.RenameColor("_Color", "_BaseColor");
            materialUpgrader.RenameFloat("_MainTex_ST", "_BaseMap_ST");
            var allUpgradePathsToNewShaders = new Dictionary<string, IReadOnlyList<MaterialUpgrader>>
            {
                { m_Resources.Material_URP.shader.name, new[] { materialUpgrader } }
            };

            var clipData = AnimationClipUpgrader.GetAssetDataForClipsFiltered(clipPaths);

            AnimationClipUpgrader.GatherClipsUsageInDependentPrefabs(
                clipDependents,
                prefabDependencies,
                clipData,
                allUpgradePathsToNewShaders,
                upgradePathsUsedByMaterials: null
            );

            return clipData.Values.Single().Usage;
        }
    }
}
