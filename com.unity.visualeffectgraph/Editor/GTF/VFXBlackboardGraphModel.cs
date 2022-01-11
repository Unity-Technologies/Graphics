using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;

namespace UnityEditor.VFX
{
    public class VFXBlackboardGraphModel : BlackboardGraphModel
    {
        /// <inheritdoc />
        public VFXBlackboardGraphModel(IGraphAssetModel graphAssetModel)
            : base(graphAssetModel)
        {
        }

        public override string GetBlackboardTitle()
        {
            var title = base.GetBlackboardTitle();
            if (string.IsNullOrEmpty(title))
                return "VFX Graph";
            return title + " VFX";
        }

        public override string GetBlackboardSubTitle()
        {
            return "Subtitle here";
        }
    }
}
