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

        // VFXModelA <>- VFXModelB <>- VFXModelC
        private class VFXModelA : VFXModel<VFXModel,  VFXModelB> 
        {
            protected override void OnInvalidate(InvalidationCause cause) { s_logs.Add("OnInvalidate VFXModelA " + cause); }
            protected override void OnAdded() { s_logs.Add("OnAdded VFXModelA"); }
            protected override void OnRemoved() { s_logs.Add("OnRemoved VFXModelA"); }
        }

        private class VFXModelB : VFXModel<VFXModelA, VFXModelC> 
        {
            protected override void OnInvalidate(InvalidationCause cause) { s_logs.Add("OnInvalidate VFXModelB " + cause); }
            protected override void OnAdded() { s_logs.Add("OnAdded VFXModelB"); }
            protected override void OnRemoved() { s_logs.Add("OnRemoved VFXModelB"); }
        }

        private class VFXModelC : VFXModel<VFXModelB, VFXModel>  
        {
            protected override void OnInvalidate(InvalidationCause cause) { s_logs.Add("OnInvalidate VFXModelC " + cause); }
            protected override void OnAdded() { s_logs.Add("OnAdded VFXModelC"); }
            protected override void OnRemoved() { s_logs.Add("OnRemoved VFXModelC"); }

            public override bool AcceptChild(VFXModel child, int index = -1) { return false; }
        }

        [Test]
        public void AddChild()
        {
            VFXModel modelA = new VFXModelA();
            VFXModel modelB = new VFXModelB();
            VFXModel modelC = new VFXModelC();

            // Test both interfaces
            s_logs.Clear();
            modelA.AddChild(modelB);
            modelC.Attach(modelB);

            Assert.AreEqual(1, modelA.GetNbChildren());
            Assert.AreEqual(modelB, modelA.GetChild(0));
            Assert.AreEqual(modelA, modelB.GetParent());

            Assert.AreEqual(1, modelB.GetNbChildren());
            Assert.AreEqual(modelC, modelB.GetChild(0));
            Assert.AreEqual(modelB, modelC.GetParent());

            Assert.AreEqual(5, s_logs.Count);
            Assert.AreEqual("OnAdded VFXModelB",                    s_logs[0]);
            Assert.AreEqual("OnInvalidate VFXModelA kModelChanged", s_logs[1]);
            Assert.AreEqual("OnAdded VFXModelC",                    s_logs[2]);
            Assert.AreEqual("OnInvalidate VFXModelB kModelChanged", s_logs[3]);
            Assert.AreEqual("OnInvalidate VFXModelA kModelChanged", s_logs[4]);
        }

        [Test]
        public void InsertChild()
        {
            VFXModel modelA = new VFXModelA();
            VFXModel modelB0 = new VFXModelB();
            VFXModel modelB1 = new VFXModelB();
            VFXModel modelB2 = new VFXModelB();

            s_logs.Clear();
            modelA.AddChild(modelB2);
            modelA.AddChild(modelB0,0);
            modelA.AddChild(modelB1,1);

            Assert.AreEqual(3, modelA.GetNbChildren(), 3);
            Assert.AreEqual(modelB0, modelA.GetChild(0));
            Assert.AreEqual(modelB1, modelA.GetChild(1));
            Assert.AreEqual(modelB2, modelA.GetChild(2));

            Assert.AreEqual(6, s_logs.Count);
            Assert.AreEqual("OnAdded VFXModelB",                    s_logs[0]);
            Assert.AreEqual("OnInvalidate VFXModelA kModelChanged", s_logs[1]);
            Assert.AreEqual("OnAdded VFXModelB",                    s_logs[2]);
            Assert.AreEqual("OnInvalidate VFXModelA kModelChanged", s_logs[3]);
            Assert.AreEqual("OnAdded VFXModelB",                    s_logs[4]);
            Assert.AreEqual("OnInvalidate VFXModelA kModelChanged", s_logs[5]);
        }

        [Test]
        public void RemoveChild()
        {
            VFXModel modelA = new VFXModelA();
            VFXModel modelB = new VFXModelB();
            VFXModel modelC = new VFXModelC();

            // First add children but dont notify
            modelB.Attach(modelA,false);
            modelC.Attach(modelB,false);

            // Test both interfaces
            s_logs.Clear();
            modelC.Detach();
            modelA.RemoveChild(modelB);

            Assert.AreEqual(0, modelA.GetNbChildren());
            Assert.IsNull(modelB.GetParent());

            Assert.AreEqual(0, modelB.GetNbChildren());
            Assert.IsNull(modelC.GetParent());

            Assert.AreEqual(5, s_logs.Count);
            Assert.AreEqual("OnRemoved VFXModelC",                  s_logs[0]);
            Assert.AreEqual("OnInvalidate VFXModelB kModelChanged", s_logs[1]);
            Assert.AreEqual("OnInvalidate VFXModelA kModelChanged", s_logs[2]);
            Assert.AreEqual("OnRemoved VFXModelB",                  s_logs[3]);
            Assert.AreEqual("OnInvalidate VFXModelA kModelChanged", s_logs[4]);
        }

        [Test]
        public void ChangeParent()
        {
            VFXModel modelA0 = new VFXModelA();
            VFXModel modelA1 = new VFXModelA();
            VFXModel modelB = new VFXModelB();

            s_logs.Clear();
            modelA0.AddChild(modelB);
            modelA1.AddChild(modelB);

            Assert.AreEqual(0, modelA0.GetNbChildren());
            Assert.AreEqual(1, modelA1.GetNbChildren());
            Assert.AreEqual(modelA1, modelB.GetParent());

            Assert.AreEqual(6, s_logs.Count);
            Assert.AreEqual("OnAdded VFXModelB",                    s_logs[0]);
            Assert.AreEqual("OnInvalidate VFXModelA kModelChanged", s_logs[1]);
            Assert.AreEqual("OnRemoved VFXModelB",                  s_logs[2]);
            Assert.AreEqual("OnInvalidate VFXModelA kModelChanged", s_logs[3]);
            Assert.AreEqual("OnAdded VFXModelB",                    s_logs[4]);
            Assert.AreEqual("OnInvalidate VFXModelA kModelChanged", s_logs[5]);
        }

        [Test]
        public void AddChild_IncompatibleModel()
        {
            VFXModel modelA = new VFXModelA();
            VFXModel modelB = new VFXModelB();

            s_logs.Clear();
            Assert.Throws<ArgumentException>( () =>
                modelB.AddChild(modelA)
            );

            Assert.AreEqual(0, modelB.GetNbChildren());
            Assert.IsNull(modelA.GetParent());
            Assert.AreEqual(0, s_logs.Count);
        }

        [Test]
        public void AddChild_OutOfBounds()
        {
            VFXModel modelA = new VFXModelA();
            VFXModel modelB = new VFXModelB();

            s_logs.Clear();
            Assert.Throws<ArgumentException>( () =>
                modelA.AddChild(modelB,2)
            );

            Assert.AreEqual(0, modelB.GetNbChildren());
            Assert.IsNull(modelA.GetParent());
            Assert.AreEqual(0, s_logs.Count);
        }
    }
}