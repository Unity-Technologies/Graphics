namespace UnityEngine.Rendering
{
    /// <summary>
    /// An utility class to compute samples on the Halton sequence.
    /// https://en.wikipedia.org/wiki/Halton_sequence
    /// </summary>
    public static class HaltonSequence
    {
        /// <summary>
        /// Gets a deterministic sample in the Halton sequence.
        /// </summary>
        /// <param name="index">The index in the sequence.</param>
        /// <param name="radix">The radix of the sequence.</param>
        /// <returns>A sample from the Halton sequence.</returns>
        public static float Get(int index, int radix)
        {
            float result = 0f;
            float fraction = 1f / radix;

            while (index > 0)
            {
                result += (index % radix) * fraction;

                index /= radix;
                fraction /= radix;
            }

            return result;
        }
    }
}
