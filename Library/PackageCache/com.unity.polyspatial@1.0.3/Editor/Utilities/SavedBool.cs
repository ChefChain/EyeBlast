namespace UnityEditor.PolySpatial.Utilities
{
    // Copied from UnityEditor.SavedBool
    internal class SavedBool
    {
        bool m_Value;
        string m_Name;
        bool m_Loaded;

        internal SavedBool(string name, bool value)
        {
            m_Name = name;
            m_Loaded = false;
            m_Value = value;
        }

        void Load()
        {
            if (m_Loaded)
                return;

            m_Loaded = true;
            m_Value = EditorPrefs.GetBool(m_Name, m_Value);
        }

        internal bool Value
        {
            get { Load(); return m_Value; }
            set
            {
                Load();
                if (m_Value == value)
                    return;
                m_Value = value;
                EditorPrefs.SetBool(m_Name, value);
            }
        }

        /// <summary>
        /// Bool cast operator to allow direct conditional checks with SavedBool.
        /// </summary>
        /// <param name="s">Saved bool to check.</param>
        public static implicit operator bool(SavedBool s) => s.Value;
    }
}
