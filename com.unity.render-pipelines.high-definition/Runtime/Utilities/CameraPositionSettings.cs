using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    /// <summary>Defines how the view matrix is provided to the camera.</summary>
    [Serializable]
    public struct CameraPositionSettings
    {
        /// <summary>Default value.</summary>
        public static readonly CameraPositionSettings @default = new CameraPositionSettings
        {
            mode = Mode.ComputeWorldToCameraMatrix,
            position = Vector3.zero,
            rotation = Quaternion.identity,
            worldToCameraMatrix = Matrix4x4.identity
        };

        /// <summary>Defines the method to use when computing the view matrix.</summary>
        public enum Mode
        {
            /// <summary>
            /// Compute the view matrix from <see cref="position"/> and <see cref="rotation"/> parameters.
            /// </summary>
            ComputeWorldToCameraMatrix,
            /// <summary>Assign the provided <see cref="worldToCameraMatrix"/> matrix.</summary>
            UseWorldToCameraMatrixField
        }

        /// <summary>Which mode to use for computing the view matrix.</summary>
        public Mode mode;

        /// <summary>The world position of the camera during rendering.</summary>
        public Vector3 position;
        /// <summary>The world rotation of the camera during rendering.</summary>
        public Quaternion rotation;

        /// <summary>
        /// The world to camera matrix to use.
        ///
        /// Important: This matrix must be in right-hand standard.
        /// Take care that Unity's transform system follow left-hand standard.
        /// </summary>
        public Matrix4x4 worldToCameraMatrix;

        /// <summary>Compute the worldToCameraMatrix to use during rendering.</summary>
        /// <returns>The worldToCameraMatrix.</returns>
        public Matrix4x4 ComputeWorldToCameraMatrix()
        {
            return GeometryUtils.CalculateWorldToCameraMatrixRHS(position, rotation);
        }

        public Matrix4x4 GetUsedWorldToCameraMatrix()
        {
            switch (mode)
            {
                case Mode.ComputeWorldToCameraMatrix: return ComputeWorldToCameraMatrix();
                case Mode.UseWorldToCameraMatrixField: return worldToCameraMatrix;
                default: throw new ArgumentException();
            }
        }
    }
}
