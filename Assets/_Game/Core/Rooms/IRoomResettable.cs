namespace Parallax.Core
{
    public interface IRoomResettable
    {
        int RoomId { get; }
        void ResetToInitial();
    }
}
