﻿using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Graphing;
 using UnityEngine;
 using UnityEngine.TestTools;

namespace UnityEditor.ShaderGraph.UnitTests
{
    public class ShaderSourceMapTests
    {
        class TestNode : AbstractMaterialNode
        {
        }

        [OneTimeSetUp]
        public void RunBeforeAnyTests()
        {
            Debug.unityLogger.logHandler = new ConsoleLogHandler();

            m_Shader = "Line 1\nLine 2\nLine 3\nLine 4\nLine 5\nLine 6\n" + // Node 1
                       "Line 7\nLine 8\nLine 9\nLine 10\nLine 11\nLine 12\n" + // Node 2
                       "Line 13\nLine 14\n" + // Node 3
                       "Line 15\nLine 16"; // Node 4

            m_Node1 = new TestNode();
            m_Node2 = new TestNode();
            m_Node3 = new TestNode();
            m_Node4 = new TestNode();
            
            testList = new List<ShaderStringMapping>()
            {
                CreateMapping(0, m_Node1, 41),
                CreateMapping(41, m_Node2, 44),
                CreateMapping(85, m_Node3, 16),
                CreateMapping(101, m_Node4, 15)
            };
            
            m_Map = new ShaderSourceMap(m_Shader, testList);
        }

        static ShaderStringMapping CreateMapping(int start, INode node, int count)
        {
            var map = new ShaderStringMapping();
            map.startIndex = start;
            map.count = count;
            map.node = node;
            return map;
        }


        string m_Shader;

        TestNode m_Node1;
        TestNode m_Node2;
        TestNode m_Node3;
        TestNode m_Node4;
        
        List<ShaderStringMapping> testList;
        ShaderSourceMap m_Map;
        
        [Test]
        public void FindNode_ReturnsNull_ForOutOfBoundsIndex()
        {

            Assert.IsNull(m_Map.FindNode(-1));
            Assert.IsNull(m_Map.FindNode(0));
            Assert.IsNull(m_Map.FindNode(17));
        }

        [Test]
        public void FindNode_FindsFirstNode()
        {
            
            Assert.AreEqual(m_Node1, m_Map.FindNode(1));
        }

        [Test]
        public void FindNode_FindsMiddleNodes()
        {
            Assert.AreEqual(m_Node1, m_Map.FindNode(6));
            Assert.AreEqual(m_Node2, m_Map.FindNode(7));
            Assert.AreEqual(m_Node2, m_Map.FindNode(12));
            Assert.AreEqual(m_Node3, m_Map.FindNode(13));
            Assert.AreEqual(m_Node3, m_Map.FindNode(14));
            Assert.AreEqual(m_Node4, m_Map.FindNode(15));
        }

        [Test]
        public void FindNode_FindsLastNode()
        {
            Assert.AreEqual(m_Node4, m_Map.FindNode(16));
        }

    }
}
