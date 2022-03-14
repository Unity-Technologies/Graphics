using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    /// <summary>
    /// A model that represents a blackboard for a graph.
    /// </summary>
    [MovedFrom(false, sourceAssembly: "Unity.GraphTools.Foundation.Overdrive.Editor")]
    public class BlackboardGraphModel : GraphElementModel, IBlackboardGraphModel
    {
        /// <inheritdoc />
        public bool Valid => GraphModel != null;


        /// <summary>
        /// Initializes a new instance of the <see cref="BlackboardGraphModel" /> class.
        /// </summary>
        /// <param name="graphAssetModel">The graph asset model used as the data source.</param>
        public BlackboardGraphModel(IGraphAssetModel graphAssetModel)
        {
            AssetModel = graphAssetModel;
        }

        /// <inheritdoc />
        public virtual string GetBlackboardTitle()
        {
            return AssetModel?.FriendlyScriptName ?? "";
        }

        /// <inheritdoc />
        public virtual string GetBlackboardSubTitle()
        {
            return "Class Library";
        }
    }
}
