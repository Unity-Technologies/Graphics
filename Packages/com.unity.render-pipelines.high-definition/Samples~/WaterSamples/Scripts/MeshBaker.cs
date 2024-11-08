using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MeshBaker : MonoBehaviour
{
    public Mesh mesh;

    [Range(0, 98)]
    public int sliceIndex = 0;

    [Range(64, 256)]
    public int resolution = 64;

    [Range(0, 0.1f)]
    public float sliceThreshold = 0.0044f;

    // Baked data
    [HideInInspector] public List<float> slicesY;
    [HideInInspector] public Bounds bounds;

    Texture2D result;

    public void Bake()
    {
        Vector3[] vertices = mesh.vertices;

        for (int i = 0; i < vertices.Length; i++)
            vertices[i].y = Mathf.Max(vertices[i].y, 0.0f);

        // Isolate slices & bounds
        slicesY = new List<float>();
        bounds = new Bounds() { min = vertices[0], max = vertices[0] };
        for (int i = 0; i < vertices.Length; i++)
        {
            bounds.Encapsulate(vertices[i]);

            bool found = false;
            for (int j = 0; j < slicesY.Count; j++)
            {
                if (Mathf.Abs(slicesY[j] - vertices[i].z) < sliceThreshold)
                {
                    found = true;
                    break;
                }
            }
            if (!found)
                slicesY.Add(vertices[i].z);
        }

        // Texture
        result = new Texture2D(resolution, slicesY.Count);

        //int idx = sliceIndex;
        for (int idx = 0; idx < slicesY.Count; idx++)
        {
            var slice = GetSlice(idx, out float sliceLength);

            for (int i = 0; i < resolution; i++)
            {
                int j = 0;
                float percentage = i / (float)(resolution - 1);
                for (; j < slice.Count - 1; j++)
                {
                    if (slice[j].dist / sliceLength > percentage)
                        break;
                }

                float startP = slice[j - 1].dist / sliceLength;
                float endP = slice[j].dist / sliceLength;
                Debug.Assert(startP <= percentage && percentage <= endP);
                float lerpFactor = Mathf.InverseLerp(startP, endP, percentage);

                var displacedPos = Vector3.Lerp(slice[j - 1].pos, slice[j].pos, lerpFactor);
                var origPos = new Vector3((1.0f - percentage) * bounds.size.x + bounds.min.x, bounds.min.y, 0.0f);
                var displacement = displacedPos - origPos;
                Debug.Assert(displacement.y >= 0.0f);

                var color = Mathf.Lerp(slice[j - 1].color.r, slice[j].color.r, lerpFactor);

                displacement = new Vector3(displacement.x / (2.0f * bounds.size.x) + 0.5f, displacement.y / bounds.size.y, color);

                result.SetPixel(i, idx, new Color(displacement.x, displacement.y, displacement.z));
            }
        }

        result.Apply();
        var bytes = result.EncodeToPNG();
        string path = Application.dataPath + "/Artifacts/" + mesh.name + ".png";
        File.WriteAllBytes(path, bytes);
        Debug.Log("Texture file written at " + path);
    }

    struct PointWithUV
    {
        public Vector3 pos;
        public Vector2 uv;
        public Color color;
        public float dist;
    }

    List<PointWithUV> GetSlice(int idx, out float sliceLength)
    {
        Vector3[] vertices = mesh.vertices;
        Color[] colors = mesh.colors;
        Vector2[] uvs = mesh.uv;

        // Isolate slice
        List<PointWithUV> slice = new();
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i].y = Mathf.Max(vertices[i].y, 0.0f);

            if (Mathf.Abs(slicesY[idx] - vertices[i].z) >= sliceThreshold)
                continue;

            var pos = vertices[i]; pos.z = 0;
            PointWithUV pointWithUV = new PointWithUV();
            pointWithUV.pos = pos;
            if(i < uvs.Length)
                pointWithUV.uv = uvs[i];
            if(i < colors.Length)
                pointWithUV.color = colors[i];

            slice.Add(pointWithUV);
        }

        slice.Sort((PointWithUV a, PointWithUV b) => {
            return a.uv.x > b.uv.x ? 1 : -1;
        });

        // Compute Length
        sliceLength = 0.0f;
        Vector3 lastPos = slice[0].pos;
        for (int i = 1; i < slice.Count; i++)
        {
            var basePos = slice[i].pos;
            sliceLength += Vector3.Distance(lastPos, basePos);
            lastPos = basePos;

            slice[i] = new PointWithUV
            {
                pos = slice[i].pos,
                uv = slice[i].uv,
                color = slice[i].color,
                dist = sliceLength,
            };
        }

        return slice;
    }


    void OnDrawGizmos()
    {
        if (slicesY == null)
            return;
        sliceIndex = Mathf.Min(sliceIndex, slicesY.Count - 1);

        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position + new Vector3(bounds.min.x, bounds.min.y, 0.0f), new Vector3(bounds.size.x, 0.0f, 0.0f));
        Gizmos.DrawRay(transform.position + new Vector3(bounds.min.x, bounds.min.y, 0.0f), new Vector3(0.0f, bounds.size.y, 0.0f));
        Gizmos.DrawRay(transform.position + new Vector3(bounds.max.x, bounds.max.y, 0.0f), -new Vector3(bounds.size.x, 0.0f, 0.0f));
        Gizmos.DrawRay(transform.position + new Vector3(bounds.max.x, bounds.max.y, 0.0f), -new Vector3(0.0f, bounds.size.y, 0.0f));

        List<PointWithUV> slice = GetSlice(sliceIndex, out float sliceLength);

        var lastPos = slice[0].pos;
        for (int i = 1; i < slice.Count; i++)
        {
            var displacedPos = slice[i].pos;

            float uv = slice[i].dist / sliceLength;
            Gizmos.color = new Color(uv, 0.0f, 0.0f);
            if (uv >= 0.8f && uv <= 0.85f)
                Gizmos.color = new Color(0.0f, 1.0f, 0.0f);

            Gizmos.DrawLine(transform.position + lastPos, transform.position + displacedPos);

            var origPos = new Vector3((1.0f - slice[i].dist / sliceLength) * bounds.size.x + bounds.min.x, bounds.min.y, 0.0f);
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position + origPos, transform.position + displacedPos);

            lastPos = displacedPos;
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(MeshBaker))]
public class MeshBakerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.DrawDefaultInspector();

        var baker = (target as MeshBaker);
        if (GUILayout.Button("Bake"))
            baker.Bake();

        if (baker.slicesY != null)
        {
            GUILayout.Label("Found " + baker.slicesY.Count + " slices");
            GUILayout.Label("Bounds: " + baker.bounds.size.x + ", " + baker.bounds.size.y);
        }
    }
}

#endif
