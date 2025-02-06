#if UNITY_EDITOR
using UnityEditor;
#endif

using NUnit.Framework;
using UnityEngine;

public class GPUSkinningToggle : MonoBehaviour
{

    public bool useGPUSkinning = true;
    private bool useGPUSkinningSetting;
    private void Start()
    {
        var spriteRenderer = Object.FindAnyObjectByType<SpriteRenderer>();
        Assert.NotNull(spriteRenderer);
    }

#if UNITY_EDITOR
    private void OnEnable()
    {
        useGPUSkinningSetting = PlayerSettings.gpuSkinning;
        PlayerSettings.gpuSkinning = useGPUSkinning;
    }

    private void OnDisable()
    {
        PlayerSettings.gpuSkinning = useGPUSkinningSetting;
    }
#endif
}
