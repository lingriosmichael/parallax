namespace Parallax.Core
{
    // PAX-085 (D-087): something in the room that changes what the LocalHuman cat's motor receives. LocalHumanDriver
    // collects every one under its own reality root when it activates and asks them before each motor step.
    public interface IControlModifier
    {
        // True when the next motor step's horizontal input is negated.
        bool InvertsMove { get; }
    }
}
