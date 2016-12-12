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
        private Dictionary<VFXValue, SignalData> m_FloatSignals = new Dictionary<VFXValue, SignalData>();

        private HashSet<VFXValue> m_DirtySignals = new HashSet<VFXValue>();

        private bool m_ColorTextureDirty = false;
        private bool m_FloatTextureDirty = false;

        public bool HasColorTexture() { return m_ColorTexture != null; }
        public bool HasFloatTexture() { return m_FloatTexture != null; }

        public Texture2D ColorTexture { get { return m_ColorTexture; } }
        public Texture2D FloatTexture { get { return m_FloatTexture; } }

        public void SetDirty(VFXValue value)
        {
            if (value.ValueType == VFXValueType.kColorGradient || value.ValueType == VFXValueType.kCurve)
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
            m_FloatSignals.Clear();
            m_DirtySignals.Clear();
        }

        public void AddValues(IEnumerable<VFXValue> values)
        {
            // Gather all value
            foreach (var value in values)
            {
                if (value.ValueType == VFXValueType.kColorGradient)
                    m_ColorSignals.Add(value, -1); // dummy value
                else if (value.ValueType == VFXValueType.kCurve)
                    m_FloatSignals.Add(value, null); // dummy value
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

            if (m_ColorTexture != null && m_ColorTexture.height != colorHeight)
            {
                DestroyTexture(m_ColorTexture);
                m_ColorTexture = null;
            }

            if (colorHeight > 0)
            {
                if (m_ColorTexture == null)
                {
                    m_ColorTexture = new Texture2D(TEXTURE_WIDTH, Mathf.NextPowerOfTwo(colorHeight), TextureFormat.RGBA32, false, false); // sRGB
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
            int floatHeight = m_FloatSignals.Count;

            if (asset != null && m_FloatTexture != asset.CurveTexture)
            {
                DestroyTexture(m_FloatTexture);
                m_FloatTexture = asset.CurveTexture;
            }

            if (m_ColorTexture != null && m_ColorTexture.height != colorHeight)
            {
                DestroyTexture(m_FloatTexture);
                m_FloatTexture = null;
            }

            if (m_FloatTexture != null && m_FloatTexture.height != floatHeight)
            {
                if (!EditorUtility.IsPersistent(m_FloatTexture)) // Do we still have ownership on the texture or has it been serialized within a VFX asset ?
                    Object.DestroyImmediate(m_FloatTexture);
                m_FloatTexture = null;
            }

            if (floatHeight > 0)
            {
                if (m_FloatTexture == null)
                {
                    m_FloatTexture = new Texture2D(TEXTURE_WIDTH, Mathf.NextPowerOfTwo((floatHeight + 3) / 4), TextureFormat.RGBAHalf, false, true); // Linear
                    m_FloatTexture.wrapMode = TextureWrapMode.Repeat;
                }
       
                int currentIndex = 0;
                var curves = new List<VFXValue>(m_FloatSignals.Keys);
                foreach (var curve in curves)
                {
                    SignalData data = new SignalData();
                    data.Y = currentIndex >> 2;
                    data.index = currentIndex & 3;

                    AnimationCurve animCurve = curve.Get<AnimationCurve>();

                    m_FloatSignals[curve] = UpdateSignalData(animCurve,data);
                    DiscretizeCurve(animCurve, data);

                    ++currentIndex;
                }

                m_FloatTextureDirty = true;
            }

            UploadChanges();
        }

        public bool Update(VFXValue value)
        {
            if (value.ValueType == VFXValueType.kColorGradient)
            {
                if (!m_ColorSignals.ContainsKey(value))
                    return false;

                DiscretizeGradient(value.Get<Gradient>(), m_ColorSignals[value]);
                m_ColorTextureDirty = true;
                return true;
            }
            else if (value.ValueType == VFXValueType.kCurve)
            {
                if (!m_FloatSignals.ContainsKey(value))
                    return false;

                var animCurve = value.Get<AnimationCurve>();
                var signalData = m_FloatSignals[value];
                m_FloatSignals[value] = UpdateSignalData(animCurve,signalData);
                DiscretizeCurve(animCurve,m_FloatSignals[value]);

                m_FloatTextureDirty = true;
            }

            return false;
        }

        public void UpdateAll()
        {
            foreach (var gradient in m_ColorSignals)
                Update(gradient.Key);

            foreach (var curve in m_FloatSignals)
                Update(curve.Key);
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
            SignalData data = m_FloatSignals[curve];
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
    }
}