using System;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using EinheitsKiste;

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
        private static readonly Dictionary<string, Func<Action<IEvent>, IEventBinding>> _eventRegisterers = new();
        private static readonly Dictionary<string, Action<IEventBinding>> _eventDeregisterers = new();

        private static IEnumerable<Type> GetEventTypes()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => typeof(IEvent).IsAssignableFrom(p) && !p.IsInterface && !p.IsAbstract);
        }

        private static void Raise<T>(T eventOject) where T : IEvent, new() => EventBus<T>.Raise(eventOject);
        private static EventBinding<T> Register<T>(Action<IEvent> onEvent) where T : IEvent, new()
        {
            var binding = new EventBinding<T>(new Action<T>(e => onEvent(e)));
            EventBus<T>.Register(binding);
            return binding;
        }

        private static void Deregister<T>(EventBinding<T> eventBinding) where T : IEvent, new()
            => EventBus<T>.Deregister(eventBinding);

        static EventBusUtil()
        {
            foreach (var type in GetEventTypes())
            {
                string eventName = $"{type.Namespace}.{type.Name}";
                var genericRaise = typeof(EventBusUtil).GetMethod(nameof(Raise), BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod(type);
                _eventRaisers.Add(eventName, e => genericRaise.Invoke(null, new object[] { e }));

                var genericRegister = typeof(EventBusUtil).GetMethod(nameof(Register), BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod(type);
                _eventRegisterers.Add(eventName, onEvent => (IEventBinding)genericRegister.Invoke(null, new object[] { onEvent }));

                var genericDeregister = typeof(EventBusUtil).GetMethod(nameof(Deregister), BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod(type);
                _eventDeregisterers.Add(eventName, onEvent => genericDeregister.Invoke(null, new object[] { onEvent }));
            };
        }

        public static void RaiseEvent(IEvent eventObject)
            => RaiseEvent(eventObject.GetType(), eventObject);
        public static void RaiseEvent(Type eventType, IEvent eventObject)
            => RaiseEvent(eventType.GetNameWithNamespace(), eventObject);

        public static void RaiseEvent(string eventTypeName, IEvent eventObject)
        {
            if (_eventRaisers.TryGetValue(eventTypeName, out var raiseEvent))
                raiseEvent(eventObject);
            else
                Debug.LogError($"Event {eventTypeName} not found");
        }

        public static IEventBinding RegisterEvent(Type eventType, Action<IEvent> onEvent)
            => RegisterEvent(eventType.GetNameWithNamespace(), onEvent);

        public static IEventBinding RegisterEvent(string eventTypeName, Action<IEvent> onEvent)
        {
            if (_eventRegisterers.TryGetValue(eventTypeName, out var registerEvent))
                return registerEvent(onEvent);

            Debug.LogError($"Event {eventTypeName} not found");
            return null;
        }

        public static void DeregisterEvent(Type eventType, IEventBinding eventBinding)
            => DeregisterEvent(eventType.GetNameWithNamespace(), eventBinding);

        public static void DeregisterEvent(string eventTypeName, IEventBinding eventBinding)
        {
            if (_eventDeregisterers.TryGetValue(eventTypeName, out var deregisterEvent))
                deregisterEvent(eventBinding);
            else
                Debug.LogError($"Event {eventTypeName} not found");
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
            var eventOptions = GetEventTypes().Select(type => new LabelValuePair(type.GetNameWithNamespace(), type.GetNameWithNamespace())).ToArray();
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