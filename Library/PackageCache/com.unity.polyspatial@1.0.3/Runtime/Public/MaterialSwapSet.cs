using System;
using UnityEditor;
using UnityEngine;

namespace Unity.PolySpatial
{
    [CreateAssetMenu(menuName = "PolySpatial/Material Swap Set")]
    public class MaterialSwapSet : ScriptableObject
    {
        [SerializeField] internal Material[] m_SrcList = new Material[0];
        [SerializeField] internal Material[] m_DstList = new Material[0];
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(MaterialSwapSet))]
    public class MaterialSwapSetEditor : UnityEditor.Editor {
        public override void OnInspectorGUI()
        {
            var o = (MaterialSwapSet)target;

            for (var i = 0; i < o.m_SrcList.Length; i++)
            {
                GUILayout.BeginHorizontal();
                EditorGUI.BeginChangeCheck();
                var newKey = (Material) EditorGUILayout.ObjectField(o.m_SrcList[i], typeof(Material), false);
                var newValue = (Material) EditorGUILayout.ObjectField(o.m_DstList[i], typeof(Material), false);
                if (EditorGUI.EndChangeCheck())
                {
                    o.m_SrcList[i] = newKey;
                    o.m_DstList[i] = newValue;
                    EditorUtility.SetDirty(target);
                    AssetDatabase.SaveAssetIfDirty(target);
                }

                var xrect = GUILayoutUtility.GetRect(16.0f, 16.0f);
                if (GUI.Button(xrect, "X"))
                {
                    var newSrc = new Material[o.m_SrcList.Length - 1];
                    var newDst = new Material[o.m_DstList.Length - 1];
                    var k = 0;
                    for (var j = 0; j < o.m_SrcList.Length; j++)
                    {
                        if (j == i)
                            continue;
                        newSrc[k] = o.m_SrcList[j];
                        newDst[k] = o.m_DstList[j];
                        k++;
                    }
                    //o.m_SrcList.RemoveAt(i);
                    //o.m_DstList.RemoveAt(i);
                    o.m_SrcList = newSrc;
                    o.m_DstList = newDst;
                    EditorUtility.SetDirty(target);
                    AssetDatabase.SaveAssetIfDirty(target);
                    // hmm
                    GUILayout.EndHorizontal();
                    return;
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            var addKey = (Material) EditorGUILayout.ObjectField((Material) null, typeof(Material), false);
            var addValue = (Material) EditorGUILayout.ObjectField((Material) null, typeof(Material), false);
            if (EditorGUI.EndChangeCheck())
            {
                Array.Resize(ref o.m_SrcList, o.m_SrcList.Length + 1);
                Array.Resize(ref o.m_DstList, o.m_DstList.Length + 1);
                o.m_SrcList[o.m_SrcList.Length - 1] = addKey;
                o.m_DstList[o.m_DstList.Length - 1] = addValue;
                //o.m_SrcList.Add(addKey);
                //o.m_DstList.Add(addValue);

                EditorUtility.SetDirty(target);
                AssetDatabase.SaveAssetIfDirty(target);
            }

            GUILayoutUtility.GetRect(16.0f, 16.0f);
            GUILayout.EndHorizontal();
        }
    }
#endif
}
