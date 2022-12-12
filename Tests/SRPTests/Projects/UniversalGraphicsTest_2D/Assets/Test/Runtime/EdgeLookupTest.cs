using NUnit.Framework;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[TestFixture]
public class EdgeLookupTest
{
    [Test]
    public void ShadowEdgeLookupTable_Add()
    {
        // Simulates the following geometry
        // Index   Edge
        // 0,      0,1  
        // 1,      1,2
        // 2,      2,0
        // 3,      2,3
        // 4,      3,4
        // 5,      4,2
        // 6,      2,5
        // 7,      5,6
        // 8,      6,2
        // 9,      2,7
        // 10,     7,8
        // 11,     8,2

        ShadowEdgeLookupTable shadowEdgeLookupTable = new ShadowEdgeLookupTable();
        shadowEdgeLookupTable.Initialize(12);
        shadowEdgeLookupTable.Add(0, 0);
        shadowEdgeLookupTable.Add(1, 1);
        shadowEdgeLookupTable.Add(2, 2);
        shadowEdgeLookupTable.Add(2, 3);
        shadowEdgeLookupTable.Add(3, 4);
        shadowEdgeLookupTable.Add(4, 5);
        shadowEdgeLookupTable.Add(2, 6);
        shadowEdgeLookupTable.Add(5, 7);
        shadowEdgeLookupTable.Add(6, 8);
        shadowEdgeLookupTable.Add(2, 9);
        shadowEdgeLookupTable.Add(7, 10);
        shadowEdgeLookupTable.Add(8, 11);

        string depths = "";
        for (int i = 0; i < shadowEdgeLookupTable.size; i++)
            depths = depths + shadowEdgeLookupTable.DepthAt(i) + " ";

        string expectedDepths = "1 1 4 1 1 1 1 1 1 0 0 0 ";
        Assert.IsTrue(string.Equals(expectedDepths, depths));


        string values = "";
        for (int i = 0; i < shadowEdgeLookupTable.size; i++)
        {
            int depth = shadowEdgeLookupTable.DepthAt(i);
            for (int j = 0; j < depth; j++)
            {
                values = values + shadowEdgeLookupTable.GetValueAt(i, j) + " ";
            }
        }
        string expectedValues = "0 1 9 6 3 2 4 5 7 8 10 11 ";
        Assert.IsTrue(string.Equals(expectedValues, values));

        shadowEdgeLookupTable.Dispose();
    }


    [Test]
    public void ShadowUtility_SortEdges()
    {
        NativeArray<ShadowEdge> unsortedEdges = new NativeArray<ShadowEdge>(12, Allocator.Persistent);
        NativeArray<ShadowEdge> sortedEdges;
        NativeArray<int> shapeStartingEdges;

        unsortedEdges[0] = new ShadowEdge(0, 1);
        unsortedEdges[1] = new ShadowEdge(1, 2);
        unsortedEdges[2] = new ShadowEdge(2, 0);
        unsortedEdges[3] = new ShadowEdge(2, 3);
        unsortedEdges[4] = new ShadowEdge(3, 4);
        unsortedEdges[5] = new ShadowEdge(4, 2);
        unsortedEdges[6] = new ShadowEdge(2, 5);
        unsortedEdges[7] = new ShadowEdge(5, 6);
        unsortedEdges[8] = new ShadowEdge(6, 2);
        unsortedEdges[9] = new ShadowEdge(2, 7);
        unsortedEdges[10] = new ShadowEdge(7, 8);
        unsortedEdges[11] = new ShadowEdge(8, 2);

        ShadowUtility.SortEdges(unsortedEdges, out sortedEdges, out shapeStartingEdges);

        string edges = "";
        for (int i = 0; i < sortedEdges.Length; i++)
            edges = edges + sortedEdges[i].v0 + "," + sortedEdges[i].v1 + " ";

        string expectedValues = "0,1 1,2 2,0 2,3 3,4 4,2 2,5 5,6 6,2 2,7 7,8 8,2 ";
        Assert.IsTrue(string.Equals(expectedValues, edges));

        unsortedEdges.Dispose();
        sortedEdges.Dispose();
        shapeStartingEdges.Dispose();
    }
}
