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
        // Listeners for MonoBehaviours 
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
        public string assemblyName;         // Object type of the method
        public string methodName;           // The short name of the method
        public string fullMethodName;       // The name of the method with parameters: e.g.: Foo(arg_type)
        public string qualifiedMethodName;  // The name of the class + method + parameters: e.g. Bar.Foo(arg_type)
        public List<Type> parameterTypes;   // none, bool, int, float, string, Object, Enum
        public ArrayList defaultParameters; // default value for the given parameter
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

    class MethodExecuter : MonoBehaviour
    {
        public static MethodExecuter instance;

        public void Awake()
        {
            instance = this;
        }
    }

    class EventListener : MonoBehaviour
    {
        public ListenerMethod listener; // listener method
        public string tagMatch = "";    // Tag Match check
        public bool bindOtherObject;    // Whether to bind the other object causing the event to trigger
        public Callback[] callbacks;    // methods to call

        // Check logic for each listener and tag combination
        public void CheckMatch(ListenerMethod listener, Transform actor)
        {
            if (this.listener == listener)
                if (tagMatch == "" || (actor != null && tagMatch == actor.tag))
                    InvokeCallbacks(actor.gameObject);
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

        // Message Listeners for MonoBehaviours
        public void Awake()
        {
            if (MethodExecuter.instance == null)
                new GameObject("EventListenerMethodExecuter", typeof(MethodExecuter)).GetComponent<MethodExecuter>();

            AddListener();

            // PlayOnAwake/Play workaround bc played events are trigger before we registered ours if it woke up before us
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
        public void OnUse(Transform actor) { CheckMatch(ListenerMethod.OnUse, actor); }
        public void OnBarkStart(Transform actor) { CheckMatch(ListenerMethod.OnBarkStart, actor); }
        public void OnBarkEnd(Transform actor) { CheckMatch(ListenerMethod.OnBarkEnd, actor); }
        public void OnConversationStart(Transform actor) { CheckMatch(ListenerMethod.OnConversationStart, actor); }
        public void OnConversationEnd(Transform actor) { CheckMatch(ListenerMethod.OnConversationEnd, actor); }
        public void OnSequenceStart(Transform actor) { CheckMatch(ListenerMethod.OnSequenceStart, actor); }
        public void OnSequenceEnd(Transform actor) { CheckMatch(ListenerMethod.OnSequenceEnd, actor); }

        // Message Listeners for Unity Collider
        public void OnTriggerStay(Collider other) { CheckMatch(ListenerMethod.OnTriggerStay, other.transform); }
        public void OnTriggerEnter(Collider other) { CheckMatch(ListenerMethod.OnTriggerEnter, other.transform); }
        public void OnTriggerExit(Collider other) { CheckMatch(ListenerMethod.OnTriggerExit, other.transform); }
        public void OnCollisionStay(Collision collision) { CheckMatch(ListenerMethod.OnCollisionStay, collision.transform); }
        public void OnCollisionEnter(Collision collision) { CheckMatch(ListenerMethod.OnCollisionEnter, collision.transform); }
        public void OnCollisionExit(Collision collision) { CheckMatch(ListenerMethod.OnCollisionExit, collision.transform); }

        // Message Listeners for Unity Collider2D
        public void OnTriggerStay2D(Collider2D other) { CheckMatch(ListenerMethod.OnTriggerStay2D, other.transform); }
        public void OnTriggerEnter2D(Collider2D other) { CheckMatch(ListenerMethod.OnTriggerEnter2D, other.transform); }
        public void OnTriggerExit2D(Collider2D other) { CheckMatch(ListenerMethod.OnTriggerExit2D, other.transform); }
        public void OnCollisionStay2D(Collision2D collision) { CheckMatch(ListenerMethod.OnCollisionStay2D, collision.transform); }
        public void OnCollisionEnter2D(Collision2D collision) { CheckMatch(ListenerMethod.OnCollisionEnter2D, collision.transform); }
        public void OnCollisionExit2D(Collision2D collision) { CheckMatch(ListenerMethod.OnCollisionExit2D, collision.transform); }

        // Event Listeners for Unity PlayableDirector
        public void OnPlayed(PlayableDirector director) { CheckMatch(ListenerMethod.OnPlayed, director.transform); }
        public void OnPaused(PlayableDirector director) { CheckMatch(ListenerMethod.OnPaused, director.transform); }
        public void OnStopped(PlayableDirector director) { CheckMatch(ListenerMethod.OnStopped, director.transform); }

        // Invokes callbacks directly
        public void InvokeCallbacks(Object obj)
        {
            if (callbacks == null) return;
            IEnumerator InvokeCallbacks()
            {
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
                    if (bindOtherObject == false)
                        obj = callback.objectReference;
                    // Instance call for GameObject types
                    if (obj is GameObject gameObject)
                    {
                        var component = gameObject.GetComponentInChildren(Type.GetType(callback.assemblyName));
                        const BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
                        MethodInfo methodInfo = component.GetType().GetMethod(callback.methodName, bindingFlags, null, types, null);
                        if (methodInfo.ReturnType != typeof(IEnumerator))
                            methodInfo.Invoke(component, arguments);
                        else
                            yield return MethodExecuter.instance.StartCoroutine((IEnumerator)methodInfo.Invoke(component, arguments));
                    }
                    // Static non-instance call for non-gameobject types
                    else
                    {
                        const BindingFlags bindingFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
                        MethodInfo methodInfo = Type.GetType(callback.assemblyName).GetMethod(callback.methodName, bindingFlags, null, types, null);
                        if (methodInfo.ReturnType != typeof(IEnumerator))
                            methodInfo.Invoke(null, arguments);
                        else
                            yield return MethodExecuter.instance.StartCoroutine((IEnumerator)methodInfo.Invoke(null, arguments));
                    }
                }
            }
            MethodExecuter.instance.StartCoroutine(InvokeCallbacks());
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
        // Cached data for speeding up editor
        class EditorCache
        {
            public Object obj; // selected game object for a given reorderable list entry
            public List<CallbackDescription> supportedMethods; // supported methods for a given reorderable list entry
            public int selectedMethodId; // selected id for a given reorderable list entry
            public string[] dropdown; // dropdown for a given reorderable list entry
        }
        Dictionary<int, EditorCache> editorCache;
        ReorderableList cachedMethodList; // object + method pairs in a reorderable list

        // Properties
        SerializedProperty m_Listener;
        SerializedProperty m_TagMatch;
        SerializedProperty m_bindOtherObject;
        SerializedProperty m_Callbacks;

        // Get serialized object properties (for UI)
        public void OnEnable()
        {
            // Functional properties
            m_Listener = serializedObject.FindProperty("listener");
            m_TagMatch = serializedObject.FindProperty("tagMatch");
            m_bindOtherObject = serializedObject.FindProperty("bindOtherObject");
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

                // Draw bind other object field
                bool cachebindOtherObject = m_bindOtherObject.boolValue;
                EditorGUILayout.PropertyField(m_bindOtherObject);
                if (cachebindOtherObject != m_bindOtherObject.boolValue)
                    cachedMethodList = null;

                // Only rebuild list if something as changed (it isn't draggable otherwise)
                if (cachedMethodList == null)
                {
                    cachedMethodList = new ReorderableList(serializedObject, m_Callbacks, true, true, true, true)
                    {
                        elementHeightCallback = GetElementHeight,
                        drawElementCallback = DrawMethodAndArguments,
                        drawHeaderCallback = delegate (Rect rect) { EditorGUI.LabelField(rect, "Object Methods"); },
                        onChangedCallback = delegate (ReorderableList list) { cachedMethodList = null; }
                    };
                    editorCache = new();
                }

                // Layout reorderable list
                cachedMethodList.DoLayoutList();

                // apply changes
                if (changeScope.changed) serializedObject.ApplyModifiedProperties();
            }
        }

        // Height determiner for a given element
        float GetElementHeight(int index)
        {
            // Retrieve element (elements are added when + is clicked in reorderable list UI)
            SerializedProperty element = cachedMethodList.serializedProperty.GetArrayElementAtIndex(index);

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
            SerializedProperty element = cachedMethodList.serializedProperty.GetArrayElementAtIndex(index);

            // Retrieve element properties
            SerializedProperty m_ObjectReference = element.FindPropertyRelative("objectReference");
            SerializedProperty m_AssemblyName = element.FindPropertyRelative("assemblyName");
            SerializedProperty m_MethodName = element.FindPropertyRelative("methodName");
            SerializedProperty m_Arguments = element.FindPropertyRelative("arguments");

            // Initialize cache for this index if not initialized
            if (!editorCache.ContainsKey(index)) editorCache.Add(index, new());
            var cache = editorCache[index];

            // Draw object property field
            var obj = EditorGUI.ObjectField(line, m_ObjectReference.objectReferenceValue, typeof(Object), true);
            line.y += EditorGUIUtility.singleLineHeight + 2;

            // return if the object is null. 
            if (obj == null) return;

            // Otherwise assign object.
            m_ObjectReference.objectReferenceValue = obj;

            // Collected supported methods and generate dropdown if the object type has changed
            if (obj != cache.obj)
            {
                // Update supported methods
                cache.supportedMethods = CollectSupportedMethods(obj, m_bindOtherObject.boolValue);

                // Get current method ID based off of stored name (index really)
                cache.selectedMethodId = cache.supportedMethods.FindMethod(m_AssemblyName, m_MethodName, m_Arguments);

                // Create dropdown
                var qualifiedMethodNames = cache.supportedMethods.Select(i => i.qualifiedMethodName);
                var dropdownList = new List<string>() { "", "" }; // Add 2x blank line entries
                dropdownList.AddRange(qualifiedMethodNames);
                cache.dropdown = dropdownList.ToArray();
            }

            // Draw popup (dropdown box)
            var previousMixedValue = EditorGUI.showMixedValue;
            {
                GUIStyle style = EditorStyles.popup;
                style.richText = true;

                // Create dropdownlist with 'pseudo entry' for the currently selected method at the top of the list
                CallbackDescription selectedMethod = cache.selectedMethodId > -1 ? cache.supportedMethods[cache.selectedMethodId] : null;
                cache.dropdown[0] = cache.selectedMethodId > -1 ? selectedMethod.assemblyName.Split(",")[0] + "." + selectedMethod.fullMethodName : "No method";

                // Store old selected method id case it isn't changed
                var oldSelectedMethodId = cache.selectedMethodId;
                cache.selectedMethodId = EditorGUI.Popup(line, 0, cache.dropdown, style);

                // Update field position
                line.y += EditorGUIUtility.singleLineHeight + 2;

                // Normalize selection
                if (cache.selectedMethodId == 0)
                    cache.selectedMethodId = oldSelectedMethodId; // No selection so restore actual method id
                else
                    cache.selectedMethodId -= 2; // normalize to get actual Id (-2 for the two 'pseudo entries'
            }

            EditorGUI.showMixedValue = previousMixedValue;

            // If selected method is valid then try to draw parameters
            if (cache.selectedMethodId > -1 && cache.selectedMethodId < cache.supportedMethods.Count)
            {
                var callbackDescription = cache.supportedMethods.ElementAt(cache.selectedMethodId);

                // Detect method change in order to initialize default parameters
                var methodChanged = m_MethodName.stringValue != callbackDescription.methodName;

                // Fillout assembly and method name properties using the selected id
                m_AssemblyName.stringValue = callbackDescription.assemblyName;
                m_MethodName.stringValue = callbackDescription.methodName;

                // Draw each argument
                DrawArguments(line, element, callbackDescription, methodChanged);
            }

            // Cache object
            cache.obj = obj;
        }

        // Create UI elements for the given parameter types of a methods arguments
        void DrawArguments(Rect rect, SerializedProperty element, CallbackDescription callbackDescription, bool initialize)
        {
            // Find the amount of user enterable arguments to compute UI entry box sizes
            int enterableArgCount = 0;
            foreach (var type in callbackDescription.parameterTypes)
                enterableArgCount++;

            // Compute the rect for the method parameters based off of the count
            var paramWidth = rect.width / enterableArgCount;
            rect.width = paramWidth - 5;

            // Grab the arguments property
            SerializedProperty m_Arguments = element.FindPropertyRelative("arguments");

            // Resize the arguments array
            m_Arguments.arraySize = callbackDescription.parameterTypes.Count;

            // Iterate and display each argument by type
            for (var i = 0; i < m_Arguments.arraySize; i++)
            {
                var type = callbackDescription.parameterTypes[i];
                var defaultValue = callbackDescription.defaultParameters[i];
                var argumentProperty = m_Arguments.GetArrayElementAtIndex(i);
                SerializedProperty m_ParameterType = argumentProperty.FindPropertyRelative("parameterType");

                // Assign Param type and generate field. The Field style is determined by the serialized property type
                if (type == typeof(bool))
                {
                    m_ParameterType.enumValueIndex = (int)ParameterType.Bool;
                    var property = argumentProperty.FindPropertyRelative("Bool");
                    if (initialize && defaultValue.GetType() != typeof(DBNull)) property.boolValue = (bool)defaultValue;
                    EditorGUI.PropertyField(rect, property, GUIContent.none);
                }
                else if (type == typeof(int))
                {
                    m_ParameterType.enumValueIndex = (int)ParameterType.Int;
                    var property = argumentProperty.FindPropertyRelative("Int");
                    if (initialize && defaultValue.GetType() != typeof(DBNull)) property.intValue = (int)defaultValue;
                    EditorGUI.PropertyField(rect, property, GUIContent.none);
                }
                else if (type == typeof(float))
                {
                    m_ParameterType.enumValueIndex = (int)ParameterType.Float;
                    var property = argumentProperty.FindPropertyRelative("Float");
                    if (initialize && defaultValue.GetType() != typeof(DBNull)) property.floatValue = (float)defaultValue;
                    EditorGUI.PropertyField(rect, property, GUIContent.none);
                }
                else if (type == typeof(string))
                {
                    m_ParameterType.enumValueIndex = (int)ParameterType.String;
                    var property = argumentProperty.FindPropertyRelative("String");
                    if (initialize && defaultValue.GetType() != typeof(DBNull)) property.stringValue = (string)defaultValue;
                    EditorGUI.PropertyField(rect, property, GUIContent.none);
                }
                else if (type == typeof(object) || type.IsSubclassOf(typeof(Object)))
                {
                    m_ParameterType.enumValueIndex = (int)ParameterType.Object;
                    var property = argumentProperty.FindPropertyRelative("Object");
                    if (initialize && defaultValue.GetType() != typeof(DBNull)) property.objectReferenceValue = (Object)defaultValue;
                    var obj = EditorGUI.ObjectField(rect, property.objectReferenceValue, type, true);
                    property.objectReferenceValue = obj;
                }
                else if (type.IsEnum)
                {
                    m_ParameterType.enumValueIndex = (int)ParameterType.Enum;
                    var intProperty = argumentProperty.FindPropertyRelative("Int");
                    var stringProperty = argumentProperty.FindPropertyRelative("String");
                    if (initialize && defaultValue.GetType() != typeof(DBNull)) intProperty.intValue = (int)defaultValue;
                    intProperty.intValue = (int)(object)EditorGUI.EnumPopup(rect, (Enum)Enum.ToObject(type, intProperty.intValue)); // Parse as enum type
                    stringProperty.stringValue = type.FullName; // store full type name in string
                }

                // Update field position
                rect.x += paramWidth;
            }
        }

        // Helper method for retrieving method signatures from an Object
        public static List<CallbackDescription> CollectSupportedMethods(Object obj, bool bindOtherObject)
        {
            // return if object is null
            if (obj == null) return Enumerable.Empty<CallbackDescription>().ToList();

            // Create a list to fill with supported methods
            List<CallbackDescription> supportedMethods = new();

            // Create a list of objects to search methods for (include base object)
            List<Object> objectList = new() { obj };

            // Get components if object is a game object
            var components = (obj as GameObject)?.GetComponentsInChildren<Component>();
            if (components != null) objectList.AddRange(components);

            // Iterate over base Object and all components
            foreach (var item in objectList)
            {
                // Get item type. If the type is a monoscript then get the class type directly
                var itemType = item is MonoScript monoScript ? monoScript.GetClass() : item.GetType();

                // Loop over type for derived type up the entire inheritence hierarchy 
                while (itemType != null)
                {
                    // Get methods for class type. Include instance methods if the type is a game object or component or set to bind the other object
                    var methods = itemType.GetMethods((item is GameObject || item is Component || bindOtherObject ? BindingFlags.Instance : 0) | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

                    foreach (var method in methods)
                    {
                        // don't support adding built in method names
                        if (method.Name == "Main" && method.Name == "Start" && method.Name == "Awake" && method.Name == "Update") continue;

                        var parameters = method.GetParameters();    // get parameters
                        List<Type> parameterTypes = new();          // create empty parameter list
                        ArrayList defaultValues = new();            // create empty default arguments list
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
                            defaultValues.Add(parameter.DefaultValue);
                            fullMethodName += strType + " " + parameter.Name;
                        }

                        // one or more argument types was not supported so don't add method.
                        if (validMethod == false) continue;

                        // Finish the full name signature
                        fullMethodName += ")";

                        // Collect the first two pieces of the FQN
                        var assemblyName = itemType.FullName + "," + itemType.Module.Assembly.GetName().Name;

                        // Create method description object
                        var supportedMethod = new CallbackDescription
                        {
                            methodName = method.Name,
                            fullMethodName = fullMethodName,
                            qualifiedMethodName = itemType + "/" + fullMethodName[0] + "/" + fullMethodName,
                            parameterTypes = parameterTypes,
                            defaultParameters = defaultValues,
                            assemblyName = assemblyName
                        };
                        supportedMethods.Add(supportedMethod);
                    }

                    // Get base type to check it for methods as well
                    itemType = itemType.BaseType;
                }
            }

            return supportedMethods.OrderBy(x => x.fullMethodName, StringComparer.Ordinal).ToList();
        }
    }
#endif
}
