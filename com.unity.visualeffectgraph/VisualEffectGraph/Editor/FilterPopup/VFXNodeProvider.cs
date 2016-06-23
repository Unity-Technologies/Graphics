using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEditor.Experimental.VFX;


namespace UnityEditor.Experimental
{
    public class VFXNodeProvider : IProvider
    {
        Vector2 m_mousePosition;
        VFXEdContextNode m_contextNode;
        VFXEdDataSource m_dataSource;
        VFXEdCanvas m_canvas;

        private class VFXContextElement : VFXFilterWindow.Element
        {
            public VFXContextDesc m_Desc;

            public VFXContextElement(int level, VFXContextDesc desc)
            {
                this.level = level;
                content = new GUIContent(VFXContextDesc.GetTypeName(desc.m_Type) + " : " + desc.Name);
                m_Desc = desc;
            }
        }

        private class VFXEventElement : VFXFilterWindow.Element
        {
            public string m_Name;
            public bool m_Locked;

            public VFXEventElement(int level, string name, bool locked)
            {
                this.level = level;
                content = new GUIContent("Event : " + name);
                m_Name = name;
                m_Locked = locked;
            }
        }

        private class VFXSpawnerElement : VFXFilterWindow.Element
        {
            public VFXSpawnerBlockModel.Type m_Type;

            public VFXSpawnerElement(int level, VFXSpawnerBlockModel.Type type)
            {
                this.level = level;
                content = new GUIContent("Spawner : " + VFXSpawnerBlockModel.TypeToName(type));
                m_Type = type;
            }
        }

        internal VFXNodeProvider(Vector2 mousePosition, VFXEdDataSource dataSource, VFXEdCanvas canvas)
        {
            m_mousePosition = mousePosition;
            m_dataSource = dataSource;
            m_canvas = canvas;
        }

        public void CreateComponentTree(List<VFXFilterWindow.Element> tree)
        {
            tree.Add(new VFXFilterWindow.GroupElement(0, "Add Nodes"));

            tree.Add(new VFXFilterWindow.GroupElement(1, "Events..."));
            tree.Add(new VFXEventElement(2, "OnStart", true));
            tree.Add(new VFXEventElement(2, "OnStop", true));
            tree.Add(new VFXEventElement(2, "Custom", false));

            tree.Add(new VFXFilterWindow.GroupElement(1, "Spawner"));
            tree.Add(new VFXSpawnerElement(2, VFXSpawnerBlockModel.Type.kConstantRate));
            tree.Add(new VFXSpawnerElement(2, VFXSpawnerBlockModel.Type.kBurst));
            tree.Add(new VFXSpawnerElement(2, VFXSpawnerBlockModel.Type.kPeriodicBurst));
            tree.Add(new VFXSpawnerElement(2, VFXSpawnerBlockModel.Type.kVariableRate));

            var contexts = new List<VFXContextDesc>(VFXEditor.ContextLibrary.GetContexts());
            contexts.Sort((blockA, blockB) => {
                int res = blockA.m_Type.CompareTo(blockB.m_Type);
                return res != 0 ? res : blockA.Name.CompareTo(blockB.Name);
            });

            HashSet<string> categories = new HashSet<string>();

            foreach(VFXContextDesc desc in contexts)
            {

                if(!categories.Contains(desc.m_Type.ToString()))
                {
                    categories.Add(desc.m_Type.ToString());
                    tree.Add(new VFXFilterWindow.GroupElement(1, VFXContextDesc.GetTypeName(desc.m_Type)));
                }

                tree.Add(new VFXContextElement(2, desc));
            }

        }

        private Vector2 GetSpawnPosition()
        {
            return m_canvas.MouseToCanvas(m_mousePosition) - new Vector2(VFXEditorMetrics.NodeDefaultWidth / 2, -20);
        }

        public bool GoToChild(VFXFilterWindow.Element element, bool addIfComponent)
        {
            if (element is VFXContextElement)
            {
                m_dataSource.CreateContext(((VFXContextElement)element).m_Desc, GetSpawnPosition());
                m_canvas.ReloadData();
                return true;
            }

            if (element is VFXSpawnerElement)
            {
                var spawnerNode = m_dataSource.CreateSpawnerNode(GetSpawnPosition());
                m_dataSource.Create(new VFXSpawnerBlockModel(((VFXSpawnerElement)element).m_Type),spawnerNode);
                m_canvas.ReloadData();
                return true;
            }

            if (element is VFXEventElement)
            {
                var eventElement = (VFXEventElement)element;
                var eventNode = m_dataSource.CreateEventNode(GetSpawnPosition(), eventElement.m_Name, eventElement.m_Locked);
                m_canvas.ReloadData();
                return true;
            }
            
            return false;
        }
    }
}
