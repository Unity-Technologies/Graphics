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
    [Ignore("https://jira.unity3d.com/browse/UUM-77935")]
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
