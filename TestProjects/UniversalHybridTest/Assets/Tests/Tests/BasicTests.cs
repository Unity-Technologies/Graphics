using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace Tests
{
    public class BasicTests
    {
        [Test]
        public void CheckIfBuildProducedUsingBuildConfigurations()
        {
#if UNITY_EDITOR
            Debug.Log("In Editor this test always succeeds, run it in player");
#else
            Debug.Log("Running test in player");
            CheckPlayerBuild();
#endif
        }

        /// <summary>
        /// See Assets\Editor\BuildCustomizer\BuildCustomizer.cs for build customization
        /// This tests checks following features of build configuration pipeline, which wouldn't be available if you would use legacy pipeline:
        /// - Checks if custom define is set when build is produced
        /// - Checks if additional file is placed to Streaming Assets folder
        /// </summary>
        private void CheckPlayerBuild()
        {
            var file = Path.Combine(Application.streamingAssetsPath, "RandomDataFile.txt");
#if !MY_CUSTOM_DEFINE
            Assert.Fail("MY_CUSTOM_DEFINE define was not set, the build wasn't produced using build configurations?");
#endif
            Assert.IsTrue(File.Exists(file), $"Failed to locate '{file}', the build wasn't produced using build configurations?");
        }

    }
}
