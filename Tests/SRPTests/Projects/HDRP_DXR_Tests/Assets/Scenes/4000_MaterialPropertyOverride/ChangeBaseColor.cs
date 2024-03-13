using UnityEngine;

[ExecuteAlways]
public class ChangeBaseColor : MonoBehaviour
{
    [SerializeField]
    public Color overrideColor;

    private void Start()
    {
        Renderer renderer = this.gameObject.GetComponent<Renderer>();
        MaterialPropertyBlock newProperties = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(newProperties);
        newProperties.SetColor("_BaseColor", overrideColor);
        renderer.SetPropertyBlock(newProperties);
    }
}
