//
// Klak - Utilities for creative coding with Unity
//
// Copyright (C) 2016 Keijiro Takahashi
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
using UnityEngine;

namespace Klak.MaterialExtension
{
    /// Extension methods (setters) for Material
    static class MaterialSetterExtension
    {
        static public Material Property(this Material m, string name, float x)
        {
            m.SetFloat(name, x);
            return m;
        }

        static public Material Property(this Material m, string name, float x, float y)
        {
            m.SetVector(name, new Vector2(x, y));
            return m;
        }

        static public Material Property(this Material m, string name, float x, float y, float z)
        {
            m.SetVector(name, new Vector3(x, y, z));
            return m;
        }

        static public Material Property(this Material m, string name, float x, float y, float z, float w)
        {
            m.SetVector(name, new Vector4(x, y, z, w));
            return m;
        }

        static public Material Property(this Material m, string name, Vector2 v)
        {
            m.SetVector(name, v);
            return m;
        }

        static public Material Property(this Material m, string name, Vector3 v)
        {
            m.SetVector(name, v);
            return m;
        }

        static public Material Property(this Material m, string name, Vector4 v)
        {
            m.SetVector(name, v);
            return m;
        }

        static public Material Property(this Material m, string name, Color color)
        {
            m.SetColor(name, color);
            return m;
        }

        static public Material Property(this Material m, string name, Texture texture)
        {
            m.SetTexture(name, texture);
            return m;
        }
    }

    /// Extension methods (setters) for MaterialProperty
    static class MaterialPropertySetterExtension
    {
        static public MaterialPropertyBlock Property(this MaterialPropertyBlock m, string name, float x)
        {
            m.SetFloat(name, x);
            return m;
        }

        static public MaterialPropertyBlock Property(this MaterialPropertyBlock m, string name, float x, float y)
        {
            m.SetVector(name, new Vector2(x, y));
            return m;
        }

        static public MaterialPropertyBlock Property(this MaterialPropertyBlock m, string name, float x, float y, float z)
        {
            m.SetVector(name, new Vector3(x, y, z));
            return m;
        }

        static public MaterialPropertyBlock Property(this MaterialPropertyBlock m, string name, float x, float y, float z, float w)
        {
            m.SetVector(name, new Vector4(x, y, z, w));
            return m;
        }

        static public MaterialPropertyBlock Property(this MaterialPropertyBlock m, string name, Vector2 v)
        {
            m.SetVector(name, v);
            return m;
        }

        static public MaterialPropertyBlock Property(this MaterialPropertyBlock m, string name, Vector3 v)
        {
            m.SetVector(name, v);
            return m;
        }

        static public MaterialPropertyBlock Property(this MaterialPropertyBlock m, string name, Vector4 v)
        {
            m.SetVector(name, v);
            return m;
        }

        static public MaterialPropertyBlock Property(this MaterialPropertyBlock m, string name, Color color)
        {
            m.SetColor(name, color);
            return m;
        }

        static public MaterialPropertyBlock Property(this MaterialPropertyBlock m, string name, Texture texture)
        {
            m.SetTexture(name, texture);
            return m;
        }
    }
}
