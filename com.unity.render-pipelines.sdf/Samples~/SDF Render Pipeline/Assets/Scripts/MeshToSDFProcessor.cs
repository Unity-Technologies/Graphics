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

        VoxelField voxelField = new VoxelField();
        voxelField.Initialize(settings.voxelCountX, settings.voxelCountY, settings.voxelCountZ, settings.inputSettings.voxelSize, mesh.bounds);

        // Fill the field with a max float value so that by default,
        // every voxel is infinitly far from a point on the mesh.
        voxelField.Fill(float.MaxValue);

        float voxelSize = settings.inputSettings.voxelSize;
        float halfVoxelSize = voxelSize * 0.5f;

        Vector3 startPosition = mesh.bounds.min;// + new Vector3(0,-voxelSize, 0);

        int voxelCounter = 0;
        int breakPoint = 28;

        for (int z = settings.inputSettings.startZ; z < settings.voxelCountZ; ++z)
        {
            for (int y = settings.inputSettings.startY; y < settings.voxelCountY; ++y)
            {
                for (int x = settings.inputSettings.startX; x < settings.voxelCountX; ++x)
                {
                    // The offset will allow us to sample from the center of the voxel.
                    Vector3 offset = new Vector3((x * voxelSize), (y * voxelSize), (z * voxelSize));// + halfVoxelSize);
                    Vector3 currentVoxelPosition = startPosition + offset;

                    float dist;
                    Transform closestPointDebugMarkerTransform = settings.debugMarkerData != null ? settings.debugMarkerData.closestPoints : null;
                    if (FindClosestDistanceFromVoxelToMesh(currentVoxelPosition, settings.triangles, settings.vertices, settings.inputSettings.closestPointMaterial, closestPointDebugMarkerTransform, out dist))
                        voxelField.Set(x, y, z, dist);

                    //Debug.Log("Dist = " + dist);

                    //if(dist >= 0.000f)
                    //    CreateMarker("Voxel Point", currentVoxelPosition, 0.1f, settings.inputSettings.voxelMaterial);

                    Transform voxelDebugMarkerTransform = settings.debugMarkerData != null ? settings.debugMarkerData.voxels : null;
                    CreateMarker(voxelDebugMarkerTransform, "Voxel Point", currentVoxelPosition, 0.1f, settings.inputSettings.voxelMaterial);

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

        VoxelFieldIO.Write(settings, voxelField);

        return true;
    }
}
