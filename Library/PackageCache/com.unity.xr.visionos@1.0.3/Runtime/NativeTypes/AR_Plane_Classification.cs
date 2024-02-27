namespace UnityEngine.XR.VisionOS
{
    /// <summary>
    /// A value describing the classification of a plane anchor.
    /// </summary>
    enum AR_Plane_Classification
    {
        /// <summary>
        /// Plane classification is currently unavailable.
        /// </summary>
        Status_not_available = 0,

        /// <summary>
        /// The classification of the plane has not yet been determined.
        /// </summary>
        Status_undetermined,

        /// <summary>
        /// The plane classification is not any of the known classes.
        /// </summary>
        Status_unknown,

        /// <summary>
        /// The classification is of type wall.
        /// </summary>
        Wall,

        /// <summary>
        /// The classification is of type floor.
        /// </summary>
        Floor,

        /// <summary>
        /// The classification is of type ceiling.
        /// </summary>
        Ceiling,

        /// <summary>
        /// The classification is of type table.
        /// </summary>
        Table,

        /// <summary>
        /// The classification is of type seat.
        /// </summary>
        Seat,

        /// <summary>
        /// The classification is of type window.
        /// </summary>
        Window,

        /// <summary>
        /// The classification is of type door.
        /// </summary>
        Door
    }
}
