using NUnit.Framework;

namespace UnityEditor.ShaderGraph
{
    static class AssertHelpers
    {
        public static void IsNotNull(object anObject, string message, params object[] args)
        {
#if SG_ASSERTIONS
            Assert.IsNotNull(anObject, message, args);
#endif
        }

        public static void Fail(string message, params object[] args)
        {
#if SG_ASSERTIONS
            Assert.Fail(message, args);
#endif
        }
    }
}
