using UnityEngine;

public class Disable026_InstancingGPUEvents : MonoBehaviour
{
    void Start()
    {
        // Disable this object on UNITY_EDITOR_OSX due to a not deterministic behavior only reproduced on virtual machines
#if UNITY_EDITOR_OSX
        gameObject.SetActive(false);
#endif
    }
}
