using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    /// Formats the provided descriptor into a piece-wise linear slider with contextual slider markers, tooltips, and icons.
    /// </summary>
    class HDPiecewiseLightUnitSlider : LightUnitSlider
    {
        struct Piece
        {
            public Vector2 domain;
            public Vector2 range;

            public float directM;
            public float directB;
            public float inverseM;
            public float inverseB;
        }

        // Piecewise function indexed by value ranges.
        private readonly Dictionary<Vector2, Piece> m_PiecewiseFunctionMap = new Dictionary<Vector2, Piece>();

        static void ComputeTransformationParameters(float x0, float x1, float y0, float y1, out float m, out float b)
        {
            m = (y0 - y1) / (x0 - x1);
            b = (m * -x0) + y0;
        }

        static float DoTransformation(in float x, in float m, in float b) => (m * x) + b;

        // Ensure clamping to (0,1) as sometimes the function evaluates to slightly below 0 (breaking the handle).
        static float ValueToSlider(Piece p, float x) => Mathf.Clamp01(DoTransformation(x, p.inverseM, p.inverseB));
        static float SliderToValue(Piece p, float x) => DoTransformation(x, p.directM, p.directB);

        // Ideally we want a continuous, monotonically increasing function, but this is useful as we can easily fit a
        // distribution to a set of (huge) value ranges onto a slider.
        public HDPiecewiseLightUnitSlider(LightUnitSliderUIDescriptor descriptor) : base(descriptor)
        {
            // Sort the ranges into ascending order
            var sortedRanges = m_Descriptor.valueRanges.OrderBy(x => x.value.x).ToArray();
            var sliderDistribution = m_Descriptor.sliderDistribution;

            // Compute the transformation for each value range.
            for (int i = 0; i < sortedRanges.Length; i++)
            {
                var r = sortedRanges[i].value;

                var x0 = sliderDistribution[i + 0];
                var x1 = sliderDistribution[i + 1];
                var y0 = r.x;
                var y1 = r.y;

                Piece piece;
                piece.domain = new Vector2(x0, x1);
                piece.range = new Vector2(y0, y1);

                ComputeTransformationParameters(x0, x1, y0, y1, out piece.directM, out piece.directB);

                // Compute the inverse
                ComputeTransformationParameters(y0, y1, x0, x1, out piece.inverseM, out piece.inverseB);

                m_PiecewiseFunctionMap.Add(sortedRanges[i].value, piece);
            }
        }

        protected override float GetPositionOnSlider(float value, Vector2 valueRange)
        {
            if (!m_PiecewiseFunctionMap.TryGetValue(valueRange, out var piecewise))
                return -1f;

            return ValueToSlider(piecewise, value);
        }

        // Search for the corresponding piece-wise function to a value on the domain and update the input piece to it.
        // Returns true if search was successful and an update was made, false otherwise.
        bool UpdatePiece(ref Piece piece, float x)
        {
            foreach (var pair in m_PiecewiseFunctionMap)
            {
                var p = pair.Value;

                if (x >= p.domain.x && x <= p.domain.y)
                {
                    piece = p;

                    return true;
                }
            }

            return false;
        }

        void SliderOutOfBounds(Rect rect, ref float value)
        {
            EditorGUI.BeginChangeCheck();
            var internalValue = GUI.HorizontalSlider(rect, value, 0f, 1f);
            if (EditorGUI.EndChangeCheck())
            {
                Piece p = new Piece();
                UpdatePiece(ref p, internalValue);
                value = SliderToValue(p, internalValue);
            }
        }

        protected override void DoSlider(Rect rect, ref float value, Vector2 sliderRange, Vector2 valueRange)
        {
            // Map the internal slider value to the current piecewise function
            if (!m_PiecewiseFunctionMap.TryGetValue(valueRange, out var piece))
            {
                // Assume that if the piece is not found, that means the unit value is out of bounds.
                SliderOutOfBounds(rect, ref value);
                return;
            }

            // Maintain an internal value to support a single linear continuous function
            EditorGUI.BeginChangeCheck();
            var internalValue = GUI.HorizontalSlider(rect, ValueToSlider(piece, value), 0f, 1f);
            if (EditorGUI.EndChangeCheck())
            {
                // Ensure that the current function piece is being used to transform the value
                UpdatePiece(ref piece, internalValue);
                value = SliderToValue(piece, internalValue);
            }
        }
    }
}
