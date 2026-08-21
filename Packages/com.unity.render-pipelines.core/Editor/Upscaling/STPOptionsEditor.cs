#if ENABLE_UPSCALER_FRAMEWORK
using UnityEditor;

[CustomEditor(typeof(STPOptions))]
public class STPOptionsEditor : UpscalerOptionsEditor
{
    protected override bool showsReactiveMaskGeneration => true;

    protected override void OnEnable()
    {
        base.OnEnable();
    }

    protected override void DrawOptions()
    {
    }
}
#endif
