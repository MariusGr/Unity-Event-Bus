namespace UnityEventBus {
    public abstract class Event : IEvent {
        public override bool Equals(object obj) => obj != null && obj.GetType() == GetType();
        public override int GetHashCode() => GetType().GetHashCode();
        public override string ToString() => GetType().Name;
    }
}