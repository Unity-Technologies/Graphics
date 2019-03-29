using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using NUnit.Framework;
using UnityEditor.Graphing;
using UnityEngine;
using UnityEditor.Graphing.Util;
using UnityEditor.Rendering;

namespace UnityEditor.ShaderGraph.UnitTests
{
    class TestMessageManager : MessageManager
    {
        public Dictionary<object, Dictionary<Identifier, List<ShaderMessage>>> Messages
        {
            get { return m_Messages; }
        }
    }
    
    class MessageManagerTests
    {
        TestMessageManager m_EmptyMgr;
        TestMessageManager m_ComplexMgr;
        string p0 = "Provider 0";
        string p1 = "Provider 1";
        Identifier node0 = new Identifier(0);
        Identifier node1 = new Identifier(1);
        Identifier node2 = new Identifier(2);
        ShaderMessage e0 = new ShaderMessage("e0");
        ShaderMessage e1 = new ShaderMessage("e1");
        ShaderMessage e2 = new ShaderMessage("e2");
        ShaderMessage e3 = new ShaderMessage("e3");
        ShaderMessage w0 = new ShaderMessage("w0", ShaderCompilerMessageSeverity.Warning);
        ShaderMessage w1 = new ShaderMessage("w1", ShaderCompilerMessageSeverity.Warning);

        [SetUp]
        public void Setup()
        {
            m_EmptyMgr = new TestMessageManager();
            
            m_ComplexMgr = new TestMessageManager();
            m_ComplexMgr.AddOrAppendError(p0, node0, e0);
            m_ComplexMgr.AddOrAppendError(p0, node0, e1);
            m_ComplexMgr.AddOrAppendError(p0, node1, e2);
            m_ComplexMgr.AddOrAppendError(p1, node0, e1);
            m_ComplexMgr.AddOrAppendError(p1, node1, e0);
            m_ComplexMgr.AddOrAppendError(p1, node1, e1);
            m_ComplexMgr.AddOrAppendError(p1, node2, e3);
        }

        // Simple helper to avoid typing that ungodly generic type
        static List<KeyValuePair<Identifier, List<ShaderMessage>>> GetListFrom(MessageManager mgr)
        {
            return new List<KeyValuePair<Identifier, List<ShaderMessage>>>(mgr.GetNodeMessages());
        }

        [Test]
        public void NewManager_IsEmpty()
        {
            var ret = GetListFrom(m_EmptyMgr);
            Assert.IsEmpty(ret);
        }
        
        [Test]
        public void AddMessage_CreatesMessage()
        {
            m_EmptyMgr.AddOrAppendError(p0, node0, e0);

            var ret = GetListFrom(m_EmptyMgr);
            Assert.IsNotEmpty(ret);
            Assert.AreEqual(node0, ret[0].Key);
            Assert.IsNotEmpty(ret[0].Value);
            Assert.AreEqual(e0, ret[0].Value[0]);
        }

        [Test]
        public void AddMessage_DirtiesManager()
        {
            m_EmptyMgr.AddOrAppendError(p0, node0, e0);

            Assert.IsTrue(m_EmptyMgr.nodeMessagesChanged);
        }

        [Test]
        public void GettingMessages_ClearsDirtyFlag()
        {
            m_EmptyMgr.AddOrAppendError(p0, node0, e0);
            GetListFrom(m_EmptyMgr);

            Assert.IsFalse(m_EmptyMgr.nodeMessagesChanged);
        }

        [Test]
        public void GettingMessages_DoesNotChangeLists()
        {
            m_EmptyMgr.AddOrAppendError(p0, node0, e0);
            m_EmptyMgr.AddOrAppendError(p0, node0, e1);
            m_EmptyMgr.AddOrAppendError(p1, node0, e2);

            GetListFrom(m_EmptyMgr);
            
            Assert.AreEqual(2, m_EmptyMgr.Messages[p0][node0].Count);
            Assert.AreEqual(e0, m_EmptyMgr.Messages[p0][node0][0]);
            Assert.AreEqual(e1, m_EmptyMgr.Messages[p0][node0][1]);
            Assert.AreEqual(1, m_EmptyMgr.Messages[p1][node0].Count);
            Assert.AreEqual(e2, m_EmptyMgr.Messages[p1][node0][0]);
        }
        
        [Test]
        public void RemoveNode_DoesNotDirty_IfNodeDoesNotExist()
        {
            m_EmptyMgr.AddOrAppendError(p0, node0, e0);
            GetListFrom(m_EmptyMgr);
            
            m_EmptyMgr.RemoveNode(node1);
            
            Assert.IsFalse(m_EmptyMgr.nodeMessagesChanged);
        }
        
        [Test]
        public void RemoveNode_DirtiesList_IfNodeExists()
        {
            m_EmptyMgr.AddOrAppendError(p0, node0, e0);
            GetListFrom(m_EmptyMgr);
            
            m_EmptyMgr.RemoveNode(node0);
            
            Assert.IsTrue(m_EmptyMgr.nodeMessagesChanged);
        }

        [Test]
        public void RemoveNode_RemovesNode()
        {
            m_EmptyMgr.AddOrAppendError(p0, node0, e0);
            m_EmptyMgr.RemoveNode(node0);

            var ret = GetListFrom(m_EmptyMgr);
            Assert.IsEmpty(ret);
        }
        
        [Test]
        public void RemoveNode_RemovesNode_FromAllProvides()
        {
            m_EmptyMgr.AddOrAppendError(p0, node0, e0);
            m_EmptyMgr.AddOrAppendError(p1, node0, e1);
            m_EmptyMgr.RemoveNode(node0);

            var ret = GetListFrom(m_EmptyMgr);
            Assert.IsEmpty(ret);
        }
        
        [Test]
        public void AppendMessage_AppendsMessage()
        {
            m_EmptyMgr.AddOrAppendError(p0, node0, e0);
            m_EmptyMgr.AddOrAppendError(p0, node0, e1);
            
            var ret = GetListFrom(m_EmptyMgr);
            Assert.IsNotEmpty(ret);
            Assert.AreEqual(node0, ret[0].Key);
            Assert.AreEqual(2, ret[0].Value.Count);
            Assert.AreEqual(e0, ret[0].Value[0]);
            Assert.AreEqual(e1, ret[0].Value[1]);
        }

        [Test]
        public void Warnings_SortedAfterErrors()
        {
            var mixedMgr = new MessageManager();
            mixedMgr.AddOrAppendError(p0, node0, e0);
            mixedMgr.AddOrAppendError(p0, node0, w0);
            mixedMgr.AddOrAppendError(p0, node0, e1);
            mixedMgr.AddOrAppendError(p0, node0, w1);

            var ret = GetListFrom(mixedMgr)[0].Value;
            Assert.AreEqual(e0, ret[0]);
            Assert.AreEqual(e1, ret[1]);
            Assert.AreEqual(w0, ret[2]);
            Assert.AreEqual(w1, ret[3]);
        }

        [Test]
        public void Warnings_FromDifferentProviders_SortedAfterErrors()
        {
            var mixedMgr = new MessageManager();
            mixedMgr.AddOrAppendError(p0, node0, e0);
            mixedMgr.AddOrAppendError(p0, node0, w0);
            mixedMgr.AddOrAppendError(p1, node0, e1);
            mixedMgr.AddOrAppendError(p1, node0, w1);
            
            var ret = GetListFrom(mixedMgr)[0].Value;
            Assert.AreEqual(e0, ret[0]);
            Assert.AreEqual(e1, ret[1]);
            Assert.AreEqual(w0, ret[2]);
            Assert.AreEqual(w1, ret[3]);
        }
        
        [Test]
        public void MultipleNodes_RemainSeparate()
        {
            m_EmptyMgr.AddOrAppendError(p0, node0, e0);
            m_EmptyMgr.AddOrAppendError(p0, node1, e1);
            
            var ret = GetListFrom(m_EmptyMgr);
            Assert.AreEqual(2, ret.Count);
            Assert.AreEqual(node0, ret[0].Key);
            Assert.AreEqual(e0, ret[0].Value[0]);
            Assert.AreEqual(node1, ret[1].Key);
            Assert.AreEqual(e1, ret[1].Value[0]);
        }
        
        [Test]
        public void MultipleCreators_AggregatePerNode()
        {
            m_EmptyMgr.AddOrAppendError(p0, node0, e0);
            m_EmptyMgr.AddOrAppendError(p1, node0, e1);
            
            var ret = GetListFrom(m_EmptyMgr);
            Assert.IsNotEmpty(ret);
            Assert.AreEqual(node0, ret[0].Key);
            Assert.AreEqual(2, ret[0].Value.Count);
            Assert.AreEqual(e0, ret[0].Value[0]);
            Assert.AreEqual(e1, ret[0].Value[1]);
        }

        [Test]
        public void DuplicateEntries_AreNotIgnored()
        {
            m_EmptyMgr.AddOrAppendError(p0, node0, e0);
            m_EmptyMgr.AddOrAppendError(p0, node0, e0);
            
            var ret = GetListFrom(m_EmptyMgr);
            Assert.IsNotEmpty(ret);
            Assert.AreEqual(node0, ret[0].Key);
            Assert.AreEqual(2, ret[0].Value.Count);
            Assert.AreEqual(e0, ret[0].Value[0]);
            Assert.AreEqual(e0, ret[0].Value[1]);
        }

        [Test]
        public void ClearAllFromProvider_ZerosMessageLists()
        {
            m_ComplexMgr.ClearAllFromProvider(p1);

            var ret = GetListFrom(m_ComplexMgr);
            Assert.IsNotEmpty(ret);
            Assert.AreEqual(3, ret.Count);
            Assert.AreEqual(node0, ret[0].Key);
            Assert.AreEqual(2, ret[0].Value.Count);
            Assert.AreEqual(node1, ret[1].Key);
            Assert.AreEqual(1, ret[1].Value.Count);
            Assert.AreEqual(node2, ret[2].Key);
            Assert.IsEmpty(ret[2].Value);
        }

        [Test]
        public void GetList_RemovesZeroLengthLists()
        {
            m_ComplexMgr.ClearAllFromProvider(p1);
            var ret = GetListFrom(m_ComplexMgr);
            Assert.IsNotEmpty(ret.Where(kvp => kvp.Key.Equals(node2)));
            Assert.IsEmpty(ret.First(kvp => kvp.Key.Equals(node2)).Value);

            ret = GetListFrom(m_ComplexMgr);
            Assert.IsEmpty(ret.Where(kvp => kvp.Key.Equals(node2)));
        }

        [Test]
        public void ClearNodesFromProvider_ClearsNodes()
        {
            var n0 = new AndNode {tempId = node0};
            var n2 = new AndNode {tempId = node2};
            var nodesToClear = new List<AbstractMaterialNode>() { n0, n2 };
            m_ComplexMgr.ClearNodesFromProvider(p1, nodesToClear);

            var ret = GetListFrom(m_ComplexMgr);
            Assert.AreEqual(2, ret.Find(kpv => kpv.Key.Equals(node0)).Value.Count);
            Assert.IsEmpty(ret.Find(kvp => kvp.Key.Equals(node2)).Value);
        }

        [Test]
        public void ClearNodesFromProvider_LeavesOtherNodes()
        {
            var n0 = new AndNode {tempId = node0};
            var n2 = new AndNode {tempId = node2};
            var nodesToClear = new List<AbstractMaterialNode>() { n0, n2 };
            m_ComplexMgr.ClearNodesFromProvider(p1, nodesToClear);

            var ret = GetListFrom(m_ComplexMgr);
            Assert.AreEqual(3, ret.Find(kpv => kpv.Key.Equals(node1)).Value.Count);
        }
    }
}

// m_ComplesMgr definition:
//    m_ComplexMgr.AddOrAppendError(p0, node0, e0);
//    m_ComplexMgr.AddOrAppendError(p0, node0, e1);
//    m_ComplexMgr.AddOrAppendError(p0, node1, e2);
//    m_ComplexMgr.AddOrAppendError(p1, node0, e1);
//    m_ComplexMgr.AddOrAppendError(p1, node1, e0);
//    m_ComplexMgr.AddOrAppendError(p1, node1, e1);
//    m_ComplexMgr.AddOrAppendError(p1, node2, e3);