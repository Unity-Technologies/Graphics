#if VFX_HAS_TIMELINE
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.VFX.Migration;
using UnityEngine.Timeline;

namespace UnityEngine.VFX
{
    class VisualEffectActivationTrack : TrackAsset
    {
        private void Awake()
        {
            var path = AssetDatabase.GetAssetPath(this);
            ActivationToControlTrack.SanitizeAssetAtPath(path);

        }
    }
}
#endif
#endif
