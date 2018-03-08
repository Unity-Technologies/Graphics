using System;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;

namespace UnityEditor.VFX.Block
{
    static class VariadicSizeUtility
    {
        public enum SizeMode
        {
            X = 0,
            XY = 1,
            XYZ = 2,
        }

        public static IEnumerable<VFXAttributeInfo> GetAttributes(SizeMode mode, VFXAttributeMode attrMode)
        {
            yield return new VFXAttributeInfo(VFXAttribute.SizeX, attrMode);

            if ((int)mode > (int)SizeMode.X)
                yield return new VFXAttributeInfo(VFXAttribute.SizeY, attrMode);

            if ((int)mode > (int)SizeMode.XY)
                yield return new VFXAttributeInfo(VFXAttribute.SizeZ, attrMode);
        }

        public static readonly VFXAttribute[] Attribute = new VFXAttribute[] { VFXAttribute.SizeX, VFXAttribute.SizeY, VFXAttribute.SizeZ };

        public static readonly string[] ChannelName = { ".x", ".y", ".z" };

    }
}

