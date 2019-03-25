using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    struct GeneratedFunction
    {
        public string name;
        public string code;
    }
    
    class SubGraphDatabase : ScriptableObject
    {
        public static SubGraphDatabase instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = AssetDatabase.LoadAssetAtPath<SubGraphDatabase>(SubGraphDatabaseImporter.path);
                }
                return s_Instance;
            }
            set => s_Instance = value;
        }

        static SubGraphDatabase s_Instance;
        
        public List<SubGraphData> subGraphs = new List<SubGraphData>();

        public List<string> subGraphGuids = new List<string>();
        
        public List<string> functionNames = new List<string>();
        
        public List<string> functionSources = new List<string>();
    }
}
