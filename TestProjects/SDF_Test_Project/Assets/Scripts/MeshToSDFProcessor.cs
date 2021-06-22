using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
public class MeshToSDFProcessorSettings
{
    // Width, Height, Depth of a single voxel
    // Assume uniform scale for each dimension.
    public float voxelSize;
}

public class MeshToSDFProcessorInternalSettings
{
    public MeshToSDFProcessorSettings inputSettings;
    public VoxelFieldDeminsions voxelFieldDimensions;

    public int[] triangles;
    public Vector3[] vertices;
    public Vector3[] normals;
}
public class VoxelField
{
    public float[] m_Field;
    private VoxelFieldDeminsions m_VoxelFieldDimensions;
    private float m_VoxelSize;
    private Bounds m_MeshBounds;
    public VoxelFieldDeminsions Dimensions
    {
        get
        {
            return m_VoxelFieldDimensions;
        }
    }
    public float VoxelSize
    {
        get
        {
            return m_VoxelSize;
        }
    }

    public Bounds MeshBounds
    {
        get
        {
            return m_MeshBounds;
        }
    }

    public void Initialize(VoxelFieldDeminsions voxelFieldDimensions, float voxelSize, Bounds meshBounds)
    {
        m_VoxelFieldDimensions = voxelFieldDimensions;
        m_VoxelSize = voxelSize;
        m_MeshBounds = meshBounds;

        m_Field = new float[m_VoxelFieldDimensions.x * m_VoxelFieldDimensions.y * m_VoxelFieldDimensions.z];
    }

    public float Get(int x, int y, int z)
    {
        int index = GetIndex(x, y, z);
        return m_Field[index];
    }

    public void Set(int x, int y, int z, float value)
    {
        int index = GetIndex(x, y, z);
        m_Field[index] = value;
    }

    int GetIndex(int x, int y, int z)
    {
        int index = x + m_VoxelFieldDimensions.x * (y + (m_VoxelFieldDimensions.z * z));
        return index;
    }

    public void Fill(float value)
    {
        for (int z = 0; z < m_VoxelFieldDimensions.z; ++z)
        {
            for (int y = 0; y < m_VoxelFieldDimensions.y; ++y)
            {
                for (int x = 0; x < m_VoxelFieldDimensions.x; ++x)
                {
                    Set(x, y, z, value);
                }
            }
        }
    }
}

public class MeshToSDFProcessor
{
    static bool FindClosestDistanceFromVoxelToMesh(Vector3 voxelPosition, int[] triangles, Vector3[] vertices, out float closestDistance)
    {
        closestDistance = float.MaxValue;

        if (triangles == null || triangles.Length <= 0)
            return false;

        if (vertices == null || vertices.Length <= 0)
            return false;

        int triangleIndexCount = triangles.Length;
        for (int triangleIndex = 0; triangleIndex < triangleIndexCount; triangleIndex += 3)
        {
            Vector3 a = vertices[triangles[triangleIndex]];
            Vector3 b = vertices[triangles[triangleIndex+1]];
            Vector3 c = vertices[triangles[triangleIndex+2]];

            float dist = VoxelUtils.DistanceFromPointToTriangle(a, b, c, voxelPosition);
            if(dist < closestDistance)
            {
                closestDistance = dist;
            }
        }

        return true;
    }

    static void OutputVoxelField(VoxelField voxelField)
    {
        if (voxelField == null)
            return;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine(string.Format("// Voxel Count X Axis = {0}", voxelField.Dimensions.x));
        sb.AppendLine(string.Format("// Voxel Count Y Axis = {0}", voxelField.Dimensions.y));
        sb.AppendLine(string.Format("// Voxel Count Z Axis = {0}", voxelField.Dimensions.z));
        sb.AppendLine(string.Format("// Voxel Size = {0}", voxelField.VoxelSize));
        sb.AppendLine(string.Format("// Mesh Min Bounds Extents = {0}", voxelField.MeshBounds.min));
        sb.AppendLine(string.Format("// Mesh Max Bounds Extents = {0}", voxelField.MeshBounds.max));
        sb.AppendLine(string.Format("float[] voxelField = new float[{0}]", voxelField.m_Field.Length));
        sb.AppendLine("{");

        for(int i = 0; i < voxelField.m_Field.Length; ++i)
        {
            sb.Append(voxelField.m_Field[i] + "f, ");
            if((i+1) % voxelField.Dimensions.x == 0)
            {
                sb.AppendLine();
            }
        }

        sb.AppendLine("};");

        Debug.Log(sb.ToString());
    }

    public static bool Convert(MeshToSDFProcessorSettings inputSettings, MeshFilter meshFilter)
    {
        if (meshFilter == null)
            return false;

        Mesh mesh = meshFilter.sharedMesh;
        if (mesh == null)
            return false;

        MeshToSDFProcessorInternalSettings settings = new MeshToSDFProcessorInternalSettings();
        settings.inputSettings = inputSettings;

        settings.triangles = mesh.triangles;
        settings.vertices = mesh.vertices;
        settings.normals = mesh.normals;

        VoxelUtils.ComputeVoxelFieldDimensions(settings.inputSettings.voxelSize, mesh.bounds, out settings.voxelFieldDimensions);

        VoxelField voxelField = new VoxelField();
        voxelField.Initialize(settings.voxelFieldDimensions, settings.inputSettings.voxelSize, mesh.bounds);

        // Fill the field with a max float value so that by default,
        // every voxel is infinitly far from a point on the mesh.
        voxelField.Fill(float.MaxValue);

        Vector3 startPosition = mesh.bounds.min;
        float voxelSize = settings.inputSettings.voxelSize;
        float halfVoxelSize = voxelSize * 0.5f;
        int voxelCounter = 0;
        int breakPoint = 28;
        for (int z = 0; z < settings.voxelFieldDimensions.z; ++z)
        {
            for (int y = 0; y < settings.voxelFieldDimensions.y; ++y)
            {
                for (int x = 0; x < settings.voxelFieldDimensions.x; ++x)
                {
                    // The offset will allow us to sample from the center of the voxel.
                    Vector3 offset = new Vector3((x * voxelSize) + halfVoxelSize, (y * voxelSize) + halfVoxelSize, (z * voxelSize) + halfVoxelSize);
                    Vector3 currentVoxelPosition = startPosition + offset;

                    float dist;
                    if (FindClosestDistanceFromVoxelToMesh(currentVoxelPosition, settings.triangles, settings.vertices, out dist))
                        voxelField.Set(x, y, z, dist);

                    if(voxelCounter >= breakPoint )
                    {
                        //Debug.Log("Break Point!");
                    }

                    ++voxelCounter;

                }

            }
        }

        OutputVoxelField(voxelField);

        return true;
    }
}
