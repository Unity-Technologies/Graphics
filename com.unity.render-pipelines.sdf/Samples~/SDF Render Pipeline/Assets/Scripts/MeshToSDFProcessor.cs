using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
public class MeshToSDFProcessorSettings
{
    public string assetName;

    // Width, Height, Depth of a single voxel
    // Assume uniform scale for each dimension.
    public float voxelSize;


    // The following are optional debug values
    public Material voxelMaterial;
    public Material closestPointMaterial;

    public int startX;
    public int startY;
    public int startZ;
}

public class MeshToSDFProcessorInternalSettings
{
    public MeshToSDFProcessorSettings inputSettings;
    public VoxelFieldDeminsions voxelFieldDimensions;

    public int[] triangles;
    public Vector3[] vertices;
    public Vector3[] normals;

    public DebugParentMarkers debugMarkerData;
}

public class DebugParentMarkers
{
    public Transform root;
    public Transform objectRoot;
    public Transform voxels;
    public Transform closestPoints;
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
        int index = x + m_VoxelFieldDimensions.x * (y + (m_VoxelFieldDimensions.y * z));
        //int index = (z * m_VoxelFieldDimensions.x * m_VoxelFieldDimensions.y) + (y * m_VoxelFieldDimensions.x) + x;
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
    static GameObject s_DebugMarkerRoot = null;

    static bool FindClosestDistanceFromVoxelToMesh(Vector3 voxelPosition, int[] triangles, Vector3[] vertices, Material closestPointMaterial, Transform closestPointDebugMarkerRoot, out float closestDistance)
    {
        closestDistance = float.MaxValue;

        if (triangles == null || triangles.Length <= 0)
            return false;

        if (vertices == null || vertices.Length <= 0)
            return false;

        Vector3 closestPositionOnTriangle = new Vector3();
        int triangleIndexCount = triangles.Length;
        for (int triangleIndex = 0; triangleIndex < triangleIndexCount; triangleIndex += 3)
        {
            Vector3 a = vertices[triangles[triangleIndex]];
            Vector3 b = vertices[triangles[triangleIndex+1]];
            Vector3 c = vertices[triangles[triangleIndex+2]];

            Vector3 positionOnTriangle;
            // TODO: a,b,c instead of a,c,b
            float dist = VoxelUtils.DistanceFromPointToTriangle(a, c, b, voxelPosition, out positionOnTriangle);
            if(dist < closestDistance)
            {
                closestDistance = dist;
                closestPositionOnTriangle = positionOnTriangle;
                //CreateMarker("Closest Point", closestPositionOnTriangle, 0.05f, closestPointMaterial);
            }
        }

        closestDistance = -closestDistance;

        CreateMarker(closestPointDebugMarkerRoot, "Closest Point", closestPositionOnTriangle, 0.05f, closestPointMaterial);

        return true;
    }

    static void CreateMarker(Transform parentTransform, string name, Vector3 position, float scale, Material material)
    {
        if (material == null)
            return;

        GameObject co = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        co.name = name;

        co.transform.localScale = new Vector3(scale, scale, scale);
        co.transform.position = position;
        co.GetComponent<Renderer>().material = material;

        if(parentTransform != null)
            co.transform.parent = parentTransform;
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

    public static DebugParentMarkers InitializeParentDebugMarkers(string assetName, Material voxelMaterial, Material closestPointMaterial)
    {
        DebugParentMarkers markerData = new DebugParentMarkers();

        if (s_DebugMarkerRoot == null)
        {
            s_DebugMarkerRoot = new GameObject();
            s_DebugMarkerRoot.name = "SDF Debug Markers";
            s_DebugMarkerRoot.transform.position = Vector3.zero;
        }

        if (voxelMaterial == null && closestPointMaterial == null)
            return markerData;

        markerData.root = s_DebugMarkerRoot.transform;

        GameObject o = new GameObject(assetName);
        o.transform.position = Vector3.zero;
        o.transform.parent = markerData.root;
        markerData.objectRoot = o.transform;

        if (voxelMaterial != null)
        {
            GameObject v = new GameObject("Voxels");
            v.transform.position = Vector3.zero;
            v.transform.parent = markerData.objectRoot;
            markerData.voxels = v.transform;
        }

        if (closestPointMaterial != null)
        {
            GameObject c = new GameObject("Closest Points");
            c.transform.position = Vector3.zero;
            c.transform.parent = markerData.objectRoot;
            markerData.closestPoints = c.transform;
        }

        return markerData;
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

        settings.debugMarkerData = InitializeParentDebugMarkers(settings.inputSettings.assetName, settings.inputSettings.voxelMaterial, settings.inputSettings.closestPointMaterial);

        VoxelUtils.ComputeVoxelFieldDimensions(settings.inputSettings.voxelSize, mesh.bounds, out settings.voxelFieldDimensions);

        VoxelField voxelField = new VoxelField();
        voxelField.Initialize(settings.voxelFieldDimensions, settings.inputSettings.voxelSize, mesh.bounds);

        // Fill the field with a max float value so that by default,
        // every voxel is infinitly far from a point on the mesh.
        voxelField.Fill(float.MaxValue);

        float voxelSize = settings.inputSettings.voxelSize;
        float halfVoxelSize = voxelSize * 0.5f;

        Vector3 startPosition = mesh.bounds.min;// + new Vector3(0,-voxelSize, 0);

        int voxelCounter = 0;
        int breakPoint = 28;

        for (int z = settings.inputSettings.startZ; z < settings.voxelFieldDimensions.z; ++z)
        {
            for (int y = settings.inputSettings.startY; y < settings.voxelFieldDimensions.y; ++y)
            {
                for (int x = settings.inputSettings.startX; x < settings.voxelFieldDimensions.x; ++x)
                {
                    // The offset will allow us to sample from the center of the voxel.
                    Vector3 offset = new Vector3((x * voxelSize), (y * voxelSize), (z * voxelSize));// + halfVoxelSize);
                    Vector3 currentVoxelPosition = startPosition + offset;

                    float dist;
                    if (FindClosestDistanceFromVoxelToMesh(currentVoxelPosition, settings.triangles, settings.vertices, settings.inputSettings.closestPointMaterial, settings.debugMarkerData.closestPoints, out dist))
                        voxelField.Set(x, y, z, dist);

                    //Debug.Log("Dist = " + dist);

                    //if(dist >= 0.000f)
                    //    CreateMarker("Voxel Point", currentVoxelPosition, 0.1f, settings.inputSettings.voxelMaterial);

                    CreateMarker(settings.debugMarkerData.voxels, "Voxel Point", currentVoxelPosition, 0.1f, settings.inputSettings.voxelMaterial);

                    if (voxelCounter >= breakPoint )
                    {
                        //Debug.Log("Break Point!");
                    }

                    ++voxelCounter;

                    //goto Finish;

                }

            }
        }

        //Finish:

        OutputVoxelField(voxelField);

        return true;
    }
}
