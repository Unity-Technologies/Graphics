
using System;
using System.Collections.Generic;

namespace UnityEngine.PathTracing.Core
{
    internal class TextureQuadTree
    {
        public class TextureNode
        {
            public TextureNode TopLeft;
            public TextureNode TopRight;
            public TextureNode BottomLeft;
            public TextureNode BottomRight;

            public TextureNode Parent;

            public int PosX;
            public int PosY;
            public int Size;
        }

        private readonly int _size;
        private readonly List<TextureNode> _leaves;

        public TextureQuadTree(int size)
        {
            _size = size;
            var root = new TextureNode() { PosX = 0, PosY = 0, Size = _size };
            _leaves = new List<TextureNode>() { root };
        }

        private void SubdivideNode(TextureNode node)
        {
            node.TopLeft = new TextureNode()
            {
                PosX = node.PosX,
                PosY = node.PosY,
                Size = node.Size / 2,
                Parent = node
            };
            node.BottomLeft = new TextureNode()
            {
                PosX = node.PosX,
                PosY = node.PosY + node.Size / 2,
                Size = node.Size / 2,
                Parent = node
            };
            node.TopRight = new TextureNode()
            {
                PosX = node.PosX + node.Size / 2,
                PosY = node.PosY,
                Size = node.Size / 2,
                Parent = node
            };
            node.BottomRight = new TextureNode()
            {
                PosX = node.PosX + node.Size / 2,
                PosY = node.PosY + node.Size / 2,
                Size = node.Size / 2,
                Parent = node
            };
        }

        public bool AddTexture(int size, out TextureNode node)
        {
            int targetSize = Mathf.Min(Mathf.NextPowerOfTwo(size), _size);

            // Search for a node with the right size
            int bestSize = int.MaxValue;
            TextureNode candidate = null;

            for (int i = _leaves.Count - 1; i >= 0; i--)
            {
                // If we have a match, return it
                int leafSize = _leaves[i].Size;
                if (targetSize == leafSize)
                {
                    node = _leaves[i];
                    _leaves.RemoveAt(i);
                    return true;
                }

                // Keep track of the best candidate we find
                if (targetSize < leafSize && leafSize < bestSize)
                {
                    candidate = _leaves[i];
                    bestSize = leafSize;
                }
            }

            // If we have a candidate, subdivide one child until we are at the right size
            if (candidate != null)
            {
                while (candidate.Size != targetSize)
                {
                    SubdivideNode(candidate);
                    _leaves.Remove(candidate);
                    _leaves.Add(candidate.BottomRight);
                    _leaves.Add(candidate.BottomLeft);
                    _leaves.Add(candidate.TopRight);
                    candidate = candidate.TopLeft;
                }
                node = candidate;
                return true;
            }

            node = null;
            return false;
        }

        public void RemoveTexture(TextureNode node)
        {
            bool ShouldCollapse(TextureNode node)
            {
                return
                    (node.TopLeft == null || _leaves.Contains(node.TopLeft)) &&
                    (node.TopRight == null || _leaves.Contains(node.TopRight)) &&
                    (node.BottomLeft == null || _leaves.Contains(node.BottomLeft)) &&
                    (node.BottomRight == null || _leaves.Contains(node.BottomRight));
            }

            // Add the node to the list of leaves
            _leaves.Add(node);

            // Recursively collapse the quad tree if the parent node is empty, to avoid fragmentation
            TextureNode parent = node.Parent;
            while (parent != null && ShouldCollapse(parent))
            {
                // Remove the children from the leaves list
                if (_leaves.Contains(parent.TopLeft)) _leaves.Remove(parent.TopLeft);
                if (_leaves.Contains(parent.TopRight)) _leaves.Remove(parent.TopRight);
                if (_leaves.Contains(parent.BottomLeft)) _leaves.Remove(parent.BottomLeft);
                if (_leaves.Contains(parent.BottomRight)) _leaves.Remove(parent.BottomRight);

                // Collapse the parent node
                parent.TopLeft = null;
                parent.TopRight = null;
                parent.BottomLeft = null;
                parent.BottomRight = null;

                // Add the parent to the leaves list, and check the next parent
                _leaves.Add(parent);
                parent = parent.Parent;
            }
        }

        public bool HasSpaceForTexture(int textureSize)
        {
            return _leaves.Exists(leaf => leaf.Size >= textureSize);
        }

        public bool IsFull => _leaves.Count == 0;
    }
}
