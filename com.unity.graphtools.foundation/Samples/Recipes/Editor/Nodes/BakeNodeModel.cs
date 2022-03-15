using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Recipes
{
    [Serializable]
    [SearcherItem(typeof(RecipeStencil), SearcherContext.Graph, "Cooking/Bake")]
    public class BakeNodeModel : RecipeNodeBaseModel
    {
        internal static string TemperatureFieldName => nameof(m_Temperature);

        [SerializeField]
        [ModelSetting]
        [Tooltip("The baking temperature.")]
        [FormerlySerializedAs("m_TemperatureC")]
        Temperature m_Temperature = new Temperature() { Value = 180, Unit = TemperatureUnit.Celsius };

        [SerializeField]
        [ModelSetting]
        [Tooltip("The bake time (minutes).")]
        [FormerlySerializedAs("m_Minutes")]
        int m_Time = 60;

        public Temperature Temperature
        {
            get => m_Temperature;
            set => m_Temperature = value;
        }

        public int Duration
        {
            get => m_Time;
            set => m_Time = value;
        }

        protected override void OnDefineNode()
        {
            base.OnDefineNode();

            AddInputPort("Cookware", PortType.Data, RecipeStencil.Cookware, options: PortModelOptions.NoEmbeddedConstant);

            AddInputPort("Ingredients", PortType.Data, RecipeStencil.Ingredient, options: PortModelOptions.NoEmbeddedConstant);
            AddOutputPort("Result", PortType.Data, RecipeStencil.Ingredient, options: PortModelOptions.NoEmbeddedConstant);
        }
    }
}
