namespace UnityEngine.XR.VisionOS
{
    /// <summary>
    /// Status of an authorization.
    /// </summary>
    enum AR_Authorization_Status
    {
        /// <summary>
        /// The user has not yet granted permission.
        /// </summary>
        ar_authorization_status_not_determined,

        /// <summary>
        /// The user has explicitly granted permission.
        /// </summary>
        ar_authorization_status_allowed,

        /// <summary>
        /// The user has explicitly denied permission.
        /// </summary>
        ar_authorization_status_denied
    }
}
