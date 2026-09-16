using Parallax.Core;

namespace Parallax.Gameplay.Observers
{
    public interface IObserverDriver
    {
        InputSourceKind Kind { get; }
        void Activate(ObserverContext observer);
        void Deactivate();
        void FixedTick(int tick);
    }
}
