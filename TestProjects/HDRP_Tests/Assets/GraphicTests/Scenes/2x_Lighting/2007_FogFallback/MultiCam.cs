using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class MultiCam : MonoBehaviour
{
    public Vector2Int singleRes = new Vector2Int(320, 180);
    public Vector2Int tiles = Vector2Int.one;

    RenderTexture rt;
    RenderTexture smallRT;

    Camera targetCam;
    HDAdditionalCameraData hdCam;

    public Renderer displayObject;
    public string displayTextureProperty = "_UnlitColorMap";

    public Transform setsParent;
    Transform[] sets;
    public UnityEvent[] modifiers;

    void Start()
    {
        GetSets();
    }

    void Update()
    {
        if (rt == null) SetupRT();
        Render();
    }

    void OnValidate()
    {
        if (singleRes.x < 16) singleRes.x = 16;
        if (singleRes.y < 16) singleRes.y = 16;
        if (tiles.x < 1) tiles.x = 1;
        if (tiles.y < 1) tiles.y = 1;

        SetupRT();
        GetSets();
    }

    void SetupRT()
    {
        RenderTextureDescriptor descriptor = new RenderTextureDescriptor();
        descriptor.width = singleRes.x * tiles.x;
        descriptor.height = singleRes.y * tiles.y;
        descriptor.dimension = TextureDimension.Tex2D;
        descriptor.depthBufferBits = 32;
        descriptor.graphicsFormat = GraphicsFormat.R8G8B8A8_SRGB;
        descriptor.volumeDepth = 1;
        descriptor.msaaSamples = 1;
        descriptor.useMipMap = false;

        RenderTextureDescriptor smallDescriptor = descriptor;
        smallDescriptor.width = singleRes.x;
        smallDescriptor.height = singleRes.y;

        if (rt != null)
#if UNITY_EDITOR
            DestroyImmediate(rt, false);
#else
            Destroy(rt);
#endif

        if (smallRT != null)
#if UNITY_EDITOR
            DestroyImmediate(smallRT, false);
#else
            Destroy(smallRT);
#endif

        rt = new RenderTexture(descriptor);
        smallRT = new RenderTexture(smallDescriptor);
    }

    void Render()
    {
        if (targetCam == null) targetCam = GetComponent<Camera>();
        if (targetCam == null) return;

        if (hdCam == null) hdCam = GetComponent<HDAdditionalCameraData>();

        var previousTargetTexture = targetCam.targetTexture;
        targetCam.targetTexture = smallRT;

        var i = 0;
        for (var y = 0; y < tiles.y; ++y)
        {
            for (var x = 0; x < tiles.x; ++x)
            {
                if ( i < sets.Length ) sets[i].gameObject.SetActive(true);
                if ( i < modifiers.Length ) modifiers[i].Invoke();

                targetCam.Render();

                Graphics.CopyTexture(smallRT, 0, 0, 0, 0, singleRes.x, singleRes.y, rt, 0, 0, x*singleRes.x, (tiles.y-y-1)*singleRes.y);

                if ( i < sets.Length ) sets[i].gameObject.SetActive(false);
                ++i;
            }
        }

        GL.Viewport(new Rect(0f, 0f, 1f, 1f));

        targetCam.targetTexture = previousTargetTexture;

        if (displayObject != null && displayObject.sharedMaterial != null)
        {
            displayObject.sharedMaterial.SetTexture(displayTextureProperty, rt);
            displayObject.transform.localScale = new Vector3( (singleRes.x*tiles.x) * 1f/(singleRes.y*tiles.y) ,1f,1f);
        }
    }

    void GetSets()
    {
        if (setsParent == null)
        {
            sets = new Transform[0];
            return;
        }

        sets = new Transform[setsParent.childCount];
        for (var t = 0; t < setsParent.childCount; ++t)
        {
            sets[t] = setsParent.GetChild(t);
            sets[t].gameObject.SetActive(false);
        }
    }
}
