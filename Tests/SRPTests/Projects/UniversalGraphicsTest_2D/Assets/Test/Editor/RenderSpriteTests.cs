using System.Collections.Generic;
using NUnit.Framework;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[TestFixture]
class RenderSpriteTests
{
    GameObject m_BaseObj;
    Camera m_BaseCamera;
    Sprite m_Sprite;
    Material m_Material;
    int m_InstanceCount = 3;

    struct InstanceData
    {
        public Matrix4x4 objectToWorld;
        public Color spriteColor;
    };

    [SetUp]
    public void Setup()
    {
        m_BaseObj = new GameObject();
        m_BaseCamera = m_BaseObj.AddComponent<Camera>();
        m_BaseCamera.transform.position = new Vector3(0, 0, -10);
        var baseCameraData = m_BaseObj.AddComponent<UniversalAdditionalCameraData>();

        m_BaseCamera.allowHDR = false;
        baseCameraData.SetRenderer(2);    // 2D Renderer. See the list of Renderers in CommonAssets/UniversalRPAsset.
        baseCameraData.renderType = CameraRenderType.Base;
        baseCameraData.renderPostProcessing = false;
        m_BaseCamera.targetTexture = new RenderTexture(m_BaseCamera.pixelWidth, m_BaseCamera.pixelHeight, 24);

        m_Sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Packages/com.unity.2d.sprite/Editor/ObjectMenuCreation/DefaultAssets/Textures/v2/Square.png");

        if (GraphicsSettings.TryGetRenderPipelineSettings<Renderer2DResources>(out var resources))
            m_Material = resources.defaultLitMaterial;
        else
            m_Material = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat");
    }

    [TearDown]
    public void Cleanup()
    {
        Object.DestroyImmediate(m_BaseObj);
    }

    void OnRenderSpriteSingle(ScriptableRenderContext context, Camera camera)
    {
        m_Material.enableInstancing = false;
        var renderParams = new RenderParams(m_Material);
        var spriteParams = new SpriteParams(m_Sprite);

        Graphics.RenderSprite(renderParams, spriteParams, 0, Matrix4x4.identity);
    }

    [Test]
    public void RenderSprite_Single()
    {
        RenderPipelineManager.beginCameraRendering += OnRenderSpriteSingle;

        m_BaseCamera.Render();

        RenderPipelineManager.beginCameraRendering -= OnRenderSpriteSingle;
    }

    void OnRenderSpriteInstanced_Managed(ScriptableRenderContext context, Camera camera)
    {
        InstanceData[] instanceData = new InstanceData[m_InstanceCount];

        for (int i = 0; i < instanceData.Length; i++)
        {
            instanceData[i].objectToWorld = Matrix4x4.TRS(Vector3.zero + new Vector3(1 * i, 0, 0), Quaternion.identity, Vector3.one);
            instanceData[i].spriteColor = new Color(Random.Range(0, 1f), Random.Range(0, 1f), Random.Range(0, 1f), 1);
        }

        m_Material.enableInstancing = true;
        var renderParams = new RenderParams(m_Material);
        var spriteParams = new SpriteParams(m_Sprite);

        Graphics.RenderSpriteInstanced(renderParams, spriteParams, 0, instanceData);
    }

    [Test]
    public void RenderSpriteInstanced_Managed()
    {
        RenderPipelineManager.beginCameraRendering += OnRenderSpriteInstanced_Managed;

        m_BaseCamera.Render();

        RenderPipelineManager.beginCameraRendering -= OnRenderSpriteInstanced_Managed;
    }

    void OnRenderSpriteInstanced_List(ScriptableRenderContext context, Camera camera)
    {
        List<InstanceData> instanceData = new List<InstanceData>(m_InstanceCount);

        for (int i = 0; i < m_InstanceCount; i++)
        {
            InstanceData data;
            data.objectToWorld = Matrix4x4.TRS(Vector3.zero + new Vector3(1 * i, 0, 0), Quaternion.identity, Vector3.one);
            data.spriteColor = new Color(Random.Range(0, 1f), Random.Range(0, 1f), Random.Range(0, 1f), 1);

            instanceData.Add(data);
        }

        m_Material.enableInstancing = true;
        var renderParams = new RenderParams(m_Material);
        var spriteParams = new SpriteParams(m_Sprite);

        Graphics.RenderSpriteInstanced(renderParams, spriteParams, 0, instanceData);
    }

    [Test]
    public void RenderSpriteInstanced_List()
    {
        RenderPipelineManager.beginCameraRendering += OnRenderSpriteInstanced_List;

        m_BaseCamera.Render();

        RenderPipelineManager.beginCameraRendering -= OnRenderSpriteInstanced_List;
    }


    void OnRenderSpriteInstanced_NativeArray(ScriptableRenderContext context, Camera camera)
    {
        NativeArray<InstanceData> instanceData = new NativeArray<InstanceData>(m_InstanceCount, Allocator.Temp);

        for (int i = 0; i < instanceData.Length; i++)
        {
            InstanceData data;
            data.objectToWorld = Matrix4x4.TRS(Vector3.zero + new Vector3(1 * i, 0, 0), Quaternion.identity, Vector3.one);
            data.spriteColor = new Color(Random.Range(0, 1f), Random.Range(0, 1f), Random.Range(0, 1f), 1);

            instanceData[i] = data;
        }

        m_Material.enableInstancing = true;
        var renderParams = new RenderParams(m_Material);
        var spriteParams = new SpriteParams(m_Sprite);

        Graphics.RenderSpriteInstanced(renderParams, spriteParams, 0, instanceData);
    }

    [Test]
    public void RenderSpriteInstanced_NativeArray()
    {
        RenderPipelineManager.beginCameraRendering += OnRenderSpriteInstanced_NativeArray;

        m_BaseCamera.Render();

        RenderPipelineManager.beginCameraRendering -= OnRenderSpriteInstanced_NativeArray;
    }

    void OnRenderSpriteInstanced_ReadOnlySpan(ScriptableRenderContext context, Camera camera)
    {
        InstanceData[] instanceData = new InstanceData[m_InstanceCount];

        for (int i = 0; i < instanceData.Length; i++)
        {
            instanceData[i].objectToWorld = Matrix4x4.TRS(Vector3.zero + new Vector3(1 * i, 0, 0), Quaternion.identity, Vector3.one);
            instanceData[i].spriteColor = new Color(Random.Range(0, 1f), Random.Range(0, 1f), Random.Range(0, 1f), 1);
        }

        var spanData = new System.ReadOnlySpan<InstanceData>(instanceData);

        m_Material.enableInstancing = true;
        var renderParams = new RenderParams(m_Material);
        var spriteParams = new SpriteParams(m_Sprite);

        Graphics.RenderSpriteInstanced(renderParams, spriteParams, 0, spanData);
    }

    [Test]
    public void RenderSpriteInstanced_ReadOnlySpan()
    {
        RenderPipelineManager.beginCameraRendering += OnRenderSpriteInstanced_ReadOnlySpan;

        m_BaseCamera.Render();

        RenderPipelineManager.beginCameraRendering -= OnRenderSpriteInstanced_ReadOnlySpan;
    }

}
