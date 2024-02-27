using System;
using System.Runtime.InteropServices;
using AOT;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.XR.ARSubsystems;

namespace UnityEngine.XR.VisionOS
{
    /// <summary>
    /// VisionOS implementation of the <c>XRSessionSubsystem</c>. Do not create this directly. Use the <c>SubsystemManager</c> instead.
    /// </summary>
    [Preserve]
    public sealed class VisionOSSessionSubsystem : XRSessionSubsystem
    {
        internal const string sessionSubsystemId = "VisionOS-Session";

        internal class VisionOSSessionProvider : Provider
        {
            // Must match UnityXRNativeSession and kUnityXRNativeSessionVersion in UnityXRNativePtrs.h
            // ReSharper disable NotAccessedField.Local
            struct XRNativeSession
            {
                public int version;
                public IntPtr sessionPtr;
            }
            // ReSharper restore NotAccessedField.Local

            // ReSharper disable once InconsistentNaming
            const int kUnityXRNativeSessionVersion = 1;

            // TODO: How to get back to instance from callbacks?
            // TODO: How to connect subsystems together without singleton?
            static readonly NativeApi.Session.AR_Session_Data_Provider_State_Change_Handler k_DataProviderStateChangeHandler = DataProviderStateChangeHandler;
            static readonly NativeApi.Session.AR_Authorization_Update_Handler k_AuthorizationUpdateHandler = AuthorizationUpdateHandler;
            static readonly NativeApi.Session.AR_Authorization_Results_Handler k_QueryAuthorizationResultsHandler = QueryAuthorizationResultsHandler;
            static readonly NativeApi.Authorization.Authorization_Results_Enumeration_Completed_Callback k_QueryAuthorizationResultsHanEnumerationCompletedCallback = QueryAuthorizationResultsEnumerationCompletedCallback;
            static readonly NativeApi.Session.AR_Authorization_Results_Handler k_RequestAuthorizationResultsHandler = RequestAuthorizationResultsHandler;
            static readonly NativeApi.Authorization.Authorization_Results_Enumeration_Completed_Callback k_RequestAuthorizationResultsHanEnumerationCompletedCallback = RequestAuthorizationResultsEnumerationCompletedCallback;
            static readonly NativeApi.Authorization.Authorization_Results_Enumeration_Step_Callback k_EnumerateAuthorizationStepCallback = EnumerateAuthorizationStepCallback;

            const float k_StartupTimeout = 5;

#if UNITY_EDITOR
            const float k_SimulatedStartupDelay = 3;
#endif

            const Feature k_MeshingFeature = Feature.Meshing;
            const Feature k_WorldTrackingFeature = Feature.PositionAndRotation;

            // Store last update so we can update m_StartTime in callbacks coming from other threads
            static float s_LastUpdateTime;

            public static VisionOSSessionProvider Instance { get; private set; }

            readonly IntPtr m_ARSession;
            GCHandle m_NativeSessionHandle;
            IntPtr m_ProviderCollection;

            bool m_ProviderStateCallbackReceived;
            float m_StartTime = Mathf.Infinity;
            Configuration? m_CurrentConfiguration;
            Feature m_RequestedFeatures;
            AR_Scene_Reconstruction_Mode m_SceneReconstructionMode;
            bool m_TimeoutWarningRaised;
            bool m_RestartRequested;
            bool m_AwaitingAuthorization;
            AR_Authorization_Type m_AllowedAuthorizations = AR_Authorization_Type.None;
            AR_Authorization_Type m_DeniedAuthorizations = AR_Authorization_Type.None;
            AR_Authorization_Type m_RequestedAuthorizations = AR_Authorization_Type.None;

            readonly VisionOSMeshProvider m_MeshProvider = new();
            readonly VisionOSWorldTrackingProvider m_WorldTrackingProvider = new();

            public override Feature requestedFeatures => m_RequestedFeatures;

            public override IntPtr nativePtr { get; } = IntPtr.Zero;

            public VisionOSSessionProvider()
            {
                Instance = this;
                m_ARSession = NativeApi.Session.ar_session_create();
                var nativeSession = new XRNativeSession
                {
                    version = kUnityXRNativeSessionVersion,
                    sessionPtr = m_ARSession
                };

                m_NativeSessionHandle = GCHandle.Alloc(nativeSession, GCHandleType.Pinned);
                nativePtr = GCHandle.ToIntPtr(m_NativeSessionHandle);

                NativeApi.Session.UnityVisionOS_impl_ar_session_set_data_provider_state_change_handler(m_ARSession, k_DataProviderStateChangeHandler);
                NativeApi.Session.UnityVisionOS_impl_ar_session_set_authorization_update_handler(m_ARSession, k_AuthorizationUpdateHandler);

                VisionOSProviderRegistration.RegisterProvider(k_MeshingFeature, m_MeshProvider);
                VisionOSProviderRegistration.RegisterProvider(k_WorldTrackingFeature, m_WorldTrackingProvider);
            }

            public override void Start()
            {
                m_StartTime = Time.realtimeSinceStartup;
                m_TimeoutWarningRaised = false;
            }

            public override void Update(XRSessionUpdateParams updateParams, Configuration configuration)
            {
                s_LastUpdateTime = Time.realtimeSinceStartup;

                // Check if authorization needs to be requested
                if (!CheckAuthorization())
                    return;

                if (!m_RestartRequested && m_CurrentConfiguration.HasValue && m_CurrentConfiguration == configuration)
                    return;

                if (!m_ProviderStateCallbackReceived && !m_TimeoutWarningRaised && s_LastUpdateTime - k_StartupTimeout > m_StartTime)
                {
                    m_TimeoutWarningRaised = true;
                    Debug.LogWarning($"AR session failed to start after {k_StartupTimeout} seconds. Is the app configured to use an immersive space?");
                }

                if (!VisionOS.IsImmersiveSpaceReady())
                    return;

                m_RestartRequested = false;

#if UNITY_EDITOR

                // No callback for mac stub, so assume session started successfully after a short delay
                if (s_LastUpdateTime - k_SimulatedStartupDelay < m_StartTime)
                    return;

                m_ProviderStateCallbackReceived = true;
#endif

                if (m_CurrentConfiguration.HasValue)
                    NativeApi.Session.ar_session_stop(m_ARSession);

                var features = configuration.features;
                m_ProviderCollection = NativeApi.DataProvider.ar_data_providers_create();
                foreach (var kvp in VisionOSProviderRegistration.EnumerateProviders())
                {
                    // Do not attempt to create provider if not supported
                    var provider = kvp.Value;
                    if (!provider.IsSupported)
                        continue;

                    // Do not create provider if feature is not requested
                    var feature = kvp.Key;
                    if ((feature & features) == 0)
                        continue;

                    // Do not create provider if authorization is not allowed
                    var requiredAuthorization = provider.RequiredAuthorizationType;
                    var hasRequiredAuthorization = requiredAuthorization != AR_Authorization_Type.None;
                    if (hasRequiredAuthorization && (m_AllowedAuthorizations & requiredAuthorization) == 0)
                        continue;

                    if (!provider.TryCreateNativeProvider(features, out var providerPointer))
                    {
                        Debug.LogError($"Failed to create provider for {feature}");
                        continue;
                    }

                    Debug.Log($"Adding AR data provider for feature {feature}");
                    NativeApi.DataProvider.ar_data_providers_add_data_provider(m_ProviderCollection, providerPointer);
                }

                m_CurrentConfiguration = configuration;

                Debug.Log("Running AR session.");
                NativeApi.Session.ar_session_run(m_ARSession, m_ProviderCollection);
            }

            void CalculateFeaturesAndAuthorizations()
            {
                m_RequestedAuthorizations = AR_Authorization_Type.None;
                m_RequestedFeatures = Feature.None;
                foreach (var kvp in VisionOSProviderRegistration.EnumerateProviders())
                {
                    var provider = kvp.Value;
                    if (!provider.ShouldBeActive)
                        continue;

                    m_RequestedFeatures |= kvp.Key;
                    m_RequestedAuthorizations |= provider.RequiredAuthorizationType;
                }

                if (m_SceneReconstructionMode == AR_Scene_Reconstruction_Mode.Classification)
                    m_RequestedFeatures |= Feature.MeshClassification;
            }

            bool CheckAuthorization()
            {
                // TODO: Error handling or retry count to prevent constantly spamming authorization request
                if (!m_AwaitingAuthorization && NeedAuthorizationRequest())
                {
                    if (VisionOS.IsSimulator())
                    {
                        // Skip authorization in the simulator, since authorization API does not call callbacks
                        m_AllowedAuthorizations = m_RequestedAuthorizations;
                    }
                    else
                    {
                        m_AwaitingAuthorization = true;
                    }

                    NativeApi.Session.UnityVisionOS_impl_ar_session_query_authorization_results(m_ARSession, m_RequestedAuthorizations, k_QueryAuthorizationResultsHandler);
                }

                return !m_AwaitingAuthorization;
            }

            bool NeedAuthorizationRequest()
            {
                // Request authorization if anything we want is not already determined
                var determinedAuthorizations = m_AllowedAuthorizations | m_DeniedAuthorizations;
                return (m_RequestedAuthorizations & ~determinedAuthorizations) != 0;
            }

            public override void Stop()
            {
                m_ProviderStateCallbackReceived = false;
                NativeApi.Session.ar_session_stop(m_ARSession);
                m_CurrentConfiguration = null;
            }

            public override void Destroy()
            {
                m_NativeSessionHandle.Free();
            }

            public override void OnApplicationResume()
            {
                if (m_ProviderStateCallbackReceived)
                {
                    // Restart session, which was paused by the system
                    Debug.Log("Resuming AR session after pause.");
                    NativeApi.Session.ar_session_run(m_ARSession, m_ProviderCollection);
                }
            }

            public override Promise<SessionAvailability> GetAvailabilityAsync() => Promise<SessionAvailability>.CreateResolvedPromise(SessionAvailability.Supported | SessionAvailability.Installed);

            // TODO: Check world tracking provider status
            public override TrackingState trackingState => m_ProviderStateCallbackReceived ? TrackingState.Tracking : TrackingState.None;

            public override NativeArray<ConfigurationDescriptor> GetConfigurationDescriptors(Allocator allocator)
            {
                CalculateFeaturesAndAuthorizations();

                var descriptors = new NativeArray<ConfigurationDescriptor>(1, allocator);
                var descriptor = new ConfigurationDescriptor(IntPtr.Zero, m_RequestedFeatures, 0);
                descriptors[0] = descriptor;
                return descriptors;
            }

            public void RequestRestart()
            {
                m_RestartRequested = true;
            }

            // ReSharper disable InconsistentNaming
            [MonoPInvokeCallback(typeof(NativeApi.Session.AR_Session_Data_Provider_State_Change_Handler))]
            static void DataProviderStateChangeHandler(IntPtr data_providers, AR_Data_Provider_State new_state, IntPtr error, IntPtr failed_data_provider)
            {
                if (failed_data_provider != IntPtr.Zero || error != IntPtr.Zero)
                {
                    var failedProviderFeature = Feature.None;
                    foreach (var kvp in VisionOSProviderRegistration.EnumerateProviders())
                    {
                        if (kvp.Value.CurrentProvider == failed_data_provider)
                        {
                            failedProviderFeature = kvp.Key;
                            break;
                        }
                    }

                    var errorCodeString = "Unknown";
                    if (error != IntPtr.Zero)
                    {
                        var errorCode = (AR_Session_Error_Code)NativeApi.Error.ar_error_get_error_code(error);
                        errorCodeString = errorCode.ToString();
                    }

                    var failedProviderName = failedProviderFeature == Feature.None ? "Unknown" : failedProviderFeature.ToString();
                    Debug.LogError($"Error in AR data provider state change handler. Error: {errorCodeString}, Failed provider: {failedProviderName}.");
                    return;
                }

                Debug.Log($"AR data provider state changed. New state is {new_state}.");

                Instance.m_ProviderStateCallbackReceived = true;
            }

            // ReSharper restore InconsistentNaming

            [MonoPInvokeCallback(typeof(NativeApi.Session.AR_Authorization_Update_Handler))]
            static void AuthorizationUpdateHandler(IntPtr authorizationResult)
            {
                if (Instance == null)
                {
                    Debug.LogError("Received AR authorization update without a provider instance.");
                    return;
                }

                Debug.Log("Received AR authorization update");

                // Assume authorization has changed, and data providers will be stopped
                Instance.RequestRestart();
                Instance.ProcessAuthorizationResult(authorizationResult);
            }

            void ProcessAuthorizationResult(IntPtr authorizationResult)
            {
                var resultType = NativeApi.Authorization.ar_authorization_result_get_authorization_type(authorizationResult);
                var resultStatus = NativeApi.Authorization.ar_authorization_result_get_status(authorizationResult);
                Debug.Log($"AR authorization result - Type: {resultType} Status: {resultStatus}");

                switch (resultStatus)
                {
                    case AR_Authorization_Status.ar_authorization_status_not_determined:
                        // Do nothing--we will request authorizations as needed
                        break;
                    case AR_Authorization_Status.ar_authorization_status_allowed:
                        m_AllowedAuthorizations |= resultType;
                        break;
                    case AR_Authorization_Status.ar_authorization_status_denied:
                        m_DeniedAuthorizations |= resultType;
                        break;
                    default:
                        Debug.LogError($"Unknown AR Authorization Status {resultStatus}");
                        break;
                }
            }

            [MonoPInvokeCallback(typeof(NativeApi.Session.AR_Authorization_Results_Handler))]
            static void QueryAuthorizationResultsHandler(IntPtr authorizationResults, IntPtr error)
            {
                if (error != IntPtr.Zero)
                {
                    var errorCode = NativeApi.Error.ar_error_get_error_code(error);
                    Debug.LogError($"Error trying to query AR authorization. Error code: {errorCode}");

                    // TODO: Not sure how to interpret error code--just log it for now
                }

                if (Instance == null)
                {
                    Debug.LogError("Received AR authorization query result without a provider instance.");
                    return;
                }

                NativeApi.Authorization.UnityVisionOS_impl_ar_authorization_results_enumerate_results(authorizationResults,
                    k_EnumerateAuthorizationStepCallback, k_QueryAuthorizationResultsHanEnumerationCompletedCallback);
            }

            [MonoPInvokeCallback(typeof(NativeApi.Authorization.Authorization_Results_Enumeration_Completed_Callback))]
            static void QueryAuthorizationResultsEnumerationCompletedCallback()
            {
                Instance.RequestAuthorizationIfNeeded();
            }

            void RequestAuthorizationIfNeeded()
            {
                if (NeedAuthorizationRequest())
                {
                    Debug.Log($"New AR authorization required. Requesting authorization for {m_RequestedAuthorizations}.");
                    NativeApi.Session.UnityVisionOS_impl_ar_session_request_authorization(m_ARSession, m_RequestedAuthorizations, k_RequestAuthorizationResultsHandler);
                }
                else
                {
                    Debug.Log("AR authorization query completed. No new authorization required.");
                    m_AwaitingAuthorization = false;
                }

                m_StartTime = s_LastUpdateTime;
            }

            [MonoPInvokeCallback(typeof(NativeApi.Session.AR_Authorization_Results_Handler))]
            static void RequestAuthorizationResultsHandler(IntPtr authorizationResults, IntPtr error)
            {
                if (error != IntPtr.Zero)
                {
                    var errorCode = NativeApi.Error.ar_error_get_error_code(error);
                    Debug.LogError($"Error trying to request AR authorization. Error code: {errorCode}");

                    // TODO: Not sure how to interpret error code--just log it for now
                }

                if (Instance == null)
                {
                    Debug.LogError("Received AR authorization request result without a provider instance.");
                    return;
                }

                NativeApi.Authorization.UnityVisionOS_impl_ar_authorization_results_enumerate_results(authorizationResults,
                    k_EnumerateAuthorizationStepCallback, k_RequestAuthorizationResultsHanEnumerationCompletedCallback);
            }

            [MonoPInvokeCallback(typeof(NativeApi.Authorization.Authorization_Results_Enumeration_Completed_Callback))]
            static void RequestAuthorizationResultsEnumerationCompletedCallback()
            {
                Instance.m_AwaitingAuthorization = false;
                Instance.m_StartTime = s_LastUpdateTime;
            }

            [MonoPInvokeCallback(typeof(NativeApi.Authorization.Authorization_Results_Enumeration_Step_Callback))]
            static void EnumerateAuthorizationStepCallback(IntPtr authorizationResult)
            {
                Instance.ProcessAuthorizationResult(authorizationResult);
            }

            public AR_Scene_Reconstruction_Mode GetSceneReconstructionMode()
            {
                return m_SceneReconstructionMode;
            }

            public void SetSceneReconstructionMode(AR_Scene_Reconstruction_Mode mode)
            {
                m_SceneReconstructionMode = mode;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void RegisterDescriptor()
        {
            XRSessionSubsystemDescriptor.RegisterDescriptor(new XRSessionSubsystemDescriptor.Cinfo
            {
                id = sessionSubsystemId,
                providerType = typeof(VisionOSSessionProvider),
                subsystemTypeOverride = typeof(VisionOSSessionSubsystem),
                supportsInstall = false,
                supportsMatchFrameRate = false
            });
        }
    }
}
