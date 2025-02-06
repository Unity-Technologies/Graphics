using UnityEngine;
using UnityEngine.U2D;

public class TintColor : MonoBehaviour
{
    void OnWillRenderObject()
    {
        var sr = GetComponent<SpriteRenderer>();
        sr.color = sr.IsSRPBatchingEnabled() ? Color.magenta : Color.blue;
    }
}
