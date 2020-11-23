using System;
using UnityEngine;

namespace UnityEditor
{
    /// <summary>
    /// Generic Control
    /// </summary>
    public class GenericControl : Control
    {
        /// <summary>
        /// Func for enable
        /// </summary>
        public Func<IGUIState, bool> enable;
        /// <summary>
        /// Func for OnBeginLayout
        /// </summary>
        public Func<IGUIState, LayoutData> onBeginLayout;
        /// <summary>
        /// Action for OnEndLayout
        /// </summary>
        public Action<IGUIState> onEndLayout;
        /// <summary>
        /// Action for OnRepaint
        /// </summary>
        public Action<IGUIState, Control, int> onRepaint;
        /// <summary>
        /// Func for GetCount
        /// </summary>
        public Func<int> getCount;
        /// <summary>
        /// Func for GetPosition
        /// </summary>
        public Func<int, Vector3> getPosition;
        /// <summary>
        /// Func for GetDistance
        /// </summary>
        public Func<IGUIState, int, float> getDistance;
        /// <summary>
        /// Func for GetForward
        /// </summary>
        public Func<int, Vector3> getForward;
        /// <summary>
        /// Func for GetUp
        /// </summary>
        public Func<int, Vector3> getUp;
        /// <summary>
        /// Func for GetRight
        /// </summary>
        public Func<int, Vector3> getRight;
        /// <summary>
        /// Func for GetUserData
        /// </summary>
        public Func<int, object> getUserData;

        /// <summary>
        /// Initializes and returns an instance of GenericControl
        /// </summary>
        /// <param name="name">Name for the Generic Control</param>
        public GenericControl(string name) : base(name)
        {
        }

        /// <summary>
        /// Override for GetEnable
        /// </summary>
        /// <param name="guiState">The GUI State</param>
        /// <returns>Is Enabled</returns>
        protected override bool GetEnabled(IGUIState guiState)
        {
            if (enable != null)
                return enable(guiState);

            return base.GetEnabled(guiState);
        }

        /// <summary>
        /// Get Count
        /// </summary>
        /// <returns>Count</returns>
        protected override int GetCount()
        {
            if (getCount != null)
                return getCount();

            return base.GetCount();
        }

        /// <summary>
        /// Override for OnEndLayout
        /// </summary>
        /// <param name="guiState">The GUI State</param>
        protected override void OnEndLayout(IGUIState guiState)
        {
            if (onEndLayout != null)
                onEndLayout(guiState);
        }

        /// <summary>
        /// Override for OnRepaint
        /// </summary>
        /// <param name="guiState">The GUI State</param>
        /// <param name="index">Current Index</param>
        protected override void OnRepaint(IGUIState guiState, int index)
        {
            if (onRepaint != null)
                onRepaint(guiState, this, index);
        }

        /// <summary>
        /// Ovveride OnBeginLayout
        /// </summary>
        /// <param name="data">The LayoutData</param>
        /// <param name="guiState">The GUI State</param>
        /// <returns>The LayoutData</returns>
        protected override LayoutData OnBeginLayout(LayoutData data, IGUIState guiState)
        {
            if (onBeginLayout != null)
                return onBeginLayout(guiState);

            return data;
        }

        /// <summary>
        /// Override for GetPosition
        /// </summary>
        /// <param name="guiState">The GUI State</param>
        /// <param name="index">The Index</param>
        /// <returns>The position</returns>
        protected override Vector3 GetPosition(IGUIState guiState, int index)
        {
            if (getPosition != null)
                return getPosition(index);

            return base.GetPosition(guiState, index);
        }

        /// <summary>
        /// Override for GetDistance
        /// </summary>
        /// <param name="guiState">The GUI State</param>
        /// <param name="index">The Index</param>
        /// <returns>The distance</returns>
        protected override float GetDistance(IGUIState guiState, int index)
        {
            if (getDistance != null)
                return getDistance(guiState, index);

            return base.GetDistance(guiState, index);
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
                return getForward(index);

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
                return getUp(index);

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
                return getRight(index);

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
                return getUserData(index);

            return base.GetUserData(guiState, index);
        }
    }
}
