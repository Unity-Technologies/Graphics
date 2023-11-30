using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    public class VFXSanitizeTest
    {
        [OneTimeTearDown]
        public void CleanUp()
        {
            VFXTestCommon.DeleteAllTemporaryGraph();
        }

        [UnityTest]
        public IEnumerator Check_SetCustomAttribute_Sanitize()
        {
            // No assert because if there's at least one error message in the console during the asset import+sanitize the test will fail
            var filePath = "Packages/com.unity.testing.visualeffectgraph/scenes/103_Lit.vfxtmp";
            var graph = VFXTestCommon.CopyTemporaryGraph(filePath);
            for (int i = 0; i < 16; i++)
                yield return null;
            Assert.IsNotNull(graph);
        }
    }
}
