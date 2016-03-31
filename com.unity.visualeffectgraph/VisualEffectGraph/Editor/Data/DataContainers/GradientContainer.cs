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
    public class GradientContainer : DataContainer
    {
        public Gradient Gradient;

        public override DataContainerInfo getContainerInfo()
        {
            return new GradientDataContainerInfo(Gradient);
        }
    }

    public class GradientDataContainerInfo : DataContainerInfo
    {
        Gradient Gradient;

        public GradientDataContainerInfo(Gradient sourceGradient)
        {
            Gradient = new Gradient();
            Gradient.SetKeys(sourceGradient.colorKeys, sourceGradient.alphaKeys);
        }

        public GradientDataContainerInfo(XElement rootElement)
        {
            XElement element = rootElement.Element("Gradient");
            int numColorKeys = int.Parse(element.Attribute("numColorKeys").Value);
            int numAlphaKeys = int.Parse(element.Attribute("numAlphaKeys").Value);

            Gradient = new Gradient();

            GradientColorKey[] colorKeys = new GradientColorKey[numColorKeys];
            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[numAlphaKeys];

            int i = 0;

            foreach(XElement xkey in element.Elements("ColorKey") )
            {
                colorKeys[i] = new GradientColorKey(
                    new Color( float.Parse(xkey.Attribute("R").Value), float.Parse(xkey.Attribute("G").Value), float.Parse(xkey.Attribute("B").Value) ),
                    float.Parse(xkey.Attribute("Time").Value));
                i++;
            }

            i = 0;
            foreach(XElement xkey in element.Elements("AlphaKey") )
            {
                alphaKeys[i] = new GradientAlphaKey(
                    float.Parse(xkey.Attribute("Alpha").Value),
                    float.Parse(xkey.Attribute("Time").Value));
                i++;
            }

            Gradient.SetKeys(colorKeys, alphaKeys);
        }

        public override DataContainer CreateDataContainer()
        {
            GradientContainer container = ScriptableObject.CreateInstance<GradientContainer>();
            container.Gradient = new Gradient();
            container.Gradient.SetKeys(Gradient.colorKeys, Gradient.alphaKeys);
            return container;
        }

        public override void Serialize(XmlWriter doc)
        {
            doc.WriteStartElement("DataContainer");
            doc.WriteAttributeString("type", "GradientContainer");

                doc.WriteStartElement("Gradient");
                doc.WriteAttributeString("numColorKeys", Gradient.colorKeys.Length.ToString());
                doc.WriteAttributeString("numAlphaKeys", Gradient.alphaKeys.Length.ToString());

                foreach(GradientColorKey k in Gradient.colorKeys)
                {
                    doc.WriteStartElement("ColorKey");
                    doc.WriteAttributeString("Time", k.time.ToString());
                    doc.WriteAttributeString("R", k.color.r.ToString());
                    doc.WriteAttributeString("G", k.color.g.ToString());
                    doc.WriteAttributeString("B", k.color.b.ToString());
                    doc.WriteEndElement();
                }

                foreach(GradientAlphaKey k in Gradient.alphaKeys)
                {
                    doc.WriteStartElement("AlphaKey");
                    doc.WriteAttributeString("Time", k.time.ToString());
                    doc.WriteAttributeString("Alpha", k.alpha.ToString());
                    doc.WriteEndElement();
                }
                doc.WriteEndElement();

            doc.WriteEndElement();
        }
    }
}
