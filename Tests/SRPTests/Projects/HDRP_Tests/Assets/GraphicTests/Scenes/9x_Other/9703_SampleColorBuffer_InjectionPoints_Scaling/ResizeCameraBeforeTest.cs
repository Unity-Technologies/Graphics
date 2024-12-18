using UnityEngine;

public class ResizeCameraBeforeTest : MonoBehaviour
{
    public int width;
    public int height;
    public Camera cam;

    public void Resize()
    {
        // Make sure that the resolution is allocated to something else than what is configured in the test
        // It allows to verify that the RTHandleScale is handled correctly in the code.
        RenderTexture rt = new RenderTexture(width, height, 32, UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_SRGB);
        cam.targetTexture = rt;
        cam.Render();
        rt.Release();
    }
}
