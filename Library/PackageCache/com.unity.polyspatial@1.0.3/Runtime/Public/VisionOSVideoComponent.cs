using Unity.PolySpatial.Internals;
using UnityEngine;
using UnityEngine.Video;

namespace Unity.PolySpatial
{
    /// <summary>
    /// A custom component to provide video capabilities to a target platform.
    /// </summary>
    public class VisionOSVideoComponent : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The mesh renderer that this video clip will be applied to.")]
        MeshRenderer m_TargetMaterialRenderer;
        public MeshRenderer targetMaterialRenderer
        {
            get => m_TargetMaterialRenderer;
            set
            {
                m_TargetMaterialRenderer = value;
                ObjectBridge.MarkDirty(this);
            }
        }

        [SerializeField]
        [Tooltip("Video clip to be used.")]
        VideoClip m_Clip;
        public VideoClip clip
        {
            get => m_Clip;
            set
            {
                m_Clip = value;
                ObjectBridge.MarkDirty(this);
            }
        }

        [SerializeField]
        [Tooltip("Whether video player should loop this clip.")]
        bool m_IsLooping = true;
        public bool isLooping
        {
            get => m_IsLooping;
            set
            {
                m_IsLooping = value;
                ObjectBridge.MarkDirty(this);
            }
        }

        [SerializeField]
        [Tooltip("Whether video clip should play on awake.")]
        bool m_PlayOnAwake = true;
        public bool playOnAwake
        {
            get => m_PlayOnAwake;
            set
            {
                m_PlayOnAwake = value;
                ObjectBridge.MarkDirty(this);
            }
        }

        [SerializeField]
        [Tooltip("Mute status of first track on the video clip. Multiple tracks currently not supported")]
        bool m_Mute = false;

        [SerializeField]
        [Tooltip("Volume of the first track on the video clip. Multiple tracks currently not supported.")]
        [Range(0.0F, 1.0F)]
        float m_Volume = 1.0f;

        /// <summary>
        /// Enum defining the current playing state of the
        /// video player.
        /// </summary>
        public enum PlayerState: int
        {
            /// <summary>
            /// The video player is currently playing.
            /// </summary>
            IsPlaying,

            /// <summary>
            /// The video player is currently stopped.
            /// </summary>
            IsStopped,

            /// <summary>
            /// The video player is currently paused.
            /// </summary>
            IsPaused
        }

        PlayerState m_State = PlayerState.IsStopped;

        void Start()
        {
            if (playOnAwake)
                m_State = PlayerState.IsPlaying;
        }

        public PlayerState GetState()
        {
            return m_State;
        }

        public void Play()
        {
            if (m_State != PlayerState.IsPlaying)
            {
                m_State = PlayerState.IsPlaying;
                this.MarkDirty();
            }
        }

        public void Stop()
        {
            if (m_State != PlayerState.IsStopped)
            {
                m_State = PlayerState.IsStopped;
                this.MarkDirty();
            }

        }

        public void Pause()
        {
            if (m_State != PlayerState.IsPaused)
            {
                m_State = PlayerState.IsPaused;
                this.MarkDirty();
            }
        }

        public bool GetDirectAudioMute(ushort trackIndex)
        {
            if (trackIndex > 0)
                Debug.LogWarning("trackIndices greater than 0 currently not supported.");

            return m_Mute;
        }

        public void SetDirectAudioMute(ushort trackIndex, bool mute)
        {
            if (trackIndex > 0)
                Debug.LogWarning("trackIndices greater than 0 currently not supported.");

            m_Mute = mute;
            this.MarkDirty();
        }

        public float GetDirectAudioVolume(ushort trackIndex)
        {
            if (trackIndex > 0)
                Debug.LogWarning("trackIndices greater than 0 currently not supported.");

            return m_Volume;
        }

        public void SetDirectAudioVolume(ushort trackIndex, float volume)
        {
            if (trackIndex > 0)
                Debug.LogWarning("trackIndices greater than 0 currently not supported.");

            m_Volume = Mathf.Clamp(volume, 0, 1);
            this.MarkDirty();
        }
    }
}
