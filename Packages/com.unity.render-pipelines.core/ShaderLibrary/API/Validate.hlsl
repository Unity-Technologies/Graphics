// Wait for a fix from Trunk #error not supported yet
/*
#define REQUIRE_DEFINED(X_) \
    #ifndef X_  \
        #error X_ must be defined (in) the platform include \
    #endif X_  \

REQUIRE_DEFINED(UNITY_UV_STARTS_AT_TOP)
REQUIRE_DEFINED(UNITY_REVERSED_Z)
REQUIRE_DEFINED(UNITY_NEAR_CLIP_VALUE)
REQUIRE_DEFINED(FACE)

REQUIRE_DEFINED(CBUFFER_START)
REQUIRE_DEFINED(CBUFFER_END)

REQUIRE_DEFINED(INITIALIZE_OUTPUT)

*/


// Default values for things that have not been defined in the platform headers

// default flow control attributes
#ifndef UNITY_BRANCH
#   define UNITY_BRANCH
#endif
#ifndef UNITY_FLATTEN
#   define UNITY_FLATTEN
#endif
#ifndef UNITY_UNROLL
#   define UNITY_UNROLL
#endif
#ifndef UNITY_UNROLLX
#   define UNITY_UNROLLX(_x)
#endif
#ifndef UNITY_LOOP
#   define UNITY_LOOP
#endif

// In situations where buffer accesses are correctly guarded by an
// out of bounds check, Unity may perform branch flattening in such
// a way that the out of bounds access happens regardless. This is
// valid in some languages (e.g. HLSL) but not in others (e.g. MSL)
// where it causes undefined behavior. See UUM-126860 for more
// details. As a workaround you may use this macro.
#ifndef UNITY_OUT_OF_BOUNDS_BRANCH
#   define UNITY_OUT_OF_BOUNDS_BRANCH
#endif
