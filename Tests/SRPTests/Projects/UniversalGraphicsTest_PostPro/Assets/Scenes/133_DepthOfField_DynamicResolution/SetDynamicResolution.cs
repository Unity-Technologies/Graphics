using UnityEngine;

public class SetDynamicResolution : MonoBehaviour
{
    public TextMesh gaussianText;
    public TextMesh bokehText;
    public float scale = 0.5f;

    private void Start()
    {
        var cameras = FindObjectsOfType<Camera>();
        foreach (var camera in cameras)
            camera.allowDynamicResolution = true;
        ScalableBufferManager.ResizeBuffers(scale, scale);
    }

    private void OnDestroy()
    {
        ScalableBufferManager.ResizeBuffers(1, 1);
    }

    private void Update()
    {
        if (ScalableBufferManager.widthScaleFactor == 1 && ScalableBufferManager.heightScaleFactor == 1)
        {
            gaussianText.text = string.Format("Gaussian\nDynamic Resolution not supported");
            bokehText.text = string.Format("Bokeh\nDynamic Resolution not supported");
        }
        else
        {
            gaussianText.text = string.Format("Gaussian\nDynamic Resolution {0}x/{1}x", ScalableBufferManager.widthScaleFactor, ScalableBufferManager.heightScaleFactor);
            bokehText.text = string.Format("Bokeh\nDynamic Resolution {0}x/{1}x", ScalableBufferManager.widthScaleFactor, ScalableBufferManager.heightScaleFactor);
        }
    }
}
