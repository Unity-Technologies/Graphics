using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering
{
    class CustomVolumePassGizmoDrawer : IVolumeAdditionalGizmo
    {
        public Type type => typeof(CustomPassVolume);

        public void OnBoxColliderDraw(IVolume scr, BoxCollider c)
        {
            var customPass = scr as CustomPassVolume;
            if (customPass.fadeRadius > 0)
            {
                var twiceFadeRadius = customPass.fadeRadius * 2;
                // invert te scale for the fade radius because it's in fixed units
                Vector3 s = new Vector3(
                    twiceFadeRadius / customPass.transform.localScale.x,
                    twiceFadeRadius / customPass.transform.localScale.y,
                    twiceFadeRadius / customPass.transform.localScale.z
                );
                Gizmos.DrawWireCube(c.center, c.size + s);
            }
        }

        public void OnMeshColliderDraw(IVolume scr, MeshCollider c)
        {
        }

        public void OnSphereColliderDraw(IVolume scr, SphereCollider c)
        {
            var customPass = scr as CustomPassVolume;
            if (customPass.fadeRadius > 0)
                Gizmos.DrawWireSphere(c.center, c.radius + customPass.fadeRadius / customPass.transform.lossyScale.x);
        }
    }
}
