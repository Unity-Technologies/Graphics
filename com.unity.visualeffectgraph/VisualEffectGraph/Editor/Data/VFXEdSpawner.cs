using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEditor;
using System.Collections;
using Object = UnityEngine.Object;
using System;

namespace UnityEditor.Experimental
{
    internal abstract class VFXEdSpawner
    {
        public readonly Vector2 m_canvasPosition;

        public VFXEdSpawner(Vector2 position)
        {
            m_canvasPosition = position;
        }

        public abstract void Spawn();
    }

    internal class VFXEdContextNodeSpawner : VFXEdSpawner
    {
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
            m_DataSource.AddElement(new VFXEdContextNode(m_canvasPosition, m_Desc, m_DataSource));
            m_Canvas.ReloadData();
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
            m_DataSource.AddElement(new VFXEdTriggerNode(m_canvasPosition, m_DataSource));
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
            m_DataSource.AddElement(new VFXEdEventNode(m_canvasPosition, m_DataSource,m_EventName));
            m_Canvas.ReloadData();
        }
    }

    internal class VFXEdDataNodeSpawner : VFXEdSpawner
    {
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
            VFXEdDataNode n = new VFXEdDataNode(m_canvasPosition, m_DataSource);
            m_DataSource.AddElement(n);
            if (m_InitialBlock != null)
            {
                VFXEdDataNodeBlockSpawner spawner = new VFXEdDataNodeBlockSpawner(m_canvasPosition, m_InitialBlock, n, m_DataSource, m_InitialBlock.Name);
                spawner.Spawn();
            }
            m_Canvas.ReloadData();
        }
    }

    internal class VFXEdProcessingNodeBlockSpawner : VFXEdSpawner
    {
        VFXEdContextNode m_Node;
        VFXBlockDesc m_Block;
        VFXEdDataSource m_DataSource;

        public VFXEdProcessingNodeBlockSpawner(Vector2 position, VFXBlockDesc block, VFXEdContextNode node, VFXEdDataSource datasource)
            : base (position)
        {
            m_Block = block;
            m_Node = node;
            m_DataSource = datasource;
        }

        public override void Spawn()
        {
            m_Node.NodeBlockContainer.CaptureDrop = true;
            m_Node.NodeBlockContainer.UpdateCaptureDrop(m_canvasPosition);
            m_Node.NodeBlockContainer.AcceptDrop(new VFXEdProcessingNodeBlock(m_Block, m_DataSource));
        }
    }

    internal class VFXEdDataNodeBlockSpawner : VFXEdSpawner
    {
        VFXEdDataNode m_Node;
        VFXDataBlockDesc m_DataBlockDesc;
        VFXEdDataSource m_DataSource;
        DataContainerInfo m_dataContainerInfo;
        string m_exposedName;

        public VFXEdDataNodeBlockSpawner(Vector2 position, VFXDataBlockDesc datablock, VFXEdDataNode node, VFXEdDataSource datasource, string exposedName)
            : base (position)
        {
            m_DataBlockDesc = datablock;
            m_Node = node;
            m_DataSource = datasource;
            m_exposedName = exposedName;
        }

        public VFXEdDataNodeBlockSpawner(Vector2 position, VFXDataBlockDesc datablock, VFXEdDataNode node, VFXEdDataSource datasource, string exposedName, DataContainerInfo dataContainerInfo)
            : this (position, datablock, node, datasource, exposedName)
        {
            m_dataContainerInfo = dataContainerInfo;
        }


        public override void Spawn()
        {
            m_Node.NodeBlockContainer.AddNodeBlock(new VFXEdDataNodeBlock(m_DataBlockDesc, m_DataSource, m_exposedName));
        }
    }

}
