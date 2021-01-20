using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace UnityEditor.ShaderGraph.Drawing
{
    class BlackboardViewModel : ISGViewModel
    {
        // TODO: Add commands from controller that views can use to bind to things that depend on them like buttons
        // Thus a view model is not just data but also commands for functional aspects of the UI
        public GraphData Model { get; set; }
        public string Subtitle { get; set; }
        public Dictionary<string, IGraphDataAction> PropertyNameToAddActionMap { get; set; }
        public Dictionary<string, IGraphDataAction> DefaultKeywordNameToAddActionMap { get; set; }
        public Dictionary<string, IGraphDataAction> BuiltInKeywordNameToAddActionMap { get; set; }

        // Can't add disbled keywords, so don't need an add action
        public List<string> DisabledKeywordNameList { get; set; }

        public BlackboardViewModel()
        {
            PropertyNameToAddActionMap = new Dictionary<string, IGraphDataAction>();
            DefaultKeywordNameToAddActionMap = new Dictionary<string, IGraphDataAction>();
            BuiltInKeywordNameToAddActionMap = new Dictionary<string, IGraphDataAction>();
            DisabledKeywordNameList = new List<string>();
        }

        public void Reset()
        {
            PropertyNameToAddActionMap.Clear();
            DefaultKeywordNameToAddActionMap.Clear();
            BuiltInKeywordNameToAddActionMap.Clear();
            DisabledKeywordNameList.Clear();
        }
    }
}
