using UnityEngine;

namespace Parallax.Core
{
    /// <summary>PAX-082: the D-056 reachability contract's arithmetic, moved verbatim out of
    /// LevelLayoutValidator.ValidateReach so the validator and the DebugTools movement readout
    /// share one copy. Continuous (analytic) motion, as the validator has always measured it.</summary>
    public static class JumpReach
    {
        // D-056 (2): a required jump's distance stays within this fraction of the measured reach.
        public const float RequiredFraction = .75f;

        public static float LaunchSpeed(float gravity, float jumpHeight) => Mathf.Sqrt(2f * gravity * jumpHeight);

        // False when the landing is higher than the jump can rise.
        public static bool CanRise(float launchSpeed, float gravity, float deltaHeight) => !(launchSpeed * launchSpeed < 2f * gravity * deltaHeight);

        // Take-off speed after accelerating from rest over the runway, capped at the run speed.
        public static float TakeoffSpeed(float maxSpeed, float acceleration, float runway) => Mathf.Min(maxSpeed, Mathf.Sqrt(2f * acceleration * runway));

        // Time in the air from take-off to landing deltaHeight above the take-off.
        public static float Flight(float launchSpeed, float gravity, float deltaHeight) => (launchSpeed + Mathf.Sqrt(launchSpeed * launchSpeed - 2f * gravity * deltaHeight)) / gravity;

        // Horizontal time-at-speed above hazardHeight, times the take-off speed: the hazard's kill window.
        public static float HazardWindow(float takeoffSpeed, float launchSpeed, float gravity, float hazardHeight) => takeoffSpeed * 2f * Mathf.Sqrt(launchSpeed * launchSpeed - 2f * gravity * hazardHeight) / gravity;

        // PAX-076 (D-083): the best take-off's reach, edge to edge: the cat leaves with its trailing side at the
        // take-off edge, runs on for the coyote time, then flies; it lands once its leading side reaches the target's
        // edge, so the collider's width counts once. The height lost during the coyote time is ignored (it only
        // shortens the flight), so this never understates the reach. Only the bait-gap rule calls it.
        public static float EdgeReach(float takeoffSpeed, float flight, float coyoteSeconds, float colliderWidth) => takeoffSpeed * (flight + coyoteSeconds) + colliderWidth;
    }
}
