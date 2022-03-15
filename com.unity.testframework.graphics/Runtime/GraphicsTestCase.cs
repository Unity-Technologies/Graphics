using UnityEngine;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Represents one automatically-generated graphics test case.
    /// </summary>
    public class GraphicsTestCase
    {
        private readonly string _name;
        private readonly string _scenePath;
        private readonly CodeBasedGraphicsTestAttribute _codeBasedGraphicsTestAttribute;
        private readonly Texture2D _referenceImage;

        public GraphicsTestCase(string scenePath, Texture2D referenceImage)
        {
            _name = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            _scenePath = scenePath;
            _codeBasedGraphicsTestAttribute = null;
            _referenceImage = referenceImage;
        }

        public GraphicsTestCase(string name, CodeBasedGraphicsTestAttribute codeBasedGraphicsTestAttrib, Texture2D referenceImage)
        {
            _name = name;
            _scenePath = null;
            _codeBasedGraphicsTestAttribute = codeBasedGraphicsTestAttrib;
            _referenceImage = referenceImage;
        }

        /// <summary>
        /// The path to the scene to be used for this test case.
        /// </summary>
        public string ScenePath { get { return _scenePath; } }

        /// <summary>
        /// The name of the test to be displayed in the TestRunner window and the Graphics Test Results window.
        /// </summary>
        public string Name => _name;

        /// <summary>
        /// The associated <see cref="CodeBasedGraphicsTestAttribute"/> if the test case is code based.
        /// </summary>
        public CodeBasedGraphicsTestAttribute CodeBasedGraphicsTestAttribute => _codeBasedGraphicsTestAttribute;

        /// <summary>
        /// The reference image that represents the expected output for this test case.
        /// </summary>
        public Texture2D ReferenceImage { get { return _referenceImage; } }
    }
}
