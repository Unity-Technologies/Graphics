//////////////////////////////////////////////////////////////////////////
// Fitter class for Linearly-Transformed Cosines
// From "Real-Time Polygonal-Light Shading with Linearly Transformed Cosines" (https://eheitzresearch.wordpress.com/415-2/)
//
// This is a C# re-implementation of the code provided by E. Heitz et S. Hill
// UPDATE: Using code from Stephen Hill's github repo instead (https://github.com/selfshadow/ltc_code/tree/master/fit)
//////////////////////////////////////////////////////////////////////////
// Some notes:
//  • The fitter probably uses L3 norm error because it's more important to have strong fitting on large values (i.e. the BRDF peak values)
//      than on the mostly 0 values at low roughness
//
//  • The fitter works on matrix M: we initialize it with appropriate directions and amplitude fitting the BRDF's
//      Then the m11, m22, m13 parameters are the ones composing the matrix M and they're the ones that are fit
//      At each step, the inverse M matrix is computed and forced into its runtime form:
//              | m11'   0   m13' |
//      M^-1 =  |  0    m22'  0   |
//              | m31'   0   m33' |
//
//        ►►► WARNING: Notice the prime! They are NOT THE SAME as the m11, m22, m13 fitting parameters of the M matrix!
//
//  • The runtime matrix M^-1 is renormalized by m22', which is apparently more stable and easier to interpolate according to S. Hill
//      We thus obtain the following runtime matrix with the 4 coefficients that need to be stored into a texture:
//              | m11"   0   m13" |
//      M^-1 =  |  0     1    0   |
//              | m31"   0   m33" |
//
//////////////////////////////////////////////////////////////////////////
//
using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline.LTCFit
{
    public class LTCFitter {

        #region CONSTANTS

        const int           MAX_ITERATIONS = 100;
        const float         FIT_EXPLORE_DELTA = 0.05f;
        const float         TOLERANCE = 1e-5f;
        const float         MIN_ALPHA = 0.00001f;        // minimal roughness (avoid singularities)

        #endregion

        #region NESTED TYPES

        public class UserAbortException : Exception {}

        /// <summary>
        /// Provides progress monitoring
        /// </summary>
        /// <param name="_progress"></param>
        /// <returns>Return false to abort computation</returns>
        public delegate bool    ProgressDelegate( float _progress );

        #endregion

        #region FIELDS

        NelderMead          m_fitter = new NelderMead( new LTC().GetFittingParms().Length );

        // Fiiting data
        IBRDF               m_BRDF = null;
        int                 m_tableSize;
        System.IO.FileInfo  m_tableFileName;

        // Results
        LTC[,]              m_results;
        int                 m_validResultsCount = 0;

        LTC                 m_lastComputedResult = null;
        Vector3             m_lastComputedView = new Vector3( 0, 0, 1 );
        float               m_lastComputedAlpha = 1.0f;

        int                 m_errorsCount = 0;
        string              m_errors = null;

        #endregion

        #region PROPERTIES

        public int      ErrorsCount         { get { lock ( this ) return m_errorsCount; } }
        public string   Errors              { get { lock ( this ) return m_errors; } }

        public LTC[,]   Results             { get { lock ( this ) return m_results; } }

        public LTC      LastComputedResult  { get { lock ( this ) return m_lastComputedResult; } }
        public Vector3  LastComputedView    { get { lock ( this ) return m_lastComputedView; } }
        public float    LastComputedAlpha   { get { lock ( this ) return m_lastComputedAlpha; } }

        #endregion

        #region METHODS

        /// <summary>
        /// Prepares the fitter with a new BRDF
        /// </summary>
        /// <param name="_BRDF"></param>
        /// <param name="_tableSize"></param>
        /// <param name="_exportFileName"></param>
        /// <returns></returns>
        public void SetupBRDF( IBRDF _BRDF, int _tableSize, System.IO.FileInfo _tableFileName ) {
            if ( _BRDF == null )
                throw new Exception( "Invalid BRDF!" );
            if ( _tableFileName == null )
                throw new Exception( "Invalid table filename! You must specify a target file to export the tables to..." );

            m_BRDF = _BRDF;
            m_tableSize = _tableSize;
            m_tableFileName = _tableFileName;

            m_results = new LTC[m_tableSize,m_tableSize];
            m_validResultsCount = 0;
            lock ( this ) {
                m_lastComputedResult = null;
                m_lastComputedView = new Vector3( 0, 0, 1 );
                m_lastComputedAlpha = 1.0f;
            }
        }

        /// <summary>
        /// Fits the entire table
        /// </summary>
        /// <param name="_overwriteExistingValues">True to recompute the entire table, overwriting existing computed values</param>
        /// <param name="_stopOnError">Stop computation as soon as we encounter an error</param>
        /// <param name="_progress">Optional progress monitor</param>
        public void Fit( bool _overwriteExistingValues, bool _stopOnError, ProgressDelegate _progress )
        {
            if ( !_overwriteExistingValues && m_tableFileName.Exists )
            {
                // Attempt to reload existing results
                try {
                    m_results = LoadTable( m_tableFileName, out m_validResultsCount );
Debug.Log( "Loaded table " + m_tableFileName.FullName + " - " + m_validResultsCount + " valid results found" );
                } catch ( Exception _e ) {
                	throw new Exception( "Failed to reload existing LTC table! Can't resume computation. Please, either delete exiting LTC file on disk or choose to overwrite existing values...", _e );
                }
            }

            for ( int roughnessIndex=m_tableSize-1; roughnessIndex >= 0; roughnessIndex-- )
            {
                for ( int thetaIndex=0; thetaIndex < m_tableSize; thetaIndex++ )
                {
                    if ( m_results[roughnessIndex,thetaIndex] != null )
                        continue;   // Already computed...

                    FitSingle( roughnessIndex, thetaIndex );
                    if ( _stopOnError && m_errorsCount > 0 )
                        throw new Exception( "Stopped because of error..." );
                }

                SaveTable( m_tableFileName, m_results, m_errors );
                if ( _progress != null && !_progress( 1.0f - (float) roughnessIndex / m_tableSize ) )
                    throw new UserAbortException(); // Abort!
            }
        }

        /// <summary>
        /// Fits a single LTC for the specified roughness and theta indices
        /// WARNING: Overwrites previous LTC value
        /// </summary>
        /// <param name="_roughnessIndex"></param>
        /// <param name="_thetaIndex"></param>
        public void FitSingle( int _roughnessIndex, int _thetaIndex ) {
            try {
                // Prepare a new LTC
                LTC     ltc = new LTC();

                float   alpha, cosTheta;
                GetRoughnessAndAngle( _roughnessIndex, _thetaIndex, m_tableSize, out alpha, out cosTheta );

                Vector3    tsView = new Vector3( Mathf.Sqrt( 1 - cosTheta*cosTheta ), 0, cosTheta );

                // Compute BRDF's magnitude and average direction
                ltc.ComputeAverageTerms( m_BRDF, ref tsView, alpha );

                // 1. first guess for the fit
                // init the hemisphere in which the distribution is fitted
                bool    isotropic;
                if ( _thetaIndex == 0 ) {
                    // if theta == 0 the lobe is rotationally symmetric and aligned with Z = (0 0 1)
                    ltc.X = new Vector3( 1, 0, 0 );
                    ltc.Y = new Vector3( 0, 1, 0 );
                    ltc.Z = new Vector3( 0, 0, 1 );

                    if ( _roughnessIndex == m_tableSize-1 || m_results[_roughnessIndex+1,_thetaIndex] == null ) {
                        // roughness = 1 or no available result
                        ltc.m11 = 1.0f;
                        ltc.m22 = 1.0f;
                    } else {
                        // init with roughness of previous fit
                        LTC previousLTC = m_results[_roughnessIndex+1,_thetaIndex];
                        ltc.m11 = previousLTC.m11;
                        ltc.m22 = previousLTC.m22;
                    }

                    ltc.m13 = 0;

                    isotropic = true;
                } else {
                    // Otherwise use average direction as Z vector
                    LTC previousLTC = m_results[_roughnessIndex,_thetaIndex-1];
                    if ( previousLTC != null ) {
                        ltc.m11 = previousLTC.m11;
                        ltc.m22 = previousLTC.m22;
                        ltc.m13 = previousLTC.m13;
                    }

                    isotropic = false;
                }
                ltc.Update();

//Debug.Log( "Computing new result at [" + _roughnessIndex + ", " + _thetaIndex + "]" );

                // Find best-fit LTC lobe (scale, alphax, alphay)
                if ( ltc.magnitude > 1e-6 ) {

                    double[]    startFit = ltc.GetFittingParms();
                    double[]    resultFit = new double[startFit.Length];

                    ltc.error = m_fitter.FindFit( resultFit, startFit, FIT_EXPLORE_DELTA, TOLERANCE, MAX_ITERATIONS, ( double[] _parameters ) => {
                        ltc.SetFittingParms( _parameters, isotropic );

                        double    currentError = ComputeError( ltc, m_BRDF, ref tsView, alpha );
                        return currentError;
                    } );
                    ltc.iterationsCount = m_fitter.m_lastIterationsCount;

                    // Update LTC with final best fitting values
                    ltc.SetFittingParms( resultFit, isotropic );
                }

                // Store new valid result
                m_results[_roughnessIndex,_thetaIndex] = ltc;
                m_validResultsCount++;

                lock ( this ) {
                    m_lastComputedResult = ltc;
                    m_lastComputedView = tsView;
                    m_lastComputedAlpha = alpha;
                }

//Debug.Log( "New result computed at [" + _roughnessIndex + ", " + _thetaIndex + "]" );

            } catch ( Exception _e ) {
                // Clear LTC!
//Debug.LogError( "Failed result at [" + _roughnessIndex + ", " + _thetaIndex + "]" );
                if ( m_results[_roughnessIndex,_thetaIndex] != null )
                    m_validResultsCount--;  // One less valid result!
                m_results[_roughnessIndex,_thetaIndex] = null;

                m_errorsCount++;
                m_errors += "An error occurred at [" + _roughnessIndex + ", " + _thetaIndex + "]: " + _e.Message + "\r\n";
            }
        }

        /// <summary>
        /// Returns the actual cos(theta) and alpha value given the table indices
        /// </summary>
        /// <param name="_roughnessIndex">Index in the roughness dimension</param>
        /// <param name="_thetaIndex">Index in the angular dimension</param>
        /// <param name="_tableSize">Size of the table</param>
        /// <param name="_alpha">Actual roughness to use with the BRDF (the "alpha", or "m" parameter found in every papers)</param>
        /// <param name="_cosTheta">Cos(theta) where theta is the view angle. theta=0 is top view, theta=PI/2 is view aligned with tangent plane</param>
        public static void    GetRoughnessAndAngle( int _roughnessIndex, int _thetaIndex, int _tableSize, out float _alpha, out float _cosTheta ) {

            // alpha = perceptualRoughness^2  (perceptualRoughness = "sRGB" representation of roughness, as painted by artists)
            float perceptualRoughness = (float) _roughnessIndex / (_tableSize-1);
            _alpha = Mathf.Max( MIN_ALPHA, perceptualRoughness * perceptualRoughness );

            // parameterised by sqrt(1 - cos(theta))
            float    x = (float) _thetaIndex / (_tableSize - 1);
            _cosTheta = 1.0f - x*x;
            _cosTheta = Mathf.Max( 3.7540224885647058065387021283285e-4f, _cosTheta );    // Clamp to cos(1.57)
        }

        /// <summary>
        /// Ensures the provided BRDF is normalized for various values of view and roughness
        /// You can call this helper to make sure your BRDF is always integrating to 1 for any viewing condition and any roughness
        /// </summary>
        /// <param name="_BRDF"></param>
        public static double[,]    CheckBRDFNormalization( IBRDF _BRDF ) {

            const int   THETA_VIEW_VALUES_COUNT = 8;
            const int   ROUGHNESS_VALUES_COUNT = 32;

            double  pdf;
            Vector3 tsView = new Vector3();
            Vector3 tsLight = new Vector3();

            double[,]   sums = new double[ROUGHNESS_VALUES_COUNT,THETA_VIEW_VALUES_COUNT];
            for ( int roughnessIndex=0; roughnessIndex < ROUGHNESS_VALUES_COUNT; roughnessIndex++ ) {
                float   perceptualRoughness = (float) roughnessIndex / (ROUGHNESS_VALUES_COUNT-1);
                float   alpha = Mathf.Max( MIN_ALPHA, perceptualRoughness * perceptualRoughness );

                for ( int thetaIndex=0; thetaIndex < THETA_VIEW_VALUES_COUNT; thetaIndex++ ) {
                    float   x = (float) thetaIndex * 0.5f * Mathf.PI / Math.Max( 1, THETA_VIEW_VALUES_COUNT-1 );
                    float   cosTheta = 1.0f - x*x;
                            cosTheta = Mathf.Max( 3.7540224885647058065387021283285e-4f, cosTheta );    // Clamp to cos(1.57)
                    tsView.Set( Mathf.Sqrt( 1 - cosTheta*cosTheta ), 0, cosTheta );

                    // Importance sampling
                    double  sum = 0;
                    int     samplesCount = 0;
                    for( float u=0; u <= 1; u+=0.02f ) {
                        for( float v=0; v < 1; v+=0.02f ) {
                            _BRDF.GetSamplingDirection( ref tsView, alpha, u, v, ref tsLight );
                            double    V = _BRDF.Eval( ref tsView, ref tsLight, alpha, out pdf );
                            if ( pdf > 0.0 )
                                sum += V / pdf;
                            samplesCount++;
                        }
                    }
                    sum /= samplesCount;

                    sums[roughnessIndex,thetaIndex] = sum;
                }
            }

            return sums;
        }

        #region Objective Function

        const int    SAMPLES_COUNT = 32;            // number of samples used to compute the error during fitting

        // Compute the error between the BRDF and the LTC using Multiple Importance Sampling
        static double   ComputeError( LTC _LTC, IBRDF _BRDF, ref Vector3 _tsView, float _alpha ) {
            Vector3 tsLight = Vector3.zero;

            double  pdf_BRDF, eval_BRDF;
            double  pdf_LTC, eval_LTC;

            double  sumError = 0.0;
            for ( int j = 0 ; j < SAMPLES_COUNT ; ++j ) {
                for ( int i = 0 ; i < SAMPLES_COUNT ; ++i ) {
                    float   U1 = (i+0.5f) / SAMPLES_COUNT;
                    float   U2 = (j+0.5f) / SAMPLES_COUNT;

                    // importance sample LTC
                    {
                        // sample
                        _LTC.GetSamplingDirection( U1, U2, ref tsLight );
                
                        // error with MIS weight
                        eval_BRDF = _BRDF.Eval( ref _tsView, ref tsLight, _alpha, out pdf_BRDF );
                        eval_LTC = _LTC.Eval( ref tsLight );
                        pdf_LTC = eval_LTC / _LTC.magnitude;
                        double  error = Math.Abs( eval_BRDF - eval_LTC );
                                error = error*error*error;        // Use L3 norm to favor large values over smaller ones

                        if ( error != 0.0 )
                            error /= pdf_LTC + pdf_BRDF;

                        if ( double.IsNaN( error ) )
                            throw new Exception( "NaN!" );
                        sumError += error;
                    }

                    // importance sample BRDF
                    {
                        // sample
                        _BRDF.GetSamplingDirection( ref _tsView, _alpha, U1, U2, ref tsLight );

                        // error with MIS weight
                        eval_BRDF = _BRDF.Eval( ref _tsView, ref tsLight, _alpha, out pdf_BRDF );            
                        eval_LTC = _LTC.Eval( ref tsLight );
                        pdf_LTC = eval_LTC / _LTC.magnitude;
                        double  error = Math.Abs( eval_BRDF - eval_LTC );
                                error = error*error*error;        // Use L3 norm to favor large values over smaller ones

                        if ( error != 0.0 )
                            error /= pdf_LTC + pdf_BRDF;

                        if ( double.IsNaN( error ) )
                            throw new Exception( "NaN!" );
                        sumError += error;
                    }
                }
            }

            sumError /= SAMPLES_COUNT * SAMPLES_COUNT;
            return sumError;
        }

        #endregion

        #region I/O

        public static LTC[,]    LoadTable( System.IO.FileInfo _tableFileName, out int _validResultsCount ) {
            if ( !_tableFileName.Exists )
                throw new Exception( "LTC Table file \"" + _tableFileName + "\" not found!" );

            LTC[,]    result = null;
            _validResultsCount = 0;
            using ( System.IO.FileStream S = _tableFileName.OpenRead() )
                using ( System.IO.BinaryReader R = new System.IO.BinaryReader( S ) ) {
                    result = new LTC[R.ReadUInt32(), R.ReadUInt32()];
                    for ( uint Y=0; Y < result.GetLength( 1 ); Y++ ) {
                        for ( uint X=0; X < result.GetLength( 0 ); X++ ) {
                            if ( R.ReadBoolean() ) {
                                result[X,Y] = new LTC( R );
                                _validResultsCount++;
                            }
//else {
//Debug.LogWarning( "Result at " + X + ", " + Y + " is invalid!" );
//}
                        }
                    }
                }

            return result;
        }

        public static void  SaveTable( System.IO.FileInfo _tableFileName, LTC[,] _table, string _errors ) {
            using ( System.IO.FileStream S = _tableFileName.Create() )
                using ( System.IO.BinaryWriter W = new System.IO.BinaryWriter( S ) ) {
                    W.Write( _table.GetLength( 0 ) );
                    W.Write( _table.GetLength( 1 ) );
                    for ( uint Y=0; Y < _table.GetLength( 1 ); Y++ )
                        for ( uint X=0; X < _table.GetLength( 0 ); X++ ) {
                            LTC    ltc = _table[X,Y];
                            if ( ltc == null ) {
                                W.Write( false );
                                continue;
                            }

                            W.Write( true );
                            ltc.Write( W );
                        }
                }

            if ( _errors == null )
                return;    // Nothing to report!

            try {
                System.IO.FileInfo    logFileName = new System.IO.FileInfo( _tableFileName.FullName + ".errorLog" );
                using ( System.IO.TextWriter W = logFileName.CreateText() )
                    W.Write( _errors );
            } catch ( Exception ) {
                // Silently fail logging errors... :/
            }
        }

        #endregion

        #endregion
    }
}
