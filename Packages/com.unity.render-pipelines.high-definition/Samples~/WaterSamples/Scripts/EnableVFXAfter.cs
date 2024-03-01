using System.Collections;
using UnityEngine;
using UnityEngine.VFX;

public class EnableVFXAfter : MonoBehaviour
{
    public int frames = 1;
    VisualEffect vfxComponent = null;

    void Awake()
    {
        vfxComponent = GetComponent<VisualEffect>();
        vfxComponent.enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (vfxComponent.enabled)
            return;

        if(Time.frameCount >= frames)
        {
            vfxComponent.enabled = true;
        }
    }
}
