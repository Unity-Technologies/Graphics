using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Rendering;

namespace UnityEngine.Rendering.Tests
{
    using TSet = VolumeComponentTestDataSet;

    class VolumeComponentTreeProviderTests
    {
        static class Properties
        {
            [Test(ExpectedResult = true)]
            public static bool ProvidedTreeContainsAllTypeExceptAlreadyContainedInProfile(
                [ValueSource(typeof(TSet), nameof(TSet.volumeComponentTypesArray))] VolumeComponentType[] profileTypes,
                [ValueSource(typeof(TSet), nameof(TSet.volumeComponentArchetypes))] VolumeComponentArchetype treeArchetype
                )
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
        }
    }
}
