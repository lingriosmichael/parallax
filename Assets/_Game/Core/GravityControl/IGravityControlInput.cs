namespace Parallax.Core
{
    public interface IGravityControlInput
    {
        bool IsAvailable { get; }
        void Calibrate();
        float ReadNormalized();
    }
}
