using UnityEngine;

public class SetResolutionScript : MonoBehaviour
{
    void Awake()
    {
#if UNITY_ANDROID
        Screen.SetResolution(1280, 720, true);
#endif
    }
}
