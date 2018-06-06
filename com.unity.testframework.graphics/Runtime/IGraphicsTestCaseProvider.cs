using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Describes an object that can provide GraphicsTestCase objects. THe framework provides different implementations
    /// for the Editor (which loads reference images directly from the Asset Database) and Players (which use the
    /// pre-built AssetBundle).
    /// </summary>
    public interface IGraphicsTestCaseProvider
    {
        /// <summary>
        /// Retrieve the list of test cases to generate tests for.
        /// </summary>
        /// <returns></returns>
        IEnumerable<GraphicsTestCase> GetTestCases();

        /// <summary>
        /// The color space that the test cases are for.
        /// </summary>
        ColorSpace ColorSpace { get; }

        /// <summary>
        /// The platform that the test cases are for.
        /// </summary>
        RuntimePlatform Platform { get; }

        /// <summary>
        /// The graphics device type that the test cases are for.
        /// </summary>
        GraphicsDeviceType GraphicsDevice { get; }
    }
}