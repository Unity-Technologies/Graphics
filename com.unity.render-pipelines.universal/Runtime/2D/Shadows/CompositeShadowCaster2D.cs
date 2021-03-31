using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;


namespace UnityEngine.Rendering.Universal
{
    [AddComponentMenu("Rendering/2D/Composite Shadow Caster 2D (Experimental)")]
    [MovedFrom("UnityEngine.Experimental.Rendering.Universal")]
    [ExecuteInEditMode]
    public class CompositeShadowCaster2D : ShadowCasterGroup2D
    {
        protected void OnEnable()
        {
            ShadowCasterGroup2DManager.AddGroup(this);
        }

        protected void OnDisable()
        {
            ShadowCasterGroup2DManager.RemoveGroup(this);
        }
    }
}
