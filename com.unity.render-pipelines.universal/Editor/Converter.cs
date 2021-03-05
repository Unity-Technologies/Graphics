using System;
using UnityEngine;

// Might need to change this name before making it public
public abstract class CoreConverter
{
    public abstract string Name { get; }
    public abstract string Info { get; }

    public abstract void Convert();
}
