using System.Linq;
using NUnit.Framework;
using UnityEditor.Rendering;

namespace UnityEditor.Rendering.Tests
{
    class UpgradeUtility_UnitTests
    {
        [Test]
        public void GetAllUpgradePathsToShaders_WhenUpgraderNotInitialized_DoesNotThrow()
        {
            var upgraders = new[] { new MaterialUpgrader() };

            Assert.DoesNotThrow(() => UpgradeUtility.GetAllUpgradePathsToShaders(upgraders));
        }

        [Test]
        public void GetAllUpgradePathsToShaders_ProducesExpectedMappings()
        {
            var aToA = new MaterialUpgrader(); aToA.RenameShader("oldA", "newA");
            var bToA = new MaterialUpgrader(); bToA.RenameShader("oldB", "newA");
            var xToX = new MaterialUpgrader(); xToX.RenameShader("oldX", "newX");
            var upgraders = new[] { aToA, bToA, xToX };

            var upgradePaths = UpgradeUtility.GetAllUpgradePathsToShaders(upgraders);

            var actualPairs = upgradePaths.SelectMany(kv => kv.Value.Select(v => (kv.Key, v)));
            var expectedPairs = new[]
            {
                ("newA", aToA),
                ("newA", bToA),
                ("newX", xToX)
            };
            Assert.That(actualPairs, Is.EquivalentTo(expectedPairs));
        }
    }
}
