using System.Collections.Generic;
using UnityEngine;

namespace UnityEventBus
{
    public static class EventBus<T> where T : IEvent
    {
        static readonly HashSet<IEventBinding<T>> bindings = new HashSet<IEventBinding<T>>();

        public static void Register(EventBinding<T> binding)
        {
            if (binding == null) throw new System.ArgumentNullException($"{nameof(binding)} for {nameof(T)} cannot be null");
            bindings.Add(binding);
        }

        public static void Deregister(EventBinding<T> binding)
        {
            if (binding == null) throw new System.ArgumentNullException($"{nameof(binding)} for {nameof(T)} cannot be null");
            bindings.Remove(binding);
        }

        public static void Raise(T @event)
        {
            var snapshot = new HashSet<IEventBinding<T>>(bindings);

            foreach (var binding in snapshot)
            {
                if (bindings.Contains(binding))
                {
                    binding.OnEvent.Invoke(@event);
                    binding.OnEventNoArgs.Invoke();
                }
            }

            static void Clear() => bindings.Clear();
        }
    }
}
