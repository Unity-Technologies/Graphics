using System;
using System.Collections.Generic;


public class ShaderBuilder
{
    // NOTE: a char is 2 bytes in C# (wide char)
    // internal char[] buf;
    // internal int length;

    List<String> strs;
    int charLength;

    public ShaderBuilder(int initialSize = 100)
    {
        // buf = new char[initialSize];
        // length = 0;

        strs = new List<string>(initialSize);
        charLength = 0;
    }

    // this looks up a generic parameter for the currently generating context
    public SandboxType FindLocalGenericType(string localName)
    {
        // TODO: actual lookup..  :)
        return Types._half;
    }

//     void Grow()
//     {
//         // var newbuf = new string[buf.Length * 3];
//         // buf.CopyTo(newbuf, 0);
//         // buf = newbuf;
//         // TODO: use linked list instead to avoid large copies?
//         // or maybe we just cache the buffers to avoid GC?
//         // we'll need lots of smallish buffers for function definition building...
//         // and very large buffers for whole-shader building...
//         var newbuf = new char[buf.Length * 3];
//         unsafe
//         {
//             fixed (char* sourcePtr = buf)
//             fixed (char* destPtr = newbuf)
//                 Buffer.MemoryCopy((byte*)destPtr, (byte*)sourcePtr, newbuf.Length * 2, length * 2);
//         }
//         buf = newbuf;
//     }

    static readonly string _space = " ";
    public void Space()
    {
        Add(_space);
        charLength += 1;
    }

    static readonly string _newline = "\n";
    public void NewLine()
    {
        Add(_newline);
        charLength += 1;
    }

//     public void Add(char c)
//     {
//         int newlength = length + 1;
//         if (newlength >= buf.Length)
//             Grow();
//         buf[length] = c;
//         length = newlength;
//     }

    public void AddLine(string l0)
    {
        Add(l0);
        NewLine();
    }

    public void Id(string id)
    {
        // TODO: lookup id mappings
    }

    public void Add(string l0)
    {
        strs.Add(l0);
        charLength += l0.Length;
    }

    public void Identifier(string i0)
    {
    }

    public void Add(string l0, string l1) { Add(l0); Add(l1); }
    public void Add(string l0, string l1, string l2) { Add(l0); Add(l1); Add(l2); }
    public void Add(string l0, string l1, string l2, string l3) { Add(l0); Add(l1); Add(l2); Add(l3); }
    public void Add(string l0, string l1, string l2, string l3, string l4) { Add(l0); Add(l1); Add(l2); Add(l3); Add(l4); }
    public void Add(string l0, string l1, string l2, string l3, string l4, string l5) { Add(l0); Add(l1); Add(l2); Add(l3); Add(l4); Add(l5); }

    public string ConvertToString()
    {
        //         var result = string.Create(charLength, this, (span, builder) =>
        //         {
        //             // TODO: once we have spans (.NEt 5) this is a better way to do it, fewer allocations
        //             foreach (var s in builder.strs)
        //             {
        //                 s.AsSpan().CopyTo(chars.Slice(position));
        //             }
        //         });

        char[] buf = new char[charLength];
        int len = 0;
        foreach (var s in strs)
        {
            int slen = s.Length;
            s.CopyTo(0, buf, len, slen);
            len += slen;
        }
        var result = new string(buf, 0, len);
        return result;
    }
}
