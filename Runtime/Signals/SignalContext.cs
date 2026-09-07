namespace BlueCheese.Core.Signals
{
    public class SignalContext
    {
        public bool IsCancelled { get; private set; }

        public void Cancel() => IsCancelled = true;
    }
}
