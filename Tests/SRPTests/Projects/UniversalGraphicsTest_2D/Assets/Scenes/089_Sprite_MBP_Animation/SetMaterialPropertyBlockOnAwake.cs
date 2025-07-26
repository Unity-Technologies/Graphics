using UnityEngine;

public class SetMaterialPropertyBlockOnAwake : MonoBehaviour
{
    private static readonly int ColorPropId = Shader.PropertyToID("_Color");
    private static readonly int SaturationPropId = Shader.PropertyToID("_Saturation");

    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color color;
    [SerializeField] private float saturation;

    [SerializeField] private bool overwriteColor;
    [SerializeField] private bool overwriteSaturation;

    private MaterialPropertyBlock _props;

    public void Awake()
    {
        _props ??= new MaterialPropertyBlock();
        spriteRenderer.GetPropertyBlock(_props);
        if (overwriteColor) _props.SetColor(ColorPropId, color);
        if (overwriteSaturation) _props.SetFloat(SaturationPropId, saturation);
        spriteRenderer.SetPropertyBlock(_props);
    }
}