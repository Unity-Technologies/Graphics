using System.Globalization;

namespace UnityEngine.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Holds information on a parsed float value.
    /// </summary>
    public readonly struct ExpressionValue : IValue
    {
        public readonly float Value;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpressionValue"/> class.
        /// </summary>
        /// <param name="value">The float value of the ExpressionValue.</param>
        public ExpressionValue(float value)
        {
            Value = value;
        }

        /// <summary>
        /// Returns a string that represents the parsed float value.
        /// </summary>
        /// <returns>A string that represents the parsed float value.</returns>
        public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
    }
}
