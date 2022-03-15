using System;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Recipes
{
    [Serializable]
    public class RecipeGraphModel : GraphModel
    {
        // This will be displayed in the basic section of the graph inspector, because of the ModelSetting attribute.
        [SerializeField]
        [ModelSetting]
        // ReSharper disable once NotAccessedField.Local
        int m_Rating;

        // This will be displayed in the advanced section of the graph inspector, because of the lack of ModelSetting attribute.
        [SerializeField]
        // ReSharper disable once NotAccessedField.Local
        string m_Author;

        // This will not be displayed in the graph inspector, because of the lack of the HideInInspector attribute.
        [SerializeField]
        [HideInInspector]
        // ReSharper disable once NotAccessedField.Local
        string m_SecretSauce;

        protected override bool IsCompatiblePort(IPortModel startPortModel, IPortModel compatiblePortModel)
        {
            return base.IsCompatiblePort(startPortModel, compatiblePortModel) && startPortModel.DataTypeHandle.Equals(compatiblePortModel.DataTypeHandle);
        }
    }
}
