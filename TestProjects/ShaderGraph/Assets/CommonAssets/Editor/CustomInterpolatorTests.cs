using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.ShaderGraph.UnitTests.Controllers;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.UnitTests
{
    [TestFixture]
    internal class CustomInterpolatorTests
    {
        static string kGraphName = "Assets/CommonAssets/Graphs/CustomInterpolatorThreshold.shadergraph";


        public void LoadGraph()
        {
            GraphData m_Graph;
            List<PropertyCollector.TextureInfo> lti;
            var assetCollection = new AssetCollection();
            ShaderGraphImporter.GetShaderText(kGraphName, out lti, assetCollection, out m_Graph);
            Assert.NotNull(m_Graph, $"Invalid graph data found for {kGraphName}");

            m_Graph.ValidateGraph();

            var m_Collector = new PropertyCollector();
            m_Graph.CollectShaderProperties(m_Collector, GenerationMode.ForReals);

            // Open up the window
            if (!ShaderGraphImporterEditor.ShowGraphEditWindow(kGraphName))
            {
                Assert.Fail("ShaderGraphImporterEditor.ShowGraphEditWindow could not open " + kGraphName);
            }

            var m_Window = EditorWindow.GetWindow<MaterialGraphEditWindow>();
        }

    }
}
