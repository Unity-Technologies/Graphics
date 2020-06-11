using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using System.Linq;

namespace UnityEngine.Experimental.Rendering.Universal
{
    internal class VertexReducer
    {
        enum OpType
        {
            CONVEX,
            CONCAVE
        }

        class CircularListNode<T>
        {
            public T m_Data;
            public CircularListNode<T> m_Next;
            public CircularListNode<T> m_Prev;

            public CircularListNode(T data)
            {
                m_Data = data;
            }

            public void Add(ref CircularListNode<T> head, ref int count)
            {
                if (head == null)
                {
                    head = this;
                    m_Next = this;
                    m_Prev = this;
                }
                else
                {
                    m_Next = head;
                    m_Prev = head.m_Prev;
                    head.m_Prev = this;
                    m_Prev.m_Next = this;
                }

                count++;
            }

            public void InsertAfter(CircularListNode<T> node, ref int count)
            {
                if (node == null)
                    return;

                CircularListNode<T> nextNode = node.m_Next;

                m_Next = nextNode;
                m_Prev = node;
                nextNode.m_Prev = this;
                node.m_Next = this;

                count++;
            }

            public void Remove(ref CircularListNode<T> head, ref int count)
            {
                if (head == null)
                    return;

                if (m_Next == this)
                {
                    head = null;
                }
                else
                {
                    m_Next.m_Prev = m_Prev;
                    m_Prev.m_Next = m_Next;
                    head = m_Next;
                }

                count--;
            }
        }

        class ReductionOperator
        {
            public float m_Area;
            public OpType m_OpType;
            public CircularListNode<Vertex> m_Operand;

            public ReductionOperator(float area, CircularListNode<Vertex> operand, OpType opType)
            {
                m_Area = area;
                m_OpType = opType;
                m_Operand = operand;

                m_Operand = operand;
                m_Operand.m_Data.m_Operator = this;
            }
        }

        class ConcaveOperator : ReductionOperator
        {
            public ConcaveOperator(float area, CircularListNode<Vertex> operand) : base(area, operand, OpType.CONCAVE) { }
        }


        class ConvexOperator : ReductionOperator
        {
            public Vector2 m_Intersection;

            public ConvexOperator(float area, CircularListNode<Vertex> operand, Vector2 intersection) : base(area, operand, OpType.CONVEX)
            {
                m_Intersection = intersection;
            }
        }

        class Vertex
        {
            public Vector2 m_Position;
            public ReductionOperator m_Operator;

            public Vertex(Vector2 position)
            {

                m_Position = position;
                m_Operator = null;
            }
        }

        class CircularList<T>
        {
            public CircularListNode<T> m_List;
            public int m_ListLength = 0;
        }

        void TestCircularList()
        {
            CircularListNode<int> list = null;
            int[] data = new int[] { 5, 4, 8, 9, 0, 3, 7, 2, 1, 6 };
            int count = 0;
            for (int i = 0; i < data.Length; i++)
            {
                CircularListNode<int> node = new CircularListNode<int>(data[i]);
                node.Add(ref list, ref count);
            }
        }

        CircularList<Vertex> m_Outline = new CircularList<Vertex>();
        SortedDictionary<float, List<ReductionOperator>> m_ReductionOperators = new SortedDictionary<float, List<ReductionOperator>>();
        List<CircularListNode<Vertex>> m_AddedVertices = new List<CircularListNode<Vertex>>();
        bool m_ConcaveReduction = true;
        ShapeLibrary m_ShapeLibrary;

        void AddReductionOperator(ReductionOperator reductionOperator)
        {
            List<ReductionOperator> operatorList = null;
            float area = reductionOperator.m_Area;

            if (!m_ReductionOperators.ContainsKey(area))
            {
                operatorList = new List<ReductionOperator>();
                m_ReductionOperators.Add(area, operatorList);
            }
            else
                operatorList = m_ReductionOperators[area];

            operatorList.Add(reductionOperator);
        }

        ReductionOperator GetReductionOperator()
        {
            if (m_ReductionOperators.Count == 0)
                return null;

            KeyValuePair<float, List<ReductionOperator>> keyValue = m_ReductionOperators.First();
            List<ReductionOperator> operatorList = keyValue.Value;
            ReductionOperator reductionOperator = operatorList[0];
            operatorList.RemoveAt(0);

            if (operatorList.Count == 0)
                m_ReductionOperators.Remove(keyValue.Key);

            return reductionOperator;
        }

        bool RemoveReductionOperator(ReductionOperator reductionOperator)
        {
            if (reductionOperator == null)
                return true;

            float area = reductionOperator.m_Area;
            if (m_ReductionOperators.ContainsKey(area))
            {
                List<ReductionOperator> operatorList = m_ReductionOperators[area];
                bool success = operatorList.Remove(reductionOperator);
                reductionOperator.m_Operand.m_Data.m_Operator = null;

                if (operatorList.Count == 0)
                    m_ReductionOperators.Remove(area);

                return success;
            }
            return false;
        }

        public void InitializeReductionOperators()
        {
            m_ReductionOperators.Clear();
            m_AddedVertices.Clear();

            CircularListNode<Vertex> curNode = m_Outline.m_List;
            for (int i = 0; i < m_Outline.m_ListLength; i++)
            {
                curNode.m_Data.m_Operator = null;
                curNode = curNode.m_Next;
                m_AddedVertices.Add(curNode);
            }
        }

        public void SetConcaveReduction()
        {
            m_ConcaveReduction = true;
            InitializeReductionOperators();
        }

        public void SetConvexReduction()
        {
            m_ConcaveReduction = false;
            InitializeReductionOperators();
        }

        public void Initialize(ShapeLibrary shapeLibrary, Vector2[] inVertices, bool isReversed, out float area)
        {
            m_ShapeLibrary = shapeLibrary;

            area = 0;
            if (!isReversed)
            {
                Vector2 prevPoint = inVertices[inVertices.Length - 1];
                for (int i = 0; i < inVertices.Length; i++)
                {
                    Vector2 curPoint = inVertices[i];
                    Vertex newVertex = new Vertex(curPoint);
                    CircularListNode<Vertex> node = new CircularListNode<Vertex>(newVertex);
                    node.Add(ref m_Outline.m_List, ref m_Outline.m_ListLength);

                    // Shoelace area calculation
                    area += prevPoint.x * curPoint.y - curPoint.x * prevPoint.y;
                    prevPoint = curPoint;
                }
            }
            else
            {
                Vector2 prevPoint = inVertices[0];
                for (int i = inVertices.Length - 1; i >= 0; i--)
                {
                    Vector2 curPoint = inVertices[i];
                    Vertex newVertex = new Vertex(curPoint);
                    CircularListNode<Vertex> node = new CircularListNode<Vertex>(newVertex);
                    node.Add(ref m_Outline.m_List, ref m_Outline.m_ListLength);

                    // Shoelace area calculation
                    area += prevPoint.x * curPoint.y - curPoint.x * prevPoint.y;
                    prevPoint = curPoint;
                }
            }

            area = 0.5f * Mathf.Abs(area);
        }


        public void GetReducedVertices(out List<Vector2> outVertices)
        {
            int outlineLength = m_Outline.m_ListLength;
            CircularListNode<Vertex> curNode = m_Outline.m_List;
            outVertices = new List<Vector2>();
            for (int i = 0; i < outlineLength; i++)
            {
                outVertices.Add(curNode.m_Data.m_Position);
                curNode = curNode.m_Next;
            }
        }

        public void GetReducedVertices(out Vector2[] outVertices)
        {
            int outlineLength = m_Outline.m_ListLength;
            CircularListNode<Vertex> curNode = m_Outline.m_List;
            outVertices = new Vector2[outlineLength];
            for (int i = 0; i < outlineLength; i++)
            {
                outVertices[i] = curNode.m_Data.m_Position;
                curNode = curNode.m_Next;
            }
        }

        public float GetSmallestArea()
        {
            ProcessAddedVertices();

            if (m_ReductionOperators.Count > 0)
                return m_ReductionOperators.First().Key;
            else
                return float.MaxValue;
        }

        public float GetLargestArea()
        {
            ProcessAddedVertices();

            if (m_ReductionOperators.Count > 0)
                return m_ReductionOperators.Last().Key;
            else
                return 0;
        }

        void ProcessAddedVertices()
        {
            // Calculate removal costs for added vertices
            while (m_AddedVertices.Count > 0)
            {
                CircularListNode<Vertex> node = m_AddedVertices[0];
                m_AddedVertices.RemoveAt(0);


                Vertex currentVertex = node.m_Data;
                Vertex nextVertex = node.m_Next.m_Data;
                Vertex next2Vertex = node.m_Next.m_Next.m_Data;
                Vertex prevVertex = node.m_Prev.m_Data;
                float concaveArea;
                float convexArea;
                float2 intersection;


                if (!m_ConcaveReduction)
                {
                    if (!OutlineUtility.GetConcaveArea(currentVertex.m_Position, prevVertex.m_Position, nextVertex.m_Position, out concaveArea) &&
                        !OutlineUtility.GetConcaveArea(nextVertex.m_Position, currentVertex.m_Position, next2Vertex.m_Position, out concaveArea))
                    {
                        if (OutlineUtility.GetConvexArea(prevVertex.m_Position, currentVertex.m_Position, nextVertex.m_Position, next2Vertex.m_Position, out intersection, out convexArea))
                        {
                            ReductionOperator newOperator = new ConvexOperator(convexArea, node, intersection);
                            AddReductionOperator(newOperator);
                        }
                    }
                }
                else
                {

                    if (OutlineUtility.GetConcaveArea(currentVertex.m_Position, prevVertex.m_Position, nextVertex.m_Position, out concaveArea))
                    {
                        ReductionOperator newOperator = new ConcaveOperator(concaveArea, node);
                        AddReductionOperator(newOperator);
                    }
                }
            }
        }

        bool Equal(Vector2 a, Vector2 b)
        {
            return a.x == b.x && a.y == b.y;
        }


        bool CheckForIntersection(uint[] lineIds, Vector2 lineStart, Vector2 lineEnd, bool debugLines)
        {
            return m_ShapeLibrary.m_LineIntersectionManager.HasIntersection(lineIds, lineStart, lineEnd, debugLines);
        }


        void AddLine(Vertex vertex)
        {

        }

        void RemoveLine(Vertex vertex)
        {


        }

        uint[] CreateConcaveLineIds(CircularListNode<Vertex> prev2Vertex, CircularListNode<Vertex> prevVertex, CircularListNode<Vertex> curVertex, CircularListNode<Vertex> nextVertex, CircularListNode<Vertex> next2Vertex)
        {
            uint[] retArray = new uint[4];

            retArray[0] = LineIntersectionManager.LineHash(prev2Vertex.m_Data.m_Position, prevVertex.m_Data.m_Position);
            retArray[1] = LineIntersectionManager.LineHash(prevVertex.m_Data.m_Position, curVertex.m_Data.m_Position);
            retArray[2] = LineIntersectionManager.LineHash(curVertex.m_Data.m_Position, nextVertex.m_Data.m_Position);
            retArray[3] = LineIntersectionManager.LineHash(nextVertex.m_Data.m_Position, next2Vertex.m_Data.m_Position);

            return retArray;
        }

        uint[] CreateConvexLineIds(CircularListNode<Vertex> prev2Vertex, CircularListNode<Vertex> prevVertex, CircularListNode<Vertex> curVertex, CircularListNode<Vertex> nextVertex, CircularListNode<Vertex> next2Vertex, CircularListNode<Vertex> next3Vertex)
        {
            uint[] retArray = new uint[5];

            retArray[0] = LineIntersectionManager.LineHash(prev2Vertex.m_Data.m_Position, prevVertex.m_Data.m_Position);
            retArray[1] = LineIntersectionManager.LineHash(prevVertex.m_Data.m_Position, curVertex.m_Data.m_Position);
            retArray[2] = LineIntersectionManager.LineHash(curVertex.m_Data.m_Position, nextVertex.m_Data.m_Position);
            retArray[3] = LineIntersectionManager.LineHash(nextVertex.m_Data.m_Position, next2Vertex.m_Data.m_Position);
            retArray[4] = LineIntersectionManager.LineHash(next2Vertex.m_Data.m_Position, next3Vertex.m_Data.m_Position);

            return retArray;
        }



        public bool ReduceShapeStep()
        {
            ProcessAddedVertices();

            int minSides = 4;

            // We don't want to reduce our vertices below 3
            if (m_Outline.m_ListLength > minSides && m_ReductionOperators.Count > 0)
            {
                // process the first item in m_ReductionOperatorsHead
                ReductionOperator bestOperator = GetReductionOperator();

                CircularListNode<Vertex> curVertex = bestOperator.m_Operand;
                CircularListNode<Vertex> nextVertex = bestOperator.m_Operand.m_Next;
                CircularListNode<Vertex> prevVertex = bestOperator.m_Operand.m_Prev;

                CircularListNode<Vertex> next2Vertex = nextVertex.m_Next;
                CircularListNode<Vertex> next3Vertex = next2Vertex.m_Next;
                CircularListNode<Vertex> prev2Vertex = prevVertex.m_Prev;


                bool operatorSuccessful = false;
                if (bestOperator.m_OpType == OpType.CONCAVE)
                {
                    uint[] ignoreIntersectionIds = CreateConcaveLineIds(prev2Vertex, prevVertex, curVertex, nextVertex, next2Vertex);

                    operatorSuccessful = !CheckForIntersection(ignoreIntersectionIds, prevVertex.m_Data.m_Position, nextVertex.m_Data.m_Position, true);
                    if (operatorSuccessful)
                    {
                        m_ShapeLibrary.m_LineIntersectionManager.RemoveLine(curVertex.m_Data.m_Position, nextVertex.m_Data.m_Position);
                        m_ShapeLibrary.m_LineIntersectionManager.RemoveLine(prevVertex.m_Data.m_Position, curVertex.m_Data.m_Position);
                        m_ShapeLibrary.m_LineIntersectionManager.AddLine(prevVertex.m_Data.m_Position, nextVertex.m_Data.m_Position);

                        RemoveReductionOperator(nextVertex.m_Data.m_Operator);
                        m_AddedVertices.Add(nextVertex); // Reevaulate the area of nextVertex
                        curVertex.Remove(ref m_Outline.m_List, ref m_Outline.m_ListLength);
                    }
                }
                else if (bestOperator.m_OpType == OpType.CONVEX)
                {
                    uint[] ignoreIntersectionIds = CreateConvexLineIds(prev2Vertex, prevVertex, curVertex, nextVertex, next2Vertex, next3Vertex);

                    ConvexOperator convexOperator = (ConvexOperator)bestOperator;

                    operatorSuccessful = !CheckForIntersection(ignoreIntersectionIds, prevVertex.m_Data.m_Position, convexOperator.m_Intersection, true);
                    operatorSuccessful &= !CheckForIntersection(ignoreIntersectionIds, convexOperator.m_Intersection, next2Vertex.m_Data.m_Position, true);

                    if (operatorSuccessful)
                    {
                        m_ShapeLibrary.m_LineIntersectionManager.RemoveLine(nextVertex.m_Data.m_Position, next2Vertex.m_Data.m_Position);
                        m_ShapeLibrary.m_LineIntersectionManager.RemoveLine(curVertex.m_Data.m_Position, nextVertex.m_Data.m_Position);
                        m_ShapeLibrary.m_LineIntersectionManager.RemoveLine(prevVertex.m_Data.m_Position, curVertex.m_Data.m_Position);

                        m_ShapeLibrary.m_LineIntersectionManager.AddLine(prevVertex.m_Data.m_Position, convexOperator.m_Intersection);
                        m_ShapeLibrary.m_LineIntersectionManager.AddLine(next2Vertex.m_Data.m_Position, convexOperator.m_Intersection);

                        CircularListNode<Vertex> newNode = new CircularListNode<Vertex>(new Vertex(convexOperator.m_Intersection));
                        newNode.InsertAfter(curVertex, ref m_Outline.m_ListLength);
                        RemoveReductionOperator(nextVertex.m_Data.m_Operator);

                        m_AddedVertices.Add(newNode);
                        curVertex.Remove(ref m_Outline.m_List, ref m_Outline.m_ListLength);
                        nextVertex.Remove(ref m_Outline.m_List, ref m_Outline.m_ListLength);

                    }
                }

                if (operatorSuccessful)
                {
                    // Remove reduction operators for vertices that will be affected. Have them be recalculated
                    RemoveReductionOperator(prevVertex.m_Data.m_Operator);
                    RemoveReductionOperator(prev2Vertex.m_Data.m_Operator);
                    RemoveReductionOperator(next2Vertex.m_Data.m_Operator);
                    m_AddedVertices.Add(prevVertex);
                    m_AddedVertices.Add(prev2Vertex);
                    m_AddedVertices.Add(next2Vertex);
                }

                return true;
            }

            return false;
        }
    }
}
