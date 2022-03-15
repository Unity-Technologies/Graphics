using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    public class MathBookPortModel: PortModel
    {
        string m_DisplayTitleCache;

        public override string DisplayTitle => m_DisplayTitleCache ??= Title.Nicify();
    }
}
