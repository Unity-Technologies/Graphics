namespace UnityEngine
{
    /// <summary>
    /// Common code for all Data-Driven Lens Flare used
    /// </summary>
    public sealed class SRPLensFlareCommon
    {
        private static SRPLensFlareCommon m_Instance = null;
        private static readonly object m_Padlock = new object();
        private System.Collections.Generic.List<SRPLensFlareOverride> m_Data;

        private SRPLensFlareCommon()
        {
            m_Data = new System.Collections.Generic.List<SRPLensFlareOverride>();
        }

        /// <summary>
        /// Current unique instance
        /// </summary>
        public static SRPLensFlareCommon Instance
        {
            get
            {
                if (m_Instance == null)
                {
                    lock (m_Padlock)
                    {
                        if (m_Instance == null)
                        {
                            m_Instance = new SRPLensFlareCommon();
                        }
                    }
                }
                return m_Instance;
            }
        }

        private System.Collections.Generic.List<SRPLensFlareOverride> Data { get { return m_Data; } }

        /// <summary>
        /// Return the pool of Lens Flare added
        /// </summary>
        /// <returns>The Lens Flare Pool</returns>
        public System.Collections.Generic.List<SRPLensFlareOverride> GetData()
        {
            return Data;
        }

        /// <summary>
        /// Check if we have at least one Lens Flare added on the pool
        /// </summary>
        /// <returns>true if no Lens Flare were added</returns>
        public bool IsEmpty()
        {
            return Data.Count == 0;
        }

        /// <summary>
        /// Add a new lens flare component on the pool.
        /// </summary>
        /// <param name="newData">The new data added</param>
        public void AddData(SRPLensFlareOverride newData)
        {
            Debug.Assert(Instance == this, "SRPLensFlareCommon can have only one instance");

            if (!m_Data.Contains(newData))
            {
                m_Data.Add(newData);
            }
        }

        /// <summary>
        /// Remove a lens flare data which exist in the pool.
        /// </summary>
        /// <param name="data">The data which exist in the pool</param>
        public void RemoveData(SRPLensFlareOverride data)
        {
            Debug.Assert(Instance == this, "SRPLensFlareCommon can have only one instance");

            if (m_Data.Contains(data))
            {
                m_Data.Remove(data);
            }
        }

        #region Panini Projection
        static Vector2 DoPaniniProjection(Vector2 screenPos, int actualWidth, int actualHeight, float fieldOfView, float paniniProjectionCropToFit, float paniniProjectionDistance, bool inverse)
        {
            float distance = paniniProjectionDistance;
            //custom-begin: hack to force panini to 1 in this resolution
            //if (camera.actualWidth == 3168 && camera.actualHeight == 1056)
            //{
            //    distance = 1;
            //}
            //custom-end:
            Vector2 viewExtents = CalcViewExtents(actualWidth, actualHeight, fieldOfView);
            Vector2 cropExtents = Panini_Generic_Inv(viewExtents, distance);

            float scaleX = cropExtents.x / viewExtents.x;
            float scaleY = cropExtents.y / viewExtents.y;
            float scaleF = Mathf.Min(scaleX, scaleY);

            float paniniD = distance;
            float paniniS = Mathf.Lerp(1.0f, Mathf.Clamp01(scaleF), paniniProjectionCropToFit);

            if (!inverse)
                return Panini_Generic(screenPos * viewExtents * paniniS, paniniD) / viewExtents;
            else
                return Panini_Generic_Inv(screenPos * viewExtents, paniniD) / (viewExtents * paniniS);
        }

        static Vector2 CalcViewExtents(int actualWidth, int actualHeight, float fieldOfView)
        {
            float fovY = fieldOfView * Mathf.Deg2Rad;
            float aspect = (float)actualWidth / (float)actualHeight;

            float viewExtY = Mathf.Tan(0.5f * fovY);
            float viewExtX = aspect * viewExtY;

            return new Vector2(viewExtX, viewExtY);
        }

        static Vector2 Panini_Generic(Vector2 view_pos, float d)
        {
            // Given
            //    S----------- E--X-------
            //    |    `  ~.  /,´
            //    |-- ---    Q
            //    |        ,/    `
            //  1 |      ,´/       `
            //    |    ,´ /         ´
            //    |  ,´  /           ´
            //    |,`   /             ,
            //    O    /
            //    |   /               ,
            //  d |  /
            //    | /                ,
            //    |/                .
            //    P
            //    |              ´
            //    |         , ´
            //    +-    ´
            //
            // Have E
            // Want to find X
            //
            // First compute line-circle intersection to find Q
            // Then project Q to find X

            float view_dist = 1.0f + d;
            float view_hyp_sq = view_pos.x * view_pos.x + view_dist * view_dist;

            float isect_D = view_pos.x * d;
            float isect_discrim = view_hyp_sq - isect_D * isect_D;

            float cyl_dist_minus_d = (-isect_D * view_pos.x + view_dist * Mathf.Sqrt(isect_discrim)) / view_hyp_sq;
            float cyl_dist = cyl_dist_minus_d + d;

            Vector2 cyl_pos = view_pos * (cyl_dist / view_dist);
            return cyl_pos / (cyl_dist - d);
        }

        static Vector2 Panini_Generic_Inv(Vector2 projPos, float d)
        {
            // given
            //    S----------- E--X-------
            //    |    `  ~.  /,´
            //    |-- ---    Q
            //    |        ,/    `
            //  1 |      ,´/       `
            //    |    ,´ /         ´
            //    |  ,´  /           ´
            //    |,`   /             ,
            //    O    /
            //    |   /               ,
            //  d |  /
            //    | /                ,
            //    |/                .
            //    P
            //    |              ´
            //    |         , ´
            //    +-    ´
            //
            // have X
            // want to find E

            float viewDist = 1f + d;
            var projHyp = Mathf.Sqrt(projPos.x * projPos.x + 1f);

            float cylDistMinusD = 1f / projHyp;
            float cylDist = cylDistMinusD + d;
            var cylPos = projPos * cylDistMinusD;

            return cylPos * (viewDist / cylDist);
        }

        #endregion
    }
}
