#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;

[CanEditMultipleObjects]
public class ImposterBaker : MonoBehaviour
{
    [System.Serializable]
    private struct Snapshot
    {
        public Vector3 _position;
        public Vector3 _direction;
    }

    private enum ImposterBakerPass : int
    {
        MinMax = 0,
        AlphaCopy,
        DepthCopy,
        MergeNormalsDepth,
        Dilate,
    }

    private enum UnityShaderPass
    {
        ForwardLit,
        ShadowCaster,
        GBuffer,
        DepthOnly,
        DepthNormals,
        Meta,
    }

    [Header("Settings")]
    [SerializeField] private int _atlasResolution = 2048;
    [SerializeField] private bool _useHalfSphere = true;
    [SerializeField] private int _frames = 12;
    [SerializeField] [Range(0, 256)] private float _framePadding = 0;
    [Header("Baked")]
    [SerializeField] private Vector3 _boundsOffset;
    [SerializeField] private float _boundsRadius;

    [Header("System")]
    // materials + shaders
    [SerializeField] private Shader _imposterBakerShader;
    [SerializeField] private Material _imposterBakerMaterial;
    // rendering data
    [SerializeField] private UnityEngine.Renderer[] _renderers;
    [SerializeField] private Mesh[] _meshes;
    [SerializeField] private Material[][] _materials;
    [SerializeField] private Snapshot[] _snapshots;
    [SerializeField] private Bounds _bounds;

    bool CheckData()
    {
        // get materials/shaders
        if (_imposterBakerShader == null)
        {
            _imposterBakerShader = Shader.Find("IMP/ImposterBaker");
            _imposterBakerMaterial = new Material(_imposterBakerShader);
        }

        // find stuff to bake
        List<MeshRenderer> renderers = new List<MeshRenderer>(transform.GetComponentsInChildren<MeshRenderer>(true));
        renderers.Remove(gameObject.GetComponent<MeshRenderer>());

        _renderers = renderers.ToArray();
        _meshes = new Mesh[_renderers.Length];
        _materials = new Material[_renderers.Length][];
        for (int i = 0; i < _renderers.Length; i++)
        {
            _renderers[i].gameObject.SetActive(false);
            _meshes[i] = _renderers[i].GetComponent<MeshFilter>().sharedMesh;
            _materials[i] = _renderers[i].sharedMaterials;
        }

        // make sure frames are even
        if (_frames % 2 != 0)
            _frames -= 1;

        // make sure min is 2 x 2
        _frames = Mathf.Max(2, _frames);

        //grow bounds, first centered on root transform 
        if (_renderers == null || _renderers.Length == 0)
            return false;

        _bounds = new Bounds(_renderers[0].transform.position, Vector3.zero);
        for (int i = 0; i < _renderers.Length; i++)
        {
            Vector3[] verts = _meshes[i].vertices;
            for (int v = 0; v < verts.Length; v++)
            {
                Vector3 meshWorldVert = _renderers[i].localToWorldMatrix.MultiplyPoint3x4(verts[v]);
                Vector3 meshLocalToRoot = transform.worldToLocalMatrix.MultiplyPoint3x4(meshWorldVert);
                Vector3 worldVert = transform.localToWorldMatrix.MultiplyPoint3x4(meshLocalToRoot);
                _bounds.Encapsulate(worldVert);
            }
        }

        _boundsRadius = Vector3.Distance(_bounds.min, _bounds.max) * 0.5f;
        _boundsOffset = _bounds.center;

        _snapshots = BuildSnapshots(_frames, _boundsRadius, _boundsOffset, _useHalfSphere);

        return true;
    }

    [ContextMenu("Bake")]
    void Bake()
    {
        // fix rotation bug ?
        Vector3 oldPosition = transform.position;
        Quaternion oldRotation = transform.rotation;
        Vector3 oldScale = transform.localScale;

        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one;
        if (CheckData())
        {
            RenderTexture albedoPackRT = RenderTexture.GetTemporary(_atlasResolution, _atlasResolution, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            RenderTexture normalPackRT = RenderTexture.GetTemporary(_atlasResolution, _atlasResolution, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);

            CaptureViews(albedoPackRT, normalPackRT);
            transform.position = oldPosition;
            transform.rotation = oldRotation;
            transform.localScale = oldScale;
            CreateImposterAssets(albedoPackRT, normalPackRT);

            RenderTexture.ReleaseTemporary(albedoPackRT);
            RenderTexture.ReleaseTemporary(normalPackRT);
        }

        transform.position = oldPosition;
        transform.rotation = oldRotation;
        transform.localScale = oldScale;
    }

    void DrawMeshesToTarget(ImposterBakerPass pass)
    {
        for (int i = 0; i < _renderers.Length; i++)
        {
            for (int j = 0; j < _meshes[i].subMeshCount; j++)
            {
                _imposterBakerMaterial.SetPass((int)pass);
                Graphics.DrawMeshNow(_meshes[i], _renderers[i].localToWorldMatrix, j);
            }
        }
    }

    void DrawMeshesToTarget(UnityShaderPass pass)
    {
        for (int i = 0; i < _renderers.Length; i++)
        {
            for (int j = 0; j < _meshes[i].subMeshCount; j++)
            {
                int passIndex = _materials[i][j].FindPass(Enum.GetName(typeof(UnityShaderPass), pass));
                if (passIndex == -1)
                    continue;
                _materials[i][j].SetPass(passIndex);
                Graphics.DrawMeshNow(_meshes[i], _renderers[i].localToWorldMatrix, j);
            }
        }
    }

    void CaptureViews(RenderTexture albedoPackRT, RenderTexture normalPackRT)
    {
        int frameResolution = _atlasResolution / _frames;

        // find better min max frame/boundsRadius size
        {
            RenderTexture minMaxTileRT = RenderTexture.GetTemporary(_atlasResolution, _atlasResolution, 0, RenderTextureFormat.R8, RenderTextureReadWrite.Linear);

            GL.PushMatrix();

            Graphics.SetRenderTarget(minMaxTileRT);
            GL.Clear(true, true, Color.clear);

            for (var i = 0; i < _snapshots.Length; i++)
            {
                Matrix4x4 ortho = Matrix4x4.Ortho(-_boundsRadius, _boundsRadius, -_boundsRadius, _boundsRadius, -_boundsRadius * 2, _boundsRadius * 2);
                GL.LoadProjectionMatrix(ortho);
                Matrix4x4 cameraMatrix = Matrix4x4.TRS(_snapshots[i]._position, Quaternion.LookRotation(_snapshots[i]._direction, Vector3.up), Vector3.one).inverse;
                GL.modelview = cameraMatrix;

                DrawMeshesToTarget(ImposterBakerPass.MinMax);
            }

            GL.PopMatrix();

            //now read render texture
            Texture2D tempMinMaxTex = new Texture2D(_atlasResolution, _atlasResolution, TextureFormat.R8, false);
            RenderTexture.active = minMaxTileRT;
            tempMinMaxTex.ReadPixels(new Rect(0f, 0f, _atlasResolution, _atlasResolution), 0, 0);
            tempMinMaxTex.Apply();

            Color32[] tempTexC = tempMinMaxTex.GetPixels32();

            // start with max min
            Vector2 min = Vector2.one * _atlasResolution;
            Vector2 max = Vector2.zero;

            //loop pixels get min max
            for (int c = 0; c < tempTexC.Length; c++)
            {
                if (tempTexC[c].r != 0x00)
                {
                    Vector2 texPos = Get2DIndex(c, _atlasResolution);
                    min.x = Mathf.Min(min.x, texPos.x);
                    min.y = Mathf.Min(min.y, texPos.y);
                    max.x = Mathf.Max(max.x, texPos.x);
                    max.y = Mathf.Max(max.y, texPos.y);
                }
            }

            // padding
            min -= Vector2.one * _framePadding;
            max += Vector2.one * _framePadding;

            //rescale radius
            Vector2 len = new Vector2(max.x - min.x, max.y - min.y);

            float maxR = Mathf.Max(len.x, len.y);

            float ratio = maxR / _atlasResolution; //assume square

            _boundsRadius = _boundsRadius * ratio;

            //recalculate snapshots
            _snapshots = BuildSnapshots(_frames, _boundsRadius, _boundsOffset, _useHalfSphere);

            RenderTexture.ReleaseTemporary(minMaxTileRT);
        }

        // Render actual tiles
        {
            Shader.EnableKeyword("_RENDER_PASS_ENABLED");
            GL.PushMatrix();
            for (int frameIndex = 0; frameIndex < _snapshots.Length; frameIndex++)
            {
                // current snap
                Snapshot currentSnapshot = _snapshots[frameIndex];

                // setup camera pos to currentSnapshot
                Matrix4x4 ortho = Matrix4x4.Ortho(-_boundsRadius, _boundsRadius, -_boundsRadius, _boundsRadius, 0, _boundsRadius * 2);
                GL.LoadProjectionMatrix(ortho);
                Matrix4x4 cameraMatrix = Matrix4x4.Inverse(Matrix4x4.TRS(currentSnapshot._position, Quaternion.LookRotation(currentSnapshot._direction, Vector3.up), new Vector3(1, 1, -1)));
                GL.modelview = cameraMatrix;

                // set buffers
                RenderTexture[] gBuffer = new RenderTexture[5];
                gBuffer[0] = RenderTexture.GetTemporary(frameResolution, frameResolution, 32, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                gBuffer[1] = RenderTexture.GetTemporary(frameResolution, frameResolution, 0, RenderTextureFormat.R8, RenderTextureReadWrite.Linear);
                gBuffer[2] = RenderTexture.GetTemporary(frameResolution, frameResolution, 32, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
                gBuffer[3] = RenderTexture.GetTemporary(frameResolution, frameResolution, 32, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                gBuffer[4] = RenderTexture.GetTemporary(frameResolution, frameResolution, 32, RenderTextureFormat.R16, RenderTextureReadWrite.Linear);

                // get frame rts
                RenderTexture depthFrameRT = RenderTexture.GetTemporary(frameResolution, frameResolution, 32, RenderTextureFormat.R16, RenderTextureReadWrite.Linear);
                RenderTexture albedoFrameRT = RenderTexture.GetTemporary(frameResolution, frameResolution, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                RenderTexture normalFrameRT = RenderTexture.GetTemporary(frameResolution, frameResolution, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                RenderTexture dilateFrameRT = RenderTexture.GetTemporary(frameResolution, frameResolution, 0, RenderTextureFormat.R8, RenderTextureReadWrite.Linear);

                // clear frame targets
                Graphics.SetRenderTarget(depthFrameRT);
                GL.Clear(true, true, Color.clear);
                Graphics.SetRenderTarget(albedoFrameRT);
                GL.Clear(true, true, Color.clear);
                Graphics.SetRenderTarget(normalFrameRT);
                GL.Clear(true, true, Color.clear);
                Graphics.SetRenderTarget(dilateFrameRT);
                GL.Clear(true, true, Color.clear);

                for (int i = 0; i < gBuffer.Length; i++)
                    gBuffer[i].Create();

                RenderBuffer[] buffers = new RenderBuffer[5];
                for (int i = 0; i < gBuffer.Length; i++)
                    buffers[i] = gBuffer[i].colorBuffer;

                // render gBuffers
                Graphics.SetRenderTarget(buffers, gBuffer[0].depthBuffer);
                GL.Clear(true, true, Color.clear);
                DrawMeshesToTarget(UnityShaderPass.GBuffer);

                // render dilateMask (alpha mask)
                {
                    // send alpha to _dilateMaskRT
                    _imposterBakerMaterial.SetTexture("_AlphaMap", gBuffer[3]);
                    _imposterBakerMaterial.SetVector("_Channels", new Vector4(1, 0, 0, 0));
                    _imposterBakerMaterial.SetPass((int)ImposterBakerPass.AlphaCopy);
                    Graphics.Blit(gBuffer[3], dilateFrameRT, _imposterBakerMaterial, (int)ImposterBakerPass.AlphaCopy);

                    // send alpha to _albedoFrameRT
                    _imposterBakerMaterial.SetTexture("_AlphaMap", gBuffer[3]);
                    _imposterBakerMaterial.SetVector("_Channels", new Vector4(0, 0, 0, 1));
                    _imposterBakerMaterial.SetPass((int)ImposterBakerPass.AlphaCopy);
                    Graphics.Blit(gBuffer[3], albedoFrameRT, _imposterBakerMaterial, (int)ImposterBakerPass.AlphaCopy);
                }

                // render depth
                {
                    _imposterBakerMaterial.SetTexture("_DepthMap", gBuffer[4]);
                    _imposterBakerMaterial.SetVector("_Channels", new Vector4(1, 0, 0, 0));
                    _imposterBakerMaterial.SetPass((int)ImposterBakerPass.DepthCopy);
                    Graphics.Blit(gBuffer[4], depthFrameRT, _imposterBakerMaterial, (int)ImposterBakerPass.DepthCopy);
                }

                // render albedo/color + normals + merge depth
                {
                    // albedo
                    {
                        // dilate albedo
                        _imposterBakerMaterial.SetTexture("_DilateMask", dilateFrameRT);
                        _imposterBakerMaterial.SetTexture("_MainTex", gBuffer[0]);
                        _imposterBakerMaterial.SetVector("_Channels", new Vector4(1, 1, 1, 0));
                        _imposterBakerMaterial.SetPass((int)ImposterBakerPass.Dilate);
                        Graphics.Blit(gBuffer[0], albedoFrameRT, _imposterBakerMaterial, (int)ImposterBakerPass.Dilate);
                    }

                    // normals + depth
                    {
                        // merge normals + depth
                        RenderTexture tempNormalsDepthRT = RenderTexture.GetTemporary(frameResolution, frameResolution, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
                        _imposterBakerMaterial.SetTexture("_NormalMap", gBuffer[2]);
                        _imposterBakerMaterial.SetTexture("_DepthMap", depthFrameRT);
                        _imposterBakerMaterial.SetPass((int)ImposterBakerPass.MergeNormalsDepth);
                        Graphics.Blit(null, tempNormalsDepthRT, _imposterBakerMaterial, (int)ImposterBakerPass.MergeNormalsDepth);

                        // dilate normals & depth
                        _imposterBakerMaterial.SetTexture("_DilateMask", dilateFrameRT);
                        _imposterBakerMaterial.SetTexture("_MainTex", tempNormalsDepthRT);
                        _imposterBakerMaterial.SetVector("_Channels", new Vector4(1, 1, 1, 1));
                        _imposterBakerMaterial.SetPass((int)ImposterBakerPass.Dilate);
                        Graphics.Blit(tempNormalsDepthRT, normalFrameRT, _imposterBakerMaterial, (int)ImposterBakerPass.Dilate);

                        RenderTexture.ReleaseTemporary(tempNormalsDepthRT);
                    }
                }

                // blit to pack rts
                {
                    //convert 1D index to flattened octahedra coordinate
                    int x;
                    int y;
                    //this is 0-(frames-1) ex, 0-(12-1) 0-11 (for 12 x 12 frames)
                    XYFromIndex(frameIndex, _frames, out x, out y);

                    //X Y position to write frame into atlas
                    //this would be frame index * frame width, ex 2048/12 = 170.6 = 170
                    //so 12 * 170 = 2040, loses 8 pixels on the right side of atlas and top of atlas

                    x *= albedoFrameRT.width;
                    y *= albedoFrameRT.height;

                    //copy base frame into base render target
                    Graphics.CopyTexture(albedoFrameRT, 0, 0, 0, 0, albedoFrameRT.width, albedoFrameRT.height, albedoPackRT, 0, 0, x, y);

                    //copy normals frame into normals render target
                    Graphics.CopyTexture(normalFrameRT, 0, 0, 0, 0, normalFrameRT.width, normalFrameRT.height, normalPackRT, 0, 0, x, y);
                }

                // dispose
                RenderTexture.ReleaseTemporary(depthFrameRT);
                RenderTexture.ReleaseTemporary(albedoFrameRT);
                RenderTexture.ReleaseTemporary(normalFrameRT);
                RenderTexture.ReleaseTemporary(dilateFrameRT);

                // dispose
                for (int i = 0; i < gBuffer.Length; i++)
                    RenderTexture.ReleaseTemporary(gBuffer[i]);
            }
            Shader.DisableKeyword("_RENDER_PASS_ENABLED");
            GL.PopMatrix();
        }
    }

    Snapshot[] BuildSnapshots(int frames, float radius, Vector3 origin, bool isHalf = true)
    {
        Snapshot[] snapshots = new Snapshot[frames * frames];

        float framesMinusOne = frames - 1;

        int i = 0;
        for (int y = 0; y < frames; y++)
        {
            for (int x = 0; x < frames; x++)
            {
                Vector2 vec = new Vector2(
                    x / framesMinusOne * 2f - 1f,
                    y / framesMinusOne * 2f - 1f
                );
                Vector3 ray = isHalf ? OctahedralHemisphereCoordToVector(vec) : OctahedralSphereCoordToVector(vec);

                ray = ray.normalized;

                snapshots[i]._position = origin + ray * radius;
                snapshots[i]._direction = -ray;
                i++;
            }
        }

        return snapshots;
    }

    Vector3 OctahedralHemisphereCoordToVector(Vector2 coord)
    {
        coord = new Vector2(coord.x + coord.y, coord.x - coord.y) * 0.5f;
        Vector3 vec = new Vector3(
            coord.x,
            1.0f - Vector2.Dot(Vector2.one,
                new Vector2(Mathf.Abs(coord.x), Mathf.Abs(coord.y))
            ),
            coord.y
        );
        return Vector3.Normalize(vec);
    }

    Vector3 OctahedralSphereCoordToVector(Vector2 f)
    {
        Vector3 n = new Vector3(f.x, 1f - Mathf.Abs(f.x) - Mathf.Abs(f.y), f.y);
        float t = Mathf.Clamp01(-n.y);
        n.x += n.x >= 0f ? -t : t;
        n.z += n.z >= 0f ? -t : t;
        return n;
    }

    Vector2 Get2DIndex(int i, int res)
    {
        float x = i % res;
        float y = (i - x) / res;
        return new Vector2(x, y);
    }

    void XYFromIndex(int index, int dims, out int x, out int y)
    {
        x = index % dims;
        y = (index - x) / dims;
    }

    string WriteTexture(Texture2D tex, string path)
    {
        byte[] bytes = tex.EncodeToPNG();

        string fullPath = path + "/" + tex.name + ".png";
        File.WriteAllBytes(fullPath, bytes);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        return fullPath;
    }

    Mesh CreateImposterMesh()
    {
        var vertices = new[]
        {
            new Vector3(0f, 0.0f, 0f),
            new Vector3(-0.5f, 0.0f, -0.5f),
            new Vector3(0.5f, 0.0f, -0.5f),
            new Vector3(0.5f, 0.0f, 0.5f),
            new Vector3(-0.5f, 0.0f, 0.5f)
        };

        var triangles = new[]
        {
            2, 1, 0,
            3, 2, 0,
            4, 3, 0,
            1, 4, 0
        };

        var uv = new[]
        {
            //UV matched to verts
            new Vector2(0.5f, 0.5f),
            new Vector2(0.0f, 0.0f),
            new Vector2(1.0f, 0.0f),
            new Vector2(1.0f, 1.0f),
            new Vector2(0.0f, 1.0f)
        };

        var normals = new[]
        {
            new Vector3(0f, 1f, 0f),
            new Vector3(0f, 1f, 0f),
            new Vector3(0f, 1f, 0f),
            new Vector3(0f, 1f, 0f),
            new Vector3(0f, 1f, 0f)
        };

        var mesh = new Mesh
        {
            vertices = vertices,
            uv = uv,
            normals = normals,
            tangents = new Vector4[5]
        };
        mesh.SetTriangles(triangles, 0);
        mesh.bounds = new Bounds(Vector3.zero + _boundsOffset, Vector3.one * _boundsRadius * 2f);
        mesh.RecalculateTangents();
        return mesh;
    }

    void CreateImposterAssets(RenderTexture albedoPackRT, RenderTexture normalPackRT)
    {
        // get path
        string assetPath = EditorUtility.SaveFilePanelInProject("Save Imposter Textures", gameObject.name, "", "Select textures save location");
        string directoryPath = Path.GetDirectoryName(assetPath);

        // mesh
        MeshRenderer meshRenderer;
        if (!gameObject.TryGetComponent<MeshRenderer>(out meshRenderer))
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
        MeshFilter meshFilter;
        if (!gameObject.TryGetComponent<MeshFilter>(out meshFilter))
            meshFilter = gameObject.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = CreateImposterMesh();

        // textures
        Texture2D albedoMapTexture = new Texture2D(albedoPackRT.width, albedoPackRT.height, TextureFormat.ARGB32, true, true);
        albedoMapTexture.name = $"{gameObject.name}_ImposterAlbedoMap";
        Graphics.SetRenderTarget(albedoPackRT);
        albedoMapTexture.ReadPixels(new Rect(0f, 0f, albedoPackRT.width, albedoPackRT.height), 0, 0);
        string albedoTexPath = WriteTexture(albedoMapTexture, directoryPath);

        Texture2D normalMapTexture = new Texture2D(normalPackRT.width, normalPackRT.height, TextureFormat.ARGB32, true, true);
        normalMapTexture.name = $"{gameObject.name}_ImposterNormalMap";
        Graphics.SetRenderTarget(normalPackRT);
        normalMapTexture.ReadPixels(new Rect(0f, 0f, normalPackRT.width, normalPackRT.height), 0, 0);
        string normalTexPath = WriteTexture(normalMapTexture, directoryPath);

        TextureImporter albedoTexImporter = AssetImporter.GetAtPath(albedoTexPath) as TextureImporter;
        if (albedoTexImporter != null)
        {
            albedoTexImporter.textureType = TextureImporterType.Default;
            albedoTexImporter.maxTextureSize = _atlasResolution;
            albedoTexImporter.alphaSource = TextureImporterAlphaSource.FromInput;
            albedoTexImporter.alphaIsTransparency = false;
            albedoTexImporter.sRGBTexture = false;
            albedoTexImporter.SaveAndReimport();
        }
        albedoMapTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(albedoTexPath);

        TextureImporter normalTexImporter = AssetImporter.GetAtPath(normalTexPath) as TextureImporter;
        if (normalTexImporter != null)
        {
            normalTexImporter.textureType = TextureImporterType.Default;
            normalTexImporter.maxTextureSize = _atlasResolution;
            normalTexImporter.alphaSource = TextureImporterAlphaSource.FromInput;
            normalTexImporter.alphaIsTransparency = false;
            normalTexImporter.sRGBTexture = false;
            normalTexImporter.SaveAndReimport();
        }
        normalMapTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(normalTexPath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // create material
        Shader imposterShader = Shader.Find("Shader Graphs/Imposter");
        string materialName = $"{gameObject.name}_ImposterMaterial";
        Material imposterMaterial = AssetDatabase.LoadAssetAtPath<Material>(directoryPath + "/" + materialName + ".asset");
        if (imposterMaterial == null)
        {
            imposterMaterial = new Material(imposterShader);
            imposterMaterial.name = materialName;
            AssetDatabase.CreateAsset(imposterMaterial, directoryPath + "/" + imposterMaterial.name + ".asset");
        }
        imposterMaterial.SetTexture("_ImposterAlbedoMap", AssetDatabase.LoadAssetAtPath<Texture2D>(albedoTexPath));
        imposterMaterial.SetTexture("_ImposterNormalMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normalTexPath));
        imposterMaterial.SetFloat("_ImposterIsHalfSphere", _useHalfSphere ? 1 : 0);
        imposterMaterial.SetFloat("_ImposterFrames", _frames);
        imposterMaterial.SetFloat("_ImposterSize", _boundsRadius);
        imposterMaterial.SetVector("_ImposterOffset", _boundsOffset);
        meshRenderer.sharedMaterial = imposterMaterial;

        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
    }
}
#endif