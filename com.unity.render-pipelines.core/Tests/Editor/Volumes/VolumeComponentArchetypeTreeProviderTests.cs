using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using FsCheck;
using NUnit.Framework;
using UnityEngine.Tests;
using Assert = UnityEngine.Assertions.Assert;

namespace UnityEngine.Rendering.Tests
{
    public class VolumeComponentArchetypeTreeProviderTests
    {
        [OneTimeSetUp]
        public static void SetupFixture()
        {
            ArbX.Register();
        }

        [Test]
        public void TreeContainsAllTypes()
        {
            bool Property(VolumeComponentType[] types)
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

            Prop.ForAll<VolumeComponentType[]>(Property).ContextualQuickCheckThrowOnFailure();
        }



        [Test]
        public void TreePathAreCorrect()
        {
            bool Property(VolumeComponentType[] types)
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

            Prop.ForAll<VolumeComponentType[]>(Property).ContextualQuickCheckThrowOnFailure();
        }
    }
}
