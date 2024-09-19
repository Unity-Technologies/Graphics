#if UNITY_EDITOR
using UnityEditor;
#endif

using NUnit.Framework;
using UnityEngine;

public class EnableGPUSkinning : MonoBehaviour
{
    private void Start()
    {
        var spriteRenderer = Object.FindAnyObjectByType<SpriteRenderer>();
        Assert.NotNull(spriteRenderer);
    }

#if UNITY_EDITOR
    private void OnEnable()
    {
        PlayerSettings.gpuSkinning = true;
    }

    private void OnDisable()
    {
        PlayerSettings.gpuSkinning = false;
    }
#endif
}
