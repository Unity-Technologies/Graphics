using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.Experimental;

namespace UnityEngine.Experimental.VFX
{
    public class VFXGeneratedTextureData
    {
        private const int TEXTURE_WIDTH = 128; // TODO This should be made dynamic based on highest frequency of signals
        private class SignalData
        {
            public int Y;               // Y coordinate of the signal (absolute)
            public int index;           // component index (x,y,z or w)
            public bool clampStart;     // clamp mode of the curve before first key
            public bool clampEnd;       // clamp mode of the curve after last key
            public float startU;
            public float scaleU;
        }

        private Texture2D m_ColorTexture; // sRGB data 8bits per component
        private Texture2D m_FloatTexture; // linear data 16bits per component

        private Dictionary<VFXValue, int> m_ColorSignals = new Dictionary<VFXValue,int>();
        private Dictionary<VFXValue, SignalData> m_CurveSignals = new Dictionary<VFXValue, SignalData>();
        private Dictionary<VFXValue, int> m_BezierSignals = new Dictionary<VFXValue, int>(); // (2 rows per entry: position and tangent)

        private HashSet<VFXValue> m_DirtySignals = new HashSet<VFXValue>();

        private bool m_ColorTextureDirty = false;
        private bool m_FloatTextureDirty = false;

        public bool HasColorTexture() { return m_ColorTexture != null; }
        public bool HasFloatTexture() { return m_FloatTexture != null; }

        public Texture2D ColorTexture { get { return m_ColorTexture; } }
        public Texture2D FloatTexture { get { return m_FloatTexture; } }

        public void SetDirty(VFXValue value)
        {
            if (value.ValueType == VFXValueType.kColorGradient || value.ValueType == VFXValueType.kCurve || value.ValueType == VFXValueType.kSpline)
                m_DirtySignals.Add(value); // Dont check whether it exists in dictionary as this will be performed later on during the update
        }

        public void UpdateAndUploadDirty()
        {
            foreach (var value in m_DirtySignals)
                Update(value);

            m_DirtySignals.Clear();

            UploadChanges();
        }

        public void RemoveAllValues()
        {
            m_ColorSignals.Clear();
            m_CurveSignals.Clear();
            m_BezierSignals.Clear();
            m_DirtySignals.Clear();
        }

        public void AddValues(IEnumerable<VFXValue> values)
        {
            // Gather all value
            foreach (var value in values)
            {
                switch(value.ValueType)
                {
                    case VFXValueType.kColorGradient:   m_ColorSignals.Add(value, -1);      break;
                    case VFXValueType.kCurve:           m_CurveSignals.Add(value, null);    break;
                    case VFXValueType.kSpline:          m_BezierSignals.Add(value, -1);     break;
                }
            }
        }

        private void DestroyTexture(Texture2D texture)
        {
            if (texture != null && !EditorUtility.IsPersistent(texture))// Do we still have ownership on the texture or has it been serialized within a VFX asset ?
                Object.DestroyImmediate(texture);
        }

        public void Generate(VFXAsset asset)
        {
            // gradients
            int colorHeight = m_ColorSignals.Count;

            if (asset != null && m_ColorTexture != asset.GradientTexture)
            {
                DestroyTexture(m_ColorTexture);
                m_ColorTexture = asset.GradientTexture;
            }

            Func<int, int> GetExpectedColorHeight = (height) => Mathf.NextPowerOfTwo(colorHeight);

            if (m_ColorTexture != null && m_ColorTexture.height != GetExpectedColorHeight(colorHeight))
            {
                DestroyTexture(m_ColorTexture);
                m_ColorTexture = null;
            }

            if (colorHeight > 0)
            {
                if (m_ColorTexture == null)
                {
                    m_ColorTexture = new Texture2D(TEXTURE_WIDTH, GetExpectedColorHeight(colorHeight), TextureFormat.RGBA32, false, false); // sRGB
                    m_ColorTexture.wrapMode = TextureWrapMode.Clamp;
                }

                int currentY = 0;
                var gradients = new List<VFXValue>(m_ColorSignals.Keys);
                foreach (var gradient in gradients)
                {
                    m_ColorSignals[gradient] = currentY;
                    DiscretizeGradient(gradient.Get<Gradient>(), currentY++);
                }

                m_ColorTextureDirty = true;
            }

            // curves
            int nbCurves = m_CurveSignals.Count;
            int nbBeziers = m_BezierSignals.Count;

            if (asset != null && m_FloatTexture != asset.CurveTexture)
            {
                DestroyTexture(m_FloatTexture);
                m_FloatTexture = asset.CurveTexture;
            }

            Func<int, int, int> GetExpectedFloatHeight = (curveCount, bezierCount) => Mathf.NextPowerOfTwo(bezierCount * 2 + (curveCount + 3) / 4);

            if (m_FloatTexture != null && m_FloatTexture.height != GetExpectedFloatHeight(nbCurves,nbBeziers))
            {
                DestroyTexture(m_FloatTexture);
                m_FloatTexture = null;
            }

            if (nbCurves + nbBeziers > 0)
            {
                if (m_FloatTexture == null)
                {
                    m_FloatTexture = new Texture2D(TEXTURE_WIDTH, GetExpectedFloatHeight(nbCurves,nbBeziers), TextureFormat.RGBAHalf, false, true); // Linear
                    m_FloatTexture.wrapMode = TextureWrapMode.Repeat;
                }
       
                int currentIndex = 0;
                var curves = new List<VFXValue>(m_CurveSignals.Keys);
                foreach (var curve in curves)
                {
                    SignalData data = new SignalData();
                    data.Y = currentIndex >> 2;
                    data.index = currentIndex & 3;

                    AnimationCurve animCurve = curve.Get<AnimationCurve>();

                    m_CurveSignals[curve] = UpdateSignalData(animCurve,data);
                    DiscretizeCurve(animCurve, data);

                    ++currentIndex;
                }

                // pad current index to be at first component
                currentIndex = ((currentIndex + 3) & ~3) >> 2;

                // add 3d beziers
                var beziers = new List<VFXValue>(m_BezierSignals.Keys);
                foreach (var bezier in beziers)
                {
                    m_BezierSignals[bezier] = currentIndex;
                    List<Vector3> controlPoints = bezier.Get<List<Vector3>>();
                    DiscretizeBezier(controlPoints, currentIndex);
                    currentIndex += 2;
                }
                

                m_FloatTextureDirty = true;
            }

            UploadChanges();
        }

        public bool Update(VFXValue value)
        {
            switch (value.ValueType)
            {
                case VFXValueType.kColorGradient:
                    {
                        if (!m_ColorSignals.ContainsKey(value))
                            return false;

                        DiscretizeGradient(value.Get<Gradient>(), m_ColorSignals[value]);
                        m_ColorTextureDirty = true;
                        return true;
                    }

                case VFXValueType.kCurve:
                    {
                        if (!m_CurveSignals.ContainsKey(value))
                            return false;

                        var animCurve = value.Get<AnimationCurve>();
                        var signalData = m_CurveSignals[value];
                        m_CurveSignals[value] = UpdateSignalData(animCurve,signalData);
                        DiscretizeCurve(animCurve,m_CurveSignals[value]);

                        m_FloatTextureDirty = true;
                        return true;
                    }

                case VFXValueType.kSpline:
                    {
                        if (!m_BezierSignals.ContainsKey(value))
                            return false;

                        DiscretizeBezier(value.Get<List<Vector3>>(), m_BezierSignals[value]);

                        m_FloatTextureDirty = true;
                        return true;
                    }
            }

            return false;
        }

        public void UpdateAll()
        {
            foreach (var gradient in m_ColorSignals)
                Update(gradient.Key);

            foreach (var curve in m_CurveSignals)
                Update(curve.Key);

            foreach (var spline in m_BezierSignals)
                Update(spline.Key);
        }

        public void UploadChanges()
        {
            if (m_ColorTextureDirty)
            {
                m_ColorTexture.Apply();
                m_ColorTextureDirty = false;
            }

            if (m_FloatTextureDirty)
            {
                m_FloatTexture.Apply();
                m_FloatTextureDirty = false;
            }
        }

        public void Dispose()
        {
            RemoveAllValues();
            Generate(null); // This will destroy existing textures as all values were removed
        }

        private void DiscretizeGradient(Gradient gradient,int y)
        {
            for (int i = 0; i < TEXTURE_WIDTH; ++i)
            {
                Color c = gradient.Evaluate(i / (TEXTURE_WIDTH - 1.0f));
                m_ColorTexture.SetPixel(i,y,c);
            }
        }

        private SignalData UpdateSignalData(AnimationCurve curve, SignalData data)
        {
            // Ensure the curve mode is supported
            if (curve.preWrapMode == WrapMode.PingPong || curve.postWrapMode == WrapMode.PingPong) // TODO Handle pingpong mode
                Debug.LogError("ping pong wrap mode is not supported for curves. Clamp is used instead");
            
            data.clampStart = curve.preWrapMode != WrapMode.Loop;
            data.clampEnd = curve.postWrapMode != WrapMode.Loop;
            data.startU = curve.length == 0 ? 0.0f : curve.keys[0].time;
            data.scaleU = curve.length == 0 ? 1.0f : curve.keys[curve.length - 1].time - data.startU;

            return data;
        }

        private void DiscretizeCurve(AnimationCurve curve,SignalData data)
        {
            for (int i = 0; i < TEXTURE_WIDTH; ++i)
            {
                float x = data.startU + ((data.clampStart || data.clampEnd) ? (data.scaleU * i) / (TEXTURE_WIDTH - 1) : data.scaleU * (0.5f + i) / TEXTURE_WIDTH); 
                Color c = m_FloatTexture.GetPixel(i,data.Y); // Get pixel because only one component will be written
                c[data.index] = curve.Evaluate(x);
                m_FloatTexture.SetPixel(i, data.Y, c);
            }
        }

        private struct PointData
        {
            public Vector3 pos;
            public Vector3 tan;
            public float length;
        }

        private void DiscretizeBezier(List<Vector3> points,int rowIdx)
        {
            Action<int, Vector3> FillLineWithValue = (row,value) => {
                Color c = new Color(value.x,value.y,value.z);
                for (int i = 0; i < TEXTURE_WIDTH; ++i)
                    m_FloatTexture.SetPixel(i,row,c);
            };

            int nbPoints = points.Count;
            int nbPieces = nbPoints >= 4 ? 1 + (nbPoints - 4) / 3 : 0; 

            if (nbPieces == 0)
            {
                FillLineWithValue(rowIdx, nbPoints == 0 ? Vector3.zero : points[0]);
                FillLineWithValue(rowIdx + 1, nbPoints < 1 ? Vector3.zero : points[1]);
                return;
            }

            List<PointData> samples = new List<PointData>();
            float totalLength = 0.0f;
            for (int i = 0; i < nbPieces; ++i)
            {
                int index = i * 4; 
                Vector3 p0 = points[index];
                Vector3 p1 = points[index + 1];
                Vector3 p2 = points[index + 2];
                Vector3 p3 = points[index + 3];

                // Could use a preestimate of the length per piece and change samples accordingly
                const int NB_SAMPLES_PER_PIECE = 128;
                for (int j = 0; j < NB_SAMPLES_PER_PIECE; ++j)
                {
                    float t = j / (NB_SAMPLES_PER_PIECE - 1.0f);
                    float t2 = t * t;
                    float t3 = t2 * t;
                    float oneMinusT = 1 - t;
                    float oneMinusT2 = oneMinusT * oneMinusT;
                    float oneMinusT3 = oneMinusT2 * oneMinusT;

                    PointData data;
                    data.pos = oneMinusT3 * p0 + 3 * oneMinusT2 * t * p1 + 3 * oneMinusT * t2 * p2 + t3 * p3;
                    data.tan = 3 * oneMinusT2 * (p1 - p0) + 6 * oneMinusT * (p2 - p1) + 3 * t2 * (p3 - p2);

                    float length = 0.0f;
                    if (samples.Count > 0)
                        length = (data.pos - samples[samples.Count - 1].pos).magnitude;
                    
                    totalLength += length;
                    data.length = totalLength;

                    samples.Add(data);
                }
            }

            // Remap t to linear sampling on the curve
            int currentIndex = 1;
            int nbSamples = samples.Count;
            for (int i = 0; i < TEXTURE_WIDTH; ++i)
            {
                float length = (totalLength * i) / (TEXTURE_WIDTH - 1);
                while (currentIndex < nbSamples - 1 && length >= samples[currentIndex].length)
                    ++currentIndex;

                PointData start = samples[currentIndex - 1];
                PointData end = samples[currentIndex];

                float coef = (length - start.length) / (end.length - start.length);
                Vector3 pos = Vector3.Lerp(start.pos, end.pos, coef);
                Vector3 tan = Vector3.Slerp(start.tan, end.tan, coef);

                m_FloatTexture.SetPixel(i, rowIdx, new Color(pos.x,pos.y,pos.z));
                m_FloatTexture.SetPixel(i, rowIdx + 1, new Color(tan.x, tan.y, tan.z));
            }
        }

        public void WriteSampleGradientFunction(ShaderSourceBuilder builder)
        {
            // Signature
            builder.WriteLine("float4 sampleSignal(float v,float u) // sample gradient");
            builder.EnterScope();

            builder.Write("return gradientTexture.SampleLevel(samplergradientTexture,float2(");
            WriteHalfTexelOffset(builder, "saturate(u)");
            builder.WriteLine(",v),0);");

            builder.ExitScope();
        }

        public void WriteSampleCurveFunction(ShaderSourceBuilder builder)
        {
            // curveData:
            // x: 1 / scaleU
            // y: -startU / scaleU
            // z: clamp flag (uint) << 2 | index (2LSB)
            // w: normalized v

            builder.WriteLine("// Non optimized generic function to allow curve edition without recompiling");
            builder.WriteLine("float sampleSignal(float4 curveData,float u) // sample curve");
            builder.EnterScope();

            builder.WriteLine("float uNorm = (u * curveData.x) + curveData.y;");
            builder.WriteLine("switch(asuint(curveData.w) >> 2)");
            builder.EnterScope();

            builder.Write("case 1: uNorm = ");
            WriteHalfTexelOffset(builder, "frac(min(1.0f - 1e-5f,uNorm))"); // Dont clamp at 1 or else the frac will make it 0...
            builder.WriteLine("; break; // clamp end");

            builder.Write("case 2: uNorm = ");
            WriteHalfTexelOffset(builder, "frac(max(0.0f,uNorm))");
            builder.WriteLine("; break; // clamp start");

            builder.Write("case 3: uNorm = ");
            WriteHalfTexelOffset(builder, "saturate(uNorm)");
            builder.WriteLine("; break; // clamp both");

            builder.ExitScope();

            builder.WriteLine("return curveTexture.SampleLevel(samplercurveTexture,float2(uNorm,curveData.z),0)[asuint(curveData.w) & 0x3];");

            builder.ExitScope();
        }

        public void WriteSampleSplineFunction(ShaderSourceBuilder builder)
        {
            builder.WriteLine("float3 sampleSpline(float v,float u)");
            builder.EnterScope();

            builder.Write("return curveTexture.SampleLevel(samplercurveTexture,float2(");
            WriteHalfTexelOffset(builder, "saturate(u)");
            builder.WriteLine(",v),0);");

            builder.ExitScope();
        }

        private void WriteHalfTexelOffset(ShaderSourceBuilder builder,string uNorm)
        {
            // layout ALU for MAD
            float a = (TEXTURE_WIDTH - 1.0f) / TEXTURE_WIDTH;
            float b = 0.5f / TEXTURE_WIDTH;
            builder.Write("((");
            builder.Write(a);
            builder.Write(" * ");
            builder.Write(uNorm);
            builder.Write(") + ");
            builder.Write(b);
            builder.Write(')');
        }

        public Vector4 GetCurveUniform(VFXValue curve) // can throw
        {
            SignalData data = m_CurveSignals[curve];
            Vector4 uniform = new Vector4();
            uniform.x = 1.0f / data.scaleU;
            uniform.y = -data.startU / data.scaleU; // arrange x and y to use a mad instruction in shader -> uNorm = (u - start) / scale is equivalent to uNorm = (u * (1 / scale)) + (-start / scale)
            uniform.z = (0.5f + data.Y) / m_FloatTexture.height; // v
            uniform.w = BitConverter.ToSingle(BitConverter.GetBytes((data.clampStart ? 0x8 : 0x0) | (data.clampEnd ? 0x4 : 0x0) | (data.index & 0x3)), 0);
            return uniform;
        }

        public float GetGradientUniform(VFXValue gradient) // can throw
        {
            return (0.5f + m_ColorSignals[gradient]) / m_ColorTexture.height;
        }

        public Vector2 GetSplineUniform(VFXValue spline)
        {
            int index = m_BezierSignals[spline];
            return new Vector4((0.5f + index) / m_FloatTexture.height,(0.5f + index + 1) / m_FloatTexture.height);
        }
    }
}