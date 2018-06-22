using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode, RequireComponent(typeof(RawImage))]
public class ReflectionProbeToTexture : MonoBehaviour
{
    [SerializeField] private ReflectionProbe targetProbe;
    [SerializeField] private bool correctGamma = false;
    [SerializeField] private bool boxLayout = false;
    
    private Material blitMat;
    private Texture probeTexture;
    private Texture2D texture;

    private int renderID;

    private void Start()
    {
        //Convert();
    }

    [ContextMenu("Refresh")]
	public void Convert ()
    {
        if (targetProbe != null)
        {
            if (texture != null) DestroyImmediate(texture);

            texture = new Texture2D(targetProbe.resolution * 4, targetProbe.resolution * 3) { wrapMode = TextureWrapMode.Clamp };

            if (blitMat != null) DestroyImmediate(blitMat);
            blitMat = new Material(Shader.Find("Hiddent/HDRP/Tests/TexCubeToTex2D"));
            blitMat.SetFloat("_CorrectGamma", correctGamma ? 1 : 0);
            blitMat.SetFloat("_BoxLayout", boxLayout ? 1 : 0);

            RenderTexture dest = new RenderTexture(texture.width, texture.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);

            while (!targetProbe.IsFinishedRendering(renderID)) { };

            probeTexture = targetProbe.texture;

            Graphics.Blit(probeTexture, dest, blitMat);

            // Readback the rendered texture
            var oldActive = RenderTexture.active;
            RenderTexture.active = dest;
            texture.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
            RenderTexture.active = oldActive;
            texture.Apply();

            GetComponent<RawImage>().texture = texture;
        }
	}

    [ContextMenu("RenderProbe")]
    public void RenderProbe()
    {
        if (targetProbe != null)
        {
            renderID =  targetProbe.RenderProbe();
            StartCoroutine(WaitForRenderRoutine());
        }
    }

    IEnumerator WaitForRenderRoutine()
    {
        while (!targetProbe.IsFinishedRendering(renderID)) yield return null;
        Convert();
    }
}
