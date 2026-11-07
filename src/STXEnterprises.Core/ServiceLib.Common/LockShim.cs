#if !NET9_0_OR_GREATER
namespace System.Threading;

internal sealed class Lock
{
    private readonly object _sync = new object();

    public Scope EnterScope()
    {
        Monitor.Enter(_sync);
        return new Scope(_sync);
    }

    public readonly struct Scope : IDisposable
    {
        private readonly object _sync;
        public Scope(object sync) => _sync = sync;
        public void Dispose() => Monitor.Exit(_sync);
    }
}
#endif

