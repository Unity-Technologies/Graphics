using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.VFX;

namespace UnityEngine.Experimental.VFX
{
    public struct VFXAttribute
    {
        public VFXAttribute(string name, VFXValueType type, bool writable = false)
        {
            m_Name = name;
            m_Type = type;
            m_Writable = writable;
        }

        public VFXAttribute(VFXAttribute other, bool writable)
        {
            m_Name = other.m_Name;
            m_Type = other.m_Type;
            m_Writable = writable;
        }

        public string m_Name;
        public VFXValueType m_Type;
        public bool m_Writable;
    }

    public sealed class VFXDataBlockDesc
    {
        public VFXProperty Property                 { get { return m_Property; } }
        public VFXPropertyTypeSemantics Semantics   { get { return Property.m_Type; } }
        public string Name                          { get { return Property.m_Name; } }
        public string Icon                          { get { return m_Icon; } }
        public string Category                      { get { return m_Category; } }

        private VFXProperty m_Property;
        private string m_Icon;
        private string m_Category;

        public VFXDataBlockDesc(VFXProperty property, string icon, string category)
        {
            m_Property = property;
            m_Icon = icon;
            m_Category = category;
        }
    }

    public sealed class VFXBlockDesc
    {
        [Flags]
        public enum Flag
        {
            kNone = 0,
            kHasRand = 1 << 0,
            kHasKill = 1 << 1,
        }

        private static readonly bool USE_SAFE_FUNCTION_NAME = false; // Set that to true to use longer but function names guaranteed with no collisions

        public string ID                    { get { return m_ID; } }
        public string FunctionName          { get { return m_FunctionName; } }
        public string Name                  { get { return m_Name; } }
        public string Icon                  { get { return m_Icon; } }
        public string Category              { get { return m_Category; } }
        public string Description           { get { return m_Description; } }
        public string Source                { get { return m_Source; } }
        public VFXProperty[] Properties     { get { return m_Properties; } }
        public VFXAttribute[] Attributes    { get { return m_Attributes; } }
        public Flag Flags                   { get { return m_Flags; } }
        public int SlotHash                 { get { return m_SlotHash; } }

        public bool IsSet(Flag flag)
        {
            return (Flags & flag) == flag;
        }

        internal VFXBlockDesc(VFXBlockType blockType)
        {
            m_ID = blockType.GetType().FullName;
            m_Name = blockType.Name;
            m_Icon = blockType.Icon;
            m_Category = blockType.Category;
            m_Description = blockType.Description;
            m_Properties = blockType.Properties.ToArray();
            m_Attributes = blockType.Attributes.ToArray();

            m_FunctionName = ConvertFunctionName(blockType);
            m_Source = ConvertSource(blockType);
            m_Flags = GetFlags(m_Source);

            m_SlotHash = ComputeSlotHash();
        }

        // Constructor from legacy block
        // TODO Remove that once blk files are no more used
        internal VFXBlockDesc(VFXBlock block)
        {
            m_ID = block.m_Name + block.m_Hash;
            m_Name = block.m_Name;
            m_Icon = block.m_IconPath;
            m_Category = block.m_Category.Length > 0 ? block.m_Category.Substring(0, block.m_Category.Length - 1) : block.m_Category; // remove the final '/'
            m_Description = "";

            m_FunctionName = new string((from c in (USE_SAFE_FUNCTION_NAME ? m_ID : m_Name) where char.IsLetterOrDigit(c) select c).ToArray());
            m_Source = block.m_Source;
            if (m_Source.StartsWith("\t"))
                m_Source = m_Source.Substring(1);

            m_Properties = new VFXProperty[block.m_Params.Length];
            for (int i = 0; i < block.m_Params.Length; ++i)
            {
                VFXParam param = block.m_Params[i];
                m_Properties[i] = new VFXProperty(VFXPropertyConverter.CreateSemantics(param.m_Type), param.m_Name);
            }

            m_Attributes = new VFXAttribute[block.m_Attribs.Length];
            for (int i = 0; i < block.m_Attribs.Length; ++i)
            {
                VFXAttrib attrib = block.m_Attribs[i];
                var attribute = new VFXAttribute();
                attribute.m_Name = attrib.m_Param.m_Name;
                attribute.m_Type = VFXPropertyConverter.ConvertType(attrib.m_Param.m_Type);
                attribute.m_Writable = attrib.m_Writable;
                m_Attributes[i] = attribute;
            }

            // Convert flag
            m_Flags = Flag.kNone;
            if ((block.m_Flags & (int)VFXBlock.Flag.kHasRand) != 0)
                m_Flags |= Flag.kHasRand;
            if ((block.m_Flags & (int)VFXBlock.Flag.kHasKill) != 0)
                m_Flags |= Flag.kHasKill;

            m_SlotHash = ComputeSlotHash();
        }

        // SlotHash is useful to determine whether the list of properties has changed. In that case links and values in the slot cannot be deserialized and must be discarded.
        // Not that if the code of semantic types changes, the hash wont change causing possible errors when deserializing slots...
        private int ComputeSlotHash()
        {
            Int32 hash = 0;
            foreach (var property in Properties)
                hash = (hash * 3) ^ Animator.StringToHash(property.m_Type.GetType().FullName);
            return hash;
        }

        private static string ConvertFunctionName(VFXBlockType blockType)
        {
            // Just use type full name with . replaced
            if (USE_SAFE_FUNCTION_NAME)
            {
                string typeName = blockType.GetType().FullName;
                return typeName.Replace('.', '_');
            }
            else
                return blockType.GetType().Name;
        }

        // Initially this was used to replace custom semantics with real code but it is now done directly via macros in shaders
        private static string ConvertSource(VFXBlockType blockType)
        {
            var lines = Regex.Split(blockType.Source, "\r\n|\r|\n");

            // just trim empty lines
            int endIndex = lines.Length - 1;
            while (endIndex > 0 && IsNullOrWhiteSpace(lines[endIndex]))
                --endIndex;

            int startIndex = 0;
            while (startIndex < endIndex && IsNullOrWhiteSpace(lines[startIndex]))
                ++startIndex;

            // Add tabulations
            StringBuilder builder = new StringBuilder(blockType.Source.Length + endIndex - startIndex);
            for (int i = startIndex; i <= endIndex; ++i)
            {
                if (i != startIndex) // as the first tab will be added directly due to the indentation
                    builder.Append('\t');
                if (i != endIndex)
                    builder.AppendLine(lines[i]);
                else
                    builder.Append(lines[i]);
            }

            return builder.ToString();
        }

        // This is a method that exists in string class from C# 4.5
        // TODO Use the standard one once we can...
        private static bool IsNullOrWhiteSpace(string str)
        {
            if (str == null)
                return true;

            foreach (var c in str)
                if (!Char.IsWhiteSpace(c))
                    return false;

            return true;
        }

        private static Flag GetFlags(string src)
        {
            Flag flag = Flag.kNone;

            // Just stupid check at the moment
            // TODO Check whether it is preceded or followed by an alphanumeric character in which case, it is not a keyword...
            if (src.IndexOf("RAND") != -1)
                flag |= Flag.kHasRand;
            if (src.IndexOf("KILL") != -1)
                flag |= Flag.kHasKill;

            return flag;
        }

        private string m_ID;
        private string m_FunctionName;
        private string m_Name;
        private string m_Icon;
        private string m_Category;
        private string m_Description;
        private string m_Source;

        private VFXProperty[] m_Properties;
        private VFXAttribute[] m_Attributes;

        private int m_SlotHash;
        private Flag m_Flags;
    }
}