using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class BottleneckSimulator : MonoBehaviour
{
    public enum InstanceMode
    {
        InstancedDraw,  // Draw directly with Graphics.DrawMeshInstanced()
        GameObject,     // Actually create GameObject instances
    }

    InstanceMode m_Mode;
    public InstanceMode Mode;

    Material m_Material;
    public Material Material;

    Mesh m_Mesh;
    public Mesh Mesh;

    [Range(0, 10000)]
    public int InstanceCount = 16;

    int MsToSleep = 0;

    List<GameObject> m_Instances = new List<GameObject>();
    List<Matrix4x4> m_Matrices = new List<Matrix4x4>();

    Vector3 GetGridPosition(int instanceIndex)
    {
        const int kGridWidth = 64;
        int x = instanceIndex % kGridWidth;
        int z = instanceIndex / kGridWidth;
        Vector3 pos = new Vector3(x, 0, z);
        return pos;
    }

    void AddTranslationMatrix(int instanceIndex)
    {
        var pos = GetGridPosition(instanceIndex);

        Matrix4x4 matrix = Matrix4x4.identity;
        matrix.SetTRS(pos, Quaternion.Euler(Vector3.zero), Vector3.one);

        m_Matrices.Add(matrix);
    }

    void Instantiate(int instanceIndex)
    {
        var instance = new GameObject();
        instance.transform.parent = transform;
        instance.transform.position = GetGridPosition(instanceIndex);
        var meshRenderer = instance.gameObject.AddComponent<MeshRenderer>();
        meshRenderer.material = m_Material;
        var meshFilter = instance.gameObject.AddComponent<MeshFilter>();
        meshFilter.mesh = m_Mesh;
        m_Instances.Add(instance.gameObject);
    }

    void ResetAll()
    {
        m_Matrices.Clear();
        m_Instances.Clear();
        while (transform.childCount > 0)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }
    }

    void OnEnable()
    {
        ResetAll();
    }

    public static IEnumerable<IEnumerable<T>> ToChunks<T>(IEnumerable<T> source, int chunkSize)
    {
        while (source.Any())
        {
            yield return source.Take(chunkSize);
            source = source.Skip(chunkSize);
        }
    }

    void Update()
    {
        // Simulated bottleneck
        /*switch (DebugManager.instance.FrameTimingData.TestRequestedBottleneck)
        {
            case FrameTimingData.PerformanceBottleneck.CPU:
                Mode = InstanceMode.GameObject;
                InstanceCount = 1;
                MsToSleep = 20;
                break;

            case FrameTimingData.PerformanceBottleneck.GPU:
                Mode = InstanceMode.InstancedDraw;
                InstanceCount = 1000;
                MsToSleep = 0;
                break;

            default:
                Mode = InstanceMode.GameObject;
                InstanceCount = 1;
                MsToSleep = 0;
                break;
        }*/

        if (Mesh != m_Mesh)
        {
            m_Mesh = Mesh;
            ResetAll();
        }

        if (Material != m_Material)
        {
            m_Material = Material;
            ResetAll();
        }

        if (Mode != m_Mode)
        {
            m_Mode = Mode;
            ResetAll();
        }
        if (m_Mesh == null || m_Material == null)
            return;

        int instanceCount = InstanceCount;

        switch (m_Mode)
        {
            case InstanceMode.GameObject:
            {
                while (m_Instances.Count > instanceCount)
                {
                    DestroyImmediate(m_Instances.Last());
                    m_Instances.RemoveAt(m_Instances.Count - 1);
                }

                while (m_Instances.Count < instanceCount)
                    Instantiate(m_Instances.Count);

                break;
            }
            case InstanceMode.InstancedDraw:
            {
                while (m_Matrices.Count > instanceCount)
                    m_Matrices.RemoveAt(m_Matrices.Count - 1);

                while (m_Matrices.Count < instanceCount)
                    AddTranslationMatrix(m_Matrices.Count);

                const int kMaxInstancesPerDrawCall = 1023;
                var matrixChunks = ToChunks(m_Matrices, kMaxInstancesPerDrawCall);
                foreach (var chunk in matrixChunks)
                {
                    Graphics.DrawMeshInstanced(m_Mesh, 0, m_Material, chunk.ToList());
                }

                break;
            }
        }

        if (MsToSleep > 0)
            Thread.Sleep(MsToSleep);
    }
}
