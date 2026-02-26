using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using UnityEngine.Rendering.Universal;
using System;

[Serializable]
internal class SimpleShadowProvider : ShadowShape2DProvider
{
    public override GUIContent ProviderName(string componentName)
    {
        return new GUIContent("Simple Shadow Provider");
    }

    public override bool IsRequiredComponentData(Component sourceComponent)
    {
        return sourceComponent is Transform;
    }

    public override void OnInitialized(Component sourceComponent, ShadowShape2D persistantShadowShape)
    {
        NativeArray<Vector3> vertices = new NativeArray<Vector3>(4, Allocator.Persistent);
        NativeArray<int> indices = new NativeArray<int>(8, Allocator.Persistent);

        // Square
        vertices[0] = new Vector3(5, 10);
        vertices[1] = new Vector3(10, 10);
        vertices[2] = new Vector3(10, 5);
        vertices[3] = new Vector3(5, 5);

        // Square
        indices[0] = 0;
        indices[1] = 1;
        indices[2] = 1;
        indices[3] = 2;
        indices[4] = 2;
        indices[5] = 3;
        indices[6] = 3;
        indices[7] = 0;

        persistantShadowShape.SetShape(vertices, indices, ShadowShape2D.OutlineTopology.Lines, createInteriorGeometry: true);

    }

    public override void OnBeforeRender(Component sourceComponent, Bounds worldCullingBounds, ShadowShape2D persistantShadowShape) { }
}
