using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using System.ComponentModel;
using UnityEngine;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [AttributeUsage(AttributeTargets.Field)]
    public class LineAttribute : Attribute
    {
        public int      lineNumber;
        public Regex    match;
        public Regex    startMatch;
        public Regex    stopMatch;
        public bool     required;
        public int      maxLines;
        public int      lineCount;
        public bool     split;

        public bool     matchMode;

        public LineAttribute(int lineNumber, string match = @"(.*)", bool required = true, int maxLines = 1, bool split = true)
        {
            this.lineNumber = lineNumber;
            this.match = new Regex(match);
            this.required = required;
            this.maxLines = maxLines;
            this.split = split;
            this.lineCount = 0;

            matchMode = true;
        }

        public LineAttribute(int lineNumber, string start, string stop)
        {
            this.lineNumber = lineNumber;
            this.startMatch = new Regex(start);
            this.stopMatch = new Regex(stop);
            
            matchMode = false;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class SkipIfEqualAttribute : Attribute
    {
        public string   fieldName;
        public string   value;

        public SkipIfEqualAttribute(string fieldName, string value)
        {
            this.fieldName = fieldName;
            this.value = value;
        }
    }

    public class AsciiFileParser
    {
        private class FileField
        {
            public FieldInfo            field;
            public LineAttribute        line;
            public SkipIfEqualAttribute skip;

            public FileField(FieldInfo field)
            {
                this.field = field;
                this.line = null;
                this.skip = null;
                
                foreach (var attr in field.GetCustomAttributes(false))
                {
                    if (attr is LineAttribute)
                        line = attr as LineAttribute;
                    if (attr is SkipIfEqualAttribute)
                        skip = attr as SkipIfEqualAttribute;
                }
            }
        }

        string[]    m_Lines;
        int         m_LineIndex = 0;

        public AsciiFileParser(string assetPath)
        {
            m_Lines = File.ReadAllLines(assetPath);
        }

        // Check if we have to skip this field
        bool SkipField<T>(FileField fileField, T instance)
        {
            if (fileField.skip != null)
            {
                string skipFieldValue = instance.GetType().GetField(fileField.skip.fieldName).GetValue(instance).ToString();
                if (skipFieldValue == fileField.skip.value)
                    return true;
            }
            return false;
        }

        IEnumerable<string> ExpressionWhileMatch(FileField f)
        {
            while (f.line.match.IsMatch(m_Lines[m_LineIndex]))
            {
                if (f.line.split)
                {
                    foreach (var w in m_Lines[m_LineIndex].Split(null))
                        yield return f.line.match.Match(w).Groups[1].Value;
                }
                else
                {
                    var match = f.line.match.Match(m_Lines[m_LineIndex]);
                    yield return match.Groups[1].Value;
                }

                m_LineIndex++;
                f.line.lineCount++;

                // If we've reached the end of the file, break
                if (m_Lines.Length == m_LineIndex)
                    yield break;
                
                if (f.line.maxLines > 0 && f.line.lineCount >= f.line.maxLines)
                    yield break;
            }
        }

        IEnumerable<string> ExpressionUntilStop(FileField f)
        {
            // If the stop is on the same line than the start, we loop until we reach it
            if (f.line.stopMatch.IsMatch(m_Lines[m_LineIndex]))
            {
                foreach (var w in m_Lines[m_LineIndex].Split(null))
                {
                    yield return w;
                    if (f.line.stopMatch.IsMatch(w))
                        break;
                }
                yield break;
            }
            
            // Else we can loop until the stop condition is valid
            while (!f.line.stopMatch.IsMatch(m_Lines[m_LineIndex]))
            {
                foreach (var w in m_Lines[m_LineIndex].Split(null))
                    yield return w;
                m_LineIndex++;

                // If we've reached the end of the file, break
                if (m_Lines.Length == m_LineIndex)
                    yield break;
            }
        }

        IEnumerable<string> ExpressionMatches<T>(List<FileField> fileFieldList, T instance)
        {
            // If we've reached the end of the file, break
            if (m_Lines.Length == m_LineIndex)
                yield break;

            if (fileFieldList.Count == 1)
            {
                FileField f = fileFieldList.First();

                if (SkipField(f, instance))
                    yield break;

                // If we're in match mode, we iterate over lines and return them until we dont match anymore
                if (f.line.matchMode)
                {
                    foreach (var e in ExpressionWhileMatch(f))
                        yield return e;
                }
                else // start / stop mode
                {
                    // If the start condition don't match, we break the loop
                    if (!f.line.startMatch.IsMatch(m_Lines[m_LineIndex]))
                        yield break;
                    
                    foreach (var e in ExpressionUntilStop(f))
                        yield return e;
                }
            }
            else
            {
                int fieldCount = 0;

                // If we have multiple fields, we split each lines and returns until we have filled all the fields
                while (fieldCount < fileFieldList.Count)
                {
                    foreach (var w in m_Lines[m_LineIndex].Split(null))
                    {
                        yield return w;
                        fieldCount++;

                        if (fieldCount == fileFieldList.Count)
                            break;
                    }
                    
                    m_LineIndex++;

                    // If we've reached the end of the file, break
                    if (m_Lines.Length == m_LineIndex)
                        yield break;
                }
            }
        }

        void ParseLines<T>(IGrouping<int, FileField> fileFieldGroup, T instance)
        {
            var     fileFieldList = fileFieldGroup.ToList();
            int     i = 0;

            foreach (var expression in ExpressionMatches(fileFieldList, instance))
            {
                FileField f = (fileFieldList.Count == 1) ? fileFieldList.First() : fileFieldList[i];
                TypeConverter converter = TypeDescriptor.GetConverter(f.field.FieldType);

                if (String.IsNullOrEmpty(expression.Trim()))
                    continue ;

                Debug.Log("field: " + f.field.Name + " | " + expression);
                
                // If the target type is a list, we call add rather than setting it's value
                if (typeof(IList).IsAssignableFrom(f.field.FieldType))
                {
                    var listValue = f.field.GetValue(instance);

                    // Create the list if it's null
                    if (listValue == null)
                    {
                        listValue = Activator.CreateInstance(f.field.FieldType);
                        f.field.SetValue(instance, listValue);
                    }
                    
                    converter = TypeDescriptor.GetConverter(f.field.FieldType.GetGenericArguments().First());
                    object value = converter.ConvertFromString(expression);
                    f.field.FieldType.GetMethod("Add").Invoke(listValue, new object[]{ value });
                }
                else
                {
                    object value = converter.ConvertFromString(expression);
                    f.field.SetValue(instance, value);
                }
                
                i++;
            }
        }

        public void Parse<T>(T instance = default(T))
        {
            List<FileField> fileFields = new List<FileField>();

            if (instance == null)
                instance = (T)Activator.CreateInstance(typeof(T));
            
            var fields = instance.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            foreach (var field in fields)
                fileFields.Add(new FileField(field));
            
            // Object parsing instructions sanity check
            if (fileFields.Count == 0)
                throw new Exception("The provided object to parse does not contanis any parsing instructions");
            if (fileFields.First().line.lineNumber != 1)
                throw new Exception("The first parsing instruction does not start at line 1");
            
            try {
                foreach (var fileFieldGroup in fileFields.Where(f => f.line != null).OrderBy(f => f.line.lineNumber).GroupBy(f => f.line.lineNumber))
                    ParseLines(fileFieldGroup, instance);
            } catch (Exception e) {
                Debug.LogError("Error at line " + m_LineIndex + ": " + m_Lines[m_LineIndex] + "\n" + e);
            }
        }
    }
}