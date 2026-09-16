namespace Parallax.Core
{
    public static class EventOrigins
    {
        public static EventOrigin Human(ObserverId id) => id == ObserverId.A ? EventOrigin.HumanA : EventOrigin.HumanB;
        public static EventOrigin Echo(ObserverId id) => id == ObserverId.A ? EventOrigin.EchoA : EventOrigin.EchoB;
    }
}
