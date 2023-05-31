using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

#if UNITY_EDITOR
[UnityEditor.InitializeOnLoad]
#endif
static class GlobalOutlinePass
{
    static GlobalOutlinePass() => RegisterCustomPasses();

    [RuntimeInitializeOnLoadMethod]
    static void RegisterCustomPasses()
    {
        var outline = new Outline
        {
            outlineColor = Color.yellow,
            outlineLayer = LayerMask.GetMask("Global Custom Pass 0"),
            threshold = 0,
            enabled = false,
        };
        CustomPassVolume.RegisterGlobalCustomPass(CustomPassInjectionPoint.BeforePostProcess, outline);

        var outline2 = new Outline
        {
            outlineColor = Color.cyan,
            outlineLayer = LayerMask.GetMask("Global Custom Pass 1"),
            threshold = 0,
            enabled = false,
        };
        CustomPassVolume.RegisterGlobalCustomPass(CustomPassInjectionPoint.AfterPostProcess, outline2);

        var outline3 = new Outline
        {
            outlineColor = Color.gray,
            outlineLayer = LayerMask.GetMask("Global Custom Pass 2"),
            threshold = 0, 
            enabled = false,
        };
        CustomPassVolume.RegisterGlobalCustomPass(CustomPassInjectionPoint.AfterPostProcess, outline3);
    }
}
