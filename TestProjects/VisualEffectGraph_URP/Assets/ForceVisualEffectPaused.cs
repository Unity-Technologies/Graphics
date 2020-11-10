using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

[ExecuteInEditMode]
public class ForceVisualEffectPaused : MonoBehaviour
{
    void Update()
    {
        var visualEffect = gameObject.GetComponent<VisualEffect>();
        visualEffect.pause = true;
    }
}
