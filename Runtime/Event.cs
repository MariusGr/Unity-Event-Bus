namespace UnityEventBus {
    public abstract class Event : IEvent
    {
        public object Sender { get; set; }
        public override bool Equals(object obj) => obj != null && obj.GetType() == GetType();
        public override int GetHashCode() => GetType().GetHashCode();
        public override string ToString() => GetType().Name;
        public Event() { }
        public Event(object sender) : this() => Sender = sender;
        public bool IsSender(object sender)
        {
            if (sender == null) return false;
            if (Sender == null) return false;
            return Sender == sender;
        }
    }
}