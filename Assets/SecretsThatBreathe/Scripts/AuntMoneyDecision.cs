namespace SecretsThatBreathe
{
    // The acceptance branch is reconsidered indefinitely; only refusal ends the decision.
    public sealed class AuntMoneyDecision
    {
        public int ThoughtIndex { get; private set; } = -1;
        public bool HasRefused { get; private set; }
        public int Accept()
        {
            if (HasRefused) return -1;
            ThoughtIndex = (ThoughtIndex + 1) % 3;
            return ThoughtIndex;
        }
        public bool Refuse()
        {
            if (HasRefused) return false;
            HasRefused = true;
            return true;
        }
        public void Reset() { ThoughtIndex = -1; HasRefused = false; }
    }
}
