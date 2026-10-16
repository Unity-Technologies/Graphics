using System;
using System.Collections;
using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

unsafe class BatchRendererGroupFilteredPickingTests
{
    const int k_InstanceCount = 2;
    const int k_WarmUpFrameCount = 10;
    const int k_SizeOfPackedMatrix = 48;
    const int k_ObjectToWorldByteOffset = 96;
    const int k_WorldToObjectByteOffset = k_ObjectToWorldByteOffset + k_InstanceCount * k_SizeOfPackedMatrix;
    const int k_InstanceDataByteSize = k_WorldToObjectByteOffset + k_InstanceCount * k_SizeOfPackedMatrix;

    static readonly Vector3 k_FrontAndBackOverlap = new Vector3(-0.125f, 0f, 0f);
    static readonly Vector3 k_BackOnly = new Vector3(0.5f, 0f, 0f);

    struct PackedMatrix
    {
        public float c0, c1, c2, c3, c4, c5, c6, c7, c8, c9, c10, c11;

        public PackedMatrix(Matrix4x4 m)
        {
            c0 = m.m00; c1 = m.m10; c2 = m.m20;
            c3 = m.m01; c4 = m.m11; c5 = m.m21;
            c6 = m.m02; c7 = m.m12; c8 = m.m22;
            c9 = m.m03; c10 = m.m13; c11 = m.m23;
        }
    }

    SceneView m_SceneView;
    bool m_PreviousAllowAsyncCompilation;

    GameObject m_Front;
    GameObject m_Back;

    BatchRendererGroup m_BatchRendererGroup;
    GraphicsBuffer m_InstanceData;
    Material m_Material;
    Material m_PickingMaterial;
    BatchID m_BatchID;
    BatchMeshID m_MeshID;
    BatchMaterialID m_MaterialID;

    [SetUp]
    public void SetUp()
    {
        m_PreviousAllowAsyncCompilation = ShaderUtil.allowAsyncCompilation;
        ShaderUtil.allowAsyncCompilation = false;

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        m_Front = new GameObject("Front");
        m_Front.transform.position = new Vector3(-0.5f, 0f, 0f);
        m_Back = new GameObject("Back");
        m_Back.transform.position = new Vector3(0.25f, 0f, 2f);

        m_Material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        m_PickingMaterial = new Material(Shader.Find("Hidden/Universal Render Pipeline/BRGPicking"));

        m_BatchRendererGroup = new BatchRendererGroup(OnPerformCulling, IntPtr.Zero);
        m_BatchRendererGroup.SetEnabledViewTypes(new[] { BatchCullingViewType.Camera, BatchCullingViewType.Picking });
        m_BatchRendererGroup.SetPickingMaterial(m_PickingMaterial);
        m_MeshID = m_BatchRendererGroup.RegisterMesh(Resources.GetBuiltinResource<Mesh>("Cube.fbx"));
        m_MaterialID = m_BatchRendererGroup.RegisterMaterial(m_Material);

        m_InstanceData = new GraphicsBuffer(GraphicsBuffer.Target.Raw, k_InstanceDataByteSize / sizeof(int), sizeof(int));
        m_InstanceData.SetData(new[] { Matrix4x4.zero }, 0, 0, 1);
        var objectToWorld = new[] { new PackedMatrix(m_Front.transform.localToWorldMatrix), new PackedMatrix(m_Back.transform.localToWorldMatrix) };
        var worldToObject = new[] { new PackedMatrix(m_Front.transform.worldToLocalMatrix), new PackedMatrix(m_Back.transform.worldToLocalMatrix) };
        m_InstanceData.SetData(objectToWorld, 0, k_ObjectToWorldByteOffset / k_SizeOfPackedMatrix, k_InstanceCount);
        m_InstanceData.SetData(worldToObject, 0, k_WorldToObjectByteOffset / k_SizeOfPackedMatrix, k_InstanceCount);

        var metadata = new NativeArray<MetadataValue>(2, Allocator.Temp);
        metadata[0] = new MetadataValue { NameID = Shader.PropertyToID("unity_ObjectToWorld"), Value = 0x80000000 | k_ObjectToWorldByteOffset };
        metadata[1] = new MetadataValue { NameID = Shader.PropertyToID("unity_WorldToObject"), Value = 0x80000000 | k_WorldToObjectByteOffset };
        m_BatchID = m_BatchRendererGroup.AddBatch(metadata, m_InstanceData.bufferHandle);
        metadata.Dispose();

        m_SceneView = EditorWindow.CreateWindow<SceneView>();
        m_SceneView.position = new Rect(EditorGUIUtility.GetMainWindowPosition().position, new Vector2(512, 512));
        m_SceneView.Focus();
    }

    [TearDown]
    public void TearDown()
    {
        m_SceneView.Close();
        m_SceneView = null;

        m_BatchRendererGroup.Dispose();
        m_InstanceData.Dispose();
        Object.DestroyImmediate(m_PickingMaterial);
        Object.DestroyImmediate(m_Material);

        ShaderUtil.allowAsyncCompilation = m_PreviousAllowAsyncCompilation;
    }

    JobHandle OnPerformCulling(BatchRendererGroup rendererGroup, BatchCullingContext cullingContext, BatchCullingOutput cullingOutput, IntPtr userContext)
    {
        int alignment = UnsafeUtility.AlignOf<long>();
        var output = (BatchCullingOutputDrawCommands*)cullingOutput.drawCommands.GetUnsafePtr();

        output->drawCommands = (BatchDrawCommand*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<BatchDrawCommand>() * k_InstanceCount, alignment, Allocator.TempJob);
        output->drawRanges = (BatchDrawRange*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<BatchDrawRange>(), alignment, Allocator.TempJob);
        output->visibleInstances = (int*)UnsafeUtility.Malloc(sizeof(int) * k_InstanceCount, alignment, Allocator.TempJob);
        output->drawCommandPickingEntityIds = null;
        output->instanceSortingPositions = null;
        output->instanceSortingPositionFloatCount = 0;
        output->drawCommandCount = k_InstanceCount;
        output->drawRangeCount = 1;
        output->visibleInstanceCount = k_InstanceCount;

        for (int i = 0; i < k_InstanceCount; ++i)
        {
            output->visibleInstances[i] = i;
            output->drawCommands[i] = new BatchDrawCommand
            {
                visibleOffset = (uint)i,
                visibleCount = 1,
                batchID = m_BatchID,
                materialID = m_MaterialID,
                meshID = m_MeshID,
                submeshIndex = 0,
                splitVisibilityMask = 0xff,
                flags = BatchDrawCommandFlags.None,
                sortingPosition = 0
            };
        }

        output->drawRanges[0] = new BatchDrawRange
        {
            drawCommandsBegin = 0,
            drawCommandsCount = k_InstanceCount,
            filterSettings = new BatchFilterSettings { renderingLayerMask = 0xffffffff }
        };

        if (cullingContext.viewType == BatchCullingViewType.Picking)
        {
            output->drawCommandPickingEntityIds = (EntityId*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<EntityId>() * k_InstanceCount, alignment, Allocator.TempJob);
            output->drawCommandPickingEntityIds[0] = m_Front.GetEntityId();
            output->drawCommandPickingEntityIds[1] = m_Back.GetEntityId();
        }

        return default;
    }

    GameObject Pick(Vector3 worldPosition, GameObject[] filter)
    {
        GameObject picked = null;

        Action<SceneView> pick = null;
        pick = _ =>
        {
            picked = HandleUtility.PickGameObject(HandleUtility.WorldToGUIPoint(worldPosition), false, null, filter);
            SceneView.duringSceneGui -= pick;
        };
        SceneView.duringSceneGui += pick;
        m_SceneView.SendEvent(EditorGUIUtility.CommandEvent("TestEvent"));

        return picked;
    }

    [UnityTest]
    public IEnumerator PickGameObject_WithFilter_OnlyPicksFilteredBatchRendererGroupDraws()
    {
        m_SceneView.LookAt(Vector3.zero, Quaternion.LookRotation(Vector3.forward, Vector3.up), 2f, true, true);

        for (int i = 0; i < k_WarmUpFrameCount && Pick(k_FrontAndBackOverlap, null) != m_Front; ++i)
        {
            m_SceneView.RepaintImmediately();
            yield return null;
        }

        Assert.AreEqual(m_Front, Pick(k_FrontAndBackOverlap, null), "Unfiltered pick didn't return the front BatchRendererGroup object");
        Assert.AreEqual(m_Front, Pick(k_FrontAndBackOverlap, new[] { m_Front }), "Filtered pick didn't return the filtered BatchRendererGroup object");
        Assert.AreEqual(m_Back, Pick(k_FrontAndBackOverlap, new[] { m_Back }), "Excluded BatchRendererGroup object occluded the filtered one");
        Assert.IsNull(Pick(k_BackOnly, new[] { m_Front }), "Excluded BatchRendererGroup object was picked");
    }
}
