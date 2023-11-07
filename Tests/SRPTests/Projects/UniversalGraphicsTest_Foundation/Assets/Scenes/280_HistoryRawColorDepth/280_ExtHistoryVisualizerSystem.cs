using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// External MonoBehaviour on Plane object that will request access to the ExtHistory type.
public class ExtHistoryVisualizerSystem : MonoBehaviour
{
    // Camera bound to this system.
    public Camera boundCam;

    public HistoryVisualizer.HistoryToVisualize historyToVisualize;

    static void RequestHistoryAccess(IPerFrameHistoryAccessTracker access)
    {
        access?.RequestAccess<RawColorHistory>();
        access?.RequestAccess<RawDepthHistory>();
    }

    public void OnEnable()
    {
        if (boundCam != null && boundCam.TryGetComponent(out UniversalAdditionalCameraData aCamData))
            aCamData.history.OnGatherHistoryRequests += RequestHistoryAccess;
    }

    public void OnDisable()
    {
        if (boundCam != null && boundCam.TryGetComponent(out UniversalAdditionalCameraData aCamData))
            aCamData.history.OnGatherHistoryRequests -= RequestHistoryAccess;
    }

    public void LateUpdate()
    {
        // Get the copied history texture from the camera public interface
        // and set it as the main texture of the plane material.
        if (boundCam != null && boundCam.TryGetComponent(out UniversalAdditionalCameraData aCamData))
        {
            if (gameObject.TryGetComponent<MeshRenderer>(out MeshRenderer renderer))
            {
                RTHandle historyTexture = null;
                switch (historyToVisualize)
                {
                    case HistoryVisualizer.HistoryToVisualize.RawDepth:
                        historyTexture = aCamData.history.GetHistoryForRead<RawDepthHistory>()?.GetCurrentTexture();
                        break;
                    case HistoryVisualizer.HistoryToVisualize.RawColor:
                    default:
                    // In LateUpdate() we know the pipe has executed and the current history texture is ready.
                    historyTexture = aCamData.history.GetHistoryForRead<RawColorHistory>()?.GetCurrentTexture();
                    break;
                }

                if (historyTexture != null)
                {
                    // TextureArray2D can't be assigned to a material _Basemap in URP Unlit shader. _Basemap is Texture2D.
                    // Disable test for VR. Defines make sure that we don't get an error.
                    var dim = historyTexture.rt.dimension;
                    if(dim == TextureDimension.Tex2D)
                        renderer.material.mainTexture = historyTexture;
                }
            }
        }
    }
}
