using System.Collections;
using Unity.Graphics.Tests;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

public class PreviewTests : MonoBehaviour
{
    [UnityTest]
    public IEnumerator AssetPreviewIsCorrect()
    {
        EditorApplication.EnterPlaymode();
        yield return new WaitForDomainReload(); // Avoid errors on domain reload
        while (!EditorApplication.isPlaying)
            yield return null;
        var threshold = 0.003f;

        yield return AssetPreviewTesting.CompareAssetPreview<Material>(
            "Assets/CommonAssets/Materials/Roofing.mat",
            "Assets/ReferenceImagesBase/Roofing.png",
            threshold);

        EditorApplication.ExitPlaymode();
    }
}
