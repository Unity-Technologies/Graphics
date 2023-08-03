//This is for fixing yamato standalone test. Attach this component to MainCamera if standalone test is fine when you run locally but fails on yamato (sub-scene objects are not rendering / not stable)
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class GraphicsTestFix : MonoBehaviour
{
#if !UNITY_EDITOR //wrapper so that it runs on player only

    private GraphicsTestSettingsCustom settings;
    private Camera cam;
    private int count = 0;
    private Texture2D texture;

    void Start()
    {
        cam = GetComponent<Camera>();
        settings = GetComponent<GraphicsTestSettingsCustom>();
        count = 0;
    }

    void Update()
    {
        if (count == settings.WaitFrames - 3)
        {
            Debug.Log(count + " GraphicsTestFix - start callback");
            RenderPipelineManager.endContextRendering += MyRenderFrame;
        }
        count++;
    }

    private void MyRenderFrame(ScriptableRenderContext context, List<Camera> cameras)
    {
        //Make a capture when reached correct frame
        if (texture == null)
        {
            texture = new Texture2D(cam.pixelWidth, cam.pixelHeight, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0, false);
            texture.Apply();
            Debug.Log(count + " GraphicsTestFix - done ReadPixel");
        }

        //Blit captured texture to the screen afterwards, so that the screen stays static
        if (texture != null)
        {
            if (cam.targetTexture != null)
            {
                Graphics.Blit(texture, cam.targetTexture);
                Debug.Log(count + " GraphicsTestFix - done Blit to cam.targetTexture = " + cam.targetTexture.name);
            }
            else
            {
                //cam.targetTexture = null;
                Graphics.Blit(texture, null as RenderTexture);
                //Debug.Log(count+" GraphicsTestFix - blit");
            }
        }
    }

    void OnDisable()
    {
        CleanUp();
    }

    void OnDestroy()
    {
        CleanUp();
    }

    private void CleanUp()
    {
        RenderPipelineManager.endContextRendering -= MyRenderFrame;
    }

#endif
}
