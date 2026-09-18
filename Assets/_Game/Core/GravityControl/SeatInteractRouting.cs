namespace Parallax.Core
{
    public static class SeatInteractRouting
    {
        // Consumes the stand edge so the interactor cannot sit again in this tick.
        public static bool Route(ref CatCommand command, bool isSeated)
        {
            if (!isSeated || !command.InteractPressed) return false;
            command.InteractPressed = false;
            return true;
        }
    }
}
