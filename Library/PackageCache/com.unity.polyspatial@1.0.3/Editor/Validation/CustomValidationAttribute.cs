using System;
using Unity.XR.CoreUtils.Editor;

namespace UnityEditor.PolySpatial.Validation
{
    /// <summary>
    /// This attribute is used to tag methods that associate component types with <see cref="IComponentRuleCreator"/> or  <see cref="ITypeMessage"/>.
    /// Unity automatically tracks the registered component types in the opened scenes, when a component has the same
    /// type (or is a derived class) the associated <see cref="IComponentRuleCreator.CreateRules"/> instance method is invoked.
    /// </summary>
    /// <remarks>
    /// The tagged method should have the following signature:
    /// <code>static void Method(List<ValueTuple<Type, IComponentRuleCreator>> ruleCreators, List<ValueTuple<Type, List<ITypeMessage>>> messages))</code>
    /// <br/>
    /// The rule creators and messages are registered in the same order as they are added in the <code>ruleCreators</code>, <code>messages<code> list parameter.
    /// It's possible to associate a component type with a <see lagword="null"/> <see cref="IComponentRuleCreator"/>,
    /// in this case no validation will be performed for the component type. This is useful to override validations for
    /// derived types.
    /// </remarks>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// public class MyInvalidComponent : MonoBehaviour
    /// { }
    ///
    /// public class MyValidDerivedComponent : MyInvalidComponent
    /// { }
    ///
    /// class MyCustomMessage : ITypeMessage
    /// {
    ///    const string k_MessageFormat = "This is a <b>{0}</b> message";
    ///    public string Message { get; } = string.Format(k_MessageFormat, "TEST");
    ///    public MessageType MessageType => MessageType.Info;
    ///    public ITypeMessage.LinkData Link { get; } = new ITypeMessage.LinkData("MyCustom Link Tittle", "www.myCustomURL.com");
    /// }
    ///
    ///
    /// public class MyRuleCreatorManager
    /// {
    ///     [CustomValidationAttribute]
    ///     public static void RegisterCustomValidationRules(List<ValueTuple<Type, IComponentRuleCreator>> ruleCreators, List<ValueTuple<Type, List<ITypeMessage>>> messages))
    ///     {
    ///         // no validation will be performed for MyValidDerivedComponent
    ///         ruleCreators.Add(new(typeof(MyValidDerivedComponent), null));
    ///
    ///         //to create a custom message for a component type
    ///         messages.Add(new(typeof(MyValidDerivedComponent), new List<ITypeMessage>() { new MyCustomMessage() }));
    ///     }
    /// }
    /// ]]>
    /// </code>
    /// </example>
    /// <seealso cref="BuildValidationRule"/>
    [AttributeUsage(AttributeTargets.Method)]
    internal class CustomValidationAttribute : Attribute
    {
        internal readonly int Priority;

        /// <summary>
        /// The attribute constructor.
        /// </summary>
        /// <param name="priority">An optional priority value to define the order to register the rule creators. High values are registered later, the default value is <code>0</code>.</param>
        public CustomValidationAttribute(int priority = 0)
        {
            Priority = priority;
        }
    }
}
