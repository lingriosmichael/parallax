namespace Parallax.Core
{
    public enum ObserverId : byte { A = 0, B = 1 }

    public static class ObserverIdExtensions
    {
        public static ObserverId Other(this ObserverId id) =>
            id == ObserverId.A ? ObserverId.B : ObserverId.A;
    }
}
