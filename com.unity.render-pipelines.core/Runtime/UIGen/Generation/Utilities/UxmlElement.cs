using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEngine.Rendering.UIGen.UXML
{
    //this should exist in UITK already. We should use UITK solution here instead
    public class UxmlElement
    {
        string m_Name;
        Dictionary<string, object> m_Attributes = new();
        List<UxmlElement> m_Childs = new();

        public string name => m_Name;

        private UxmlElement(string name)
            => m_Name = name;

        public static UxmlElement CreateRoot()
        {
            UxmlElement root = new UxmlElement("ui:UXML");
            root.SetAttribute("xmlns:ui", "UnityEngine.UIElements");
            root.SetAttribute("xmlns:uie", "UnityEditor.UIElements");
            root.SetAttribute("editor-extension-mode", false);

            //todo: handle styles (should have dedicated UxmlDocument for that?)

            return root;
        }

        //should enforce type known by uxml in UITK
        // This is done by IUxmlFactory but we cannot gather the BaseUxmlFactory.uxmlName from it
        // Use a custom type for now
        public static UxmlElement Create<T>() where T : UxmlElementType
            => new UxmlElement(GetTypeName<T>());

        public void TransformTo<T>() where T : UxmlElementType
            => m_Name = GetTypeName<T>();

        static string GetTypeName<T>() where T : UxmlElementType
            => $"ui:{typeof(T).Name}";

        public void SetAttribute(string name, object value)
            => m_Attributes[name] = value;

        public void AddChild(UxmlElement child)
            => m_Childs.Add(child);

        //ouput parts
        void WriteIndentation(int depth, StringBuilder builder)
        {
            for (int i = depth; i > 0; --i)
                builder.Append('\t');
        }

        void WriteAttributes(StringBuilder builder)
        {
            foreach (var kvp in m_Attributes)
            {
                builder.Append(' ');
                builder.Append(kvp.Key);
                builder.Append("=\"");
                builder.Append(kvp.Value.ToString());
                builder.Append('\"');
            }
        }

        void WriteElementNoChild(StringBuilder builder)
        {
            builder.Append('<')
                .Append(m_Name);
            WriteAttributes(builder);
            builder.Append(" />");
        }

        void WriteStartElementWithChild(StringBuilder builder)
        {
            builder.Append('<')
                .Append(m_Name);
            WriteAttributes(builder);
            builder.Append('>');
        }

        void WriteEndElementWithChild(StringBuilder builder)
        {
            builder.Append("</")
                .Append(m_Name)
                .Append('>');
        }

        //output in file
        public void WriteTo(System.IO.TextWriter writter)
            => WriteToWithIndentation(0, writter, new());

        void WriteToWithIndentation(int depth, System.IO.TextWriter writter, StringBuilder builder)
        {
            WriteIndentation(depth, builder);

            if (m_Childs.Count == 0)
            {
                WriteElementNoChild(builder);
                writter.WriteLine(builder);
            }
            else
            {
                WriteStartElementWithChild(builder);
                writter.WriteLine(builder);
                foreach (var child in m_Childs)
                    child.WriteToWithIndentation(depth + 1, writter, builder.Clear());
                WriteEndElementWithChild(builder.Clear());
                writter.WriteLine(builder);
            }
        }

        //output in string
        public override string ToString()
            => ToStringWithIndentation(0, new());

        string ToStringWithIndentation(int depth, StringBuilder builder)
        {
            WriteIndentation(depth, builder);

            if (m_Childs.Count == 0)
            {
                WriteElementNoChild(builder);
                return builder.ToString();
            }
            else
            {
                WriteStartElementWithChild(builder);
                builder.Append("\n");
                foreach (var child in m_Childs)
                {
                    child.ToStringWithIndentation(depth + 1, builder);
                    builder.Append("\n");
                }
                WriteIndentation(depth, builder);
                WriteEndElementWithChild(builder);
                return builder.ToString();
            }
        }
    }


    public abstract class UxmlElementType
    {
        protected UxmlElementType() { throw new Exception("Should not be instanciated."); }
    }
    public sealed class Button : UxmlElementType { }
    public sealed class ScrollView : UxmlElementType { }
    public sealed class ProgressBar : UxmlElementType { }
    public sealed class Slider : UxmlElementType { }
    public sealed class DropdownField : UxmlElementType { }
    public sealed class EnumField : UxmlElementType { }
    public sealed class Toggle : UxmlElementType { }
    public sealed class TextField : UxmlElementType { }
    public sealed class IntergerField : UxmlElementType { }
    public sealed class FloatField : UxmlElementType { }
    public sealed class LongField : UxmlElementType { }
    public sealed class DoubleField : UxmlElementType { }
    public sealed class Hash128Field : UxmlElementType { }
    public sealed class Vector2Field : UxmlElementType { }
    public sealed class Vector2IntField : UxmlElementType { }
    public sealed class Vector3Field : UxmlElementType { }
    public sealed class Vector3IntField : UxmlElementType { }
    public sealed class Vector4Field : UxmlElementType { }
    public sealed class RectField : UxmlElementType { }
    public sealed class RectIntField : UxmlElementType { }
    public sealed class BoundsField : UxmlElementType { }
    public sealed class BoundsIntField : UxmlElementType { }
}
