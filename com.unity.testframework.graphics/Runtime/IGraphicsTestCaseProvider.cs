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
        /// Retrieve a single test case from scene path.
        /// </summary>
        /// <returns></returns>
        GraphicsTestCase GetTestCaseFromPath(string scenePath);

    }
}
