using System;
using System.Linq;
using FsCheck;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine.Tests;
using Assert = UnityEngine.Assertions.Assert;

namespace UnityEngine.Rendering.Tests
{
    class VolumeComponentTreeProviderTests
    {
        [OneTimeSetUp]
        public static void SetupFixture()
        {
            ArbX.Register();
        }

        [Test]
        public void ProvidedTreeContainsAllTypeExceptAlreadyContainedInProfile()
        {
            bool Property(VolumeComponentType[] profileTypes, VolumeComponentArchetype treeArchetype)
            {
                // create volume profile
                var profile = ScriptableObject.CreateInstance<VolumeProfile>();
                foreach (var type in profileTypes)
                {
                    if (!profile.Has(type.AsType()))
                        profile.Add(type.AsType());
                }

                // Create the editor
                var editor = Editor.CreateEditor(profile);
                var volumeProfileEditor = new VolumeComponentListEditor(editor);
                var treeProvider = new VolumeComponentTreeProvider(profile, volumeProfileEditor, treeArchetype);

                // Create the tree
                using (ListPool<FilterWindow.Element>.Get(out var elements))
                {
                    treeProvider.CreateComponentTree(elements);

                    if (!treeArchetype.GetOrAddPathAndType(out var pathAndType))
                        return false;
                    var expectedTypes = pathAndType.volumeComponentPathAndTypes.Select(p => p.type).Except(profileTypes).ToHashSet();
                    var currentTypes = elements.Where(e => e is VolumeComponentTreeProvider.VolumeComponentElement)
                        .Cast<VolumeComponentTreeProvider.VolumeComponentElement>()
                        .Select(e => e.type)
                        .ToHashSet();

                    // Cleanup resources
                    Object.DestroyImmediate(editor);
                    Object.DestroyImmediate(profile);

                    return expectedTypes.SetEquals(currentTypes);
                }
            }

            Prop.ForAll<VolumeComponentType[], VolumeComponentArchetype>(Property).ContextualQuickCheckThrowOnFailure();
        }
    }
}
