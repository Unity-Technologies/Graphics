#if !UNITY_EDITOR_OSX || MAC_FORCE_TESTS
using System;
using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    public class VFXModelTests
    {
        static List<string> s_logs = new List<string>();

        private class VFXDummyModel : VFXModel<VFXGraph, VFXModel>
        {}

        // VFXModelA <>- VFXModelB <>- VFXModelC
        private class VFXModelA : VFXModel<VFXModel, VFXModelB>
        {
            protected override void OnInvalidate(VFXModel model, InvalidationCause cause) { s_logs.Add("OnInvalidate VFXModelA " + cause); }
            protected override void OnAdded() { s_logs.Add("OnAdded VFXModelA"); }
            protected override void OnRemoved() { s_logs.Add("OnRemoved VFXModelA"); }
        }

        private class VFXModelB : VFXModel<VFXModelA, VFXModelC>
        {
            protected override void OnInvalidate(VFXModel model, InvalidationCause cause) { s_logs.Add("OnInvalidate VFXModelB " + cause); }
            protected override void OnAdded() { s_logs.Add("OnAdded VFXModelB"); }
            protected override void OnRemoved() { s_logs.Add("OnRemoved VFXModelB"); }
        }

        private class VFXModelC : VFXModel<VFXModelB, VFXModel>
        {
            protected override void OnInvalidate(VFXModel model, InvalidationCause cause) { s_logs.Add("OnInvalidate VFXModelC " + cause); }
            protected override void OnAdded() { s_logs.Add("OnAdded VFXModelC"); }
            protected override void OnRemoved() { s_logs.Add("OnRemoved VFXModelC"); }

            public override bool AcceptChild(VFXModel child, int index = -1) { return false; }
        }

        [Test]
        public void AddChild()
        {
            VFXModel modelA = ScriptableObject.CreateInstance<VFXModelA>();
            VFXModel modelB = ScriptableObject.CreateInstance<VFXModelB>();
            VFXModel modelC = ScriptableObject.CreateInstance<VFXModelC>();

            // Test both interfaces
            s_logs.Clear();
            modelA.AddChild(modelB);
            modelC.Attach(modelB);

            Assert.AreEqual(1, modelA.GetNbChildren());
            Assert.AreEqual(modelB, modelA[0]);
            Assert.AreEqual(modelA, modelB.GetParent());

            Assert.AreEqual(1, modelB.GetNbChildren());
            Assert.AreEqual(modelC, modelB[0]);
            Assert.AreEqual(modelB, modelC.GetParent());

            Assert.AreEqual(5, s_logs.Count);
            Assert.AreEqual("OnAdded VFXModelB", s_logs[0]);
            Assert.AreEqual("OnInvalidate VFXModelA kStructureChanged", s_logs[1]);
            Assert.AreEqual("OnAdded VFXModelC", s_logs[2]);
            Assert.AreEqual("OnInvalidate VFXModelB kStructureChanged", s_logs[3]);
            Assert.AreEqual("OnInvalidate VFXModelA kStructureChanged", s_logs[4]);
        }

        [Test]
        public void InsertChild()
        {
            VFXModel modelA = ScriptableObject.CreateInstance<VFXModelA>();
            VFXModel modelB0 = ScriptableObject.CreateInstance<VFXModelB>();
            VFXModel modelB1 = ScriptableObject.CreateInstance<VFXModelB>();
            VFXModel modelB2 = ScriptableObject.CreateInstance<VFXModelB>();

            s_logs.Clear();
            modelA.AddChild(modelB2);
            modelA.AddChild(modelB0, 0);
            modelA.AddChild(modelB1, 1);

            Assert.AreEqual(3, modelA.GetNbChildren(), 3);
            Assert.AreEqual(modelB0, modelA[0]);
            Assert.AreEqual(modelB1, modelA[1]);
            Assert.AreEqual(modelB2, modelA[2]);

            Assert.AreEqual(6, s_logs.Count);
            Assert.AreEqual("OnAdded VFXModelB",                        s_logs[0]);
            Assert.AreEqual("OnInvalidate VFXModelA kStructureChanged", s_logs[1]);
            Assert.AreEqual("OnAdded VFXModelB",                        s_logs[2]);
            Assert.AreEqual("OnInvalidate VFXModelA kStructureChanged", s_logs[3]);
            Assert.AreEqual("OnAdded VFXModelB",                        s_logs[4]);
            Assert.AreEqual("OnInvalidate VFXModelA kStructureChanged", s_logs[5]);
        }

        [Test]
        public void RemoveChild()
        {
            VFXModel modelA = ScriptableObject.CreateInstance<VFXModelA>();
            VFXModel modelB = ScriptableObject.CreateInstance<VFXModelB>();
            VFXModel modelC = ScriptableObject.CreateInstance<VFXModelC>();

            // First add children but don't notify
            modelB.Attach(modelA, false);
            modelC.Attach(modelB, false);

            // Test both interfaces
            s_logs.Clear();
            modelC.Detach();
            modelA.RemoveChild(modelB);

            Assert.AreEqual(0, modelA.GetNbChildren());
            Assert.IsNull(modelB.GetParent());

            Assert.AreEqual(0, modelB.GetNbChildren());
            Assert.IsNull(modelC.GetParent());

            Assert.AreEqual(5, s_logs.Count);
            Assert.AreEqual("OnRemoved VFXModelC",                      s_logs[0]);
            Assert.AreEqual("OnInvalidate VFXModelB kStructureChanged", s_logs[1]);
            Assert.AreEqual("OnInvalidate VFXModelA kStructureChanged", s_logs[2]);
            Assert.AreEqual("OnRemoved VFXModelB",                      s_logs[3]);
            Assert.AreEqual("OnInvalidate VFXModelA kStructureChanged", s_logs[4]);
        }

        [Test]
        public void RemoveAllChildren()
        {
            VFXModel modelA = ScriptableObject.CreateInstance<VFXModelA>();
            VFXModel modelB0 = ScriptableObject.CreateInstance<VFXModelB>();
            VFXModel modelB1 = ScriptableObject.CreateInstance<VFXModelB>();
            VFXModel modelB2 = ScriptableObject.CreateInstance<VFXModelB>();

            modelA.AddChild(modelB0);
            modelA.AddChild(modelB1);
            modelA.AddChild(modelB2);

            s_logs.Clear();
            modelA.RemoveAllChildren();

            Assert.AreEqual(0, modelA.GetNbChildren());
            Assert.IsNull(modelB0.GetParent());
            Assert.IsNull(modelB1.GetParent());
            Assert.IsNull(modelB2.GetParent());

            Assert.AreEqual(6, s_logs.Count);
            for (int i = 0; i < 6; i += 2)
            {
                Assert.AreEqual("OnRemoved VFXModelB", s_logs[i]);
                Assert.AreEqual("OnInvalidate VFXModelA kStructureChanged", s_logs[i + 1]);
            }
        }

        [Test]
        public void ChangeParent()
        {
            VFXModel modelA0 = ScriptableObject.CreateInstance<VFXModelA>();
            VFXModel modelA1 = ScriptableObject.CreateInstance<VFXModelA>();
            VFXModel modelB = ScriptableObject.CreateInstance<VFXModelB>();

            s_logs.Clear();
            modelA0.AddChild(modelB);
            modelA1.AddChild(modelB);

            Assert.AreEqual(0, modelA0.GetNbChildren());
            Assert.AreEqual(1, modelA1.GetNbChildren());
            Assert.AreEqual(modelA1, modelB.GetParent());

            Assert.AreEqual(6, s_logs.Count);
            Assert.AreEqual("OnAdded VFXModelB",                    s_logs[0]);
            Assert.AreEqual("OnInvalidate VFXModelA kStructureChanged", s_logs[1]);
            Assert.AreEqual("OnRemoved VFXModelB",                  s_logs[2]);
            Assert.AreEqual("OnInvalidate VFXModelA kStructureChanged", s_logs[3]);
            Assert.AreEqual("OnAdded VFXModelB",                    s_logs[4]);
            Assert.AreEqual("OnInvalidate VFXModelA kStructureChanged", s_logs[5]);
        }

        [Test]
        public void AddChild_IncompatibleModel()
        {
            VFXModel modelA = ScriptableObject.CreateInstance<VFXModelA>();
            VFXModel modelB = ScriptableObject.CreateInstance<VFXModelB>();

            s_logs.Clear();
            Assert.Throws<ArgumentException>(() =>
                modelB.AddChild(modelA)
            );

            Assert.AreEqual(0, modelB.GetNbChildren());
            Assert.IsNull(modelA.GetParent());
            Assert.AreEqual(0, s_logs.Count);
        }

        [Test]
        public void AddChild_OutOfBounds()
        {
            VFXModel modelA = ScriptableObject.CreateInstance<VFXModelA>();
            VFXModel modelB = ScriptableObject.CreateInstance<VFXModelB>();

            s_logs.Clear();
            Assert.Throws<ArgumentException>(() =>
                modelA.AddChild(modelB, 2)
            );

            Assert.AreEqual(0, modelB.GetNbChildren());
            Assert.IsNull(modelA.GetParent());
            Assert.AreEqual(0, s_logs.Count);
        }

        [Test]
        public void OnInvalidateDelegate()
        {
            s_logs.Clear();

            var graph = ScriptableObject.CreateInstance<VFXGraph>();
            var model = ScriptableObject.CreateInstance<VFXDummyModel>();
            graph.AddChild(model);
            graph.onInvalidateDelegate += OnModelInvalidated;
            graph.AddChild(ScriptableObject.CreateInstance<VFXDummyModel>());
            graph.onInvalidateDelegate -= OnModelInvalidated;
            graph.AddChild(ScriptableObject.CreateInstance<VFXDummyModel>());
            graph.onInvalidateDelegate += OnModelInvalidated;
            model.position = new Vector2(32.0f, 32.0f);
            graph.RemoveAllChildren();
            graph.onInvalidateDelegate -= OnModelInvalidated;

            Assert.AreEqual("OnInvalidateDelegate  (UnityEditor.VFX.VFXGraph) kStructureChanged", s_logs[0]);
            Assert.AreEqual("OnInvalidateDelegate  (VFXDummyModel) kUIChanged", s_logs[1]);

            // Removal
            Assert.AreEqual("OnInvalidateDelegate  (UnityEditor.VFX.VFXGraph) kStructureChanged", s_logs[2]);
            Assert.AreEqual("OnInvalidateDelegate  (UnityEditor.VFX.VFXGraph) kStructureChanged", s_logs[3]);
            Assert.AreEqual("OnInvalidateDelegate  (UnityEditor.VFX.VFXGraph) kStructureChanged", s_logs[4]);
        }

        private void OnModelInvalidated(VFXModel model, VFXModel.InvalidationCause cause)
        {
            s_logs.Add("OnInvalidateDelegate " + model + " " + cause);
        }
    }
}
#endif
