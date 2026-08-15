#if !UNITY_EDITOR_OSX || MAC_FORCE_TESTS
using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    public class VFXNestedEventCycleCompileTest
    {
        const string kParentPath = "Assets/AllTests/Editor/Tests/Repro_UUM_121002_Parent.vfx";

        [UnityTest, Description("Cover UUM-121002")]
        public IEnumerator Nested_Event_Cycle_Compiles_Without_Stack_Overflow()
        {
            // The nested reserved-name event makes the spawner flow cyclic; before the fix this
            // reimport recursed forever and overflowed the compiler stack.
            AssetDatabase.ImportAsset(kParentPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            yield return null;

            var asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(kParentPath);
            Assert.IsNotNull(asset, $"Missing fixture at {kParentPath}");

            // Reaching here means the cycle terminated instead of overflowing the stack.
            var graph = asset.GetResource().GetGraph();
            Assert.IsNotNull(graph, "Cyclic nested-event asset failed to compile.");
        }
    }
}
#endif
