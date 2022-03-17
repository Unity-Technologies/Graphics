using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.UI;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    class SerializedFieldsInspectorTests : BaseUIFixture
    {
        /// <inheritdoc />
        protected override bool CreateGraphOnStartup => false;

        [Serializable]
        class TestCanBeInspected : IModel
        {
            // ReSharper disable Unity.RedundantSerializeFieldAttribute
            // ReSharper disable Unity.RedundantHideInInspectorAttribute
            // ReSharper disable NotAccessedField.Local
            // ReSharper disable UnusedMember.Local
            public int m_InspectableA;

            [SerializeField]
            int m_InspectableB;

            [NonSerialized]
            public int c;

            [SerializeField]
            [NonSerialized]
            int m_D;

            [HideInInspector]
            public int m_E;

            [SerializeField]
            [HideInInspector]
            int m_F;

            [NonSerialized]
            [HideInInspector]
            public int g;

            [SerializeField]
            [NonSerialized]
            [HideInInspector]
            int m_H;

            [Obsolete]
            public int m_I;

            [SerializeField]
            [Obsolete]
            int m_J;

            [NonSerialized]
            [Obsolete]
            public int k;

            [SerializeField]
            [NonSerialized]
            [Obsolete]
            int m_L;
            // ReSharper restore UnusedMember.Local
            // ReSharper restore NotAccessedField.Local
            // ReSharper restore Unity.RedundantHideInInspectorAttribute
            // ReSharper restore Unity.RedundantSerializeFieldAttribute

            /// <inheritdoc />
            public SerializableGUID Guid { get; set; }

            /// <inheritdoc />
            public void AssignNewGuid()
            {
            }
        }

        [Test]
        public void CanBeInspectedFiltersCorrectly()
        {
            var t = typeof(TestCanBeInspected);
            foreach (var fieldInfo in t.GetFields())
            {
                var isInspectable = SerializedFieldsInspector.CanBeInspected(fieldInfo);
                var expectedInspectable = fieldInfo.Name.StartsWith("m_Inspectable");
                Assert.AreEqual(expectedInspectable, isInspectable, $"{fieldInfo.Name} fails.");
            }
        }

        [Serializable]
        class Derived : TestCanBeInspected
        {
            [SerializeField]
            // ReSharper disable once NotAccessedField.Local
            int m_AA;
        }

        class TestableSerializedFieldsInspector : SerializedFieldsInspector
        {
            /// <inheritdoc />
            public TestableSerializedFieldsInspector(string name, IModel model)
                : base(name, model, null, null, null) { }

            public IEnumerable<BaseModelPropertyField> TestableGetFields()
            {
                return base.GetFields();
            }
        }

        [Test]
        public void GetFieldsReturnBaseClassFields()
        {
            var model = new Derived();
            var inspector = new TestableSerializedFieldsInspector("Test", model);
            Assert.Greater(inspector.TestableGetFields().Count(), 1);
        }
    }
}
