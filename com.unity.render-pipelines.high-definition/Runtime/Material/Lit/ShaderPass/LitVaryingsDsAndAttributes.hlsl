/* Varyings_DS are domain shader inputs. */

#ifdef TESSELLATION_ON
        // Position and normal are always required for tessellation.
        #define VARYINGS_DS_NEED_NORMAL

    #ifdef VARYINGS_NEED_TANGENT_WS
        #define VARYINGS_DS_NEED_TANGENT
    #endif

    #if (defined(VARYINGS_NEED_TEXCOORD0) || defined(_TESSELLATION_DISPLACEMENT)) // UV0 is always available
        #define VARYINGS_DS_NEED_TEXCOORD0
    #endif

    #if (defined(VARYINGS_NEED_TEXCOORD1) || (defined(_TESSELLATION_DISPLACEMENT) && defined(TEX_UV1)))
        #define VARYINGS_DS_NEED_TEXCOORD1
    #endif

    #if (defined(VARYINGS_NEED_TEXCOORD2) || (defined(_TESSELLATION_DISPLACEMENT) && defined(TEX_UV2)))
        #define VARYINGS_DS_NEED_TEXCOORD2
    #endif

    #if (defined(VARYINGS_NEED_TEXCOORD3) || (defined(_TESSELLATION_DISPLACEMENT) && defined(TEX_UV3)))
        #define VARYINGS_DS_NEED_TEXCOORD3
    #endif

    #if (defined(VARYINGS_NEED_COLOR) || (defined(_TESSELLATION_DISPLACEMENT) && defined(TEX_COL)))
        #define VARYINGS_DS_NEED_COLOR
    #endif

#define VARYINGS_DS_HAVE_BEEN_DEFINED // Avoid multiple definitions

#endif // TESSELLATION_ON

/* Attributes are vertex shader inputs. */

#if (defined(VARYINGS_NEED_NORMAL_WS) || defined(VARYINGS_DS_NEED_NORMAL) || defined(_VERTEX_DISPLACEMENT))
    #define ATTRIBUTES_NEED_NORMAL
#endif

#if (defined(VARYINGS_NEED_TANGENT_WS) || defined(VARYINGS_DS_NEED_TANGENT))
    #define ATTRIBUTES_NEED_TANGENT
#endif

#if (defined(VARYINGS_NEED_TEXCOORD0) || defined(VARYINGS_DS_NEED_TEXCOORD0) || defined(_VERTEX_DISPLACEMENT)) // UV0 is always available
    #define ATTRIBUTES_NEED_TEXCOORD0
#endif

#if (defined(VARYINGS_NEED_TEXCOORD1) || defined(VARYINGS_DS_NEED_TEXCOORD1) || (defined(_VERTEX_DISPLACEMENT) && defined(TEX_UV1)))
    #define ATTRIBUTES_NEED_TEXCOORD1
#endif

#if (defined(VARYINGS_NEED_TEXCOORD2) || defined(VARYINGS_DS_NEED_TEXCOORD2) || (defined(_VERTEX_DISPLACEMENT) && defined(TEX_UV2)))
    #define ATTRIBUTES_NEED_TEXCOORD2
#endif

#if (defined(VARYINGS_NEED_TEXCOORD3) || defined(VARYINGS_DS_NEED_TEXCOORD3) || (defined(_VERTEX_DISPLACEMENT) && defined(TEX_UV3)))
    #define ATTRIBUTES_NEED_TEXCOORD3
#endif

#if (defined(VARYINGS_NEED_COLOR) || defined(VARYINGS_DS_NEED_COLOR) || (defined(_VERTEX_DISPLACEMENT) && defined(TEX_COL)))
    #define ATTRIBUTES_NEED_COLOR
#endif
