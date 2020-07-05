using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace UnityEngine.Experimental.Rendering.Universal
{

    [AddComponentMenu("Rendering/2D/Composite Shadow Caster 2D (Experimental)")]
    [ExecuteInEditMode]
    public class CompositeShadowCaster2D : ShadowCasterGroup2D
    {
        public static Dictionary<Transform, CompositeShadowCaster2D> CompositeShadowCaster2DsByTransform =
            new Dictionary<Transform, CompositeShadowCaster2D>();

        protected void OnEnable()
        {
            ShadowCasterGroup2DManager.AddGroup(this);
            CompositeShadowCaster2DsByTransform.Add(transform, this);
        }

        protected void OnDisable()
        {
            ShadowCasterGroup2DManager.RemoveGroup(this);
            CompositeShadowCaster2DsByTransform.Remove(transform);
        }
    }
}
