using NUnit.Framework;
using UnityEditor.Rendering.Universal;

namespace UnityEngine.Rendering.Universal.Tests
{
    class UniversalLightingSearchColumnProvidersTests
    {
        // MaskField bit order: 0 = Realtime Direct, 1 = Realtime Indirect, 2 = Baked.
        const int k_RealtimeDirectBit = 1 << 0;
        const int k_BakedBit = 1 << 2;

        [Test]
        public void ToMaskValue_SupportsCombinationsAndIgnoresUnrelatedBits()
        {
            var flags = MaterialGlobalIlluminationFlags.RealtimeDirectEmission
                        | MaterialGlobalIlluminationFlags.BakedEmission
                        | MaterialGlobalIlluminationFlags.EmissiveIsBlack;

            Assert.AreEqual(k_RealtimeDirectBit | k_BakedBit,
                UniversalLightingSearchColumnProviders.ToMaskValue(flags),
                "URP emission is a multi-select mask; unrelated bits (EmissiveIsBlack) must be ignored.");
        }

        [Test]
        public void FromMaskValue_PreservesUnrelatedBits()
        {
            var current = MaterialGlobalIlluminationFlags.RealtimeIndirectEmission
                          | MaterialGlobalIlluminationFlags.EmissiveIsBlack;

            var result = UniversalLightingSearchColumnProviders.FromMaskValue(current, k_BakedBit);

            Assert.IsTrue((result & MaterialGlobalIlluminationFlags.BakedEmission) != 0,
                "The Baked bit selected in the mask must be applied.");
            Assert.IsTrue((result & MaterialGlobalIlluminationFlags.RealtimeIndirectEmission) == 0,
                "Emission-mode bits not present in the mask must be cleared.");
            Assert.IsTrue((result & MaterialGlobalIlluminationFlags.EmissiveIsBlack) != 0,
                "Unrelated bits (EmissiveIsBlack) must be preserved.");
        }
    }
}
