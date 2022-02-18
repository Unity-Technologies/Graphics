using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Serialization;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    enum ShaderGraphVersion
    {
        NeverMigrated = 0,
        FirstTimeMigration,
    }
}
