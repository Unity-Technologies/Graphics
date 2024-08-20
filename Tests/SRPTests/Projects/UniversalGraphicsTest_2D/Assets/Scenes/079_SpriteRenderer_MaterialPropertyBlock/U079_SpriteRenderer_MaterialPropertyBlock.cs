using UnityEngine;

[ExecuteAlways]
public class U079_SpriteRenderer_MaterialPropertyBlock : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void OnEnable()
    {
        SetSRP();
    }

    void Start()
    {
        SetSRP();
    }

    void SetSRP()
    {
        var sr = this.GetComponent<SpriteRenderer>();
        if (sr == null)
            return;
        var mpb = new MaterialPropertyBlock();
        sr.GetPropertyBlock(mpb);
        mpb.SetColor("_TestColor", Color.cyan);
        sr.SetPropertyBlock(mpb);
    }
}
