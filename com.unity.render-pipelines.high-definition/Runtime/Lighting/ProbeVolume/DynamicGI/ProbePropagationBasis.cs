namespace UnityEngine.Rendering.HighDefinition
{
    static class ProbePropagationBasis
    {
        // Spherical Gaussian
        
        // http://research.microsoft.com/en-us/um/people/johnsny/papers/sg.pdf
        public static float SGEvaluateFromDirection(float sgAmplitude, float sgSharpness, Vector3 sgMean, Vector3 direction)
        {
            // MADD optimized form of: a.amplitude * exp(a.sharpness * (dot(a.mean, direction) - 1.0));
            return sgAmplitude * Mathf.Exp(Vector3.Dot(sgMean, direction) * sgSharpness - sgSharpness);
        }

        public static float SGClampedCosineWindowEvaluateFromDirection(float sgAmplitude, float sgSharpness, Vector3 sgMean, Vector3 direction)
        {
            // MADD optimized form of: a.amplitude * exp(a.sharpness * (dot(a.mean, direction) - 1.0));
            float mDotD = Vector3.Dot(sgMean, direction);
            return sgAmplitude * Mathf.Clamp01(mDotD) * Mathf.Exp(mDotD * sgSharpness - sgSharpness);
        }
        
        // https://www.desmos.com/calculator/li7vrrctk6
        // https://www.shadertoy.com/view/NlyXzR
        public static float ComputeSGAmplitudeFromSharpnessBasis26Fit(float sharpness)
        {
            return sharpness * 0.0734695f + 0.00862805f;
        }

        public static float ComputeSGClampedCosineWindowAmplitudeFromSharpnessBasis26Fit(float sharpness)
        {
            return sharpness * 0.0633994f + 0.144223f;
        }

        public static float ComputeSGAmplitudeMultiplierFromAxisDirection(Vector3 axisDirection)
        {
            int componentNonZeroCount = 0;
            componentNonZeroCount += Mathf.Abs(axisDirection.x) > 1e-5 ? 1 : 0;
            componentNonZeroCount += Mathf.Abs(axisDirection.y) > 1e-5 ? 1 : 0;
            componentNonZeroCount += Mathf.Abs(axisDirection.z) > 1e-5 ? 1 : 0;
            return (componentNonZeroCount == 3)
                ? 0.912f // diagonal
                : ((componentNonZeroCount == 2)
                    ? 0.9595f // edge
                    : 1.2445f); // center
        }

        public static float ComputeSGAmplitudeFromSharpnessAndAxisBasis26Fit(float sharpness, Vector3 axisDirection)
        {
            return ComputeSGAmplitudeFromSharpnessBasis26Fit(sharpness) * ComputeSGAmplitudeMultiplierFromAxisDirection(axisDirection);
        }

        public static float ComputeSGClampedCosineWindowAmplitudeFromSharpnessAndAxisBasis26Fit(float sharpness, Vector3 axisDirection)
        {
            return ComputeSGClampedCosineWindowAmplitudeFromSharpnessBasis26Fit(sharpness) * ComputeSGAmplitudeMultiplierFromAxisDirection(axisDirection);
        }
        
        // Ambient Dice
        // https://www.shadertoy.com/view/NlyXzR

        public static float AmbientDiceEvaluateFromDirection(float amplitude, float sharpness, Vector3 axis, Vector3 direction)
        {
            return amplitude * Mathf.Pow(Mathf.Clamp01(Vector3.Dot(axis, direction)), sharpness);
        }
 
        public static float AmbientDiceWrappedEvaluateFromDirection(float amplitude, float sharpness, Vector3 axis, Vector3 direction)
        {
            return 0.5f * amplitude * Mathf.Pow(Mathf.Clamp01(Vector3.Dot(axis, direction) * 0.5f + 0.5f), sharpness);
        }

        public static void ComputeAmbientDiceSharpAmplitudeAndSharpnessFromAxisDirectionBasis26Fit(out float amplitude, out float sharpness, Vector3 axisDirection)
        {
            int componentNonZeroCount = 0;
            componentNonZeroCount += Mathf.Abs(axisDirection.x) > 1e-3 ? 1 : 0;
            componentNonZeroCount += Mathf.Abs(axisDirection.y) > 1e-3 ? 1 : 0;
            componentNonZeroCount += Mathf.Abs(axisDirection.z) > 1e-3 ? 1 : 0;

            amplitude = (componentNonZeroCount == 3)
                ? 0.3087f // diagonal
                : ((componentNonZeroCount == 2)
                    ? 0.693f // edge
                    : 0.64575f); // center
            sharpness = (componentNonZeroCount == 3)
                ? 9f // diagonal
                : 6f; // center
        }

        public static void ComputeAmbientDiceSofterAmplitudeAndSharpnessFromAxisDirectionBasis26Fit(out float amplitude, out float sharpness, Vector3 axisDirection)
        {
            int componentNonZeroCount = 0;
            componentNonZeroCount += Mathf.Abs(axisDirection.x) > 1e-3 ? 1 : 0;
            componentNonZeroCount += Mathf.Abs(axisDirection.y) > 1e-3 ? 1 : 0;
            componentNonZeroCount += Mathf.Abs(axisDirection.z) > 1e-3 ? 1 : 0;

            amplitude = (componentNonZeroCount == 3)
                ? 0.209916f // diagonal
                : ((componentNonZeroCount == 2)
                    ? 0.47124f // edge
                    : 0.43911f); // center
            sharpness = 4.0f;
        }

        public static void ComputeAmbientDiceSuperSoftAmplitudeAndSharpnessFromAxisDirectionBasis26Fit(out float amplitude, out float sharpness, Vector3 axisDirection)
        {
            amplitude = 0.23f;
            sharpness = 2.0f;
        }

        public static void ComputeAmbientDiceUltraSoftAmplitudeAndSharpnessFromAxisDirectionBasis26Fit(out float amplitude, out float sharpness, Vector3 axisDirection)
        {
            amplitude = 0.15f;
            sharpness = 1.0f;
        }
    }
}
