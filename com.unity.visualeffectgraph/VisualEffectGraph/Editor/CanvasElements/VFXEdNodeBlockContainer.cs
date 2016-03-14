using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
    internal class VFXEdNodeBlockContainer: CanvasElement
    {

        internal class DropInfo
        {
            public Vector2 MousePosition = Vector2.zero;
            public Rect highlightRect = new Rect();
            public int DropIndex = 0;
        }

        public bool CaptureDrop
        {
            get { return dropInfo != null; }
            set
            {
                if (value == false)
                    dropInfo = null;
                else
                {
                    dropInfo = new DropInfo();
                }
            }
        }

        public  List<VFXEdNodeBlockDraggable> nodeBlocks { get { return m_NodeBlocks; } }

        private DropInfo dropInfo;
        private List<VFXEdNodeBlockDraggable> m_NodeBlocks;

        public bool OwnsBlock(VFXEdNodeBlockDraggable item)
        {
            return m_NodeBlocks.Contains(item);
        }

        public VFXEdNodeBlockContainer(Vector2 size)
        {
            translation = VFXEditorMetrics.NodeBlockContainerPosition;
            scale = size + VFXEditorMetrics.NodeBlockContainerSizeOffset;
            m_NodeBlocks = new List<VFXEdNodeBlockDraggable>();
            m_Caps = Capabilities.Normal;
        }


        public void UpdateCaptureDrop(Vector2 MousePosition)
        {
            if (!CaptureDrop)
                CaptureDrop = true;
            dropInfo.MousePosition = MousePosition;

            Rect r = canvasBoundingRect;
            Vector2 localmousepos = new Vector2(dropInfo.MousePosition.x - r.x, dropInfo.MousePosition.y - r.y);

            float curY = 0.0f;
            int curIdx = 0;
            foreach (VFXEdNodeBlockDraggable b in m_NodeBlocks)
            {
                Rect offset = GetDrawableRect();

                if (localmousepos.y >= curY && localmousepos.y < (curY + b.scale.y / 2))
                {
                    dropInfo.highlightRect = new Rect(offset.x, offset.y + curY, scale.x, VFXEditorMetrics.NodeBlockContainerSeparatorHeight);
                    curY += VFXEditorMetrics.NodeBlockContainerSeparatorOffset;
                    curY += b.scale.y;
                    dropInfo.DropIndex = curIdx;
                }
                else if (localmousepos.y >= (curY + b.scale.y / 2) && localmousepos.y < (curY + b.scale.y))
                {
                    curY += b.scale.y;
                    dropInfo.highlightRect = new Rect(offset.x, offset.y + curY, scale.x, VFXEditorMetrics.NodeBlockContainerSeparatorHeight);
                    curY += VFXEditorMetrics.NodeBlockContainerSeparatorOffset;
                    dropInfo.DropIndex = curIdx + 1;
                }
                else
                {
                    curY += b.scale.y;
                }
                curIdx++;
            }
        }

        public void AddNodeBlock(VFXEdNodeBlockDraggable block)
        {
            AddNodeBlock(block, m_NodeBlocks.Count);
        }

        public void AddNodeBlock(VFXEdNodeBlockDraggable block, int index)
        {
            if (block.parent == null)
                AddChild(block);
            else
                block.SetParent(this);
            m_NodeBlocks.Insert(index, block);
            block.translation = Vector3.zero;
            Layout();

            // Update the model if inside a Context Node
            VFXEdContextNode nodeParent = FindParent<VFXEdContextNode>();
            if (nodeParent != null)
                try
                {
                    nodeParent.OnAddNodeBlock(block, index);
                }
                catch (Exception e)
                {
                    Debug.LogError(e.ToString());
                }

        }

        public int GetBlockIndex(VFXEdNodeBlockDraggable block)
        {
            return m_NodeBlocks.IndexOf(block);
        }

        public void DetachNodeBlock(VFXEdNodeBlockDraggable block)
        {
            if (m_NodeBlocks.Contains(block))
            {
                if ((ParentCanvas() as VFXEdCanvas).SelectedNodeBlock == block)
                {
                    (ParentCanvas() as VFXEdCanvas).SelectedNodeBlock = null;
                }

                m_NodeBlocks.Remove(block);
                m_Children.Remove(block);

                Layout();
                Invalidate();
            }
        }


        public void RemoveNodeBlock(VFXEdNodeBlockDraggable block)
        {
                DetachNodeBlock(block);
                block.OnRemoved();
        }

        public void ClearNodeBlocks()
        {
            List<VFXEdNodeBlockDraggable> todelete = new List<VFXEdNodeBlockDraggable>();
            foreach (VFXEdNodeBlockDraggable block in m_NodeBlocks)
            {
                todelete.Add(block);
            }

            foreach (VFXEdNodeBlockDraggable block in todelete)
            {
                RemoveNodeBlock(block);
            }

        }


        public void AcceptDrop(VFXEdNodeBlockDraggable block)
        {
            AddNodeBlock(block, dropInfo.DropIndex);
            CaptureDrop = false;
            Invalidate();
        }

        public void RevertDrop(VFXEdNodeBlockDraggable block, int index)
        {
            AddNodeBlock(block, index);
            CaptureDrop = false;
            Invalidate();
        }

        public override void Layout()
        {
            base.Layout();
            if (Children().Length > 0)
            {
                float curY = 0.0f;
                float curIdx = 0;
                foreach (VFXEdNodeBlockDraggable b in m_NodeBlocks)
                {
                    // Insert space for separator if capturing drop
                    if (CaptureDrop && dropInfo.DropIndex == curIdx)
                    {
                        curY += VFXEditorMetrics.NodeBlockContainerSeparatorOffset;
                    }
                    b.translation = new Vector3(0.0f, curY, 0.0f);
                    b.scale = new Vector2(scale.x, b.scale.y);
                    curY += b.scale.y;
                    curIdx++;
                }
                scale = new Vector2(scale.x, curY);
            }
            else
            {
                scale = new Vector2(scale.x, VFXEditorMetrics.NodeBlockContainerEmptyHeight);
            }
        }



        public override void Render(Rect parentRect, Canvas2D canvas)
        {
            Rect r = GetDrawableRect();

            if (Children().Length == 0 && CaptureDrop)
            {
               EditorGUI.DrawRect(r, new Color(1.0f, 1.0f, 1.0f, 0.1f));
            }

            if (Children().Length == 0)
            {
                GUI.Label(r, "(No NodeBlocks)", VFXEditor.styles.NodeInfoText);
            }

            base.Render(parentRect, canvas);

            if (CaptureDrop)
            {
                GUI.Box(dropInfo.highlightRect, "", VFXEditor.styles.NodeBlockDropSeparator);
            }
        }

        public bool RenderOverlayForbiddenDrop(CanvasElement element, Event e, Canvas2D canvas)
        {
            GUI.color = Color.white;
            Vector2 pos = canvas.CanvasToScreen(canvasBoundingRect.center);
            Rect r = new Rect(pos - new Vector2(64f, 64f), new Vector2(128f, 128f));
            GUI.DrawTexture(r, VFXEditor.styles.ForbidDrop.normal.background);
            return false;
        }

    }
}

