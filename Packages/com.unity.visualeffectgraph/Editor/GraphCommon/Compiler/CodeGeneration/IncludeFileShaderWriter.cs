using System.Text;
using UnityEngine;

namespace Unity.GraphCommon.LowLevel.Editor
{
    class IncludeFileShaderWriter : ShaderWriter
    {
        private string m_GuardName;
        private int m_BodyStart;

        public override void Begin(string name)
        {
            base.Begin(name);

            Debug.Assert(m_GuardName == null);

            m_GuardName = BuildGuardName(name);

            WriteLine($"#ifndef {m_GuardName}");
            WriteLine($"#define {m_GuardName}");
            NewLine();

            m_BodyStart = ShaderBuilder.Length;
        }

        /// <summary>
        /// The guarded include file, or null when nothing was written into it: an include file with no body
        /// is no include file, and callers skip it rather than emitting an empty guard.
        /// </summary>
        public override string End()
        {
            var guardName = m_GuardName;
            m_GuardName = null;

            if (ShaderBuilder.Length == m_BodyStart)
            {
                base.End();
                return null;
            }

            NewLine();
            WriteLine($"#endif // {guardName}");
            return base.End();
        }

        string BuildGuardName(string name)
        {
            Debug.Assert(ShaderBuilder.Length == 0);

            StringBuilder builder = new();
            void AddWord(ref int from, int to)
            {
                int length = to - from;
                if (length > 0)
                {
                    builder.Append('_');
                    builder.Append(name.Substring(from, to - from).ToUpperInvariant());
                    from = to;
                }
            }

            builder.Append("VFX");

            int index = 0;
            bool wasLowercase = false;
            for (int i = 0; i < name.Length; ++i)
            {
                if (char.IsWhiteSpace(name[i]))
                {
                    AddWord(ref index, i);
                    index++;
                }

                bool isLowercase = char.IsLower(name[i]);
                if (wasLowercase && !isLowercase)
                {
                    AddWord(ref index, i);
                }
                wasLowercase = isLowercase;

            }
            AddWord(ref index, name.Length);

            return builder.ToString();
        }
    }
}
