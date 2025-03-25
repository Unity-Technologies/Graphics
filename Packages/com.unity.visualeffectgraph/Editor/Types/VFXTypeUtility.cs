using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX
{
    static class VFXTypeUtility
    {
        public static int GetComponentCount(VFXSlot slot)
        {
            var slotType = slot.refSlot.property.type;
            if (slotType == typeof(float) || slotType == typeof(uint) || slotType == typeof(int))
                return 1;
            else if (slotType == typeof(Vector2))
                return 2;
            else if (slotType == typeof(Vector3))
                return 3;
            else if (slotType == typeof(Vector4) || slotType == typeof(Color))
                return 4;
            return 0;
        }

        public static bool IsFinite(Vector3 v)
        {
            return float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z);
        }

        public static bool IsFinite(OrientedBox box)
        {
            return IsFinite(box.center) && IsFinite(box.angles) && IsFinite(box.size);
        }

        public static bool IsFinite(Transform transform)
        {
            return IsFinite(transform.position) && IsFinite(transform.angles) && IsFinite(transform.scale);
        }

        public static bool IsFinite(TCircle circle)
        {
            return IsFinite(circle.transform) && float.IsFinite(circle.radius);
        }

        public static bool IsFinite(TArcCircle arcCircle)
        {
            return IsFinite(arcCircle.circle) && float.IsFinite(arcCircle.arc);
        }

        public static bool IsFinite(TCone cone)
        {
            return IsFinite(cone.transform) && float.IsFinite(cone.baseRadius) && float.IsFinite(cone.topRadius) && float.IsFinite(cone.height);
        }

        public static bool IsFinite(TArcCone arcCone)
        {
            return IsFinite(arcCone.cone) && float.IsFinite(arcCone.arc);
        }

        public static bool IsFinite(TSphere sphere)
        {
            return IsFinite(sphere.transform) && float.IsFinite(sphere.radius);
        }

        public static bool IsFinite(TArcSphere arcSphere)
        {
            return IsFinite(arcSphere.sphere) && float.IsFinite(arcSphere.arc);
        }

        public static bool IsFinite(TTorus torus)
        {
            return IsFinite(torus.transform) && float.IsFinite(torus.majorRadius) && float.IsFinite(torus.minorRadius);
        }

        public static bool IsFinite(TArcTorus arcTorus)
        {
            return IsFinite(arcTorus.torus) && float.IsFinite(arcTorus.arc);
        }

        public static bool IsFinite(Line line)
        {
            return IsFinite(line.start) && IsFinite(line.end);
        }

        public static bool IsFinite(AABox box)
        {
            return IsFinite(box.size) && IsFinite(box.center);
        }

        public static bool IsFinite(Plane plane)
        {
            return IsFinite(plane.position) && IsFinite(plane.normal);
        }
    }
}
