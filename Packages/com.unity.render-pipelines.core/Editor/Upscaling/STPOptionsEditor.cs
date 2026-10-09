using UnityEditor;

/// <summary>
/// Inspector for <see cref="STPOptions"/>.
/// </summary>
[CustomEditor(typeof(STPOptions))]
public class STPOptionsEditor : UpscalerOptionsEditor
{
    /// <inheritdoc/>
    protected override bool showsReactiveMaskGeneration => true;

    /// <inheritdoc/>
    protected override void OnEnable()
    {
        base.OnEnable();
    }

    /// <inheritdoc/>
    protected override void DrawOptions()
    {
    }
}
