using System;
using System.Collections.Generic;
using Parallax.Core;

namespace Parallax.Gameplay.Levels
{
    /// <summary>PAX-066 (D-279): today's Entitlements, from the level list, the config and what's saved. A wait begins the first time a
    /// level reached by progress is asked about, which is as the player leaves the level before it. Tests swap UtcNow.</summary>
    public static class LevelAccess
    {
        public static Func<long> UtcNow = () => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        public static Entitlements Load(LevelListConfig levelList, IapConfig config, LevelProgress progress)
        {
            long now = UtcNow();
            IReadOnlyList<string> ids = levelList.OrderedIds();
            return new Entitlements(ids, levelList.FreeLevels, config.WaitSeconds, LevelProgressStore.PaidUnlocked,
                id => progress.IsUnlocked(id) ? LevelProgressStore.WaitStartedAt(id, now) : -1);
        }
    }
}
