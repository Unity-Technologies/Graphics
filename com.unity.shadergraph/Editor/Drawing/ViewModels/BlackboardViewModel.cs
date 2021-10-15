using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.Experimental.GraphView;

namespace UnityEditor.ShaderGraph.Drawing
{
    class BlackboardViewModel : ISGViewModel
    {
        public GraphData model { get; set; }
        public VisualElement parentView { get; set; }
        public string title { get; set; }
        public string subtitle { get; set; }
        public Dictionary<string, IGraphDataAction> propertyNameToAddActionMap { get; set; }
        public Dictionary<string, IGraphDataAction> defaultKeywordNameToAddActionMap { get; set; }
        public Dictionary<string, IGraphDataAction> builtInKeywordNameToAddActionMap { get; set; }
        public Tuple<string, IGraphDataAction> defaultDropdownNameToAdd { get; set; }

        public IGraphDataAction addCategoryAction { get; set; }
        public Action<IGraphDataAction> requestModelChangeAction { get; set; }
        public List<CategoryData> categoryInfoList { get; set; }

        // Can't add disbled keywords, so don't need an add action
        public List<string> disabledKeywordNameList { get; set; }
        public List<string> disabledDropdownNameList { get; set; }

        public BlackboardViewModel()
        {
            propertyNameToAddActionMap = new Dictionary<string, IGraphDataAction>();
            defaultKeywordNameToAddActionMap = new Dictionary<string, IGraphDataAction>();
            builtInKeywordNameToAddActionMap = new Dictionary<string, IGraphDataAction>();
            defaultDropdownNameToAdd = null;
            categoryInfoList = new List<CategoryData>();
            disabledKeywordNameList = new List<string>();
            disabledDropdownNameList = new List<string>();
        }

        public void ResetViewModelData()
        {
            subtitle = String.Empty;
            propertyNameToAddActionMap.Clear();
            defaultKeywordNameToAddActionMap.Clear();
            builtInKeywordNameToAddActionMap.Clear();
            defaultDropdownNameToAdd = null;
            categoryInfoList.Clear();
            disabledKeywordNameList.Clear();
            disabledDropdownNameList.Clear();
            requestModelChangeAction = null;
        }
    }
}
