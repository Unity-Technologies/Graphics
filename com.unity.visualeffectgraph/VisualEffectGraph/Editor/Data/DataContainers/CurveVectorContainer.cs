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
    internal class CurveVectorContainer : DataContainer
    {
        public AnimationCurve CurveX;
        public AnimationCurve CurveY;
        public AnimationCurve CurveZ;

        public override DataContainerInfo getContainerInfo()
        {
            return new CurveVectorDataContainerInfo(CurveX, CurveY, CurveZ);
        }
    }

    
    public class CurveVectorDataContainerInfo : DataContainerInfo
    {

        AnimationCurve CurveX;
        AnimationCurve CurveY;
        AnimationCurve CurveZ;

        public CurveVectorDataContainerInfo(AnimationCurve sourceCurveX,AnimationCurve sourceCurveY,AnimationCurve sourceCurveZ)
        {
            CurveX = CurveFloatDataContainerInfo.Clone(sourceCurveX);
            CurveY = CurveFloatDataContainerInfo.Clone(sourceCurveY);
            CurveZ = CurveFloatDataContainerInfo.Clone(sourceCurveZ);
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

        public CurveVectorDataContainerInfo(XElement rootElement)
        {
            XElement elementX = rootElement.Element("CurveX");
            XElement elementY = rootElement.Element("CurveY");
            XElement elementZ = rootElement.Element("CurveZ");

            CurveX = new AnimationCurve();
            CurveX.postWrapMode = GetWrapMode(elementX.Attribute("postWrapMode").Value);
            CurveX.preWrapMode = GetWrapMode(elementX.Attribute("preWrapMode").Value);

            foreach(XElement e in elementX.Elements("KeyFrame"))
            {
                Keyframe k = new Keyframe();
                k.time = float.Parse(e.Attribute("Time").Value);
                k.value = float.Parse(e.Attribute("Value").Value);
                k.tangentMode = int.Parse(e.Attribute("TangentMode").Value);
                k.inTangent = float.Parse(e.Attribute("InTangent").Value);
                k.outTangent = float.Parse(e.Attribute("OutTangent").Value);
                CurveX.AddKey(k);
            }

            CurveY = new AnimationCurve();
            CurveY.postWrapMode = GetWrapMode(elementY.Attribute("postWrapMode").Value);
            CurveY.preWrapMode = GetWrapMode(elementY.Attribute("preWrapMode").Value);

            foreach(XElement e in elementY.Elements("KeyFrame"))
            {
                Keyframe k = new Keyframe();
                k.time = float.Parse(e.Attribute("Time").Value);
                k.value = float.Parse(e.Attribute("Value").Value);
                k.tangentMode = int.Parse(e.Attribute("TangentMode").Value);
                k.inTangent = float.Parse(e.Attribute("InTangent").Value);
                k.outTangent = float.Parse(e.Attribute("OutTangent").Value);
                CurveY.AddKey(k);
            }

            CurveZ = new AnimationCurve();
            CurveZ.postWrapMode = GetWrapMode(elementZ.Attribute("postWrapMode").Value);
            CurveZ.preWrapMode = GetWrapMode(elementZ.Attribute("preWrapMode").Value);

            foreach(XElement e in elementZ.Elements("KeyFrame"))
            {
                Keyframe k = new Keyframe();
                k.time = float.Parse(e.Attribute("Time").Value);
                k.value = float.Parse(e.Attribute("Value").Value);
                k.tangentMode = int.Parse(e.Attribute("TangentMode").Value);
                k.inTangent = float.Parse(e.Attribute("InTangent").Value);
                k.outTangent = float.Parse(e.Attribute("OutTangent").Value);
                CurveZ.AddKey(k);
            }
        }

        public override void Serialize(XmlWriter doc)
        {
            doc.WriteStartElement("DataContainer");
            doc.WriteAttributeString("type", "CurveVectorContainer");


                doc.WriteStartElement("CurveX");
                doc.WriteAttributeString("postWrapMode", CurveX.postWrapMode.ToString());
                doc.WriteAttributeString("preWrapMode", CurveX.preWrapMode.ToString());

                foreach(Keyframe k in CurveX.keys)
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

                doc.WriteStartElement("CurveY");
                doc.WriteAttributeString("postWrapMode", CurveY.postWrapMode.ToString());
                doc.WriteAttributeString("preWrapMode", CurveY.preWrapMode.ToString());

                foreach(Keyframe k in CurveY.keys)
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

                doc.WriteStartElement("CurveZ");
                doc.WriteAttributeString("postWrapMode", CurveZ.postWrapMode.ToString());
                doc.WriteAttributeString("preWrapMode", CurveZ.preWrapMode.ToString());

                foreach(Keyframe k in CurveZ.keys)
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
            CurveVectorContainer container = ScriptableObject.CreateInstance<CurveVectorContainer>();
            container.CurveX = new AnimationCurve();
            foreach(Keyframe k in CurveX.keys)
            {

            }
            return container;
        }
    }
}
