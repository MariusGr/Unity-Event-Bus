namespace UnityEventBus {
    public interface IEvent { }

    public struct TestEvent : IEvent {
        public int health;
    }

    public struct PlayerEvent : IEvent {
        public int health;
        public int mana;
    }
}