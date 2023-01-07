using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Playables;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

namespace EventListenerTools
{
    public enum ListenerMethod
    {
        // Listeners for Monobehaviours 
        Awake,
        Start,
        OnEnable,
        OnDisable,

        // Listeners for Dialogue System for Unity (PixelCrushers) Messages
        OnUse,
        OnBarkStart,
        OnBarkEnd,
        OnConversationStart,
        OnConversationEnd,
        OnSequenceStart,
        OnSequenceEnd,

        // Listeners for Unity Collider Messages
        OnTriggerStay,
        OnTriggerEnter,
        OnTriggerExit,
        OnCollisionStay,
        OnCollisionEnter,
        OnCollisionExit,

        // Listeners for Unity Collider2D Messages
        OnTriggerStay2D,
        OnTriggerEnter2D,
        OnTriggerExit2D,
        OnCollisionStay2D,
        OnCollisionEnter2D,
        OnCollisionExit2D,

        // Listeners for Unity PlayableDirector Events
        OnPlayed, // play event
        OnPaused, // paused event
        OnStopped // stopped event
    }

    // For storing parsed method information in the editor
    public class CallbackDescription
    {
        public string assemblyName;        // Object type of the method
        public string methodName;          // The short name of the method
        public string fullMethodName;      // The name of the method with parameters: e.g.: Foo(arg_type)
        public string qualifiedMethodName; // The name of the class + method + parameters: e.g. Bar.Foo(arg_type)
        public List<Type> parameterTypes;  // none, bool, int, float, string, Object, Enum
    }

    public enum ParameterType
    {
        None,
        Bool,
        Int,
        Float,
        String,
        Object,
        Enum
    }

    [Serializable]
    public class Argument
    {
        // Argument type
        public ParameterType parameterType;
        // argument properties
        public bool Bool;
        public int Int;
        public string String;
        public float Float;
        public Object Object;
    }

    [Serializable]
    public class Callback
    {
        // Names
        public Object objectReference;
        public string assemblyName;
        public string methodName;
        public Argument[] arguments;
    }

    class EventListener : MonoBehaviour
    {
        public ListenerMethod listener; // listener method
        public string tagMatch = "";    // Tag Match check
        public Callback[] callbacks;    // methods to call

        // Check logic for each listener and tag combination
        public void CheckMatch(ListenerMethod listener, string tag)
        {
            if (this.listener == listener)
                if (tagMatch == "" || tagMatch == tag)
                    InvokeCallbacks();
        }

        // Add Event listener for PlayableDirector Events
        public void AddListener()
        {
            var director = GetComponent<PlayableDirector>();
            if (director != null)
            {
                RemoveListener();
                if (listener == ListenerMethod.OnPlayed)
                    director.played += OnPlayed;
                else if (listener == ListenerMethod.OnPaused)
                    director.paused += OnPaused;
                else if (listener == ListenerMethod.OnStopped)
                    director.stopped += OnStopped;
            }
        }

        // Remove Event listener for PlayableDirector Events
        public void RemoveListener()
        {
            var director = GetComponent<PlayableDirector>();
            if (director != null)
            {
                director.played -= OnPlayed;
                director.paused -= OnPaused;
                director.stopped -= OnStopped;
            }
        }

        // Message Listeners for Monobehaviours
        public void Awake()
        {
            AddListener();

            // PlayOnAwake/Play workaround bc played events are trigger before we registered ours if it woke up before
            // us
            if (listener == ListenerMethod.OnPlayed)
            {
                // if the playablegraph is playing and it just started then we need to invoke our callbacks
                var director = GetComponent<PlayableDirector>();
                if (director != null && director.playableGraph.IsValid() && director.playableGraph.IsPlaying()
                    && director.time == director.initialTime)
                {
                    // Wait one frame to make sure all scene objects are initialized before invoking callbacks 
                    IEnumerator DelayInvokeCallbacks()
                    {
                        yield return null;
                        OnPlayed(director); // Manually call OnPlayed callback
                        yield break;
                    };
                    StartCoroutine(DelayInvokeCallbacks());
                }
            }

            CheckMatch(ListenerMethod.Awake, null);
        }
        public void Start() { CheckMatch(ListenerMethod.Start, null); }
        public void OnEnable() { CheckMatch(ListenerMethod.OnEnable, null); }
        public void OnDisable() { CheckMatch(ListenerMethod.OnDisable, null); }

        // Message Listeners for Dialogue System for Unity (PixelCrushers)
        public void OnUse(Transform actor) { CheckMatch(ListenerMethod.OnUse, actor.tag); }
        public void OnBarkStart(Transform actor) { CheckMatch(ListenerMethod.OnBarkStart, actor.tag); }
        public void OnBarkEnd(Transform actor) { CheckMatch(ListenerMethod.OnBarkEnd, actor.tag); }
        public void OnConversationStart(Transform actor) { CheckMatch(ListenerMethod.OnConversationStart, actor.tag); }
        public void OnConversationEnd(Transform actor) { CheckMatch(ListenerMethod.OnConversationEnd, actor.tag); }
        public void OnSequenceStart(Transform actor) { CheckMatch(ListenerMethod.OnSequenceStart, actor.tag); }
        public void OnSequenceEnd(Transform actor) { CheckMatch(ListenerMethod.OnSequenceEnd, actor.tag); }

        // Message Listeners for Unity Collider
        public void OnTriggerStay(Collider other) { CheckMatch(ListenerMethod.OnTriggerStay, other.tag); }
        public void OnTriggerEnter(Collider other) { CheckMatch(ListenerMethod.OnTriggerEnter, other.tag); }
        public void OnTriggerExit(Collider other) { CheckMatch(ListenerMethod.OnTriggerExit, other.tag); }
        public void OnCollisionStay(Collision collision) { CheckMatch(ListenerMethod.OnCollisionStay, collision.gameObject.tag); }
        public void OnCollisionEnter(Collision collision) { CheckMatch(ListenerMethod.OnCollisionEnter, collision.gameObject.tag); }
        public void OnCollisionExit(Collision collision) { CheckMatch(ListenerMethod.OnCollisionExit, collision.gameObject.tag); }

        // Message Listeners for Unity Collider2D
        public void OnTriggerStay2D(Collider2D other) { CheckMatch(ListenerMethod.OnTriggerStay2D, other.gameObject.tag); }
        public void OnTriggerEnter2D(Collider2D other) { CheckMatch(ListenerMethod.OnTriggerEnter2D, other.gameObject.tag); }
        public void OnTriggerExit2D(Collider2D other) { CheckMatch(ListenerMethod.OnTriggerExit2D, other.gameObject.tag); }
        public void OnCollisionStay2D(Collision2D collision) { CheckMatch(ListenerMethod.OnCollisionStay2D, collision.gameObject.tag); }
        public void OnCollisionEnter2D(Collision2D collision) { CheckMatch(ListenerMethod.OnCollisionEnter2D, collision.gameObject.tag); }
        public void OnCollisionExit2D(Collision2D collision) { CheckMatch(ListenerMethod.OnCollisionExit2D, collision.gameObject.tag); }

        // Event Listeners for Unity PlayableDirector
        public void OnPlayed(PlayableDirector director) { CheckMatch(ListenerMethod.OnPlayed, director.tag); }
        public void OnPaused(PlayableDirector director) { CheckMatch(ListenerMethod.OnPaused, director.tag); }
        public void OnStopped(PlayableDirector director) { CheckMatch(ListenerMethod.OnStopped, director.tag); }

        // Invokes callbacks directly
        public void InvokeCallbacks()
        {
            if (callbacks == null) return;

            foreach (var callback in callbacks)
            {
                // Setup arguments
                object[] arguments = new object[callback.arguments.Length];
                Type[] types = new Type[callback.arguments.Length];
                for (int i = 0; i < arguments.Length; i++)
                {
                    var argument = callback.arguments[i];
                    if (argument.parameterType == ParameterType.Bool)
                        arguments[i] = argument.Bool;
                    else if (argument.parameterType == ParameterType.Int)
                        arguments[i] = argument.Int;
                    else if (argument.parameterType == ParameterType.Float)
                        arguments[i] = argument.Float;
                    else if (argument.parameterType == ParameterType.Object)
                        arguments[i] = argument.Object;
                    else if (argument.parameterType == ParameterType.String)
                        arguments[i] = argument.String;
                    else if (argument.parameterType == ParameterType.Enum)
                        arguments[i] = Enum.ToObject(Type.GetType(argument.String + ",Assembly-CSharp"), argument.Int);

                    types[i] = arguments[i].GetType();
                }

                // Call method
                var component = (callback.objectReference as GameObject).GetComponentInChildren(Type.GetType(callback.assemblyName));
                MethodInfo methodInfo = component.GetType().GetMethod(callback.methodName, types);
                methodInfo.Invoke(component, arguments);
            }
        }
    }

#if UNITY_EDITOR
    // For uniquely identifying Stored SerializedProperty methods with found CallbackDescription methods using assembly name, method name, and argument types
    public static class ListExtensions
    {
        public static int FindMethod(this IList<CallbackDescription> callbacks, SerializedProperty assemblyName, SerializedProperty methodName, SerializedProperty arguments)
        {
            // Iterate each callback method in list
            for (int id = 0; id < callbacks.Count; id++)
            {
                var callback = callbacks[id];

                // if num params, assembly name, or method name don't match continue to next callback
                if (arguments.arraySize != callback.parameterTypes.Count || assemblyName.stringValue.Split(",")[0] != callback.assemblyName.Split(",")[0] || methodName.stringValue != callback.methodName)
                    continue;

                // Iterate each param type
                bool isMatch = true;
                int i;
                for (i = 0; i < callback.parameterTypes.Count; i++)
                {
                    // Grab types
                    var type = callback.parameterTypes[i];
                    var argumentProperty = arguments.GetArrayElementAtIndex(i);
                    SerializedProperty m_ParameterType = argumentProperty.FindPropertyRelative("parameterType");
                    var type2 = m_ParameterType.enumValueIndex;

                    // break early if no match
                    if (type == typeof(bool) && type2 == (int)ParameterType.Bool)
                        continue;
                    else if (type == typeof(int) && type2 == (int)ParameterType.Int)
                        continue;
                    else if (type == typeof(float) && type2 == (int)ParameterType.Float)
                        continue;
                    else if (type == typeof(string) && type2 == (int)ParameterType.String)
                        continue;
                    else if ((type == typeof(object) || type.IsSubclassOf(typeof(Object))) && type2 == (int)ParameterType.Object)
                        continue;
                    else if (type.IsEnum && (type2 == (int)ParameterType.Enum || argumentProperty.FindPropertyRelative("String").stringValue.Split(",")[0] == type.FullName))
                        continue;
                    isMatch = false;
                    break;
                }

                // if count match then method matches so return id
                if (isMatch) return id;
            }
            return -1;
        }
    }

    // Custom Inspector for creating EventListener
    [CustomEditor(typeof(EventListener)), CanEditMultipleObjects]
    public class EventListenerEditor : Editor
    {
        SerializedProperty m_Listener;
        SerializedProperty m_TagMatch;
        SerializedProperty m_Callbacks;
        ReorderableList list;

        // Get serialized object properties (for UI)
        public void OnEnable()
        {
            // Functional properties
            m_Listener = serializedObject.FindProperty("listener");
            m_TagMatch = serializedObject.FindProperty("tagMatch");
            m_Callbacks = serializedObject.FindProperty("callbacks");
        }

        // Draw inspector GUI
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            {
                using var changeScope = new EditorGUI.ChangeCheckScope();

                // Draw listener field
                EditorGUILayout.PropertyField(m_Listener);

                // Draw tag selector field
                if (m_TagMatch.stringValue == "") m_TagMatch.stringValue = "Untagged";
                m_TagMatch.stringValue = EditorGUILayout.TagField("Tag", m_TagMatch.stringValue);
                if (m_TagMatch.stringValue == "Untagged") m_TagMatch.stringValue = "";
                //EditorGUILayout.Space();

                // Only rebuild list if something as changed (it isn't draggable otherwise)
                list ??= new ReorderableList(serializedObject, m_Callbacks, true, true, true, true)
                {
                    elementHeightCallback = GetElementHeight,
                    drawElementCallback = DrawMethodAndArguments,
                    drawHeaderCallback = delegate (Rect rect) { EditorGUI.LabelField(rect, "GameObject Methods"); }
                };

                // Layout reorderable list
                list.DoLayoutList();

                // apply changes
                if (changeScope.changed) serializedObject.ApplyModifiedProperties();
            }
        }

        // Height determiner for a given element
        float GetElementHeight(int index)
        {
            // Retrieve element (elements are added when + is clicked in reorderable list UI)
            SerializedProperty element = list.serializedProperty.GetArrayElementAtIndex(index);

            // Retrieve element properties
            SerializedProperty m_ObjectReference = element.FindPropertyRelative("objectReference");
            SerializedProperty m_Arguments = element.FindPropertyRelative("arguments");

            // Determine height
            if (m_ObjectReference.objectReferenceValue == null) return EditorGUIUtility.singleLineHeight + 10;
            else if (m_Arguments.arraySize == 0) return EditorGUIUtility.singleLineHeight * 2 + 10;
            else return EditorGUIUtility.singleLineHeight * 3 + 10;
        }

        // Draw drawer entry for given element
        void DrawMethodAndArguments(Rect rect, int index, bool isActive, bool isFocused)
        {
            // Compute first field position
            Rect line = new(rect.x, rect.y + 4, rect.width, EditorGUIUtility.singleLineHeight);

            // Retrieve element (elements are added when + is clicked in reorderable list UI)
            SerializedProperty element = list.serializedProperty.GetArrayElementAtIndex(index);

            // Retrieve element properties
            SerializedProperty m_ObjectReference = element.FindPropertyRelative("objectReference");
            SerializedProperty m_AssemblyName = element.FindPropertyRelative("assemblyName");
            SerializedProperty m_MethodName = element.FindPropertyRelative("methodName");
            SerializedProperty m_Arguments = element.FindPropertyRelative("arguments");

            var gameObject = EditorGUI.ObjectField(line, m_ObjectReference.objectReferenceValue, typeof(GameObject), true) as GameObject;
            line.y += EditorGUIUtility.singleLineHeight + 2;
            if (gameObject != null)
            {
                m_ObjectReference.objectReferenceValue = gameObject;

                var supportedMethods = CollectSupportedMethods(gameObject).ToList();

                // Get current method ID based off of stored name (index really)
                var selectedMethodId = supportedMethods.FindMethod(m_AssemblyName, m_MethodName, m_Arguments);

                // Draw popup (dropdown box)
                var previousMixedValue = EditorGUI.showMixedValue;
                {
                    GUIStyle style = EditorStyles.popup;
                    style.richText = true;

                    // Create dropdownlist with 'pseudo entries' for the currently selected method at the top of the list followed by a blank line
                    var dropdownList = supportedMethods.Select(i => i.qualifiedMethodName).ToList();
                    CallbackDescription selectedMethod = selectedMethodId > -1 ? supportedMethods[selectedMethodId] : null;
                    dropdownList.Insert(0, ""); // insert line
                    dropdownList.Insert(0, selectedMethodId > -1 ? selectedMethod.assemblyName.Split(",")[0] + "." + selectedMethod.fullMethodName : "No method");

                    // Store old selected method id case it isn't changed
                    var oldSelectedMethodId = selectedMethodId;
                    selectedMethodId = EditorGUI.Popup(line, 0, dropdownList.ToArray(), style);

                    // Update field position
                    line.y += EditorGUIUtility.singleLineHeight + 2;

                    // Normalize selection
                    if (selectedMethodId == 0)
                        selectedMethodId = oldSelectedMethodId; // No selection so restore actual method id
                    else
                        selectedMethodId -= 2; // normalize to get actual Id (-2 for the two 'pseudo entries'
                }

                EditorGUI.showMixedValue = previousMixedValue;

                // If selected method is valid then try to draw parameters
                if (selectedMethodId > -1 && selectedMethodId < supportedMethods.Count)
                {
                    var callbackDescription = supportedMethods.ElementAt(selectedMethodId);

                    // Fillout assembly and method name properties using the selected id
                    m_AssemblyName.stringValue = callbackDescription.assemblyName;
                    m_MethodName.stringValue = callbackDescription.methodName;

                    // Draw each argument
                    DrawArguments(line, element, callbackDescription);
                }
            }
        }

        // Create UI elements for the given parameter types of a methods arguments
        void DrawArguments(Rect rect, SerializedProperty element, CallbackDescription callbackDescription)
        {
            // Find the amount of user enterable arguments to compute UI entry box sizes
            int enterableArgCount = 0;
            foreach (var type in callbackDescription.parameterTypes)
                enterableArgCount++;

            // Compute the rect for the method parameters based off of the count
            var paramWidth = (rect.width - 10) / enterableArgCount;
            rect.width = paramWidth;

            // Grab the arguments property
            SerializedProperty m_Arguments = element.FindPropertyRelative("arguments");

            // Resize the arguments array
            m_Arguments.arraySize = callbackDescription.parameterTypes.Count;

            // Iterate and display each argument by type
            for (var i = 0; i < m_Arguments.arraySize; i++)
            {
                var type = callbackDescription.parameterTypes[i];
                var argumentProperty = m_Arguments.GetArrayElementAtIndex(i);
                SerializedProperty m_ParameterType = argumentProperty.FindPropertyRelative("parameterType");

                // Assign Param type and generate field. The Field style is determined by the serialized property type
                if (type == typeof(bool))
                {
                    m_ParameterType.enumValueIndex = (int)ParameterType.Bool;
                    EditorGUI.PropertyField(rect, argumentProperty.FindPropertyRelative("Bool"), GUIContent.none);
                }
                else if (type == typeof(int))
                {
                    m_ParameterType.enumValueIndex = (int)ParameterType.Int;
                    EditorGUI.PropertyField(rect, argumentProperty.FindPropertyRelative("Int"), GUIContent.none);
                }
                else if (type == typeof(float))
                {
                    m_ParameterType.enumValueIndex = (int)ParameterType.Float;
                    EditorGUI.PropertyField(rect, argumentProperty.FindPropertyRelative("Float"), GUIContent.none);
                }
                else if (type == typeof(string))
                {
                    m_ParameterType.enumValueIndex = (int)ParameterType.String;
                    EditorGUI.PropertyField(rect, argumentProperty.FindPropertyRelative("String"), GUIContent.none);
                }
                else if (type == typeof(object) || type.IsSubclassOf(typeof(Object)))
                {
                    m_ParameterType.enumValueIndex = (int)ParameterType.Object;
                    var objectProperty = argumentProperty.FindPropertyRelative("Object");
                    var obj = EditorGUI.ObjectField(rect, objectProperty.objectReferenceValue, type, true);
                    objectProperty.objectReferenceValue = obj;
                }
                else if (type.IsEnum)
                {
                    m_ParameterType.enumValueIndex = (int)ParameterType.Enum;
                    var intProperty = argumentProperty.FindPropertyRelative("Int");
                    var stringProperty = argumentProperty.FindPropertyRelative("String");
                    intProperty.intValue = (int)(object)EditorGUI.EnumPopup(rect, (Enum)Enum.ToObject(type, intProperty.intValue)); // Parse as enum type
                    stringProperty.stringValue = type.FullName; // store full type name
                }

                // Update field position
                rect.x += paramWidth + 5;
            }
        }

        // Helper method for retrieving method signatures from a game object
        public static IEnumerable<CallbackDescription> CollectSupportedMethods(GameObject gameObject)
        {
            if (gameObject == null) return Enumerable.Empty<CallbackDescription>();

            List<CallbackDescription> supportedMethods = new();
            var components = gameObject.GetComponentsInChildren<Component>();

            foreach (var component in components)
            {
                if (component == null)
                    continue;

                var componentType = component.GetType();
                while (componentType != typeof(Component) && componentType != null)
                {
                    var methods = componentType.GetMethods(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

                    foreach (var method in methods)
                    {
                        // don't support adding built in method names
                        if (method.Name == "Main" && method.Name == "Start" && method.Name == "Awake" && method.Name == "Update") continue;

                        var parameters = method.GetParameters();    // get parameters
                        List<Type> parameterTypes = new();          // create empty parameter list
                        string fullMethodName = method.Name + "(";  // start full method name signature
                        bool validMethod = true;                    // mark the method as valid until proven otherwise

                        // Parse parameter types
                        for (int i = 0; i < parameters.Length; i++)
                        {
                            if (i > 0) fullMethodName += ", ";
                            var parameter = parameters[i];
                            var strType = "";
                            if (parameter.ParameterType == typeof(bool))
                                strType = "bool";
                            else if (parameter.ParameterType == typeof(int))
                                strType = "int";
                            else if (parameter.ParameterType == typeof(float))
                                strType = "float";
                            else if (parameter.ParameterType == typeof(string))
                                strType = "string";
                            else if (parameter.ParameterType == typeof(object) || parameter.ParameterType.IsSubclassOf(typeof(Object)))
                                strType = "Object";
                            else if (parameter.ParameterType.IsEnum && Enum.GetUnderlyingType(parameter.ParameterType) == typeof(int))
                                strType = parameter.ParameterType.Name; // use underlying typename for fullanme string
                            else
                                validMethod = false;

                            // Add parameter and update full method name with parameter type and name
                            parameterTypes.Add(parameter.ParameterType);
                            fullMethodName += strType + " " + parameter.Name;
                        }

                        // one or more argument types was not supported so don't add method.
                        if (validMethod == false) continue;

                        // Finish the full name signature
                        fullMethodName += ")";

                        // Collect the first two pieces of the FQN
                        var assemblyName = componentType.FullName + "," + componentType.Module.Assembly.GetName().Name;

                        // Create method description object
                        var supportedMethod = new CallbackDescription
                        {
                            methodName = method.Name,
                            fullMethodName = fullMethodName,
                            qualifiedMethodName = componentType + "/" + fullMethodName[0] + "/" + fullMethodName,
                            parameterTypes = parameterTypes,
                            assemblyName = assemblyName
                        };
                        supportedMethods.Add(supportedMethod);
                    }
                    componentType = componentType.BaseType;
                }
            }

            return supportedMethods.OrderBy(x => x.fullMethodName, StringComparer.Ordinal).ToList();
        }
    }
#endif
}
