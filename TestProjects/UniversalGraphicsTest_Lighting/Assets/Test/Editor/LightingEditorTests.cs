using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

class LightingEditorTests
{
    const string kProjectName = "Lighting";

    [Test]
    public void AllRenderersPostProcessingDisabled()
    {
        UniversalProjectAssert.AllRenderersPostProcessing(kProjectName, expectDisabled: true);
    }

    [Test]
    public void AllUrpAssetsHaveMixedLightingEnabled()
    {
        UniversalProjectAssert.AllUrpAssetsHaveMixedLighting(kProjectName, expectDisabled: false);
    }

    [Test]
    public void EnsureSingleQualityOption()
    {
        // TODO: Ideally we only want 1 quality setting, but we need to have a second quality setting for Light Layer test.
        Assert.IsTrue(QualitySettings.names?.Length == 2, $"{kProjectName} project MUST have ONLY single quality setting to ensure test consistency!!!");

        //TODO: Test that the "Ultra" quality setting is the first == default for consistent results
        {
            // A non-allocating string compare
            bool isSame = true;
            {
                var r = "Ultra";
                var s = QualitySettings.names[0];
                var len = s.Length < r.Length ? s.Length : r.Length;
                for (int i = 0; i < len; i++)
                    if (s[i] != r[i])
                    {
                        isSame = false;
                        break;
                    }
            }
            Assert.IsTrue(isSame, $"{kProjectName} project MUST have 'Ultra' quality setting as first (default) to ensure test consistency!!!");
        }

    }
}
