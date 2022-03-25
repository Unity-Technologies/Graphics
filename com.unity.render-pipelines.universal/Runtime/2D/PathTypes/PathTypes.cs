using System.Collections;
using System.Collections.Generic;
using UnityEngine;

internal struct PathTypes 
{
    public const string k_CreationError = "Path type has either been disposed or has not been created with a size.";
    public const string k_OutOfRangeError = "Array index out of range.";

    public enum DisposeOptions
    {
        Shallow,
        Deep
    }
}
