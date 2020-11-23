using System;
using UnityEngine;

namespace UnityEditor
{
    /// <summary>
    /// GenericDefaultControl
    /// </summary>
    public class GenericDefaultControl : DefaultControl
    {
        /// <summary>
        /// Func for GetEnable
        /// </summary>
        public Func<IGUIState, bool> getEnable;
        /// <summary>
        /// Func for GetPosition
        /// </summary>
        public Func<IGUIState, Vector3> getPosition;
        /// <summary>
        /// Func for GetForward
        /// </summary>
        public Func<IGUIState, Vector3> getForward;
        /// <summary>
        /// Func for GetUp
        /// </summary>
        public Func<IGUIState, Vector3> getUp;
        /// <summary>
        /// Func for GetRight
        /// </summary>
        public Func<IGUIState, Vector3> getRight;
        /// <summary>
        /// Func for GetUserData
        /// </summary>
        public Func<IGUIState, object> getUserData;

        /// <summary>
        /// Initializes and returns an instance of GenericDefaultControl
        /// </summary>
        /// <param name="name">Name of the GenericDefaultControl</param>
        public GenericDefaultControl(string name) : base(name)
        {
        }

        /// <summary>
        /// Test if enabled
        /// </summary>
        /// <param name="guiState">The GUI State</param>
        /// <returns>true if enabled</returns>
        protected override bool GetEnabled(IGUIState guiState)
        {
            if (getEnable != null)
                return getEnable(guiState);

            return base.GetEnabled(guiState);
        }

        /// <summary>
        /// Override for GetDistance
        /// </summary>
        /// <param name="guiState">The GUI State</param>
        /// <param name="index">The Index</param>
        /// <returns>The distance</returns>
        protected override Vector3 GetPosition(IGUIState guiState, int index)
        {
            if (getPosition != null)
                return getPosition(guiState);

            return base.GetPosition(guiState, index);
        }

        /// <summary>
        /// Override for GetForward
        /// </summary>
        /// <param name="guiState">The GUI State</param>
        /// <param name="index">The Index</param>
        /// <returns>Forward Vector</returns>
        protected override Vector3 GetForward(IGUIState guiState, int index)
        {
            if (getForward != null)
                return getForward(guiState);

            return base.GetForward(guiState, index);
        }

        /// <summary>
        /// Override for GetUp
        /// </summary>
        /// <param name="guiState">The GUI State</param>
        /// <param name="index">The Index</param>
        /// <returns>Up Vector</returns>
        protected override Vector3 GetUp(IGUIState guiState, int index)
        {
            if (getUp != null)
                return getUp(guiState);

            return base.GetUp(guiState, index);
        }

        /// <summary>
        /// Override for GetRight
        /// </summary>
        /// <param name="guiState">The GUI State</param>
        /// <param name="index">The Index</param>
        /// <returns>Right Vector</returns>
        protected override Vector3 GetRight(IGUIState guiState, int index)
        {
            if (getRight != null)
                return getRight(guiState);

            return base.GetRight(guiState, index);
        }

        /// <summary>
        /// Override for GetUserData
        /// </summary>
        /// <param name="guiState">The GUI State</param>
        /// <param name="index">The Index</param>
        /// <returns>Return the user data</returns>
        protected override object GetUserData(IGUIState guiState, int index)
        {
            if (getUserData != null)
                return getUserData(guiState);

            return base.GetUserData(guiState, index);
        }
    }
}
