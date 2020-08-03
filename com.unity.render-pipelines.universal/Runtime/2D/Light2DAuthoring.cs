using System.Collections.Generic;

namespace UnityEngine.Experimental.Rendering.Universal
{
    public sealed partial class Light2D
    {

#if UNITY_EDITOR
        private const string s_IconsPath = "Packages/com.unity.render-pipelines.universal/Editor/2D/Resources/SceneViewIcons/";
        private static readonly string[] s_LightIconFileNames = new []
        {
            "ParametricLight.png",
            "FreeformLight.png",
            "SpriteLight.png",
            "PointLight.png",
            "PointLight.png"
        };

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawIcon(transform.position, s_IconsPath + s_LightIconFileNames[(int)m_LightType], true);
        }

        void Reset()
        {
            m_ShapePath = new Vector3[] { new Vector3(-0.5f, -0.5f), new Vector3(0.5f, -0.5f), new Vector3(0.5f, 0.5f), new Vector3(-0.5f, 0.5f) };
        }

        internal List<Vector2> GetFalloffShape()
        {
            List<Vector2> shape = new List<Vector2>();
            List<Vector2> extrusionDir = new List<Vector2>();
            LightUtility.GetFalloffShape(m_ShapePath, ref extrusionDir);
            for (int i = 0; i < m_ShapePath.Length; i++)
            {
                Vector2 position = new Vector2();
                position.x = m_ShapePath[i].x + this.shapeLightFalloffSize * extrusionDir[i].x;
                position.y = m_ShapePath[i].y + this.shapeLightFalloffSize * extrusionDir[i].y;
                shape.Add(position);
            }
            return shape;
        }
#endif

    }
}
