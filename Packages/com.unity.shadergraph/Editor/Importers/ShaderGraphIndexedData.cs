using System;

using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    class ShaderGraphIndexedData : ScriptableObject
    {
        [SerializeField] internal Experimental.GraphView.DataBag additionalSearchTerms;


        public Experimental.GraphView.DataBag AdditionalSeachTerms
        {
            get => additionalSearchTerms;
            set => additionalSearchTerms = value;
        }
    }
}
