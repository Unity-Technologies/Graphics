using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEditor;
using UnityEditor.Experimental.VFX;
using System.Collections;
using Object = UnityEngine.Object;
using System;

namespace UnityEditor.Experimental
{
    internal abstract class VFXEdSpawner
    {
        public readonly Vector2 m_CanvasPosition;

        public VFXEdSpawner(Vector2 position)
        {
            m_CanvasPosition = position;
        }

        public abstract void Spawn();
    }

    internal class VFXEdContextNodeSpawner : VFXEdSpawner
    {
        public VFXEdContextNode SpawnedNode { get { return m_SpawnedNode;} }
        VFXEdContextNode m_SpawnedNode;

        VFXEdDataSource m_DataSource;
        VFXEdCanvas m_Canvas;
        VFXContextDesc m_Desc;

        public VFXEdContextNodeSpawner(VFXEdDataSource datasource, VFXEdCanvas canvas, Vector2 position, VFXContextDesc desc)
            : base (position)
        {
            m_DataSource = datasource;
            m_Canvas = canvas;
            m_Desc = desc;
        }

        public override void Spawn()
        {
            VFXContextModel context =  m_DataSource.CreateContext( m_Desc, m_CanvasPosition);
            m_Canvas.ReloadData();
            m_SpawnedNode = m_DataSource.GetUI<VFXEdContextNode>(context);
        }
    }

    internal class VFXEdTriggerNodeSpawner : VFXEdSpawner
    {
        VFXEdDataSource m_DataSource;
        VFXEdCanvas m_Canvas;

        public VFXEdTriggerNodeSpawner(VFXEdDataSource datasource, VFXEdCanvas canvas, Vector2 position)
            : base (position)
        {
            m_DataSource = datasource;
            m_Canvas = canvas;
        }

        public override void Spawn()
        {
            m_DataSource.AddElement(new VFXEdTriggerNode(m_CanvasPosition, m_DataSource));
            m_Canvas.ReloadData();
        }
    }

    internal class VFXEdEventNodeSpawner : VFXEdSpawner
    {
        VFXEdDataSource m_DataSource;
        VFXEdCanvas m_Canvas;
        string m_EventName;

        public VFXEdEventNodeSpawner(VFXEdDataSource datasource, VFXEdCanvas canvas, Vector2 position, string eventname)
            : base (position)
        {
            m_DataSource = datasource;
            m_Canvas = canvas;
            m_EventName = eventname;
        }

        public override void Spawn()
        {
            m_DataSource.AddElement(new VFXEdEventNode(m_CanvasPosition, m_DataSource, m_EventName));
            m_Canvas.ReloadData();
        }
    }

    internal class VFXEdDataNodeSpawner : VFXEdSpawner
    {
        public VFXEdDataNode SpawnedNode { get { return m_SpawnedNode;} }
        public VFXEdDataNodeBlock SpawnedNodeBlock { get { return m_SpawnedNodeBlock;} }
        VFXEdDataNode m_SpawnedNode;
        VFXEdDataNodeBlock m_SpawnedNodeBlock;

        VFXEdDataSource m_DataSource;
        VFXEdCanvas m_Canvas;
        VFXDataBlockDesc m_InitialBlock;

        public VFXEdDataNodeSpawner(VFXEdDataSource datasource, VFXEdCanvas canvas, Vector2 position)
            : base (position)
        {
            m_DataSource = datasource;
            m_Canvas = canvas;
        }

        public VFXEdDataNodeSpawner(VFXEdDataSource datasource, VFXEdCanvas canvas, Vector2 position, VFXDataBlockDesc block)
            : this (datasource, canvas,position)
        {
            m_InitialBlock = block;
        }

        public override void Spawn()
        {
            var node = m_DataSource.CreateDataNode(m_CanvasPosition);

            if (m_InitialBlock != null)
            {
                VFXEdDataNodeBlockSpawner spawner = new VFXEdDataNodeBlockSpawner(m_CanvasPosition, m_InitialBlock, m_DataSource.GetUI<VFXEdDataNode>(node), m_DataSource, m_InitialBlock.Name);
                spawner.Spawn();
                m_SpawnedNodeBlock = spawner.SpawnedNodeBlock;
            }
            m_Canvas.ReloadData();
        }
    }

    internal class VFXEdProcessingNodeBlockSpawner : VFXEdSpawner
    {
        VFXBlockDesc m_BlockDesc;
        VFXContextModel m_Context;
        int m_Index;
        VFXEdDataSource m_DataSource;

        public VFXEdProcessingNodeBlockSpawner(Vector2 position, VFXBlockDesc block, VFXEdContextNode node, VFXEdDataSource datasource)
            : base (position)
        {
            m_Index = node.NodeBlockContainer.GetDropIndex(position);
            m_Context = node.Model;
            m_BlockDesc = block;
            m_DataSource = datasource;
        }

        public override void Spawn()
        {
            m_DataSource.Create(new VFXBlockModel(m_BlockDesc), m_Context, m_Index);
        }
    }

    internal class VFXEdDataNodeBlockSpawner : VFXEdSpawner
    {
        public VFXEdDataNodeBlock SpawnedNodeBlock { get { return m_SpawnedNodeBlock;} }
        VFXEdDataNodeBlock m_SpawnedNodeBlock;

        VFXDataBlockDesc m_DataBlockDesc;
        VFXDataNodeModel m_Node;    
        VFXEdDataSource m_DataSource;
        string m_exposedName;

        public VFXEdDataNodeBlockSpawner(Vector2 position, VFXDataBlockDesc datablock, VFXEdDataNode node, VFXEdDataSource datasource, string exposedName)
            : base (position)
        {
            m_DataBlockDesc = datablock;
            m_Node = node.Model;
            m_DataSource = datasource;
            m_exposedName = exposedName;
        }

        public VFXEdDataNodeBlockSpawner(Vector2 position, VFXDataBlockDesc datablock, VFXEdDataNode node, VFXEdDataSource datasource, string exposedName, DataContainerInfo dataContainerInfo)
            : this (position, datablock, node, datasource, exposedName)
        {}

        public override void Spawn()
        {
            var blockModel = new VFXDataBlockModel(m_DataBlockDesc);
            m_DataSource.Create(blockModel, m_Node);
            m_SpawnedNodeBlock = m_DataSource.GetUI<VFXEdDataNodeBlock>(blockModel);
        }
    }

}
