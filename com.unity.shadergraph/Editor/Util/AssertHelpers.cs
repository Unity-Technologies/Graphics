using UnityEngine.Assertions;

namespace UnityEditor.ShaderGraph
{
    static class AssertHelpers
    {
        public static void IsNotNull(object anObject, string message)
        {
#if SG_ASSERTIONS
            Assert.IsNotNull(anObject, message);
#endif
        }

        public static void Fail(string message)
        {
#if SG_ASSERTIONS
            throw new AssertionException(message, null);
#endif
        }
    }
}
