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

                InternalEditorBridge.GenerateOutline(texture, spriteRect.rect, detail, 0, true, out var solidPaths);
                foreach (var path in solidPaths)
                {
                    var intPath = new List<IntPoint>(path.Length);
                    for(var i = 0; i < path.Length; i++)
                        intPath.Add(new IntPoint(path[i].x * ClipperScale, path[i].y * ClipperScale));
                    clipper.AddPath(intPath, PolyType.ptSubject, true);
                }

                // generate the translucent path
                var translucentTex = GenerateTranslucentTexture(texture);
                InternalEditorBridge.GenerateOutline(translucentTex, spriteRect.rect, detail, 0, true, out var translucentPaths);

                if (translucentPaths.Length == 0)
                {
                    Debug.Log("No paths");
                    return;
                }

                foreach (var path in translucentPaths)
                {
                    var intPath = new List<IntPoint>(path.Length);
                    for(var i = 0; i < path.Length; i++)
                        intPath.Add(new IntPoint(path[i].x * ClipperScale, path[i].y * ClipperScale));
                    clipper.AddPath(intPath, PolyType.ptClip, true);
                }

                var solution = new List<List<IntPoint>>();
                clipper.Execute(ClipType.ctDifference, solution);

                var tess = new Tess();
                foreach (var path in solution)
                {
                    var contour = new ContourVertex[path.Count];
                    for (var i = 0; i < path.Count; i++)
                    {
                        contour[i] = new ContourVertex
                        {
                            Position = new Vec3 {X = path[i].X / ClipperScale, Y = path[i].Y / ClipperScale, Z = 0.0f}
                        };
                    }
                    tess.AddContour(contour);
                }

                tess.Tessellate(WindingRule.EvenOdd, ElementType.Polygons, 3);

                var vertices = new NativeArray<Vector3>(tess.VertexCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                for (var i = 0; i < vertices.Length; i++)
                {
                    var pos = tess.Vertices[i].Position;
                    vertices[i] = new Vector3(pos.X / sprite.pixelsPerUnit, pos.Y / sprite.pixelsPerUnit, 0);
                }

                var indices = new NativeArray<ushort>(tess.Elements.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                for (var i = 0; i < indices.Length; i++)
                {
                    indices[i] = (ushort) tess.Elements[i];
                }
                sprite.SetVertexCount(vertices.Length);
                sprite.SetVertexAttribute(VertexAttribute.Position, vertices);
                sprite.SetIndices(indices, 0);

            }
        }

        void OnPostprocessTexture(Texture2D texture)
        {
            // Debug.Log("Texture2D: (" + texture.width + "x" + texture.height + ")");
        }
    }
}
