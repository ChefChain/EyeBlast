
namespace UnityEditor.PolySpatial.Validation
{
    /// <summary>
    /// An interface that represents a message to be displayed in the Inspector view.
    /// </summary>
    internal interface ITypeMessage
    {
        /// <summary>
        /// The message text.
        /// </summary>
        string Message { get; }

        /// <summary>
        /// The message type.
        /// </summary>
        MessageType MessageType { get; }

        /// <summary>
        /// The message's link, if it has one.
        /// </summary>
        LinkData Link { get; }

        /// <summary>
        /// The message's data for the url.
        /// </summary>
        public struct LinkData
        {
            readonly string m_LinkURL;
            readonly string m_linkTittle;

            /// <summary>
            /// the Link URL
            /// </summary>
            public string LinkUrl => m_LinkURL;

            /// <summary>
            /// the tittle of the Link
            /// </summary>
            public  string LinkTitle => m_linkTittle;

            /// <summary>
            /// Constructor for the LinkData
            /// </summary>
            /// <param name="linkTitle"></param>
            /// <param name="linkUrl"></param>
            public LinkData(string linkTitle, string linkUrl)
            {
                m_linkTittle = linkTitle;
                m_LinkURL = linkUrl;
            }
        }
    }
}
