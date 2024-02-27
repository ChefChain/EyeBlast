using System.Collections;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PolySpatial.Internals.InternalBridge
{
    internal interface IEditorElementDecoratorProxy
    {
        VisualElement OnCreateFooter(Editor editor);
    }

    internal static class EditorElementBridge
    {
        class EditorElementDecoratorProxy : IEditorElementDecorator
        {
            readonly IEditorElementDecoratorProxy m_DecoratorProxy;
            internal EditorElementDecoratorProxy(IEditorElementDecoratorProxy decoratorProxy) => m_DecoratorProxy = decoratorProxy;
            public VisualElement OnCreateFooter(Editor editor) => m_DecoratorProxy.OnCreateFooter(editor);
        }

        static readonly Dictionary<IEditorElementDecoratorProxy, EditorElementDecoratorProxy> s_DecoratorsMap = new ();

        internal static void AddDecorator(IEditorElementDecoratorProxy decoratorProxy)
        {
            if (!s_DecoratorsMap.TryGetValue(decoratorProxy, out var decorator))
            {
                decorator = new EditorElementDecoratorProxy(decoratorProxy);
                s_DecoratorsMap.Add(decoratorProxy, decorator);
            }

            EditorElement.AddDecorator(decorator);
        }

        internal static void RemoveDecorator(IEditorElementDecoratorProxy decoratorProxy)
        {
            if (!s_DecoratorsMap.TryGetValue(decoratorProxy, out var decorator))
                return;

            s_DecoratorsMap.Remove(decoratorProxy);
            EditorElement.RemoveDecorator(decorator);
        }
    }
}
