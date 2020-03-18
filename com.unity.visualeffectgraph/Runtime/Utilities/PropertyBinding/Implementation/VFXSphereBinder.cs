using UnityEngine.VFX;

namespace UnityEngine.VFX.Utility
{
    [AddComponentMenu("VFX/Property Binders/Sphere Collider Binder")]
    [VFXBinder("Collider/Sphere")]
    class VFXSphereBinder : VFXBinderBase
    {
        public string Property { get { return (string)m_Property; } set { m_Property = value; UpdateSubProperties(); } }

        [VFXPropertyBinding("UnityEditor.VFX.Sphere"), SerializeField, UnityEngine.Serialization.FormerlySerializedAs("m_Parameter")]
        protected ExposedProperty m_Property = "Sphere";
        public SphereCollider Target = null;

        private ExposedProperty Center;
        private ExposedProperty Radius;

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
            Center = m_Property + "_center";
            Radius = m_Property + "_radius";
        }

        public override bool IsValid(VisualEffect component)
        {
            return Target != null && component.HasVector3(Center) && component.HasFloat(Radius);
        }

        public override void UpdateBinding(VisualEffect component)
        {
            component.SetVector3(Center, Target.transform.position + Target.center);
            component.SetFloat(Radius, Target.radius * GetSphereColliderScale(Target.transform.localScale));
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
