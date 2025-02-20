using UnityEngine;
using UnityEngine.U2D;

public class SpriteColor : MonoBehaviour
{
    void OnWillRenderObject()
    {
        var sr = GetComponent<SpriteRenderer>();
        sr.color = sr.IsSRPBatchingEnabled() ? Color.green : Color.cyan;
    }
}
