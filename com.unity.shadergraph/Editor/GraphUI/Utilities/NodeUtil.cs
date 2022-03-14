using System;
using System.Globalization;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class NodeUtil
    {
        public static string FloatToShaderValue(float value)
        {
            if (Single.IsPositiveInfinity(value))
            {
                return "1.#INF";
            }
            if (Single.IsNegativeInfinity(value))
            {
                return "-1.#INF";
            }
            if (Single.IsNaN(value))
            {
                return "NAN";
            }

            return value.ToString(CultureInfo.InvariantCulture);
        }

        // A number large enough to become Infinity (~FLOAT_MAX_VALUE * 10) + explanatory comment
        private const string k_ShaderLabInfinityAlternatrive = "3402823500000000000000000000000000000000 /* Infinity */";

        // ShaderLab doesn't support Scientific Notion nor Infinity. To stop from generating a broken shader we do this.
        public static string FloatToShaderValueShaderLabSafe(float value)
        {
            if (Single.IsPositiveInfinity(value))
            {
                return k_ShaderLabInfinityAlternatrive;
            }
            if (Single.IsNegativeInfinity(value))
            {
                return "-" + k_ShaderLabInfinityAlternatrive;
            }
            if (Single.IsNaN(value))
            {
                return "NAN"; // A real error has occured, in this case we should break the shader.
            }

            // For single point precision, reserve 54 spaces (e-45 min + ~9 digit precision). See floating-point-numeric-types (Microsoft docs).
            return value.ToString("0.######################################################", CultureInfo.InvariantCulture);
        }
    }
}
