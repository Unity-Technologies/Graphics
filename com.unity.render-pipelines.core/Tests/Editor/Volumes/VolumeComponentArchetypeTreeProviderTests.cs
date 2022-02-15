using System;
using System.Linq;
using NUnit.Framework;

namespace UnityEngine.Rendering.Tests
{
    using TSet = VolumeComponentTestDataSet;

    public class VolumeComponentArchetypeTreeProviderTests
    {
        static class Properties
        {
            public static bool TreeContainsAllTypes(
                VolumeComponentType[] types
                )
            {
                var archetype = VolumeComponentArchetype.FromTypes(types);
                if (!archetype.GetOrAddTreeProvider(out var treeProvider))
                    return false;
                if (!archetype.GetPathAndType(out var pathAndType))
                    return false;

                using (HashSetPool<VolumeComponentType>.Get(out var treeTypes))
                using (HashSetPool<VolumeComponentType>.Get(out var givenTypes))
                {
                    void Traverse(VolumeComponentArchetypeTreeProvider.PathNode node)
                    {
                        foreach (var child in node.nodes)
                        {
                            Traverse(child);
                            if (child.type.AsType() != null)
                                treeTypes.Add(child.type);
                        }
                    }

                    Traverse(treeProvider.root);
                    givenTypes.UnionWith(pathAndType.volumeComponentPathAndTypes.Select(p => p.type));

                    return givenTypes.SetEquals(treeTypes);
                }
            }

            public static bool TreePathAreCorrect(
                VolumeComponentType[] types
                )
            {
                var archetype = VolumeComponentArchetype.FromTypes(types);
                if (!archetype.GetOrAddTreeProvider(out var treeProvider))
                    return false;
                if (!archetype.GetPathAndType(out var pathAndType))
                    return false;

                using (HashSetPool<VolumeComponentType>.Get(out var treeTypes))
                using (DictionaryPool<VolumeComponentType, string>.Get(out var typeToPath))
                {
                    foreach (var (path, type) in pathAndType.volumeComponentPathAndTypes)
                    {
                        typeToPath.Add(type, path);
                    }

                    bool Traverse(VolumeComponentArchetypeTreeProvider.PathNode node, string path)
                    {
                        foreach (var child in node.nodes)
                        {
                            var childPath = string.IsNullOrEmpty(path) ? child.name : $"{path}/{child.name}";
                            if (child.type.AsType() != null)
                            {
                                if (!typeToPath.TryGetValue(child.type, out var expectedPath))
                                    // expectedPath should be here, something was wrong with PathAndType extension
                                    return false;

                                if (childPath != expectedPath)
                                    return false;
                            }

                            if (!Traverse(child, childPath))
                                return false;
                        }

                        return true;
                    }

                    return Traverse(treeProvider.root, null);
                }
            }
        }

        [Test]
        public void TreeContainsAllTypesProperty(
            [ValueSource(typeof(TSet), nameof(TSet.volumeComponentTypesArray))]
            VolumeComponentType[] types
        ) => Assert.True(Properties.TreeContainsAllTypes(types));

        [Test]
        public void TreePathAreCorrectProperty(
            [ValueSource(typeof(TSet), nameof(TSet.volumeComponentTypesArray))]
            VolumeComponentType[] types
        ) => Assert.True(Properties.TreePathAreCorrect(types));
    }
}
