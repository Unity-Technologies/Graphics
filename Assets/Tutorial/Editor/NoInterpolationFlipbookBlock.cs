using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.VFX;
using UnityEngine.Experimental.VFX;

// A very simple node block
// Just to fake no interpolation with flipbooks (no interpolation should be handled direclty however...)
// You can take a look at Assets\VFXEditor\Editor\Blocks\Library to see all block description
public class VFXBlockNoInterpolationFlipbook : VFXBlockType
{
    public VFXBlockNoInterpolationFlipbook()
    {
        Name = "Fake No Interpolation";
        Icon = "Flipbook";
        Category = "Tutorial";
        CompatibleContexts = VFXContextDesc.Type.kTypeOutput;

        Add(new VFXAttribute(CommonAttrib.TexIndex, true));

        Source = @"
texIndex = floor(texIndex);";
    }
}