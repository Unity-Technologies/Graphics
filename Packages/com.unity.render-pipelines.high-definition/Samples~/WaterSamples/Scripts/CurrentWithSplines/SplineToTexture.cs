using System.IO;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.PackageManager.Requests;
using UnityEditor.PackageManager;
#if SPLINE_PACKAGE_INSTALLED
    using UnityEditor.Splines;
    using UnityEngine.Splines;
#endif

[ExecuteInEditMode]
public class SplineToTexture : MonoBehaviour
{
#if SPLINE_PACKAGE_INSTALLED
    public SplineContainer splineContainer;
#endif
    public ComputeShader computeShader;
    public WaterSurface waterSurface;
    public Material waterDecalMaterial;
    public float splineWidth = 20;
    public float splineBlendWidth = 5;
    public int searchStepsPerCurve = 8;
    public Vector2Int resolution = new Vector2Int(256,256);
    public int currentMapBlurSize = 8;
    public int heightMapBlurSize = 8;
    public Vector2 heightMapMinMax = new Vector2(0, 1);

    public RenderTexture currentMap = null;
    public RenderTexture heightMap = null;

    private int currentHandle = -1;
    private int heightHandle = -1;
    private int blurHandle = -1;
    private int fillWithNeutralHandle = -1;

    private bool isInitialized = false;
#if SPLINE_PACKAGE_INSTALLED
    private Bounds highestBounds = new Bounds();
#endif

    private int curvesCount = 0;
    ComputeBuffer buffer;
    float4[] array;

    void OnEnable()
    {
        Init();
    }

    void HookCallbacks()
    {
#if SPLINE_PACKAGE_INSTALLED
        if (splineContainer != null)
            foreach (Spline spline in splineContainer.Splines)
                EditorSplineUtility.AfterSplineWasModified += OnAfterSplineWasModified;
#endif
    }

    void UnhookCallbacks()
    {
#if SPLINE_PACKAGE_INSTALLED
        if (splineContainer != null)
            foreach (Spline spline in splineContainer.Splines)
                EditorSplineUtility.AfterSplineWasModified -= OnAfterSplineWasModified;
#endif
    }

    void OnDisable()
    {
        isInitialized = false;
        UnhookCallbacks();
    }

#if SPLINE_PACKAGE_INSTALLED
    private void OnAfterSplineWasModified(Spline spline)
    {
        Refresh();
    }
#endif

    void Init()
    {
#if SPLINE_PACKAGE_INSTALLED
        if (splineContainer == null)
            splineContainer = this.GetComponent<SplineContainer>();
#endif

        HookCallbacks();
        InitRenderTexture();
        InitComputeBuffer(GetCurveCount());

        isInitialized = true;

        Refresh();
    }

    private int GetCurveCount()
    {
        // This is to avoid creating a zero length compute buffer when there's no spline package installed. 
        int c = 1;
#if SPLINE_PACKAGE_INSTALLED
        for (int i = 0; i < splineContainer.Splines.Count; i++)
            c += splineContainer[i].GetCurveCount();
#endif
        return c;
    }

    

    private void InitComputeBuffer(int count)
    {
        // *16 because there's 4 vector4 per curve. 
        buffer = new ComputeBuffer(count, sizeof(float) * 16);  
        
        // *4 because there's 4 float4 to define a curve;
        array = new float4[count * 4];   
    }

    private void InitRenderTexture(bool current = true, bool height = true)
    {
        if (current)
        {
            if (currentMap != null)
                currentMap.Release();

            currentMap = new RenderTexture(resolution.x, resolution.y, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            currentMap.enableRandomWrite = true;
            currentMap.Create();
        }


        if (height)
        {
            if (heightMap != null)
                heightMap.Release();

            heightMap = new RenderTexture(resolution.x, resolution.y, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            heightMap.enableRandomWrite = true;
            heightMap.Create();
        }
        
    }

    void Refresh()
    {
        if(!isInitialized) return;

        if (currentMap != null)
        {
            if (currentMap.width != resolution.x || currentMap.height != resolution.y)
                InitRenderTexture(true, false);
        }

        if (heightMap != null)
        {
            if (heightMap.width != resolution.x || heightMap.height != resolution.y)
                InitRenderTexture(false, true);
        }

        UpdateSpline();
        RunCompute();
    }

    void Update()
    {
        waterDecalMaterial?.SetTexture("_CurrentMap", currentMap);
        waterDecalMaterial?.SetTexture("_Deformation_Texture", heightMap);
        
        // Uncomment this to update everyframe
        Refresh();
    }

    private void UpdateSpline()
    {
        int count = GetCurveCount();
        if (count != curvesCount)
        {
            curvesCount = count;
            InitComputeBuffer(curvesCount);
        }
#if SPLINE_PACKAGE_INSTALLED
        int currentIndex = 0;
        // For each spline
        for (int i = 0; i < splineContainer.Splines.Count; i++)
        {
            var spline = splineContainer[i];
            int currentCurveCount = spline.GetCurveCount();
            for (int j = 0; j < currentCurveCount; j++)
            {
                var curve = spline.GetCurve(j);

                array[currentIndex + 0] = (Vector4)(Vector3)curve.P0;
                array[currentIndex + 1] = (Vector4)(Vector3)curve.P1;
                array[currentIndex + 2] = (Vector4)(Vector3)curve.P2;
                array[currentIndex + 3] = (Vector4)(Vector3)curve.P3;
                currentIndex += 4;
            }
            if(buffer == null)
                Debug.Log("buffer is null");
            if(array == null)
                Debug.Log("array is null");

            buffer.SetData(array);
        }

        // This is for heightMap only
        float amplitudeOfTheSteepestSpline = 0;
        highestBounds = new Bounds();
        foreach (Spline spline in splineContainer.Splines)
        {
            Bounds b = spline.GetBounds();

            float amplitude = b.max.y - b.min.y;
            if (amplitude > amplitudeOfTheSteepestSpline)
            {
                amplitudeOfTheSteepestSpline = amplitude;
                highestBounds = b;
            }
        }
#endif
    }

    public void RunCompute()
    {

        if (computeShader != null)
        {
            if(fillWithNeutralHandle < 0)
                fillWithNeutralHandle = computeShader.FindKernel("FillWithNeutral");
            if (currentHandle < 0)
                currentHandle = computeShader.FindKernel("GenerateCurrent");
            if (heightHandle < 0)
                heightHandle = computeShader.FindKernel("GenerateHeight");
            if (blurHandle < 0)
                blurHandle = computeShader.FindKernel("MotionBlur");

            if (currentMap == null || heightMap == null)
                InitRenderTexture();

            // Fill with neutral kernel
            // First, a neutral color is set on each texture, #FF80FF for current map
            computeShader.SetVector("neutralColor", new Vector4(1, 0.5f, 1, 0));
            computeShader.SetTexture(fillWithNeutralHandle, "result", currentMap);
            computeShader.Dispatch(fillWithNeutralHandle, resolution.x / 8, resolution.y / 8, 1);

#if SPLINE_PACKAGE_INSTALLED
            // Generate Current kernel
            // We need to pass all those information to this pass for the compute shader to be able to match the correct position and scale. 
            // Then, for each texture to generate, we iterate through each pixel to find the closest matching point on any of the splines.
            // By evaluating the position and tangent of the closest point, we can calculate a color for the current pixel.

            computeShader.SetVector("WaterSurfaceLocalScale", waterSurface != null ? waterSurface.transform.localScale : Vector3.one);
            computeShader.SetVector("WaterSurfacePosition", waterSurface != null ? waterSurface.transform.position : Vector3.zero);
            computeShader.SetVector("resolution", new Vector4(resolution.x, resolution.y, 0, 0));
            computeShader.SetFloat("curvesCount", curvesCount);
            computeShader.SetFloat("splineWidth", splineWidth);
            computeShader.SetFloat("splineBlendWidth", splineBlendWidth);
            computeShader.SetFloat("stepsPerCurve", searchStepsPerCurve);
            computeShader.SetBuffer(currentHandle, "curvesData", buffer);
            computeShader.SetTexture(currentHandle, "result", currentMap);
            computeShader.Dispatch(currentHandle, resolution.x / 8, resolution.y / 8, 1);


            // Directional Blur kernel
            // Lastly, to avoid blocky artifacts, a blur pass is done in the direction of the current to smooth out the result.
            if (currentMapBlurSize > 0)
            {
                computeShader.SetFloat("blurSize", currentMapBlurSize);
                computeShader.SetTexture(blurHandle, "currentMap", currentMap);
                computeShader.SetTexture(blurHandle, "result", currentMap);
                computeShader.Dispatch(blurHandle, resolution.x / 8, resolution.y / 8, 1);
            }
#endif
            // Fill with neutral kernel
            // First, a neutral color is set on each texture, #808080 for height map map
            computeShader.SetVector("neutralColor", new Vector4(0.5f, 0.5f, 0.5f, 0.5f));
            computeShader.SetTexture(fillWithNeutralHandle, "result", heightMap);
            computeShader.Dispatch(fillWithNeutralHandle, resolution.x / 8, resolution.y / 8, 1);

#if SPLINE_PACKAGE_INSTALLED
            // Generate height kernel
            // We need to pass all those information to this pass for the compute shader to be able to match the correct position and scale. 
            // Then, for each texture to generate, we iterate through each pixel to find the closest matching point on any of the splines.
            // By evaluating the position and tangent of the closest point, we can calculate a color for the current pixel.

            computeShader.SetVector("centerBounds", highestBounds.center);
            computeShader.SetVector("extentsBounds", highestBounds.extents);
            computeShader.SetVector("maxBounds", highestBounds.max);
            computeShader.SetVector("minBounds", highestBounds.min);
            computeShader.SetVector("sizeBounds", highestBounds.size);
            computeShader.SetVector("minMaxHeight", heightMapMinMax);
            computeShader.SetBuffer(heightHandle, "curvesData", buffer);
            computeShader.SetTexture(heightHandle, "result", heightMap);
            computeShader.Dispatch(heightHandle, resolution.x / 8, resolution.y / 8, 1);

            // Directional Blur kernel
            // Lastly, to avoid blocky artifacts, a blur pass is done in the direction of the current to smooth out the result.
            // In this example, the deformation map is not blurred because it specifically needs a steep dropoff at the waterfall.
            if (heightMapBlurSize > 0)
            {
                computeShader.SetFloat("blurSize", heightMapBlurSize);
                computeShader.SetTexture(blurHandle, "currentMap", currentMap);
                computeShader.SetTexture(blurHandle, "result", heightMap);
                computeShader.Dispatch(blurHandle, resolution.x / 8, resolution.y / 8, 1);
            }
#endif
        }
    }
    public void OpenDialogAndSaveCurrentMap()
    {
        var path = EditorUtility.SaveFilePanel("Save current map", "","currentMap","png");
        if (path.Length != 0)
            SaveTextureOnDisk(currentMap, path);
    }

    public void OpenDialogAndSaveHeightMap()
    {
        var path = EditorUtility.SaveFilePanel("Save height map", "", "heightMap", "png");
        if (path.Length != 0)
            SaveTextureOnDisk(heightMap, path);
    }

    public static void SaveTextureOnDisk(RenderTexture renderTexture, string path)
    {
        Texture2D tex = ToTexture2D(renderTexture);
        var bytesHeight = tex.EncodeToPNG();
        File.WriteAllBytes(path, bytesHeight);
        AssetDatabase.Refresh();
    }

    public static Texture2D ToTexture2D(RenderTexture rTex)
    {
        Texture2D tex = new Texture2D(rTex.width, rTex.height, TextureFormat.ARGB32, false, true);
        // ReadPixels looks at the active RenderTexture.
        RenderTexture.active = rTex;
        tex.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
        tex.Apply();
        return tex;
    }

    // This move the spline knots so that the spline gameobject is at the origin with a scale of 1 to simplify the calculations.
    public void ApplyScaleAndPosition()
    {
#if SPLINE_PACKAGE_INSTALLED
        foreach (Spline spline in splineContainer.Splines)
        {

            BezierKnot[] array = spline.ToArray();

            // Apply Scale
            for (int i = 0; i < array.Length; i++)
            {
                array[i].Position *= this.transform.localScale;
                array[i].TangentIn *= this.transform.localScale;
                array[i].TangentOut *= this.transform.localScale;
            }

            // Apply position
            for (int i = 0; i < array.Length; i++)
            {
                array[i].Position += new float3(this.transform.position.x, this.transform.position.y, this.transform.position.z);
            }

            spline.Clear();
            for (int i = 0; i < array.Length; i++)
                spline.Add(array[i]);

        }

        this.transform.position = Vector3.zero;
        this.transform.localScale = Vector3.one;
#endif
    }

    void OnValidate()
    {
        if (splineWidth < 0)
            splineWidth = 0;

        if (splineBlendWidth < 0)
            splineBlendWidth = 0;

        if (searchStepsPerCurve < 0)
            searchStepsPerCurve = 0;

        if (resolution.x < 0)
            resolution.x = 0;

        if (resolution.y < 0)
            resolution.y = 0;

        if (currentMapBlurSize < 0)
            currentMapBlurSize = 0;

        if (heightMapBlurSize < 0)
            heightMapBlurSize = 0;

        if (heightMapBlurSize < 0)
            heightMapBlurSize = 0;

        heightMapMinMax.x = Mathf.Clamp01(heightMapMinMax.x);
        heightMapMinMax.y = Mathf.Clamp01(heightMapMinMax.y);

        if (currentMap != null)
        {
            if (currentMap.width != resolution.x || currentMap.height != resolution.y)
                InitRenderTexture(true, false);
        }

        if (heightMap != null)
        {
            if (heightMap.width != resolution.x || heightMap.height != resolution.y)
                InitRenderTexture(false, true);
        }

        if (isInitialized)
            Refresh();
    }

    // This is to ensure the script is called every update in Edit Mode. 
    void OnDrawGizmos()
    {

#if UNITY_EDITOR
        // Ensure continuous Update calls.
        if (!Application.isPlaying)
        {
            UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
            UnityEditor.SceneView.RepaintAll();
        }
#endif
    }

    void OnDestroy()
    {
        currentMap?.Release();
        heightMap?.Release();
        buffer?.Dispose();

        UnhookCallbacks();
    }
}


