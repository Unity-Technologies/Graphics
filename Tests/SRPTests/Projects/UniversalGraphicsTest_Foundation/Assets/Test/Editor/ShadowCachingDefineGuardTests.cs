using System;
using NUnit.Framework;

namespace Unity.Testing.SRP.Universal.Foundation.Editor.ShadowCaching
{
    class ShadowCachingDefineGuardTests
    {
        const string k_ExpectDefineArgument = "-expect-urp-shadow-caching";

#if URP_SHADOW_CACHING
        const bool k_DefineCompiledIn = true;
#else
        const bool k_DefineCompiledIn = false;
#endif

        static bool RunExpectsDefine => Array.IndexOf(Environment.GetCommandLineArgs(), k_ExpectDefineArgument) >= 0;

        static string FailureMessage =>
            $"URP_SHADOW_CACHING compiled in: {k_DefineCompiledIn}. {k_ExpectDefineArgument} on the command line: {RunExpectsDefine}.";

        [Test]
        public void UrpShadowCachingVariant_WhenRunDoesNotExpectIt_IsNotCompiledIn()
        {
            if (!RunExpectsDefine)
                Assert.That(k_DefineCompiledIn, Is.False, FailureMessage);
        }

        [Test]
        public void UrpShadowCachingVariant_WhenRunExpectsIt_IsCompiledIn()
        {
            if (RunExpectsDefine)
                Assert.That(k_DefineCompiledIn, Is.True, FailureMessage);
        }
    }
}
