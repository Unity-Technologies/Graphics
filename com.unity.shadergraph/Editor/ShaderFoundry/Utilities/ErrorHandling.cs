using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderFoundry
{
    internal class ErrorHandling
    {
        // TODO @ SHADERS: This is effectively a stub for us to report error messages and later fill this in with more information (an asset location of some kind). This will make updating later easier as they all share a common location.
        internal static void ReportError(string message)
        {
            throw new Exception(message);
        }
    }
}
