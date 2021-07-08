using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;

namespace GtfPlayground.DataModel
{
    [SearcherItem(typeof(PlaygroundStencil), SearcherContext.Graph, "Randomized Node")]
    public class RandomizedNodeModel : NodeModel
    {
        protected override void OnDefineNode()
        {
            base.OnDefineNode();

            for (var i = 0; i < 5; i++)
            {
                this.AddDataInputPort<float>($"Port {i}");
                if (Random.value <= 0.5) break;
            }

            for (var i = 0; i < 5; i++)
            {
                this.AddDataOutputPort<float>($"Port {i}");
                if (Random.value <= 0.5) break;
            }
        }
    }
}
