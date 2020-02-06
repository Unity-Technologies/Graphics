// 0, 1, 2, 3 here corresponds to layers, not UV sets.
#if ((defined(_NORMALMAP_TANGENT_SPACE ) && (defined(_NORMALMAP ) || defined(_BENTNORMALMAP ) || defined(_TANGENTMAP))) || \
     (defined(_NORMALMAP_TANGENT_SPACE0) && (defined(_NORMALMAP0) || defined(_BENTNORMALMAP0))) || \
     (defined(_NORMALMAP_TANGENT_SPACE1) && (defined(_NORMALMAP1) || defined(_BENTNORMALMAP1))) || \
     (defined(_NORMALMAP_TANGENT_SPACE2) && (defined(_NORMALMAP2) || defined(_BENTNORMALMAP2))) || \
     (defined(_NORMALMAP_TANGENT_SPACE3) && (defined(_NORMALMAP3) || defined(_BENTNORMALMAP3)))    \
     )
// {
    #define NORMAL_MAP_TS
// }
#endif

#if (defined(_REQUIRE_UV01) || defined(_REQUIRE_UV012) || defined(_REQUIRE_UV0123))
    #define TEX_UV1
#endif

#if (defined(_REQUIRE_UV012) || defined(_REQUIRE_UV0123))
    #define TEX_UV2
#endif

#if (defined(_REQUIRE_UV0123))
    #define TEX_UV3
#endif

#if (defined(LAYERED_LIT_SHADER) && (defined(_LAYER_MASK_VERTEX_COLOR_MUL) || defined(_LAYER_MASK_VERTEX_COLOR_ADD)))
    #define TEX_COL
#endif

#if ((SHADERPASS == SHADERPASS_DEPTH_ONLY) || (SHADERPASS == SHADERPASS_SHADOWS))
    #define DEPTH_PASS
#endif

#if (defined(DEPTH_PASS) && (defined(_DEPTHOFFSET_ON) || defined(_ALPHATEST_ON) || defined(LOD_FADE_CROSSFADE)))
    #define DEPTH_PASS_HAS_PS
#endif

// Remember to #undef in