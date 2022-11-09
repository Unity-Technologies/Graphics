using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public class TiledRendering : MonoBehaviour
{
    public Camera m_Camera;
    public Volume m_Volume;
    private int tileCount = 2;
    private int spp = -1;
    public Material material;
    public RenderTexture tiledTexture;
    public RenderTexture outputTexture;
    
    private float spread = 0.5f;
    private int width = 512;
    private int height = 512;
    
    List<Vector2> tiles;
    
    private PathTracing pt;
    private VolumeProfile profile;
    Matrix4x4 initialProjectionMatrix;

    private bool runNow = true;
    
    // Start is called before the first frame update
    void Start()
    {

        profile = m_Volume.sharedProfile;
        if (!profile.TryGet<PathTracing>(out var pt))
        {
            pt = profile.Add<PathTracing>(false);
        }
        
        spp = pt.maximumSamples.value;
        
        initialProjectionMatrix = m_Camera.projectionMatrix;
        m_Camera.targetTexture = tiledTexture;
        m_Camera.GetComponent<HDAdditionalCameraData>().antialiasing = HDAdditionalCameraData.AntialiasingMode.None;
        m_Camera.GetComponent<HDAdditionalCameraData>().dithering = false;
        m_Camera.useJitteredProjectionMatrixForTransparentRendering = false;

        tiles = new List<Vector2>();
        for(int i=0; i<tileCount; i++)
        {
            for(int j=0; j<tileCount; j++)
            {
                tiles.Add(new Vector2(i,j));
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        var hdPipeline = RenderPipelineManager.currentPipeline as HDRenderPipeline;
        
        if (runNow && hdPipeline != null)
        {
            if (!profile.TryGet<PathTracing>(out var pt))
            {
                pt = profile.Add<PathTracing>(false);
            }
            
            var hdCamera = HDCamera.GetOrCreate(m_Camera);

            foreach (var tile in tiles)
            {
 
                m_Camera.pixelRect = new Rect(tile.x / tileCount, tile.y / tileCount, width / tileCount, height / tileCount);
                var dx = width / (tileCount);
                var dy = height / (tileCount);

                var projectionMatrix = initialProjectionMatrix;
                projectionMatrix.m02 -= 2 * (tile.x + spread) / width;
                projectionMatrix.m12 -= 2 * (tile.y + spread) / height;
                m_Camera.pixelRect = new Rect(tile.x * dx, tile.y * dy, dx, dy);
                m_Camera.projectionMatrix = projectionMatrix;

                // path tracing needs an additional tile setup
                pt.tilingParameters.overrideState = true;
                pt.tilingParameters.value = new Vector4(tileCount, tileCount, tile.x, tile.y);
                
                
    
                for(int i=0; i<(spp+2); ++i)
                {
                    m_Camera.Render();
                }
                
                hdCamera.Reset();
                hdPipeline?.ResetPathTracing(hdCamera);
                    
            }
            
            runNow = false;
            
            //Uncomment if need to save the texture on disk
            //SaveTexture(m_Camera.targetTexture, "tiled");
            
            material.SetTexture("_MainTex", m_Camera.targetTexture);
            material.SetInt("_TileCount", tileCount);
            material.SetFloat("_Spread", spread);
            material.SetColor("_Color", Color.white);

            Graphics.Blit(m_Camera.targetTexture, outputTexture, material);
            
            //Uncomment if need to save the texture on disk
            //SaveTexture(outputTexture, "result");
        }
        
        
    }
    
    public void SaveTexture (RenderTexture rt, string name) {
        
        string path = Application.dataPath+"/";
        bool folderExists = Directory.Exists(path);
        if(!folderExists)
        {
            System.IO.Directory.CreateDirectory(path);
        }
        
        path += name+".png";
        
        Debug.Log(path);
        
        byte[] bytes = toTexture2D(rt).EncodeToPNG();
        System.IO.File.WriteAllBytes(path, bytes);
    }
    
    Texture2D toTexture2D(RenderTexture rTex)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        RenderTexture.active = rTex;
        tex.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
        tex.Apply();
        return tex;
    }
    
}
