using NUnit.Framework;
using UnityEngine;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    public class VFXModelTests
    {
        // VFXModelA <>- VFXModelB <>- VFXModelC
        private class VFXModelA : VFXModel<VFXModel,  VFXModelB> 
        {
            protected override void OnInvalidate(InvalidationCause cause) { Debug.Log("OnInvalidate " + this.GetType().Name + " " + cause); }
            protected override void OnAdded() { Debug.Log("OnAdded " + this.GetType().Name); }
            protected override void OnRemoved() { Debug.Log("OnRemoved " + this.GetType().Name); }
        }

        private class VFXModelB : VFXModel<VFXModelA, VFXModelC> 
        {}

        private class VFXModelC : VFXModel<VFXModelB, VFXModel>  
        {
            public override bool AcceptChild(VFXModel child, int index = -1) { return false; }
        }

        [Test]
        public void CanAddChild()
        {
            VFXModel modelA = new VFXModelA();
            VFXModel modelB = new VFXModelB();
            VFXModel modelC = new VFXModelC();

            // Test both interfaces
            modelA.AddChild(modelB);
            modelC.Attach(modelB);

            Assert.AreEqual(modelA.GetNbChildren(), 1);
            Assert.AreEqual(modelA.GetChild(0), modelB);
            Assert.AreEqual(modelB.GetParent(), modelA);

            Assert.AreEqual(modelB.GetNbChildren(), 1);
            Assert.AreEqual(modelB.GetChild(0), modelC);
            Assert.AreEqual(modelC.GetParent(), modelB);
        }

        [Test]
        public void CanInsertChild()
        {
            VFXModel modelA = new VFXModelA();
            VFXModel modelB0 = new VFXModelB();
            VFXModel modelB1 = new VFXModelB();
            VFXModel modelB2 = new VFXModelB();

            modelA.AddChild(modelB2);
            modelA.AddChild(modelB0,0);
            modelA.AddChild(modelB1,1);

            Assert.AreEqual(modelA.GetNbChildren(), 3);
            Assert.AreEqual(modelA.GetChild(0), modelB0);
            Assert.AreEqual(modelA.GetChild(1), modelB1);
            Assert.AreEqual(modelA.GetChild(2), modelB2);
        }

        [Test]
        public void CanRemoveChild()
        {
            VFXModel modelA = new VFXModelA();
            VFXModel modelB = new VFXModelB();
            VFXModel modelC = new VFXModelC();

            // First add children but dont notify
            modelB.Attach(modelA,false);
            modelB.Attach(modelA, false);

            // Test both interfaces
            modelC.Detach();
            modelA.RemoveChild(modelB);

            Assert.AreEqual(modelA.GetNbChildren(), 0);
            Assert.IsNull(modelB.GetParent());

            Assert.AreEqual(modelB.GetNbChildren(), 0);
            Assert.IsNull(modelC.GetParent());
        }
    }
}