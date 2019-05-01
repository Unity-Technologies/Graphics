using UnityEngine;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Represents one automatically-generated graphics test case.
    /// </summary>
    public class GraphicsTestCase
    {
        private readonly string _scenePath;
        private readonly Texture2D _expectedImage;

        public GraphicsTestCase(string scenePath, Texture2D expectedImage)
        {
            _scenePath = scenePath;
            _expectedImage = expectedImage;
        }

        /// <summary>
        /// The path to the scene to be used for this test case.
        /// </summary>
        public string ScenePath { get { return _scenePath; } }

        /// <summary>
        /// The reference image that represents the expected output for this test case.
        /// </summary>
        public Texture2D ExpectedImage {  get { return _expectedImage; } }
    }
}
