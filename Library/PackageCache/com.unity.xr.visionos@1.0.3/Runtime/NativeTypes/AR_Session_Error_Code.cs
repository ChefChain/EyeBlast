namespace UnityEngine.XR.VisionOS
{
    /// <summary>
    /// Error codes for AR Session
    /// </summary>
    enum AR_Session_Error_Code
    {
        /// <summary>
        /// Error code indicating that a data provider requires an authorization that has not been granted by the user.
        /// </summary>
        ar_session_error_code_data_provider_not_authorized = 100,

        /// <summary>
        /// Error code indicating the data provider has failed to run.
        /// </summary>
        ar_session_error_code_data_provider_failed_to_run = 101
    }
}
