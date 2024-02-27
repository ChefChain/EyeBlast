namespace UnityEngine.XR.VisionOS
{
    /// <summary>
    /// Error codes for AR World Tracking
    /// </summary>
    enum AR_World_Tracking_Error_Code
    {
        /// <summary>
        /// Error code indicating that adding a world anchor has failed.
        /// </summary>
        ar_world_tracking_error_code_add_anchor_failed = 200,

        /// <summary>
        /// Error code indicating that the maximum amount of world anchors have been added.
        /// </summary>
        ar_world_tracking_error_code_anchor_max_limit_reached = 201,

        /// <summary>
        /// Error code indicating that a world anchor failed to be removed.
        /// </summary>
        ar_world_tracking_error_code_remove_anchor_failed = 202
    }
}
