#if VFX_HAS_PHYSICS
using UnityEngine.VFX;

namespace UnityEngine.VFX.Utility
{
    [AddComponentMenu("VFX/Property Binders/Sphere Collider Binder")]
    [VFXBinder("Collider/Sphere")]
    class VFXSphereBinder : VFXBinderBase
    {
        public string Property { get { return (string)m_Property; } set { m_Property = value; UpdateSubProperties(); } }

        [VFXPropertyBinding("UnityEditor.VFX.Sphere", "UnityEditor.VFX.TSphere"), SerializeField, UnityEngine.Serialization.FormerlySerializedAs("m_Parameter")]
        protected ExposedProperty m_Property = "Sphere";
        public SphereCollider Target = null;

        private ExposedProperty m_Old_Center;
        private ExposedProperty m_New_Center;
        private ExposedProperty m_Radius;

        protected override void OnEnable()
        {
            base.OnEnable();
            UpdateSubProperties();
        }

        void OnValidate()
        {
            UpdateSubProperties();
        }

        void UpdateSubProperties()
        {
            //Support the new "TSphere" structure and the previous "Sphere" type
            m_Old_Center = m_Property + "_center";
            m_New_Center = m_Property + "_transform_position";
            m_Radius = m_Property + "_radius";
        }

        public override bool IsValid(VisualEffect component)
        {
            return Target != null
                && (component.HasVector3(m_New_Center) || component.HasVector3(m_Old_Center))
                && component.HasFloat(m_Radius);
        }

        public override void UpdateBinding(VisualEffect component)
        {
            var center = Target.transform.position + Target.center;
            if (component.HasVector3(m_New_Center))
                component.SetVector3(m_New_Center, center);
            else
                component.SetVector3(m_Old_Center, center);

            component.SetFloat(m_Radius, Target.radius * GetSphereColliderScale(Target.transform.localScale));
        }

        public float GetSphereColliderScale(Vector3 scale)
        {
            return Mathf.Max(scale.x, Mathf.Max(scale.y, scale.z));
        }

        public override string ToString()
        {
            return string.Format("Sphere : '{0}' -> {1}", m_Property, Target == null ? "(null)" : Target.name);
        }
    }
}
#endif
