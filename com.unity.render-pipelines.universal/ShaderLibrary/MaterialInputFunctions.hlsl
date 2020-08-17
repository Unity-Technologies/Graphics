#ifndef UNITY_SHADER_INPUT_FUNCTIONS_INCLUDED
#define UNITY_SHADER_INPUT_FUNCTIONS_INCLUDED

// -----------------------------------
// CBuffer UnityPerMaterial Functions
// -----------------------------------
#define _SurfaceType _Surface.x

half GetSurfaceType()
{
    return _SurfaceType;
}

// -----------------------------------

#endif // UNITY_SHADER_INPUT_FUNCTIONS_INCLUDED
