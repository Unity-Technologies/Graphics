#if USING_2DCOMMON
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEditor.U2D.Common.Path;

namespace UnityEditor.Rendering.Universal
{
    internal class ShadowCasterPath : ScriptablePath
    {
        internal Bounds GetBounds()
        {
            ShadowCaster2D shadowCaster = (ShadowCaster2D)owner;
            Renderer m_Renderer = shadowCaster.GetComponent<Renderer>();
            if (m_Renderer != null)
            {
                return m_Renderer.bounds;
            }
            else
            {
                Collider2D collider = shadowCaster.GetComponent<Collider2D>();
                if (collider != null)
                    return collider.bounds;
            }

            return new Bounds(shadowCaster.transform.position, shadowCaster.transform.lossyScale);
        }

        internal void SetDefaultShape()
        {
            Clear();
            Bounds bounds = GetBounds();

            AddPoint(new ControlPoint() {position = bounds.min});
            AddPoint(new ControlPoint() {position = new Vector3(bounds.min.x, bounds.max.y)});
            AddPoint(new ControlPoint() {position = bounds.max});
            AddPoint(new ControlPoint() {position = new Vector3(bounds.max.x, bounds.min.y)});
        }
    }
}
#endif
