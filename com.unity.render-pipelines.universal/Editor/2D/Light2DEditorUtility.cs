using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Experimental.Rendering.Universal;

namespace UnityEditor.Experimental.Rendering.Universal
{
    internal static class Light2DEditorUtility
    {
        static Material s_TexCapMaterial = CoreUtils.CreateEngineMaterial(Shader.Find("Hidden/Internal-GUITexture"));
        
        static internal void GUITextureCap(int controlID, Texture texture, Vector3 position, Quaternion rotation, float size, EventType eventType, bool isAngleHandle)
        {
            switch (eventType)
            {
                case (EventType.Layout):
                    {
                        Vector2 size2 = Vector2.one * size * 0.5f;
                        if (isAngleHandle)
                            size2.x = 0.0f;

                        HandleUtility.AddControl(controlID, DistanceToRectangle(position, rotation, size2));
                        break;
                    }

                case (EventType.Repaint):
                    {
                        s_TexCapMaterial.mainTexture = texture;
                        s_TexCapMaterial.SetPass(0);

                        float w = texture.width;
                        float h = texture.height;
                        float max = Mathf.Max(w, h);
                        Vector3 scale = new Vector2(w / max, h / max) * size * 0.5f;

                        if (Camera.current == null)
                            scale.y *= -1f;

                        Matrix4x4 matrix = new Matrix4x4();
                        matrix.SetTRS(position, rotation, scale);

                        Graphics.DrawMeshNow(RenderingUtils.fullscreenMesh, matrix);
                    }
                    break;
            }
        }

        static float DistanceToRectangle(Vector3 position, Quaternion rotation, Vector2 size)
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

            for (int i = 0; i < 5; ++i)
            {
                if ((points[i].y > pos.y) != (points[j].y > pos.y))
                {
                    if (pos.x < (points[j].x - points[i].x) * (pos.y - points[i].y) / (points[j].y - points[i].y) + points[i].x)
                        oddNodes = !oddNodes;
                }

                j = i;
            }

            if (!oddNodes)
            {
                // Distance to closest edge (not so fast)
                float dist, closestDist = -1f;
                j = 1;

                for (int i = 0; i < 4; ++i)
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

        public static Renderer2DData GetRenderer2DData()
        {
            UniversalRenderPipelineAsset pipelineAsset = UniversalRenderPipeline.asset;
            if (pipelineAsset == null)
                return null;

            // try get the default
            Renderer2DData rendererData = pipelineAsset.scriptableRendererData as Renderer2DData;
            if(rendererData == null)
            {
                foreach (Camera camera in Camera.allCameras)
                {
                    UniversalAdditionalCameraData additionalCameraData = camera.GetComponent<UniversalAdditionalCameraData>();
                    ScriptableRenderer renderer = additionalCameraData?.scriptableRenderer;
                    Renderer2D renderer2D = renderer as Renderer2D;
                    if (renderer2D != null)
                        return renderer2D.GetRenderer2DData();
                }
            }

            return rendererData;
        }

        public static bool IsUsing2DRenderer()
        {
            return GetRenderer2DData() != null;
        }
    }
}
