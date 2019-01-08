using UnityEngine;

namespace UnityEditor.Experimental.Rendering.LWRP
{
    public class Light2DEditorUtility
    {

        private static Material s_Material;
        private static Material material
        {
            get
            {
                if (s_Material == null)
                {
                    s_Material = new Material(Shader.Find("Hidden/InternalSpritesInspector"));
                    s_Material.hideFlags = HideFlags.DontSave;
                }
                s_Material.SetFloat("_AdjustLinearForGamma", PlayerSettings.colorSpace == ColorSpace.Linear ? 1.0f : 0.0f);
                return s_Material;
            }
        }

        private static Mesh s_TexCapMesh;
        private static Mesh texCapMesh
        {
            get
            {
                if (s_TexCapMesh == null)
                {
                    s_TexCapMesh = new Mesh();
                    s_TexCapMesh.hideFlags = HideFlags.DontSave;
                    s_TexCapMesh.vertices = new Vector3[] {
                        new Vector2(-0.5f, -0.5f),
                        new Vector2(-0.5f, 0.5f),
                        new Vector2(0.5f, 0.5f),
                        new Vector2(-0.5f, -0.5f),
                        new Vector2(0.5f, 0.5f),
                        new Vector2(0.5f, -0.5f)
                    };
                    s_TexCapMesh.uv = new Vector2[] {
                        Vector3.zero,
                        Vector3.up,
                        Vector3.up + Vector3.right,
                        Vector3.zero,
                        Vector3.up + Vector3.right,
                        Vector3.right
                    };
                    s_TexCapMesh.SetTriangles(new int[] { 0, 1, 2, 3, 4, 5 }, 0);
                }

                return s_TexCapMesh;
            }
        }

        public static void DrawMesh(Mesh mesh, Material material, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            Matrix4x4 matrix = new Matrix4x4();
            matrix.SetTRS(position, rotation, scale);
            material.SetPass(0);
            Graphics.DrawMeshNow(mesh, matrix);
        }

        public static void GUITextureCap(int controlID, Texture texture, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            switch (eventType)
            {
                case (EventType.Layout):
                    HandleUtility.AddControl(controlID, DistanceToRectangle(position, rotation, Vector2.one * size * 0.5f));
                    break;
                case (EventType.Repaint):

                    FilterMode filterMode = texture.filterMode;
                    texture.filterMode = FilterMode.Bilinear;

                    material.mainTexture = texture;

                    float w = (float)texture.width;
                    float h = (float)texture.height;
                    float max = Mathf.Max(w, h);

                    Vector3 scale = new Vector2(w / max, h / max) * size;

                    if (Camera.current == null)
                        scale.y *= -1f;

                    DrawMesh(texCapMesh, material, position, rotation, scale);

                    texture.filterMode = filterMode;
                    break;
            }
        }

        public static float DistanceToRectangle(Vector3 position, Quaternion rotation, Vector2 size)
        {
            Vector3[] points = { Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero };
            Vector3 sideways = rotation * new Vector3(size.x, 0, 0);
            Vector3 up = rotation * new Vector3(0, size.y, 0);
            points[0] = HandleUtility.WorldToGUIPoint(position + sideways + up);
            points[1] = HandleUtility.WorldToGUIPoint(position + sideways - up);
            points[2] = HandleUtility.WorldToGUIPoint(position - sideways - up);
            points[3] = HandleUtility.WorldToGUIPoint(position - sideways + up);
            points[4] = points[0];

            Vector2 pos = Event.current.mousePosition;
            bool oddNodes = false;
            int j = 4;
            for (int i = 0; i < 5; i++)
            {
                if ((points[i].y > pos.y) != (points[j].y > pos.y))
                {
                    if (pos.x < (points[j].x - points[i].x) * (pos.y - points[i].y) / (points[j].y - points[i].y) + points[i].x)
                    {
                        oddNodes = !oddNodes;
                    }
                }
                j = i;
            }
            if (!oddNodes)
            {
                // Distance to closest edge (not so fast)
                float dist, closestDist = -1f;
                j = 1;
                for (int i = 0; i < 4; i++)
                {
                    dist = HandleUtility.DistancePointToLineSegment(pos, points[i], points[j++]);
                    if (dist < closestDist || closestDist < 0)
                        closestDist = dist;
                }
                return closestDist;
            }
            else
                return 0;
        }
    }
}
