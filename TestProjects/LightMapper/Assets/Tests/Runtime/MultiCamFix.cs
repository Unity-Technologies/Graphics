using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Need to turn off HDR/MSAA on mainCam

public class MultiCamFix : MonoBehaviour
{
    private Camera cam;
    private Texture2D tex2d;
    private Vector2Int size;
    private GraphicsTestSettingsCustom settings;
    private int count = 0;

    void Start()
    {
        cam = GetComponent<Camera>();
        settings = GetComponent<GraphicsTestSettingsCustom>();
        count = 0;
    }

    void OnPostRender()
    {
        if (count == settings.WaitFrames - 1)
        {
            //Create Texture
            //size = new Vector2Int(Screen.width,Screen.height);//new Vector2Int(rt.width,rt.height);
            size = new Vector2Int(cam.pixelWidth, cam.pixelHeight);
            // Debug.Log(cam.pixelWidth);
            // Debug.Log(Screen.width);
            tex2d = new Texture2D(size.x, size.y, TextureFormat.RGB24, false);

            //Read screen pixel
            tex2d.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0, false);
            tex2d.Apply();
        }
        else if (count > settings.WaitFrames - 1)
        {
            if (cam.targetTexture != null)
            {
                Graphics.Blit(tex2d, cam.targetTexture);
                //This will make the GraphicsTest result contains contents of multi-cam
            }
        }

        count++;

    }
}
