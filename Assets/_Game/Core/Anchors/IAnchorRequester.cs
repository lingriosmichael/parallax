namespace Parallax.Core
{
    public interface IAnchorRequester
    {
        EventOrigin Origin { get; }
        void Request(AnchorId anchor, float targetValue);
    }
}
