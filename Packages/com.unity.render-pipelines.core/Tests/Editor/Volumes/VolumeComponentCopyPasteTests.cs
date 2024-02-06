using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.Tests
{
    class VolumeComponentCopyPasteTests
    {
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
