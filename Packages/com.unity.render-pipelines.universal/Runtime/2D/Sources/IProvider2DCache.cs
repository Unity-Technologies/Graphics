using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEngine.Rendering.Universal
{
    internal interface IProvider2DCache
    {
        public IEnumerable<Provider2DKVPair> Cache { get; }
        public void UpdateCache(GameObject gameObj);
    }
}
