using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

class RuntimeProfilerBottleneckUtility
{
    public enum InstanceMode
    {
        InstancedDraw, // Draw directly with Graphics.DrawMeshInstanced()
        GameObject, // Actually create GameObject instances
    }

    bool m_Dirty = false;

    InstanceMode m_Mode;
    public InstanceMode Mode
    {
        private get => m_Mode;
        set
        {
            if (Mode != value)
            {
                m_Mode = value;
                m_Dirty = true;
            }
        }
    }

    Material m_Material;
    public Material Material
    {
        private get => m_Material;
        set
        {
            if (Material != value)
            {
                m_Material = value;
                m_Dirty = true;
            }
        }
    }

    Mesh m_Mesh;
    public Mesh Mesh
    {
        private get => m_Mesh;
        set
        {
            if (Mesh != value)
            {
                m_Mesh = value;
                m_Dirty = true;
            }
        }
    }

    public int InstanceCount { get; set; }
    public int MsToSleep { get; set; }

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

    IEnumerable<IEnumerable<T>> ToChunks<T>(IEnumerable<T> source, int chunkSize)
    {
        while (source.Any())
        {
            yield return source.Take(chunkSize);
            source = source.Skip(chunkSize);
        }
    }

    void Instantiate(Transform parent, int instanceIndex)
    {
        var instance = new GameObject();
        instance.transform.parent = parent;
        instance.transform.position = GetGridPosition(instanceIndex);
        var meshRenderer = instance.gameObject.AddComponent<MeshRenderer>();
        meshRenderer.material = m_Material;
        var meshFilter = instance.gameObject.AddComponent<MeshFilter>();
        meshFilter.mesh = m_Mesh;
        m_Instances.Add(instance.gameObject);
    }

    public void ResetAll()
    {
        int numInstances = m_Instances.Count;
        for (int i = numInstances-1; i >= 0; i--)
        {
            GameObject.DestroyImmediate(m_Instances[i]);
        }

        m_Instances.Clear();
        m_Matrices.Clear();

        m_Dirty = false;
    }

    public void UpdateInstances(Transform parent)
    {
        if (m_Dirty)
            ResetAll();

        if (Mesh == null || Material == null)
            return;

        switch (m_Mode)
        {
            case InstanceMode.GameObject:
            {
                while (m_Instances.Count > InstanceCount)
                {
                    GameObject.DestroyImmediate(m_Instances.Last());
                    m_Instances.RemoveAt(m_Instances.Count - 1);
                }

                while (m_Instances.Count < InstanceCount)
                    Instantiate(parent, m_Instances.Count);

                break;
            }
            case InstanceMode.InstancedDraw:
            {
                while (m_Matrices.Count > InstanceCount)
                    m_Matrices.RemoveAt(m_Matrices.Count - 1);

                while (m_Matrices.Count < InstanceCount)
                    AddTranslationMatrix(m_Matrices.Count);

                break;
            }
        }
    }

    public void Execute(Transform parent)
    {
        if (Mesh == null || Material == null)
            return;

        switch (m_Mode)
        {
            case InstanceMode.InstancedDraw:
            {
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

[ExecuteInEditMode]
public class RuntimeProfilerTester : MonoBehaviour
{
    RuntimeProfilerBottleneckUtility utility = new RuntimeProfilerBottleneckUtility();

    RuntimeProfilerBottleneckUtility.InstanceMode Mode;

    public Material Material;

    public Mesh Mesh;

    [Range(0, 10000)]
    public int InstanceCount;

    public int MsToSleep;

    void OnEnable()
    {
        utility.ResetAll();
    }

    void Update()
    {
        utility.Mode = Mode;
        utility.Material = Material;
        utility.Mesh = Mesh;
        utility.InstanceCount = InstanceCount;
        utility.MsToSleep = MsToSleep;

        utility.UpdateInstances(transform);
        utility.Execute(transform);
    }
}
