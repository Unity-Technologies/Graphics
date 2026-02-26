using System.Text;
using UnityEngine;

namespace Unity.GraphCommon.LowLevel.Editor
{
    class IncludeFileShaderWriter : ShaderWriter
    {
        private string m_GuardName;
        private StringBuilder m_Builder = new();

        public override void Begin(string name)
        {
            base.Begin(name);

            Debug.Assert(m_GuardName == null);

            m_GuardName = BuildGuardName(name);

            WriteLine($"#ifndef {m_GuardName}");
            WriteLine($"#define {m_GuardName}");
            NewLine();
        }

        public override string End()
        {
            NewLine();
            WriteLine($"#endif // {m_GuardName}");
            m_GuardName = null;
            return base.End();
        }

        string BuildGuardName(string name)
        {
            int index = 0;
            bool wasLowercase = false;

            Debug.Assert(ShaderBuilder.Length == 0);
            m_Builder.Append("VFX");
            for (int i = 0; i <= name.Length; ++i)
            {
                bool split = i == name.Length;
                if (!split)
                {
                    bool isLowercase = char.IsLower(name[i]);
                    split = wasLowercase && !isLowercase;
                    wasLowercase = isLowercase;
                }
                if (split)
                {
                    m_Builder.Append('_');
                    m_Builder.Append(name.Substring(index, i - index).ToUpperInvariant());
                    index = i;
                }
            }
            string guardName = m_Builder.ToString();
            m_Builder.Clear();
            return guardName;
        }
    }
}
