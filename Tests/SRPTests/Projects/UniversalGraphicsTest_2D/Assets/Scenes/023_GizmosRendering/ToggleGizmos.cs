using UnityEngine;

#if UNITY_EDITOR
using UnityEditorInternal;
#endif

public class ToggleGizmos : MonoBehaviour
{
    private void OnEnable()
    {
#if UNITY_EDITOR
        InternalEditorUtility.SetShowGizmos(true);
#endif
    }

    private void OnDisable()
    {
#if UNITY_EDITOR
        InternalEditorUtility.SetShowGizmos(false);
#endif
    }
}
