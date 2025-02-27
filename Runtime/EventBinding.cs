﻿using System;

namespace UnityEventBus {
    public interface IEventBinding<T> : IEventBinding {
        public Action<T> OnEvent { get; set; }
        public Action OnEventNoArgs { get; set; }
    }

    public interface IEventBinding {
    }

    public class EventBinding<T> : IEventBinding<T> where T : IEvent {
        Action<T> onEvent = _ => { };
        Action onEventNoArgs = () => { };

        Action<T> IEventBinding<T>.OnEvent
        {
            get => onEvent;
            set
            {
                if (onEvent == null) throw new ArgumentNullException($"{nameof(IEventBinding<T>.OnEvent)} for {nameof(T)} cannot be null");
                onEvent = value;
            }
        }

        Action IEventBinding<T>.OnEventNoArgs {
            get => onEventNoArgs;
            set => onEventNoArgs = value;
        }

        public EventBinding(Action<T> onEvent) => this.onEvent = onEvent;
        public EventBinding(Action onEventNoArgs) => this.onEventNoArgs = onEventNoArgs;
        
        public void Add(Action onEvent) => onEventNoArgs += onEvent;
        public void Remove(Action onEvent) => onEventNoArgs -= onEvent;
        
        public void Add(Action<T> onEvent) => this.onEvent += onEvent;
        public void Remove(Action<T> onEvent) => this.onEvent -= onEvent;
    }
}