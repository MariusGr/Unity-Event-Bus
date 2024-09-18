using System;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

# if UNITY_EDITOR
using MyBox;
#endif

namespace UnityEventBus
{
    /// <summary>
    /// Contains methods and properties related to event buses and event types in the Unity application.
    /// </summary>
    public static class EventBusUtil
    {
        public static IReadOnlyList<Type> EventTypes { get; set; }
        public static IReadOnlyList<Type> EventBusTypes { get; set; }

        private static readonly Dictionary<string, Action<IEvent>> _eventRaisers = new();
        private static readonly Dictionary<string, Action<Action<IEvent>>> _eventRegisterers = new();

        private static IEnumerable<Type> GetEventTypes()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => typeof(IEvent).IsAssignableFrom(p) && !p.IsInterface && !p.IsAbstract);
        }

        private static void Raise<T>(T eventOject) where T : IEvent, new() => EventBus<T>.Raise(eventOject);
        private static void Register<T>(Action<IEvent> onEvent) where T : IEvent, new() => EventBus<T>.Register(new EventBinding<T>(new Action<T>(e => onEvent(e))));

        static EventBusUtil()
        {
            foreach (var type in GetEventTypes())
            {
                string eventName = $"{type.Namespace}.{type.Name}";
                var genericRaise = typeof(EventBusUtil).GetMethod(nameof(Raise), BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod(type);
                _eventRaisers.Add(eventName, e => genericRaise.Invoke(null, new object[] { e }));

                var genericRegister = typeof(EventBusUtil).GetMethod(nameof(Register), BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod(type);
                _eventRegisterers.Add(eventName, onEvent => genericRegister.Invoke(null, new object[] { onEvent }));
            };
        }

        public static void RaiseEvent(string eventName, IEvent eventObject)
        {
            if (_eventRaisers.TryGetValue(eventName, out var raiseEvent))
                raiseEvent(eventObject);
            else
                Debug.LogError($"Event {eventName} not found");
        }

        public static void RegisterEvent(string eventName, Action<IEvent> onEvent)
        {
            if (_eventRegisterers.TryGetValue(eventName, out var registerEvent))
                registerEvent(onEvent);
            else
                Debug.LogError($"Event {eventName} not found");
        }

#if UNITY_EDITOR
        public static PlayModeStateChange PlayModeState { get; set; }

        /// <summary>
        /// Initializes the Unity Editor related components of the EventBusUtil.
        /// The [InitializeOnLoadMethod] attribute causes this method to be called every time a script
        /// is loaded or when the game enters Play Mode in the Editor. This is useful to initialize
        /// fields or states of the class that are necessary during the editing state that also apply
        /// when the game enters Play Mode.
        /// The method sets up a subscriber to the playModeStateChanged event to allow
        /// actions to be performed when the Editor's play mode changes.
        /// </summary>    
        [InitializeOnLoadMethod]
        public static void InitializeEditor()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            PlayModeState = state;
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                ClearAllBuses();
            }
        }

        public static LabelValuePair[] GetEventOptions()
        {
            var eventOptions = GetEventTypes().Select(type => new LabelValuePair(type.Name, type.Name)).ToArray();
            eventOptions = eventOptions.OrderBy(option => option.Label).ToArray();
            var optionsWithNone = new List<LabelValuePair> { new LabelValuePair("None", "") };
            optionsWithNone.AddRange(eventOptions);
            return optionsWithNone.ToArray();
        }
#endif

        /// <summary>
        /// Initializes the EventBusUtil class at runtime before the loading of any scene.
        /// The [RuntimeInitializeOnLoadMethod] attribute instructs Unity to execute this method after
        /// the game has been loaded but before any scene has been loaded, in both Play Mode and after
        /// a Build is run. This guarantees that necessary initialization of bus-related types and events is
        /// done before any game objects, scripts or components have started.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Initialize()
        {
            EventTypes = PredefinedAssemblyUtil.GetTypes(typeof(IEvent));
            EventBusTypes = InitializeAllBuses();
        }

        static List<Type> InitializeAllBuses()
        {
            List<Type> eventBusTypes = new();

            var typedef = typeof(EventBus<>);
            foreach (var eventType in EventTypes)
            {
                var busType = typedef.MakeGenericType(eventType);
                eventBusTypes.Add(busType);
            }

            return eventBusTypes;
        }

        /// <summary>
        /// Clears (removes all listeners from) all event buses in the application.
        /// </summary>
        public static void ClearAllBuses()
        {
            for (int i = 0; i < EventBusTypes.Count; i++)
            {
                var busType = EventBusTypes[i];
                var clearMethod = busType.GetMethod("Clear", BindingFlags.Static | BindingFlags.NonPublic);
                clearMethod?.Invoke(null, null);
            }
        }
    }
}