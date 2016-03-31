using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;
using System.Xml;
using System.Xml.Linq;


namespace UnityEditor.Experimental
{
    internal class CurveFloatContainer : DataContainer
    {
        public AnimationCurve Curve;

        public override DataContainerInfo getContainerInfo()
        {
            return new CurveFloatDataContainerInfo(Curve);
        }

    }

    public class CurveFloatDataContainerInfo : DataContainerInfo
    {
        AnimationCurve Curve;

        public static AnimationCurve Clone(AnimationCurve Other)
        {
            AnimationCurve output = new AnimationCurve();
            output.postWrapMode = Other.postWrapMode;
            output.preWrapMode = Other.preWrapMode;

            foreach(Keyframe k in Other.keys)
            {
                output.AddKey(k);
            }
            return output;
        }

        public CurveFloatDataContainerInfo(AnimationCurve sourceCurve)
        {
            Curve = Clone(sourceCurve);
        }

        public WrapMode GetWrapMode(string s)
        {
            switch(s)
            {
                case "Clamp": return WrapMode.Clamp;
                case "ClampForever": return WrapMode.ClampForever;
                case "Default": return WrapMode.Default;
                case "Loop": return WrapMode.Loop;
                case "Once": return WrapMode.Once;
                case "PingPong": return WrapMode.PingPong;
                default: return WrapMode.Default;
            }
        }

        public CurveFloatDataContainerInfo(XElement rootElement)
        {
            XElement element = rootElement.Element("Curve");

            Curve = new AnimationCurve();
            Curve.postWrapMode = GetWrapMode(element.Attribute("postWrapMode").Value);
            Curve.preWrapMode = GetWrapMode(element.Attribute("preWrapMode").Value);

            foreach(XElement e in element.Elements("KeyFrame"))
            {
                Keyframe k = new Keyframe();
                k.time = float.Parse(e.Attribute("Time").Value);
                k.value = float.Parse(e.Attribute("Value").Value);
                k.tangentMode = int.Parse(e.Attribute("TangentMode").Value);
                k.inTangent = float.Parse(e.Attribute("InTangent").Value);
                k.outTangent = float.Parse(e.Attribute("OutTangent").Value);
                Curve.AddKey(k);
            }
        }

        public override void Serialize(XmlWriter doc)
        {
            doc.WriteStartElement("DataContainer");
            doc.WriteAttributeString("type", "CurveFloatContainer");

                doc.WriteStartElement("Curve");
                doc.WriteAttributeString("postWrapMode", Curve.postWrapMode.ToString());
                doc.WriteAttributeString("preWrapMode", Curve.preWrapMode.ToString());

                foreach(Keyframe k in Curve.keys)
                {
                    doc.WriteStartElement("KeyFrame");
                    doc.WriteAttributeString("Time", k.time.ToString());
                    doc.WriteAttributeString("Value", k.value.ToString());
                    doc.WriteAttributeString("TangentMode", k.tangentMode.ToString());
                    doc.WriteAttributeString("InTangent", k.inTangent.ToString());
                    doc.WriteAttributeString("OutTangent", k.outTangent.ToString());
                    doc.WriteEndElement();
                }

                doc.WriteEndElement();

            doc.WriteEndElement();
        }

        public override DataContainer CreateDataContainer()
        {
            CurveFloatContainer container = ScriptableObject.CreateInstance<CurveFloatContainer>();
            container.Curve = Clone(Curve);
            return container;
        }
    }
}
