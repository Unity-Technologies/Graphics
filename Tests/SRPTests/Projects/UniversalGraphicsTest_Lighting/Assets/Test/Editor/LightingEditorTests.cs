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
}
