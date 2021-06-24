using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
public class MeshToSDFProcessorSettings
{
    public string outputFilePath;
    public string assetName;

    // Width, Height, Depth of a single voxel
    // Assume uniform scale for each dimension.
    public float voxelSize;

    // Instead of using a uniform voxel grid to sample the
    // mesh, randomly select points anywhere within the 
    // mesh bounds.
    public bool sampleRandomPoints;

    // Should the normals be faceted like a diamond
    // or should they be smooth?
    public bool smoothNormals;

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
    public int voxelCountX;
    public int voxelCountY;
    public int voxelCountZ;

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
public class MeshToSDFProcessor
{
    static GameObject s_DebugMarkerRoot = null;

    public static Vector3 VecMult(Vector3 v, Matrix4x4 mat)
    {
        Vector4 vv = new Vector4(v.x, v.y, v.z, 1.0f);
        vv = mat * vv;

        return new Vector3(vv.x, vv.y, vv.z);
    }

    static bool FindClosestDistanceFromVoxelToMesh(Matrix4x4 localToWorldMat, Vector3 voxelPosition, int[] triangles, Vector3[] vertices, bool smoothNormals, Material closestPointMaterial, Transform closestPointDebugMarkerRoot, out float closestDistance, out Vector3 closestNormal)
    {
        closestDistance = float.MaxValue;
        closestNormal = new Vector3();

        if (triangles == null || triangles.Length <= 0)
            return false;

        if (vertices == null || vertices.Length <= 0)
            return false;

        Vector3 closestPositionOnTriangle = new Vector3();
        int triangleIndexCount = triangles.Length;
        int closestTriangleStartIndex = -1;
        for (int triangleIndex = 0; triangleIndex < triangleIndexCount; triangleIndex += 3)
        {
            Vector3 a = vertices[triangles[triangleIndex + 0]];
            Vector3 b = vertices[triangles[triangleIndex + 1]];
            Vector3 c = vertices[triangles[triangleIndex + 2]];

            Vector3 positionOnTriangle;
            Vector3 normal;
            float dist = VoxelUtils.DistanceFromPointToTriangle(a, b, c, voxelPosition, smoothNormals, out positionOnTriangle, out normal);

            if(Mathf.Abs(dist) < Mathf.Abs(closestDistance))
            {
                closestTriangleStartIndex = triangleIndex;
                closestDistance = dist;
                closestPositionOnTriangle = positionOnTriangle;
                closestNormal = normal;
            }
        }

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
        sb.AppendLine(string.Format("// Voxel Count X Axis = {0}", voxelField.m_VoxelCountX));
        sb.AppendLine(string.Format("// Voxel Count Y Axis = {0}", voxelField.m_VoxelCountY));
        sb.AppendLine(string.Format("// Voxel Count Z Axis = {0}", voxelField.m_VoxelCountZ));
        sb.AppendLine(string.Format("// Voxel Size = {0}", voxelField.VoxelSize));
        sb.AppendLine(string.Format("// Mesh Min Bounds Extents = {0}", voxelField.MeshBounds.min));
        sb.AppendLine(string.Format("// Mesh Max Bounds Extents = {0}", voxelField.MeshBounds.max));
        sb.AppendLine(string.Format("float[] voxelField = new float[{0}]", voxelField.m_Field.Length));
        sb.AppendLine("{");

        for(int i = 0; i < voxelField.m_Field.Length; ++i)
        {
            sb.Append(voxelField.m_Field[i] + "f, ");
            if((i+1) % voxelField.m_VoxelCountX == 0)
            {
                sb.AppendLine();
            }
        }

        sb.AppendLine("};");

        Debug.Log(sb.ToString());
    }

    public static DebugParentMarkers InitializeParentDebugMarkers(string assetName, Material voxelMaterial, Material closestPointMaterial)
    {
        if (voxelMaterial == null && closestPointMaterial == null)
            return null;

        DebugParentMarkers markerData = new DebugParentMarkers();

        s_DebugMarkerRoot = GameObject.Find("SDF Debug Markers");
        if (s_DebugMarkerRoot == null)
        {
            s_DebugMarkerRoot = new GameObject();
            s_DebugMarkerRoot.name = "SDF Debug Markers";
            s_DebugMarkerRoot.transform.position = Vector3.zero;
        }

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

        VoxelUtils.ComputeVoxelFieldDimensions(settings.inputSettings.voxelSize, mesh.bounds, out settings.voxelCountX, out settings.voxelCountY, out settings.voxelCountZ);

        VoxelField voxelField = ScriptableObject.CreateInstance<VoxelField>();
        voxelField.Initialize(settings.voxelCountX, settings.voxelCountY, settings.voxelCountZ, settings.inputSettings.voxelSize, mesh.bounds, settings.inputSettings.sampleRandomPoints, settings.inputSettings.smoothNormals);

        // Fill the field with a max float value so that by default,
        // every voxel is infinitly far from a point on the mesh.
        voxelField.Fill(float.MaxValue);

        float voxelSize = settings.inputSettings.voxelSize;
        float halfVoxelSize = voxelSize * 0.5f;

        Vector3 startPosition = mesh.bounds.min;

        for (int z = settings.inputSettings.startZ; z < settings.voxelCountZ; ++z)
        {
            for (int y = settings.inputSettings.startY; y < settings.voxelCountY; ++y)
            {
                for (int x = settings.inputSettings.startX; x < settings.voxelCountX; ++x)
                {
                    // The offset will allow us to sample from the center of the voxel.
                    Vector3 offset = new Vector3((x * voxelSize), (y * voxelSize), (z * voxelSize));
                    Vector3 currentVoxelPosition = startPosition + offset;

                    if (settings.inputSettings.sampleRandomPoints)
                    {
                        //float offsetX = UnityEngine.Random.Range(mesh.bounds.min.x, mesh.bounds.max.x);
                        //float offsetY = UnityEngine.Random.Range(mesh.bounds.min.y, mesh.bounds.max.y);
                        //float offsetZ = UnityEngine.Random.Range(mesh.bounds.min.z, mesh.bounds.max.z);
                        //offset = new Vector3(offsetX, offsetY, offsetZ);

                        float mx = Mathf.Abs(mesh.bounds.max.x - mesh.bounds.min.x);
                        float my = Mathf.Abs(mesh.bounds.max.y - mesh.bounds.min.y);
                        float mz = Mathf.Abs(mesh.bounds.max.z - mesh.bounds.min.z);

                        // The fudge factor is a hack because not all the models
                        // we have are modeled exactly at the origin (i.e. tekka and man)
                        float fudgeFactor = 0.2f;
                        float halfRadius = Mathf.Max(mx, Mathf.Max(my, mz)) * (0.5f + fudgeFactor);

                        offset = UnityEngine.Random.insideUnitSphere * halfRadius;
                        currentVoxelPosition = offset;
                    }

                    float dist;
                    Vector3 normal;
                    Transform closestPointDebugMarkerTransform = settings.debugMarkerData != null ? settings.debugMarkerData.closestPoints : null;
                    if (FindClosestDistanceFromVoxelToMesh(meshFilter.transform.localToWorldMatrix, currentVoxelPosition, settings.triangles, settings.vertices, settings.inputSettings.smoothNormals, settings.inputSettings.closestPointMaterial, closestPointDebugMarkerTransform, out dist, out normal))
                    {
                        voxelField.Set(x, y, z, dist);
                        voxelField.Set(x, y, z, normal);
                    }

                    Transform voxelDebugMarkerTransform = settings.debugMarkerData != null ? settings.debugMarkerData.voxels : null;
                    CreateMarker(voxelDebugMarkerTransform, "Voxel Point", currentVoxelPosition, 0.1f, settings.inputSettings.voxelMaterial);

                    //goto Finish;

                }

            }
        }

        //Finish:

        OutputVoxelField(voxelField);
        VoxelFieldIO.Write(settings, voxelField);

        return true;
    }
}
