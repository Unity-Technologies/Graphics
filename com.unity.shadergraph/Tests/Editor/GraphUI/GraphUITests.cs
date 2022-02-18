using System;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using UnityEngine.UIElements;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;

namespace UnityEditor.ShaderGraph.GraphUI.UnitTests
{
    [TestFixture]
    class UITestFixture
    {
        static readonly Rect k_WindowRect = new Rect(Vector2.zero, new Vector2(SelectionDragger.panAreaWidth * 8, SelectionDragger.panAreaWidth * 6));

        ShaderGraphEditorWindow m_EditorWindow;

        [OneTimeSetUp]
        public void Setup()
        {
            //m_EditorWindow = EditorWindow.GetWindowWithRect<ShaderGraphEditorWindow>(k_WindowRect);
        }

        [TearDown]
        public void TestCleanup()
        {
        }
    }
}
