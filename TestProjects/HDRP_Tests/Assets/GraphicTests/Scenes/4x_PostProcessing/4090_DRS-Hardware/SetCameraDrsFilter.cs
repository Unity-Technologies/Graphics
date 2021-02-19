using UnityEngine;
using UnityEngine.Rendering;

public class SetCameraDrsFilter : MonoBehaviour
{
    public bool EnableDrs = false;
    public DynamicResUpscaleFilter DrsFilter = DynamicResUpscaleFilter.Bilinear;
}
