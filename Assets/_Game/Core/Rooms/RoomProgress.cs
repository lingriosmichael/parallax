namespace Parallax.Core
{
    public sealed class RoomProgress
    {
        public int LastCompleted { get; private set; } = -1;
        public bool LevelComplete { get; private set; }

        public bool TryComplete(int roomId)
        {
            if (LevelComplete || roomId <= LastCompleted) return false;
            LastCompleted = roomId;
            return true;
        }

        public void MarkLevelComplete() => LevelComplete = true;
    }
}
