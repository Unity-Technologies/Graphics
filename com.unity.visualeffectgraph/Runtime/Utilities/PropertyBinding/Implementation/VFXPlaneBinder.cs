using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEngine.VFX.Utility
{
    [AddComponentMenu("VFX/Property Binders/Plane Binder")]
    [VFXBinder("Utility/Plane")]
    class VFXPlaneBinder : VFXSpaceableBinder
    {
        public string Property { get { return (string)m_Property; } set { m_Property = value; UpdateSubProperties(); } }

        [VFXPropertyBinding("UnityEditor.VFX.Plane"), SerializeField, UnityEngine.Serialization.FormerlySerializedAs("m_Parameter")]
        protected ExposedProperty m_Property = "Plane";
        public Transform Target = null;

        private ExposedProperty Position;
        private ExposedProperty Normal;

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
            Position = m_Property + "_position";
            Normal = m_Property + "_normal";
        }

        public override bool IsValid(VisualEffect component)
        {
            return Target != null && component.HasVector3(Position) && component.HasVector3(Normal);
        }

        public override void UpdateBinding(VisualEffect component)
        {
            ApplySpacePositionNormal(component, Position, Target.transform, out var position, out var normal);

            component.SetVector3(Position, position);
            component.SetVector3(Normal, normal);
        }

        public override string ToString()
        {
            return string.Format("Plane : '{0}' -> {1}", m_Property, Target == null ? "(null)" : Target.name);
        }
    }
}
