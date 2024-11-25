namespace UnityEngine.Rendering
{
    /// <summary>
    /// User API to request access for an instance of the user history type.
    /// Tracks the history types that were requested by the render pipeline features on this frame.
    /// Requested history types are then made available for the future frames.
    /// Request is active for one frame only and a new request should be made every frame.
    /// Types that were not requested are eventually reset and GPU resources released.
    /// </summary>
    public interface IPerFrameHistoryAccessTracker
    {
        /// <summary>
        /// Mark a certain history texture type (class) as a requirement for next frame.
        /// Note: Requesting a history doesn't mean it will be actually available.
        ///       E.g. The first frame doesn't have any history data available at all.
        /// </summary>
        /// <typeparam name="Type">Type of the history instance.</typeparam>
        public void RequestAccess<Type>() where Type : ContextItem;
    }

    /// <summary>
    /// User API to get history write access for a user history type instance.
    /// Write access is valid and available after the history type has been requested.
    /// Otherwise a null is returned.
    /// Typically called by the history type producer render pass in the render pipeline.
    /// </summary>
    public interface ICameraHistoryWriteAccess
    {
        /// <summary>
        /// Check if a type has been requested and should be written this frame.
        /// </summary>
        /// <typeparam name="Type">Type of the history instance.</typeparam>
        /// <returns>True if a type has been requested earlier. False otherwise.</returns>
        public bool IsAccessRequested<Type>() where Type : ContextItem;

        /// <summary>
        /// Get write access to an instance of certain history type.
        /// It is expected that the caller will filling the contents of the type textures.
        /// Null if not requested beforehand.
        /// On first get of a type, the type instance is created.
        /// </summary>
        /// <typeparam name="Type">Type of the history instance.</typeparam>
        /// <returns>True if a type has been requested earlier. False otherwise.</returns>
        public Type GetHistoryForWrite<Type>() where Type : ContextItem, new();

        /// <summary>
        /// Check if a type was already written this frame by some render pass.
        /// </summary>
        /// <typeparam name="Type">Type of the history instance.</typeparam>
        /// <returns>True if a type has been written earlier. False otherwise.</returns>
        public bool IsWritten<Type>()  where Type : ContextItem;
    }

    /// <summary>
    /// User API to get history read access for a user history type instance.
    /// Read access is valid and available after the history type has been requested and written by a render pass.
    /// Otherwise a null is returned.
    /// Typically called by the history type consumer render pass in the render pipeline.
    ///
    /// User API for external systems to register history read access callbacks.
    /// </summary>
    public interface ICameraHistoryReadAccess
    {
        /// <summary>
        /// Get read access to an instance of certain history type.
        /// Available only if the type instance has been requested and written earlier.
        /// </summary>
        /// <typeparam name="Type">Type of the history instance.</typeparam>
        /// <returns>A class instance of Type. Null if not available on this frame.</returns>
        // Get a certain history item from the camera or null if not available this frame.
        public Type GetHistoryForRead<Type>() where Type : ContextItem;

        /// <summary>
        /// Callback type for requesting various history type instances for read.
        /// Typically used by systems external to the pipeline.
        /// For example: A MonoBehavior requesting access for MonoBehavior.LateUpdate() call.
        /// </summary>
        /// <param name="historyAccess">A container for history type requests.</param>
        public delegate void HistoryRequestDelegate(IPerFrameHistoryAccessTracker historyAccess);

        /// <summary>
        /// A callback event used to register a callback for requesting history types.
        /// </summary>
        public event HistoryRequestDelegate OnGatherHistoryRequests;
    }

    /// <summary>
    /// A convenience base class for camera history items/types.
    /// It is recommended to derive from this class to make new history item type.
    ///
    /// The owning camera BufferedRTHandleSystem reference is used for central storage.
    /// The central storage allows the camera to track all of the history types in a single place.
    /// And gives the deriving type a direct access to texture allocation services.
    /// Type id is used to deconflict RTHandle ids from different types.
    ///
    /// The user is responsible for designing the derived type to work well with the
    /// producing and consuming render passes.
    /// For example:
    /// Add the necessary cpu-side tracking data and update logic.
    /// Add methods for accessing the history data and design a suitable API for the type.
    /// Handle allocation and deallocation of the history texture RTHandles etc.
    /// </summary>
    public abstract class CameraHistoryItem : ContextItem
    {
        // BufferedRTHandleSystem of the owning camera.
        private BufferedRTHandleSystem m_owner = null;
        // Unique id for this type (derived) given by the owning camera.
        private uint m_TypeId = uint.MaxValue;

        /// <summary>
        /// Called internally when a CameraHistoryItem type is created to initialize the RTHandle storage and type id.
        ///
        /// User types can override to do additional initialization, such as creating the ids for multiple history RTHandles.
        /// Deriving type should call the base.OnCreate() to correctly initialize the CameraHistoryItem first.
        /// </summary>
        /// <param name="owner">BufferedRTHandleSystem of the owning camera.</param>
        /// <param name="typeId">Unique id given to this class type by the owning camera.</param>
        public virtual void OnCreate(BufferedRTHandleSystem owner, uint typeId)
        {
            m_owner = owner;
            m_TypeId = typeId;
        }

        // The user API is protected, so that the BufferedRTHandleSystem is visible only for the custom Type implementation.

        /// <summary>
        /// The owning camera RTHandle storage for the history textures.
        /// </summary>
        protected BufferedRTHandleSystem storage => m_owner;

        /// <summary>
        /// Creates unique ids for the RTHandle storage.
        /// Index == 0, returns the TypeId of this CameraHistoryItem.
        /// Index == N, generates new ids in case the user wants to store multiple history textures in the same CameraHistoryItem.
        /// </summary>
        /// <param name="index">Index of the type RTHandle, a type local Enum or a user id.</param>
        /// <returns>A unique id for each type, index and camera.</returns>
        protected int MakeId(uint index)
        {
            return (int)(((m_TypeId & 0xFFFF) << 16) | (index & 0xFFFF));
        }

        /// <summary>
        /// Allocate a history frame RTHandle[] using a descriptor.
        /// </summary>
        /// <param name="id">Id for the history RTHandle storage.</param>
        /// <param name="count">Number of RTHandles allocated for the id.</param>
        /// <param name="desc">Texture descriptor used for each RTHandle in the allocation.</param>
        /// <param name="name">User visible debug name of the texture.</param>
        /// <returns>Current frame RTHandle in the allocation.</returns>
        protected RTHandle AllocHistoryFrameRT(int id, int count,
            ref RenderTextureDescriptor desc, string name = "")
        {
            // Simplified for typical history textures:
            // Sampling is usually bilinear & clamp. Point sample can be a texture.Load() or done with inline samplers.
            return AllocHistoryFrameRT(id, count, ref desc, FilterMode.Bilinear, name);
        }

        /// <summary>
        /// Allocate a history frame RTHandle[] using a descriptor.
        /// </summary>
        /// <param name="id">Id for the history RTHandle storage.</param>
        /// <param name="count">Number of RTHandles allocated for the id.</param>
        /// <param name="desc">Texture descriptor used for each RTHandle in the allocation.</param>
        /// <param name="filterMode">Filtering mode of the texture.</param>
        /// <param name="name">User visible debug name of the texture.</param>
        /// <returns>Current frame RTHandle in the allocation.</returns>
        protected RTHandle AllocHistoryFrameRT(int id, int count,
            ref RenderTextureDescriptor desc,
            FilterMode filterMode,
            string name = "")
        {
            RenderTextureDescriptor d = desc;
            // Simplified for typical history textures:
            // No shadows, no mipmaps, no aniso.
            m_owner.AllocBuffer(id, count, ref desc, filterMode, TextureWrapMode.Clamp, false, 0, 0, name);
            return GetCurrentFrameRT(0);
        }

        /// <summary>
        /// Release the RTHandles allocated for the id.
        /// </summary>
        /// <param name="id">Id for the history RTHandle storage.</param>
        protected void ReleaseHistoryFrameRT(int id)
        {
            m_owner.ReleaseBuffer(id);
        }

        /// <summary>
        /// Returns the id RTHandle from the previous frame.
        /// </summary>
        /// <param name="id">Id for the history RTHandle storage.</param>
        /// <returns>The RTHandle from previous frame.</returns>
        protected RTHandle GetPreviousFrameRT(int id)
        {
            return m_owner.GetFrameRT(id, 1);
        }

        /// <summary>
        /// Returns the id RTHandle of the current frame.
        /// </summary>
        /// <param name="id">Id for the history RTHandle storage.</param>
        /// <returns>The RTHandle of the current frame.</returns>
        protected RTHandle GetCurrentFrameRT(int id)
        {
            return m_owner.GetFrameRT(id, 0);
        }
    }
}
