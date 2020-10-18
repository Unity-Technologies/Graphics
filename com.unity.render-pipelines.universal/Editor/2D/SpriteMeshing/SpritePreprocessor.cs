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

        Vector2[][] TransformOutline(Vector2[][] customOutline, Vector2 offset, float scale)
        {
            if (customOutline != null && customOutline.Length > 0)
            {
                Vector2[][] retOutline = new Vector2[customOutline.Length][];

                for(int outlineIndex=0; outlineIndex < customOutline.Length; outlineIndex++)
                {
                    Vector2[] outline = customOutline[outlineIndex];
                    Vector2[] transformedOutline = new Vector2[outline.Length];

                    for (int i = 0; i < outline.Length; i++)
                        transformedOutline[i] = scale * (outline[i] + offset);

                    retOutline[outlineIndex] = transformedOutline;
                }
                return retOutline;
            }

            return null;
       }

        void AddGeometry(Vector3[] vertices, int[] triangles, Vector2[] uvs, Vector2 pivot, float correctedPPU, bool isOpaque, List<ushort> colorTriangles, List<ushort> depthTriangles, List<Vector3> allVertices)
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
                allVertices.Add((vertices[i] - v3Pivot) * correctedPPU);
            
        }

        void CreateSplitSpriteMesh(Sprite sprite, RectInt rect, Vector2 pivot, Vector2[][] customOutline, float pixelsPerUnit)
        {
            ShapeLibrary shapeLibrary = new ShapeLibrary();
            Texture2D texture = sprite.texture;

            shapeLibrary.SetRegion(rect);

            GenerateMeshes.MakeShapes(shapeLibrary, customOutline, texture, 1, 4096); // 2048 will give us a pretty good balance of verts to area. 4096 is the same or better area as previously and less than half the vertices.

            Debug.Log("Shapes generated: " + shapeLibrary.m_Shapes.Count);

            List<Vector3> allVertices = new List<Vector3>();
            List<ushort> colorTriangles = new List<ushort>();
            List<ushort> depthTriangles = new List<ushort>();

            // Tesselate shapes made with MakeShapes
            GenerateMeshes.TesselateShapes(shapeLibrary, (vertices, triangles, uvs, isOpaque) => AddGeometry(vertices, triangles, uvs, pivot, pixelsPerUnit, isOpaque, colorTriangles, depthTriangles, allVertices));

            if (customOutline != null && customOutline.Length > 0)
                GenerateMeshes.TesselateShapes(customOutline, rect, (vertices, triangles, uvs, isOpaque) => AddGeometry(vertices, triangles, uvs, pivot, pixelsPerUnit, false, colorTriangles, depthTriangles, allVertices));

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

            // sprite bounds considers both texture scale and pixels per unit. If we want to get only the texture scaling we need to multiply by spritePixelsPerUnit
            float scaleFromOriginalToCurrent = sprites[0].rect.width / (sprites[0].bounds.size.x * textureImporter.spritePixelsPerUnit);
            float scaleFromCurrentToOriginal = (sprites[0].bounds.size.x * textureImporter.spritePixelsPerUnit) / sprites[0].rect.width;
            float correctedPPU = scaleFromCurrentToOriginal / textureImporter.spritePixelsPerUnit;

            if (textureImporter.spriteMeshType == SpriteMeshType.Tight && textureImporter.spriteGenerateDepthMesh)
            {
                if (textureImporter.spriteImportMode == SpriteImportMode.Single)
                {
                    // I'm not sure if all of this is correct for subsets of the original image
                    Vector2 originalSpriteSize = new Vector2(sprites[0].bounds.size.x, sprites[0].bounds.size.y) * textureImporter.spritePixelsPerUnit;
                    Vector2 spriteSize = new Vector2(sprites[0].rect.width, sprites[0].rect.height);

                    Vector2 spriteCenter = 0.5f * originalSpriteSize;
                    Vector2[][] transformedOutline = TransformOutline(textureImporter.spriteOutline, spriteCenter, scaleFromOriginalToCurrent);  // rework spriteCenter

                    RectInt rect = new RectInt(new Vector2Int((int)sprites[0].rect.position.x, (int)sprites[0].rect.position.y), new Vector2Int((int)sprites[0].rect.size.x, (int)sprites[0].rect.size.y));
                    CreateSplitSpriteMesh(sprites[0], rect, sprites[0].pivot, transformedOutline, correctedPPU);
                }
                else if (textureImporter.spriteImportMode == SpriteImportMode.Multiple)
                {
                    for (int i = 0; i < textureImporter.spritesheet.Length; i++)
                    {
                        Vector2 originalSpriteSize = new Vector2(sprites[i].bounds.size.x, sprites[i].bounds.size.y) * textureImporter.spritePixelsPerUnit;
                        Vector2 spriteSize = new Vector2(sprites[i].rect.width, sprites[i].rect.height);

                        Vector2 spriteCenter = 0.5f * originalSpriteSize;
                        Vector2[][] transformedOutline = TransformOutline(textureImporter.spritesheet[i].outline, spriteCenter, scaleFromOriginalToCurrent);

                        RectInt rect = new RectInt(new Vector2Int((int)sprites[i].rect.position.x, (int)sprites[i].rect.position.y), new Vector2Int((int)sprites[i].rect.size.x, (int)sprites[i].rect.size.y));
                        CreateSplitSpriteMesh(sprites[i], rect, sprites[i].pivot, transformedOutline, correctedPPU);
                    }
                }
            }
        }
    }
}
