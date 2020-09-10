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
        
        // Rough shape only used in Inspector for quick preview. 
        static List<Vector2> GetExtrusionDir(Vector3[] shapePath)
        {
            var extrusionDir = new List<Vector2>();
            for (var i = 0; i < shapePath.Length; ++i)
            {
                var h = (i == 0) ? (shapePath.Length - 1) : (i - 1);
                var j = (i + 1) % shapePath.Length;

                var pp = shapePath[h];
                var cp = shapePath[i];
                var np = shapePath[j];

                var cpd = cp - pp;
                var npd = np - cp;
                if (cpd.magnitude < 0.001f || npd.magnitude < 0.001f)
                    continue;

                var vl = cpd.normalized;
                var vr = npd.normalized;

                vl = new Vector2(-vl.y, vl.x);
                vr = new Vector2(-vr.y, vr.x);

                var va = vl.normalized + vr.normalized;
                var vn = -va.normalized;

                if (va.magnitude > 0 && vn.magnitude > 0)
                {
                    var dir = new Vector2(vn.x, vn.y);
                    extrusionDir.Add(dir);
                }
            }

            return extrusionDir;
        }        

        internal List<Vector2> GetFalloffShape()
        {
            var shape = new List<Vector2>();
            var extrusionDir = GetExtrusionDir(m_ShapePath);
            for (var i = 0; i < m_ShapePath.Length; i++)
            {
                shape.Add(new Vector2
                {
                    x = m_ShapePath[i].x + this.shapeLightFalloffSize * extrusionDir[i].x,
                    y = m_ShapePath[i].y + this.shapeLightFalloffSize * extrusionDir[i].y
                });
            }
            return shape;
        }
#endif

    }
}
