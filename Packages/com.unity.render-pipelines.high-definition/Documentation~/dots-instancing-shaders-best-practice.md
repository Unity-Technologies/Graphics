
# Best practice for DOTS Instancing shaders

It is best practice to initialize the first 64 bytes of all `unity_DOTSInstanceData` buffers to zero and leave them unused. This is because the default metadata value that Unity uses for all metadata values not specified during batch creation is zero. Specifically, when a shader loads a zero metadata value from the `UNITY_ACCESS_DOTS_INSTANCED_PROP` macro, the shader loads this value from the address `zero` because the instance index will be disregarded. Ensuring that the first 64 bytes, which is the size of the largest value type (a float4x4 matrix), are zeroes guarantees that such loads predictably return a result of zero. Otherwise, the shader could load something unpredictable, depending on what happens to be located at address zero.

When using DOTS Instancing, Shader Graphs and Shaders that Unity provides use a special convention for transform matrices. To save GPU memory and bandwidth, they store these matrices using only 12 floats instead of the full 16, because four floats are always constant. These shaders expect floats formatted in such a way that the x, y, and z of each column in the matrix are stored in order. In other words, the first three floats are the x, y, and z of the first column, the next three floats are the x, y, and z of the second column, and so on. The matrices don't store the `w` element of each column. The transform matrices this affects are:

* `unity_ObjectToWorld`
* `unity_WorldToObject`
* `unity_MatrixPreviousM`
* `unity_MatrixPreviousMI`

The following code sample includes a struct that converts regular four-by-four matrices into the 12 floats convention.

```lang-csharp
struct PackedMatrix
{
    public float c0x;
    public float c0y;
    public float c0z;
    public float c1x;
    public float c1y;
    public float c1z;
    public float c2x;
    public float c2y;
    public float c2z;
    public float c3x;
    public float c3y;
    public float c3z;

    public PackedMatrix(Matrix4x4 m)
    {
        c0x = m.m00;
        c0y = m.m10;
        c0z = m.m20;
        c1x = m.m01;
        c1y = m.m11;
        c1z = m.m21;
        c2x = m.m02;
        c2y = m.m12;
        c2z = m.m22;
        c3x = m.m03;
        c3y = m.m13;
        c3z = m.m23;
    }
}
```
