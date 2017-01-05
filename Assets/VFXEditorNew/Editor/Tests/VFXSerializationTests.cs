using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Graphing;
using System.Collections.Generic;

namespace UnityEditor.VFX.Test
{
    class VFXAssetTest : ScriptableObject, ISerializationCallbackReceiver
    {
        [NonSerialized]
        public List<VFXSystem> m_Roots;

        [SerializeField]
        private List<SerializationHelper.JSONSerializedElement> m_SerializedRoots = null;
 
        public virtual void OnBeforeSerialize()
        {
            m_SerializedRoots = SerializationHelper.Serialize<VFXSystem>(m_Roots);
        }

        public virtual void OnAfterDeserialize()
        {
            m_Roots = SerializationHelper.Deserialize<VFXSystem>(m_SerializedRoots, null);
            //m_SerializedRoots = null; // No need to keep it
        }

        void OnEnable()
        {
            if (m_Roots == null)
                m_Roots = new List<VFXSystem>();
        }
    }

    [TestFixture]
    public class VFXSerializationTests
    {
        class VFXContextDescInit : VFXContextDesc
        {
            public VFXContextDescInit() : base(VFXContextDesc.Type.kTypeInit, "init") { }
        }

        class VFXContextDescUpdate : VFXContextDesc
        {
            public VFXContextDescUpdate() : base(VFXContextDesc.Type.kTypeUpdate, "update") { }
        }

        class VFXContextDescOutput : VFXContextDesc
        {
            public VFXContextDescOutput() : base(VFXContextDesc.Type.kTypeOutput, "output") { }
        }


        [Test]
        public void SerializeModel()
        {
            VFXAssetTest assetSrc = ScriptableObject.CreateInstance<VFXAssetTest>();
            VFXAssetTest assetDst = ScriptableObject.CreateInstance<VFXAssetTest>();

            VFXSystem system0 = new VFXSystem();
            system0.AddChild(new VFXContext(new VFXContextDescInit()));
            system0.AddChild(new VFXContext(new VFXContextDescUpdate()));
            system0.AddChild(new VFXContext(new VFXContextDescOutput()));

            VFXSystem system1 = new VFXSystem();
            system1.AddChild(new VFXContext(new VFXContextDescInit()));
            system1.AddChild(new VFXContext(new VFXContextDescOutput()));

            assetSrc.m_Roots.Add(system0);
            assetSrc.m_Roots.Add(system1);

            EditorUtility.CopySerialized(assetSrc, assetDst);

            Assert.AreEqual(2, assetDst.m_Roots.Count);
            Assert.AreEqual(3, assetDst.m_Roots[0].GetNbChildren());
            Assert.AreEqual(2, assetDst.m_Roots[1].GetNbChildren());

            Assert.IsNotNull(assetDst.m_Roots[0].GetChild(0).Desc);
            Assert.IsNotNull(assetDst.m_Roots[0].GetChild(1).Desc);
            Assert.IsNotNull(assetDst.m_Roots[0].GetChild(2).Desc);
            Assert.IsNotNull(assetDst.m_Roots[1].GetChild(0).Desc);
            Assert.IsNotNull(assetDst.m_Roots[1].GetChild(1).Desc);

        }
    }
}