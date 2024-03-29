#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using Unity.Graphics.Tests;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

public class PreviewTests : MonoBehaviour
{
    [UnityTest]
    public IEnumerator AssetPreviewIsCorrect()
    {
        var threshold = 0.003f;

        yield return AssetPreviewTesting.CompareAssetPreview<Material>(
            "Assets/CommonAssets/Materials/Roofing.mat",
            "Assets/ReferenceImagesBase/Roofing.png",
            threshold);
    }
}
#endif
