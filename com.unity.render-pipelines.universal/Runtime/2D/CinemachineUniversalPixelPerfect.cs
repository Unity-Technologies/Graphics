namespace UnityEngine.Experimental.Rendering.Universal
{
    /// <summary>
    /// (Deprecated) An add-on module for Cinemachine Virtual Camera that tweaks the orthographic size
    /// of the virtual camera. It detects the presence of the Pixel Perfect Camera component and use the
    /// settings from that Pixel Perfect Camera to correct the orthographic size so that pixel art
    /// sprites would appear pixel perfect when the virtual camera becomes live.
    /// </summary>
    [AddComponentMenu("")] // Hide in menu
    public class CinemachineUniversalPixelPerfect : MonoBehaviour
    {
        void OnEnable()
        {
            Debug.LogError("CinemachineUniversalPixelPerfect is now deprecated and doesn't function properly. Instead, use the one from Cinemachine v2.4.0 or newer.");
        }
    }
}
