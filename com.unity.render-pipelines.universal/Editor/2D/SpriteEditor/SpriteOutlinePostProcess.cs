using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEditor.U2D;
using UnityEditor.U2D.Sprites;
using UnityEditor.U2D.Common;
using UnityEngine.Rendering.Universal.LibTessDotNet;
using UnityEngine.U2D;
using UnityEngine.Rendering;


namespace UnityEditor.Rendering.Universal
{
    public class SpriteOutlinePostProcess : AssetPostprocessor
    {
        static ISpriteEditorDataProvider GetSpriteEditorDataProvider(string assetPath)
        {
            var dataProviderFactories = new SpriteDataProviderFactories();
            dataProviderFactories.Init();
            return dataProviderFactories.GetSpriteEditorDataProviderFromObject(AssetImporter.GetAtPath(assetPath));
        }

        private const float ClipperScale = 100000.0f;

        Texture2D GenerateTranslucentTexture(Texture2D texture)
        {
            var translucentTex = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
            // if (translucentTex != null)
            //     translucentTex.filterMode = texture.filterMode;

            // Set the override alpha
            {
                var pixels = texture.GetPixels32();

                for(var i = 0; i < pixels.Length; i++)
                {
                    if (pixels[i].a != 255)
                        continue;
                    ref var c = ref pixels[i];
                    c.a = 0;
                }
                translucentTex.SetPixels32(pixels);
                translucentTex.Apply();
            }
            return translucentTex;
        }

        Vector2[][] GenerateOutline(Clipper clipper, Texture2D texture, SpriteRect spriteRect, float detail, PolyType polyType)
        {
            InternalEditorBridge.GenerateOutline(texture, spriteRect.rect, detail, 0, true, out var solidPaths);
            foreach (var path in solidPaths)
            {
                var intPath = new List<IntPoint>(path.Length);
                for(var i = 0; i < path.Length; i++)
                    intPath.Add(new IntPoint(path[i].x * ClipperScale, path[i].y * ClipperScale));
                clipper.AddPath(intPath, polyType, true);
            }

            return solidPaths;
        }

        void Tessellate<T>(Tess tess, IReadOnlyList<IReadOnlyList<T>> paths, Func<T, Vec3> ToVec3)
        {
            foreach (var path in paths)
            {
                var contour = new ContourVertex[path.Count()];
                for (var i = 0; i < path.Count(); i++)
                {
                    contour[i] = new ContourVertex
                    {
                        Position = ToVec3(path[i])
                    };
                }
                tess.AddContour(contour);
            }
        }

        void FillSprite(Sprite sprite, params Tess[] tesses)
        {
            var totalVertices = tesses.Sum(t => t.VertexCount);
            var vertices = new NativeArray<Vector3>(totalVertices, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var vertexOffset = 0;
            foreach (var tess in tesses)
            {
                for (var i = 0; i < tess.VertexCount; i++)
                {
                    var pos = tess.Vertices[i].Position;
                    vertices[i + vertexOffset] = new Vector3(pos.X / sprite.pixelsPerUnit, pos.Y / sprite.pixelsPerUnit, 0);
                }

                vertexOffset += tess.VertexCount;
            }

            var totalIndices = tesses.Sum(t => t.Elements.Length);
            var indices = new NativeArray<ushort>(totalIndices, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            vertexOffset = 0;
            var indexOffset = 0;
            foreach (var tess in tesses)
            {
                for (var i = 0; i < tess.Elements.Length; i++)
                {
                    indices[i + indexOffset] = (ushort) (tess.Elements[i] + vertexOffset);
                }

                indexOffset += tess.Elements.Length;
                vertexOffset += tess.VertexCount;
            }

            sprite.SetVertexCount(vertices.Length);
            sprite.SetVertexAttribute(VertexAttribute.Position, vertices);
            sprite.SetIndices(indices);
            // set indices

            // setup the submeshes
            sprite.SetSubMeshCount(tesses.Length + 1);
            vertexOffset = 0;
            indexOffset = 0;
            var subMeshIndex = 0;

            // submesh 0 is always the entire thing
            sprite.SetSubMesh(subMeshIndex++, 0, totalVertices, 0, totalIndices);

            foreach (var tess in tesses)
            {
                sprite.SetSubMesh(subMeshIndex++, vertexOffset, tess.VertexCount, indexOffset, tess.Elements.Length);
                vertexOffset += tess.VertexCount;
                indexOffset += tess.Elements.Length;
            }
        }

        /// <summary>
        /// Submesh 0: Opaque, but degenerate if not split
        /// Submesh 1: Translucent
        ///
        /// Sprite Renderer is always configured with 2 materials
        /// 2 Materials
        /// - Opaque
        /// - Translucent
        ///
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="sprites"></param>
        private void OnPostprocessSprites(Texture2D texture, Sprite[] sprites)
        {
            var ai = GetSpriteEditorDataProvider(assetPath);
            ai.InitSpriteEditorDataProvider();
            // var textureDataProvider = ai.GetDataProvider<ITextureDataProvider>();
            var outlineDataProvider = ai.GetDataProvider<ISpriteOutlineDataProvider>();

            var spriteRects = ai.GetSpriteRects();
            foreach (var sprite in sprites)
            {
                var guid = sprite.GetSpriteID();
                var detail = outlineDataProvider.GetTessellationDetail(guid);
                var spriteRect = spriteRects.First(s => s.spriteID == guid);
                var clipper = new Clipper();

                if (sprite.name.EndsWith("_split"))
                {
                    GenerateOutline(clipper, texture, spriteRect, detail, PolyType.ptSubject);

                    var translucentTexture = GenerateTranslucentTexture(texture);
                    var translucentPaths = GenerateOutline(clipper, translucentTexture, spriteRect, detail, PolyType.ptClip);

                    var solution = new List<List<IntPoint>>();
                    clipper.Execute(ClipType.ctDifference, solution);

                    var opaqueTess = new Tess();
                    Tessellate(opaqueTess, solution, (pos) => new Vec3 {X = pos.X / ClipperScale, Y = pos.Y / ClipperScale, Z = 0});
                    opaqueTess.Tessellate(WindingRule.NonZero, ElementType.Polygons, 3);

                    var translucentTess = new Tess();
                    Tessellate(translucentTess, translucentPaths, (pos) => new Vec3 {X = pos.x, Y = pos.y, Z = 0});
                    translucentTess.Tessellate(WindingRule.NonZero, ElementType.Polygons, 3);

                    FillSprite(sprite, opaqueTess, translucentTess);
                }
                else
                {
                    var indices = sprite.GetIndices();
                    var newIndices = new NativeArray<ushort>(indices.Length + 3, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

                    // degenerate triangle for submesh 0
                    var i = 0;
                    newIndices[i++] = 0;
                    newIndices[i++] = 0;
                    newIndices[i++] = 0;

                    // the rest of the proper mesh in submesh 1
                    for(;i < indices.Length; i++)
                    {
                        newIndices[i] = indices[i-3];
                    }

                    sprite.SetSubMeshCount(2);
                    // submesh 0, opaque and degenerate
                    sprite.SetSubMesh(0, 0, 3, 0, 3);
                    sprite.SetSubMesh(1, 0, sprite.GetVertexCount(), 3, indices.Length);
                }

                // FillSprite(sprite, translucentTess, opaqueTess);
            }
        }

        void OnPostprocessTexture(Texture2D texture)
        {
            // Debug.Log("Texture2D: (" + texture.width + "x" + texture.height + ")");
        }
    }
}
