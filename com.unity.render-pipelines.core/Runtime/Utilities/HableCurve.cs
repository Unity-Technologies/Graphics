using static UnityEngine.Mathf;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// An implementation of Hable's artist-friendly tonemapping curve.
    /// http://filmicworlds.com/blog/filmic-tonemapping-with-piecewise-power-curves/
    /// </summary>
    public class HableCurve
    {
        /// <summary>
        /// Individual curve segment.
        /// </summary>
        public class Segment
        {
            /// <summary>
            /// The offset of the segment on the X axis.
            /// </summary>
            public float offsetX;

            /// <summary>
            /// The offset of the segment on the Y axis.
            /// </summary>
            public float offsetY;

            /// <summary>
            /// The scale of the segment on the X axis.
            /// </summary>
            public float scaleX;

            /// <summary>
            /// The scale of the segment on the Y axis.
            /// </summary>
            public float scaleY;

            /// <summary>
            /// <c>ln(A)</c> constant in the power curve <c>y = e^(ln(A) + B*ln(x))</c>.
            /// </summary>
            public float lnA;

            /// <summary>
            /// <c>B</c> constant in the power curve <c>y = e^(ln(A) + B*ln(x))</c>.
            /// </summary>
            public float B;

            /// <summary>
            /// Evaluate a point on the curve.
            /// </summary>
            /// <param name="x">The point to evaluate.</param>
            /// <returns>The value of the curve, at the point specified.</returns>
            public float Eval(float x)
            {
                float x0 = (x - offsetX) * scaleX;
                float y0 = 0f;

                // log(0) is undefined but our function should evaluate to 0. There are better ways
                // to handle this, but it's doing it the slow way here for clarity.
                if (x0 > 0)
                    y0 = Exp(lnA + B * Log(x0));

                return y0 * scaleY + offsetY;
            }
        }

        struct DirectParams
        {
            internal float x0;
            internal float y0;
            internal float x1;
            internal float y1;
            internal float W;

            internal float overshootX;
            internal float overshootY;

            internal float gamma;
        }

        /// <summary>
        /// The white point.
        /// </summary>
        public float whitePoint { get; private set; }

        /// <summary>
        /// The inverse of the white point.
        /// </summary>
        /// <seealso cref="whitePoint"/>
        public float inverseWhitePoint { get; private set; }

        /// <summary>
        /// The start of the linear section (middle segment of the curve).
        /// </summary>
        public float x0 { get; private set; }

        /// <summary>
        /// The end of the linear section (middle segment of the curve).
        /// </summary>
        public float x1 { get; private set; }


        /// <summary>
        /// The three segments of the curve.
        /// </summary>
        public readonly Segment[] segments = new Segment[3];

        /// <summary>
        /// Creates a new curve.
        /// </summary>
        public HableCurve()
        {
            for (int i = 0; i < 3; i++)
                segments[i] = new Segment();

            uniforms = new Uniforms(this);
        }

        /// <summary>
        /// Evaluates a point on the curve.
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public float Eval(float x)
        {
            float normX = x * inverseWhitePoint;
            int index = (normX < x0) ? 0 : ((normX < x1) ? 1 : 2);
            var segment = segments[index];
            float ret = segment.Eval(normX);
            return ret;
        }

        /// <summary>
        /// Initializes the curve.
        /// </summary>
        /// <param name="toeStrength">The strength of the transition between the curve's toe and the curve's mid-section. A value of 0 results in no transition and a value of 1 results in a very hard transition.</param>
        /// <param name="toeLength">The length of the curve's toe. Higher values result in longer toes and therefore contain more of the dynamic range.</param>
        /// <param name="shoulderStrength">The strength of the transition between the curve's midsection and the curve's shoulder. A value of 0 results in no transition and a value of 1 results in a very hard transition.</param>
        /// <param name="shoulderLength">The amount of f-stops to add to the dynamic range of the curve. This is how much of the highlights that the curve takes into account.</param>
        /// <param name="shoulderAngle">How much overshoot to add to the curve's shoulder.</param>
        /// <param name="gamma">A gamma correction to the entire curve.</param>
        public void Init(float toeStrength, float toeLength, float shoulderStrength, float shoulderLength, float shoulderAngle, float gamma)
        {
            var dstParams = new DirectParams();

            // This is not actually the display gamma. It's just a UI space to avoid having to
            // enter small numbers for the input.
            const float kPerceptualGamma = 2.2f;

            // Constraints
            {
                toeLength = Pow(Clamp01(toeLength), kPerceptualGamma);
                toeStrength = Clamp01(toeStrength);
                shoulderAngle = Clamp01(shoulderAngle);
                shoulderStrength = Clamp(shoulderStrength, 1e-5f, 1f - 1e-5f);
                shoulderLength = Max(0f, shoulderLength);
                gamma = Max(1e-5f, gamma);
            }

            // Apply base params
            {
                // Toe goes from 0 to 0.5
                float x0 = toeLength * 0.5f;
                float y0 = (1f - toeStrength) * x0; // Lerp from 0 to x0

                float remainingY = 1f - y0;

                float initialW = x0 + remainingY;

                float y1_offset = (1f - shoulderStrength) * remainingY;
                float x1 = x0 + y1_offset;
                float y1 = y0 + y1_offset;

                // Filmic shoulder strength is in F stops
                float extraW = Pow(2f, shoulderLength) - 1f;

                float W = initialW + extraW;

                dstParams.x0 = x0;
                dstParams.y0 = y0;
                dstParams.x1 = x1;
                dstParams.y1 = y1;
                dstParams.W = W;

                // Bake the linear to gamma space conversion
                dstParams.gamma = gamma;
            }

            dstParams.overshootX = (dstParams.W * 2f) * shoulderAngle * shoulderLength;
            dstParams.overshootY = 0.5f * shoulderAngle * shoulderLength;

            InitSegments(dstParams);
        }

        void InitSegments(DirectParams srcParams)
        {
            var paramsCopy = srcParams;

            whitePoint = srcParams.W;
            inverseWhitePoint = 1f / srcParams.W;

            // normalize params to 1.0 range
            paramsCopy.W = 1f;
            paramsCopy.x0 /= srcParams.W;
            paramsCopy.x1 /= srcParams.W;
            paramsCopy.overshootX = srcParams.overshootX / srcParams.W;

            float toeM = 0f;
            float shoulderM = 0f;
            {
                float m, b;
                AsSlopeIntercept(out m, out b, paramsCopy.x0, paramsCopy.x1, paramsCopy.y0, paramsCopy.y1);

                float g = srcParams.gamma;

                // Base function of linear section plus gamma is
                // y = (mx+b)^g
                //
                // which we can rewrite as
                // y = exp(g*ln(m) + g*ln(x+b/m))
                //
                // and our evaluation function is (skipping the if parts):
                /*
                    float x0 = (x - offsetX) * scaleX;
                    y0 = exp(m_lnA + m_B*log(x0));
                    return y0*scaleY + m_offsetY;
                */

                var midSegment = segments[1];
                midSegment.offsetX = -(b / m);
                midSegment.offsetY = 0f;
                midSegment.scaleX = 1f;
                midSegment.scaleY = 1f;
                midSegment.lnA = g * Log(m);
                midSegment.B = g;

                toeM = EvalDerivativeLinearGamma(m, b, g, paramsCopy.x0);
                shoulderM = EvalDerivativeLinearGamma(m, b, g, paramsCopy.x1);

                // apply gamma to endpoints
                paramsCopy.y0 = Max(1e-5f, Pow(paramsCopy.y0, paramsCopy.gamma));
                paramsCopy.y1 = Max(1e-5f, Pow(paramsCopy.y1, paramsCopy.gamma));

                paramsCopy.overshootY = Pow(1f + paramsCopy.overshootY, paramsCopy.gamma) - 1f;
            }

            this.x0 = paramsCopy.x0;
            this.x1 = paramsCopy.x1;

            // Toe section
            {
                var toeSegment = segments[0];
                toeSegment.offsetX = 0;
                toeSegment.offsetY = 0f;
                toeSegment.scaleX = 1f;
                toeSegment.scaleY = 1f;

                float lnA, B;
                SolveAB(out lnA, out B, paramsCopy.x0, paramsCopy.y0, toeM);
                toeSegment.lnA = lnA;
                toeSegment.B = B;
            }

            // Shoulder section
            {
                // Use the simple version that is usually too flat
                var shoulderSegment = segments[2];

                float x0 = (1f + paramsCopy.overshootX) - paramsCopy.x1;
                float y0 = (1f + paramsCopy.overshootY) - paramsCopy.y1;

                float lnA, B;
                SolveAB(out lnA, out B, x0, y0, shoulderM);

                shoulderSegment.offsetX = (1f + paramsCopy.overshootX);
                shoulderSegment.offsetY = (1f + paramsCopy.overshootY);

                shoulderSegment.scaleX = -1f;
                shoulderSegment.scaleY = -1f;
                shoulderSegment.lnA = lnA;
                shoulderSegment.B = B;
            }

            // Normalize so that we hit 1.0 at our white point. We wouldn't have do this if we
            // skipped the overshoot part.
            {
                // Evaluate shoulder at the end of the curve
                float scale = segments[2].Eval(1f);
                float invScale = 1f / scale;

                segments[0].offsetY *= invScale;
                segments[0].scaleY *= invScale;

                segments[1].offsetY *= invScale;
                segments[1].scaleY *= invScale;

                segments[2].offsetY *= invScale;
                segments[2].scaleY *= invScale;
            }
        }

        // Find a function of the form:
        //   f(x) = e^(lnA + Bln(x))
        // where
        //   f(0)   = 0; not really a constraint
        //   f(x0)  = y0
        //   f'(x0) = m
        void SolveAB(out float lnA, out float B, float x0, float y0, float m)
        {
            B = (m * x0) / y0;
            lnA = Log(y0) - B * Log(x0);
        }

        // Convert to y=mx+b
        void AsSlopeIntercept(out float m, out float b, float x0, float x1, float y0, float y1)
        {
            float dy = (y1 - y0);
            float dx = (x1 - x0);

            if (dx == 0)
                m = 1f;
            else
                m = dy / dx;

            b = y0 - x0 * m;
        }

        // f(x) = (mx+b)^g
        // f'(x) = gm(mx+b)^(g-1)
        float EvalDerivativeLinearGamma(float m, float b, float g, float x)
        {
            return g * m * Pow(m * x + b, g - 1f);
        }

        /// <summary>
        /// An utility class to ease the binding of curve parameters to shaders.
        /// </summary>
        public class Uniforms
        {
            HableCurve parent;

            internal Uniforms(HableCurve parent)
            {
                this.parent = parent;
            }

            /// <summary>
            /// Main curve settings, stored as <c>(inverseWhitePoint, x0, x1, 0)</c>.
            /// </summary>
            public Vector4 curve => new Vector4(parent.inverseWhitePoint, parent.x0, parent.x1, 0f);

            /// <summary>
            /// Toe segment settings, stored as <c>(offsetX, offsetY, scaleX, scaleY)</c>.
            /// </summary>
            public Vector4 toeSegmentA => new Vector4(parent.segments[0].offsetX, parent.segments[0].offsetY, parent.segments[0].scaleX, parent.segments[0].scaleY);

            /// <summary>
            /// Toe segment settings, stored as <c>(ln1, B, 0, 0)</c>.
            /// </summary>
            public Vector4 toeSegmentB => new Vector4(parent.segments[0].lnA, parent.segments[0].B, 0f, 0f);

            /// <summary>
            /// Mid segment settings, stored as <c>(offsetX, offsetY, scaleX, scaleY)</c>.
            /// </summary>
            public Vector4 midSegmentA => new Vector4(parent.segments[1].offsetX, parent.segments[1].offsetY, parent.segments[1].scaleX, parent.segments[1].scaleY);

            /// <summary>
            /// Mid segment settings, stored as <c>(ln1, B, 0, 0)</c>.
            /// </summary>
            public Vector4 midSegmentB => new Vector4(parent.segments[1].lnA, parent.segments[1].B, 0f, 0f);

            /// <summary>
            /// Shoulder segment settings, stored as <c>(offsetX, offsetY, scaleX, scaleY)</c>.
            /// </summary>
            public Vector4 shoSegmentA => new Vector4(parent.segments[2].offsetX, parent.segments[2].offsetY, parent.segments[2].scaleX, parent.segments[2].scaleY);

            /// <summary>
            /// Shoulder segment settings, stored as <c>(ln1, B, 0, 0)</c>.
            /// </summary>
            public Vector4 shoSegmentB => new Vector4(parent.segments[2].lnA, parent.segments[2].B, 0f, 0f);
        }

        /// <summary>
        /// An instance of the <see cref="Uniforms"/> utility class for this curve.
        /// </summary>
        public readonly Uniforms uniforms;
    }
}
