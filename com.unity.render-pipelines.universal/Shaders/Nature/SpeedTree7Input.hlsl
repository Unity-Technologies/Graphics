#ifndef UNIVERSAL_SPEEDTREE7_INPUT_INCLUDED
#define UNIVERSAL_SPEEDTREE7_INPUT_INCLUDED

#define SPEEDTREE_Y_UP

#ifdef GEOM_TYPE_BRANCH_DETAIL
    #define GEOM_TYPE_BRANCH
#endif

#ifdef GEOM_TYPE_BRANCH_DETAIL
    sampler2D _DetailTex;
#endif

#if defined(GEOM_TYPE_FROND) || defined(GEOM_TYPE_LEAF) || defined(GEOM_TYPE_FACING_LEAF)
#define SPEEDTREE_ALPHATEST
    half _Cutoff;
#endif

#ifdef SCENESELECTIONPASS
    int _ObjectId;
    int _PassValue;
#endif

#include "SpeedTree7CommonInput.hlsl"

#define _Surface 0.0 // Speed Trees are always opaque

#endif
