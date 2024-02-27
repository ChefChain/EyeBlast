using System;
using System.Collections.Generic;
using Unity.PolySpatial.Internals;
using UnityEngine;

namespace Unity.PolySpatial
{
    // Special component that allows users to tap into platform-level sorting groups.
    [DisallowMultipleComponent]
    public class VisionOSSortingGroup : MonoBehaviour
    {
        [Serializable]
        public struct RendererSorting
        {
            [Tooltip("Order within the sort group. Lower values indicate they should be drawn first.")]
            public int order;

            [Tooltip("The renderer the sort order should apply to.")]
            public GameObject renderer;

            [Tooltip("Whether the sort order should also be applied to all descendant renderers.")]
            public bool applyToDescendants;
        }

        /// <summary>
        /// Enum defining depth passs types.
        /// </summary>
        public enum DepthPass: int
        {
            /// Draws depth of renderer after drawing all color.
            PostPass,

            /// Draws depth of renderer before drawing any color.
            PrePass,

            /// Draws depth and color at the same time.
            Unseparated,
        }

        [SerializeField]
        [Tooltip("When depth is drawn with respect to color.")]
        DepthPass m_DepthPass;
        public DepthPass depthPass
        {
            get => m_DepthPass;
            set
            {
                m_DepthPass = value;
                ObjectBridge.MarkDirty(this);
            }
        }

        [SerializeField]
        [Tooltip("List of all renderers belonging to this sort group.")]
        List<RendererSorting> m_Renderers = new List<RendererSorting>();
        public List<RendererSorting> renderers
        {
            get => m_Renderers;
            set
            {
                m_Renderers = value;
                ObjectBridge.MarkDirty(this);
            }
        }
    }
}
