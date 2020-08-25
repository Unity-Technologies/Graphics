using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.U2D;
using Unity;
using Unity.Collections;
using UnityEditor.Experimental.AssetImporters;



namespace UnityEngine.Experimental.Rendering.Universal
{
    public class SpritePreprocessor : AssetPostprocessor
    {
        NativeArray<T> ListToNativeArray<T>(List<T> list) where T : struct
        {
            NativeArray<T> nativeArray = new NativeArray<T>(list.Count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < list.Count; i++)
                nativeArray[i] = list[i];
            return nativeArray;
        }

        NativeArray<T> ListsToNativeArray<T>(List<T> list0, List<T> list1) where T : struct
        {
            NativeArray<T> nativeArray = new NativeArray<T>(list0.Count + list1.Count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < list0.Count; i++)
                nativeArray[i] = list0[i];

            for (int i = 0; i < list1.Count; i++)
                nativeArray[i + list0.Count] = list1[i];

            return nativeArray;
        }


        List<Vector2[]> TransformOutline(List<Vector2[]> customOutline, Vector2 offset, float scale)
        {
            List<Vector2[]> retOutline = new List<Vector2[]>();

            foreach (Vector2[] outline in customOutline)
            {
                Vector2[] transformedOutline = new Vector2[outline.Length];

                for (int i = 0; i < outline.Length; i++)
                    transformedOutline[i] = scale * outline[i] + offset;

                retOutline.Add(transformedOutline);
            }

            return retOutline;
       }

        void CreateSplitSpriteMesh(Sprite sprite, Vector2 pivot, List<Vector2[]> customOutline, float pixelsPerUnit)
        {
            ShapeLibrary shapeLibrary = new ShapeLibrary();
            Texture2D texture = sprite.texture;

            RectInt rect = new RectInt(new Vector2Int((int)sprite.rect.position.x, (int)sprite.rect.position.y), new Vector2Int((int)sprite.rect.size.x, (int)sprite.rect.size.y));
            shapeLibrary.SetRegion(rect);

            List<Vector2[]> transformedOutline = null;
            if (customOutline != null && customOutline.Count > 0)
                 transformedOutline = TransformOutline(customOutline, sprite.rect.center, pixelsPerUnit);
            GenerateMeshes.MakeShapes(shapeLibrary, transformedOutline, texture, 1, 4096); // 2048 will give us a pretty good balance of verts to area. 4096 is the same or better area as previously and less than half the vertices.

            Debug.Log("Shapes generated: " + shapeLibrary.m_Shapes.Count);

            List<Vector3> allVertices = new List<Vector3>();
            List<ushort> colorTriangles = new List<ushort>();
            List<ushort> depthTriangles = new List<ushort>();

            // Tesselate shapes made with MakeShapes
            GenerateMeshes.TesselateShapes(shapeLibrary, (vertices, triangles, uvs, isOpaque) =>
            {
                int startingIndex = allVertices.Count;
                for (int i = 0; i < triangles.Length; i++)
                {
                    ushort index = (ushort)(triangles[i] + startingIndex);

                    if (!isOpaque)
                        colorTriangles.Add(index);
                    else
                        depthTriangles.Add(index);
                }

                Vector3 v3Pivot = new Vector3(pivot.x, pivot.y);
                for (int i = 0; i < vertices.Length; i++)
                    allVertices.Add(vertices[i] / pixelsPerUnit - v3Pivot);
            });

            if (customOutline != null && customOutline.Count > 0)
            {
                GenerateMeshes.TesselateShapes(transformedOutline, rect, (vertices, triangles, uvs, isOpaque) =>
                {
                    int startingIndex = allVertices.Count;
                    for (int i = 0; i < triangles.Length; i++)
                    {
                        ushort index = (ushort)(triangles[i] + startingIndex);
                        depthTriangles.Add(index);
                    }

                    Vector3 v3Pivot = new Vector3(pivot.x, pivot.y);
                    for (int i = 0; i < vertices.Length; i++)
                        allVertices.Add(vertices[i] / pixelsPerUnit - v3Pivot);
                });
            }

            NativeArray<Vector3> nativeVertices = ListToNativeArray<Vector3>(allVertices);
            NativeArray<ushort> nativeIndices = ListsToNativeArray<ushort>(colorTriangles, depthTriangles);

            sprite.SetVertexCount(allVertices.Count);
            sprite.SetVertexAttribute(UnityEngine.Rendering.VertexAttribute.Position, nativeVertices);
            sprite.SetIndices(nativeIndices);
            sprite.ModifySubmesh(0, allVertices.Count, 0, colorTriangles.Count, 0);

            if (depthTriangles.Count > 0)
            {
                int meshCount = sprite.GetSubmeshCount();
                if (meshCount == 1)
                    sprite.AddSubmesh();
                sprite.ModifySubmesh(0, allVertices.Count, colorTriangles.Count, depthTriangles.Count, 1);
            }

            nativeVertices.Dispose();
            nativeIndices.Dispose();
        }

        void OnPostprocessSprites(Texture2D texture, Sprite[] sprites)
        {
            if (sprites.Length == 0 || !(assetImporter is TextureImporter))
                return;

            TextureImporter textureImporter = (TextureImporter)assetImporter;

            float correctedPPU = sprites[0].rect.width / sprites[0].bounds.size.x;

            if (textureImporter.spriteMeshType == SpriteMeshType.Tight && textureImporter.spriteGenerateDepthMesh)
            {
                if (textureImporter.spriteImportMode == SpriteImportMode.Single)
                {
                    float width = texture.width / correctedPPU;
                    float height = texture.height / correctedPPU;
                    Vector2 spriteSize = new Vector2(width, height);
                    Vector2 pivotOffset = textureImporter.spritePivot * spriteSize;  // This is probably wrong. Should use rect instead.
                    CreateSplitSpriteMesh(sprites[0], pivotOffset, textureImporter.spriteOutline, correctedPPU);
                }
                else if (textureImporter.spriteImportMode == SpriteImportMode.Multiple)
                {
                    for (int i = 0; i < textureImporter.spritesheet.Length; i++)
                    {
                        float width = sprites[i].rect.width / correctedPPU;
                        float height = sprites[i].rect.height / correctedPPU;
                        Vector2 spriteSize = new Vector2(width, height);
                        Vector2 pivotOffset = new Vector2(sprites[i].pivot.x / sprites[i].rect.width, sprites[i].pivot.y / sprites[i].rect.height) * spriteSize;
                        CreateSplitSpriteMesh(sprites[i], pivotOffset, textureImporter.spritesheet[i].outline, correctedPPU);
                    }
                }
            }
        }
    }
}
