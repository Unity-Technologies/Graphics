using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.Tests
{
    public class VolumeComponentCopyPasteTests
    {
        class CopyPasteTestComponent1 : VolumeComponent
        {
            public FloatParameter p1 = new FloatParameter(0f);
            public IntParameter p2 = new IntParameter(0);

            public CopyPasteTestComponent1 WithModifiedValues()
            {
                p1.value = 123.0f;
                p2.value = 123;
                return this;
            }

            public void AssertEquality(CopyPasteTestComponent1 other, Action<object, object> assertionFunction)
            {
                Assert.AreEqual(GetType(), other.GetType());
                assertionFunction(p1.value, other.p1.value);
                assertionFunction(p2.value, other.p2.value);
            }
        }

        class CopyPasteTestComponent2 : CopyPasteTestComponent1
        {
            public BoolParameter p21 = new BoolParameter(false);

            public new CopyPasteTestComponent2 WithModifiedValues()
            {
                base.WithModifiedValues();
                p21.value = true;
                return this;
            }

            public void AssertEquality(CopyPasteTestComponent2 other, Action<object, object> assertionFunction)
            {
                base.AssertEquality(other, assertionFunction);
                assertionFunction(p21.value, other.p21.value);
            }
        }

        class CopyPasteTestComponent3 : CopyPasteTestComponent1
        {
            public ColorParameter p31 = new ColorParameter(Color.black);

            public new CopyPasteTestComponent3 WithModifiedValues()
            {
                base.WithModifiedValues();
                p31.value = Color.green;
                return this;
            }

            public void AssertEquality(CopyPasteTestComponent3 other, Action<object, object> assertionFunction)
            {
                base.AssertEquality(other, assertionFunction);
                assertionFunction(p31.value, other.p31.value);
            }
        }

        static T CreateComponent<T>() where T : CopyPasteTestComponent1 => ScriptableObject.CreateInstance<T>();

        CopyPasteTestComponent1 m_Src1;
        CopyPasteTestComponent2 m_Src2;
        CopyPasteTestComponent3 m_Src3;
        CopyPasteTestComponent1 m_Dst1;
        CopyPasteTestComponent2 m_Dst2;
        CopyPasteTestComponent3 m_Dst3;
        CopyPasteTestComponent1 m_Default1;
        CopyPasteTestComponent2 m_Default2;
        CopyPasteTestComponent3 m_Default3;

        [SetUp]
        public void SetUp()
        {
            EditorGUIUtility.systemCopyBuffer = "";

            m_Src1 = CreateComponent<CopyPasteTestComponent1>().WithModifiedValues();
            m_Src2 = CreateComponent<CopyPasteTestComponent2>().WithModifiedValues();
            m_Src3 = CreateComponent<CopyPasteTestComponent3>().WithModifiedValues();
            m_Dst1 = CreateComponent<CopyPasteTestComponent1>();
            m_Dst2 = CreateComponent<CopyPasteTestComponent2>();
            m_Dst3 = CreateComponent<CopyPasteTestComponent3>();
            m_Default1 = CreateComponent<CopyPasteTestComponent1>();
            m_Default2 = CreateComponent<CopyPasteTestComponent2>();
            m_Default3 = CreateComponent<CopyPasteTestComponent3>();
        }

        [Test]
        public void CopyPasteSingle()
        {
            VolumeComponentCopyPaste.CopySettings(m_Src1);
            m_Src1.AssertEquality(m_Dst1, Assert.AreNotEqual);
            VolumeComponentCopyPaste.PasteSettings(m_Dst1);
            m_Src1.AssertEquality(m_Dst1, Assert.AreEqual);
        }

        [Test]
        public void CopyPasteSingleUndoRedo()
        {
            VolumeComponentCopyPaste.CopySettings(m_Src1);
            VolumeComponentCopyPaste.PasteSettings(m_Dst1);
            Undo.PerformUndo();
            m_Dst1.AssertEquality(m_Default1, Assert.AreEqual); // paste target is unchanged
            Undo.PerformRedo();
            m_Dst1.AssertEquality(m_Src1, Assert.AreEqual); // paste target matches source
        }

        [Test]
        public void CopyPasteMultiple()
        {
            VolumeComponentCopyPaste.CopySettings(new List<VolumeComponent> { m_Src1, m_Src2, m_Src3 });
            m_Src1.AssertEquality(m_Dst1, Assert.AreNotEqual);
            m_Src2.AssertEquality(m_Dst2, Assert.AreNotEqual);
            m_Src3.AssertEquality(m_Dst3, Assert.AreNotEqual);

            VolumeComponentCopyPaste.PasteSettings(new List<VolumeComponent> { m_Dst1, m_Dst2, m_Dst3 });
            m_Src1.AssertEquality(m_Dst1, Assert.AreEqual);
            m_Src2.AssertEquality(m_Dst2, Assert.AreEqual);
            m_Src3.AssertEquality(m_Dst3, Assert.AreEqual);
        }

        [Test]
        public void CopyPasteMultipleInDifferentOrder()
        {
            VolumeComponentCopyPaste.CopySettings(new List<VolumeComponent> { m_Src1, m_Src2, m_Src3 });
            m_Src1.AssertEquality(m_Dst1, Assert.AreNotEqual);
            m_Src2.AssertEquality(m_Dst2, Assert.AreNotEqual);
            m_Src3.AssertEquality(m_Dst3, Assert.AreNotEqual);

            VolumeComponentCopyPaste.PasteSettings(new List<VolumeComponent> { m_Dst3, m_Dst1, m_Dst2 });
            m_Src1.AssertEquality(m_Dst1, Assert.AreEqual);
            m_Src2.AssertEquality(m_Dst2, Assert.AreEqual);
            m_Src3.AssertEquality(m_Dst3, Assert.AreEqual);
        }

        [Test]
        public void CopyPasteMultipleToSingleComponent()
        {
            VolumeComponentCopyPaste.CopySettings(new List<VolumeComponent> { m_Src1, m_Src2, m_Src3 });
            VolumeComponentCopyPaste.PasteSettings(new List<VolumeComponent> { m_Dst3 });
            VolumeComponentCopyPaste.PasteSettings(new List<VolumeComponent> { m_Dst2 });
            VolumeComponentCopyPaste.PasteSettings(new List<VolumeComponent> { m_Dst1 });
            m_Src1.AssertEquality(m_Dst1, Assert.AreEqual);
            m_Src2.AssertEquality(m_Dst2, Assert.AreEqual);
            m_Src3.AssertEquality(m_Dst3, Assert.AreEqual);
        }

        [Test]
        public void CopyPasteSingleToMultipleComponent()
        {
            VolumeComponentCopyPaste.CopySettings(new List<VolumeComponent> { m_Src1 });
            VolumeComponentCopyPaste.PasteSettings(new List<VolumeComponent> { m_Dst3, m_Dst1, m_Dst2 });
            m_Src1.AssertEquality(m_Dst1, Assert.AreEqual);
        }

        [Test]
        public void CopyPasteMultipleUndoRedo()
        {
            VolumeComponentCopyPaste.CopySettings(new List<VolumeComponent> { m_Src1, m_Src2, m_Src3 });
            VolumeComponentCopyPaste.PasteSettings(new List<VolumeComponent> { m_Dst1, m_Dst2, m_Dst3 });

            Undo.PerformUndo();

            // paste target is unchanged
            m_Dst1.AssertEquality(m_Default1, Assert.AreEqual);
            m_Dst2.AssertEquality(m_Default2, Assert.AreEqual);
            m_Dst3.AssertEquality(m_Default3, Assert.AreEqual);

            Undo.PerformRedo();

            // paste target matches source
            m_Dst1.AssertEquality(m_Src1, Assert.AreEqual);
            m_Dst2.AssertEquality(m_Src2, Assert.AreEqual);
            m_Dst3.AssertEquality(m_Src3, Assert.AreEqual);
        }

        [Test]
        public void CannotPasteWithEmptyCopyBuffer()
        {
            Assert.False(VolumeComponentCopyPaste.CanPaste(m_Src1));
        }

        [Test]
        public void CanPasteToSelf()
        {
            VolumeComponentCopyPaste.CopySettings(m_Src1);
            Assert.True(VolumeComponentCopyPaste.CanPaste(m_Src1));
        }

        [Test]
        public void CanPasteToMatchingType()
        {
            VolumeComponentCopyPaste.CopySettings(m_Src1);
            Assert.True(VolumeComponentCopyPaste.CanPaste(m_Dst1));
        }

        [Test]
        public void CannotPasteToDifferentType()
        {
            VolumeComponentCopyPaste.CopySettings(m_Src1);
            Assert.False(VolumeComponentCopyPaste.CanPaste(m_Dst3));
        }

        [Test]
        public void CanPasteIfSingleMatchingType()
        {
            VolumeComponentCopyPaste.CopySettings(m_Src1);
            Assert.True(VolumeComponentCopyPaste.CanPaste(new List<VolumeComponent> { m_Dst1, m_Dst2, m_Dst3 }));
        }

        [Test]
        public void CanPasteIfMultipleMatchingTypes()
        {
            VolumeComponentCopyPaste.CopySettings(new List<VolumeComponent> { m_Src1, m_Src2, m_Src3 });
            Assert.True(VolumeComponentCopyPaste.CanPaste(new List<VolumeComponent> { m_Dst1,  m_Dst3, m_Dst2 }));
        }
    }
}
