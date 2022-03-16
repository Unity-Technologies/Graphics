using System.Linq;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Recipes
{
    public class RecipeProcessor : IGraphProcessor
    {
        static readonly Temperature k_MinTemperature = new Temperature { Unit = TemperatureUnit.Celsius, Value = 50 };
        static readonly Temperature k_MaxTemperature = new Temperature { Unit = TemperatureUnit.Celsius, Value = 500 };
        const int k_MinDuration = 1;
        const int k_MaxDuration = 480;

        GraphProcessingResult m_LastResults;

        /// <inheritdoc />
        public GraphProcessingResult ProcessGraph(IGraphModel graphModel, GraphChangeDescription changes)
        {
            var doProcessing = changes == null ||
                changes.NewModels.Any(m => m is BakeNodeModel || m is MixNodeModel) ||
                changes.DeletedModels.Any(m => m is BakeNodeModel || m is MixNodeModel) ||
                changes.ChangedModels.Any(m =>
                    m.Key is BakeNodeModel && m.Value.HasChange(ChangeHint.Data) ||
                    m.Key is MixNodeModel && m.Value.HasChange(ChangeHint.GraphTopology) ||
                    m.Key is IPortModel portModel && portModel.NodeModel is MixNodeModel && m.Value.HasChange(ChangeHint.GraphTopology));

            if (!doProcessing)
                return m_LastResults;

            Debug.Log($"Checking {graphModel.Name}");

            m_LastResults = new GraphProcessingResult();
            foreach (var nodeModel in graphModel.NodeModels)
            {
                switch (nodeModel)
                {
                    case BakeNodeModel bnm:
                        if (bnm.Temperature < k_MinTemperature)
                            m_LastResults.AddError("Not hot enough to bake!", bnm,
                                new QuickFix("Adjust temperature", cd => cd.Dispatch(new SetTemperatureCommand(k_MinTemperature.As(bnm.Temperature.Unit), bnm))));
                        else if (bnm.Temperature > k_MaxTemperature)
                            m_LastResults.AddError("Too hot to bake!", bnm,
                                new QuickFix("Adjust temperature", cd => cd.Dispatch(new SetTemperatureCommand(k_MaxTemperature.As(bnm.Temperature.Unit), bnm))));

                        if (bnm.Duration < k_MinDuration)
                            m_LastResults.AddError("Baking duration isn't long enough!", bnm,
                                new QuickFix("Adjust duration", cd => cd.Dispatch(new SetDurationCommand(k_MinDuration, bnm))));
                        else if (bnm.Duration > k_MaxDuration)
                            m_LastResults.AddError("Baking duration is too long!", bnm,
                                new QuickFix("Adjust duration", cd => cd.Dispatch(new SetDurationCommand(k_MaxDuration, bnm))));
                        break;

                    case MixNodeModel mnm:
                        if (mnm.InputsByDisplayOrder.Count(
                            p => p.UniqueName.StartsWith("Ingredient") && p.IsConnected()) < 2)
                        {
                            m_LastResults.AddWarning("Mixing requires at least 2 ingredients!", mnm);
                        }
                        break;

                    case FryNodeModel _:
                    case BeatNodeModel _:
                        // Nothing for now.
                        break;
                }
            }

            return m_LastResults;
        }
    }
}
