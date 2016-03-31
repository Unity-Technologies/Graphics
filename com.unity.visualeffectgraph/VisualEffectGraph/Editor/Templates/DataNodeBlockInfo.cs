using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;
using System.Xml.Linq;

namespace UnityEditor.Experimental
{
    internal class DataNodeBlockInfo : NodeBlockInfo
    {
        public string ExposedName;
        public DataContainerInfo dataContainer;

        public static DataNodeBlockInfo Create(string blockname, string exposedname, string blockLibraryName)
        {
            DataNodeBlockInfo ni = CreateInstance<DataNodeBlockInfo>();
            ni.BlockName = blockname;
            ni.BlockLibraryName = blockLibraryName;
            ni.ExposedName = exposedname;
            ni.dataContainer = null;
            return ni;
        }

        public static DataNodeBlockInfo Create(string blockname, string exposedname, string blockLibraryName, DataContainer container)
        {
            DataNodeBlockInfo ni = CreateInstance<DataNodeBlockInfo>();
            ni.BlockName = blockname;
            ni.BlockLibraryName = blockLibraryName;
            ni.ExposedName = exposedname;
            ni.dataContainer = container.getContainerInfo();
            return ni;
        }

        public static DataNodeBlockInfo Create(string blockname, string exposedname, string blockLibraryName, XElement dataContainerElement)
        {
            DataNodeBlockInfo ni = CreateInstance<DataNodeBlockInfo>();
            ni.BlockName = blockname;
            ni.ExposedName = exposedname;
            ni.BlockLibraryName = blockLibraryName;

            switch(dataContainerElement.Attribute("type").Value)
            {
                case "CurveFloatContainer": ni.dataContainer = new CurveFloatDataContainerInfo(dataContainerElement); break;
                case "CurveVectorContainer": ni.dataContainer = new CurveVectorDataContainerInfo(dataContainerElement); break;
                case "GradientContainer": ni.dataContainer = new GradientDataContainerInfo(dataContainerElement); break;

                default: Debug.LogError("ERROR IN DESERIALIZE TEMPLATE LIBRARY : UNKNOWN " + dataContainerElement.Attribute("type").Value);
                    break;
            }

            return ni;
        }
             
        public DataNodeBlockInfo() : base() { }
    }



}
