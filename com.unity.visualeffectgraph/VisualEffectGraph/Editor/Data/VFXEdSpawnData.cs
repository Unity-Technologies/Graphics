using UnityEngine;
using UnityEditor;
using System.Collections;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
    internal class VFXEdSpawnData
    {
        public readonly VFXEdCanvas targetCanvas;
        public readonly Vector2 mousePosition;
        public readonly string libraryName;
        public readonly SpawnType spawnType;

        public VFXEdSpawnData(VFXEdCanvas canvas, Vector2 mouseposition, string libraryname, SpawnType spawntype)
        {
            targetCanvas = canvas;
            mousePosition = mouseposition;
            libraryName = libraryname;
            spawnType = spawntype;
        }
    }

    internal enum SpawnType
    {
        Node,
        NodeBlock,
        Context,
        Comment,
        Event
    };
}
