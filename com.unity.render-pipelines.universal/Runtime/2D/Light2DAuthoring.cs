using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine.Splines;

namespace UnityEngine.Rendering.Universal
{
    public sealed partial class Light2D
    {
#if UNITY_EDITOR
        private const string s_IconsPath = "Packages/com.unity.render-pipelines.universal/Editor/2D/Resources/SceneViewIcons/";
        private static readonly string[] s_LightIconFileNames = new[]
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
            spline.Closed = true;
            spline.EditType = SplineType.Linear;
            spline.Resize(4);
            var bk = new BezierKnot();
            bk.Position = new Vector3(-0.5f, -0.5f);
            bk.TangentIn = float3.zero;
            bk.TangentOut = float3.zero;
            spline[0] = bk;
            bk.Position = new Vector3(0.5f, -0.5f);
            spline[1] = bk;
            bk.Position = new Vector3(0.5f, 0.5f);
            spline[2] = bk;
            bk.Position = new Vector3(-0.5f, 0.5f);
            spline[3] = bk;
        }

        internal List<Vector2> GetFalloffShape()
        {
            return LightUtility.GetOutlinePath(GetPath(), m_ShapeLightFalloffSize);
        }

#endif
    }
}
