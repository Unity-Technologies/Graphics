using System;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.ShaderGraph.GraphDelta;
using System.Linq;
using UnityEditor.ShaderGraph.HeadlessPreview.UnitTests;

namespace UnityEditor.ShaderGraph.HeadlessPreview.NodeTests
{

    // TODO: Consider async compile/rendering, need to wait until thread is done and results are returned
    // https://forum.unity.com/threads/support-for-async-await-in-tests.787853/#post-6838493

    [TestFixture]
    class DefaultPreviewsTestFixture
    {
        static ShaderGraphRegistry InitSGR()
        {
            ShaderGraphRegistry SGR = new();
            SGR.InitializeDefaults();
            return SGR;
        }

        static string[] InitNodeNames()
        {
            bool HasPreview(NodeHandler node)
            {
                var nodeKey = node.GetRegistryKey();
                var nodeUI = SGR.GetNodeUIDescriptor(nodeKey, node);

                if (nodeUI.HasPreview == false)
                    return false;

                bool hasPreviewableOutputPort = node.GetPorts().Where(e => !e.IsInput)?.First()?.GetTypeField()?.GetRegistryKey().Name == GraphType.kRegistryKey.Name;
                return hasPreviewableOutputPort;
            }

            return SGR.DefaultTopologies.GetNodes().Where(HasPreview).Select(e => e.ID.LocalPath).ToArray();
        }

        static ShaderGraphRegistry SGR = InitSGR();
        // Need to resolve the node names statically so that the Test Runner is happy and shows each node.
        static string[] nodeNames = InitNodeNames();

        [TestCaseSource("nodeNames")]
        public void DoesPreviewCompile(string nodeName)
        {
            Registry Registry = SGR.Registry;
            GraphHandler Graph = SGR.DefaultTopologies;

            HeadlessPreviewManager Preview = new();
            Preview.SetActiveRegistry(Registry);
            Preview.SetActiveGraph(Graph);
            Preview.Initialize("ThisDontMatter", new UnityEngine.Vector2(125, 125));

            var material = Preview.RequestNodePreviewMaterial(nodeName);
            var value = PreviewTestFixture.SampleMaterialColor(material);
            Assert.AreNotEqual(new Color(1,1,0,1), value);
        }
    }
}
