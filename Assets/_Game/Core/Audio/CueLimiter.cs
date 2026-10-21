using System.Collections.Generic;

namespace Parallax.Core
{
    /// <summary>PAX-064 (D-158): decides whether a cue may start now. A cue already sounding MaxVoices times, or started less
    /// than MinGap ago, is skipped. Times are seconds on any clock that only moves forward (the audio player uses unscaled
    /// time, so a pause doesn't hold voices open).</summary>
    public sealed class CueLimiter
    {
        readonly Dictionary<int, List<float>> endsByCue = new Dictionary<int, List<float>>();
        readonly Dictionary<int, float> lastStart = new Dictionary<int, float>();

        public bool TryStart(int cue, float now, float length, int maxVoices, float minGap)
        {
            if (maxVoices <= 0) return false;
            if (lastStart.TryGetValue(cue, out float last) && now - last < minGap) return false;
            if (!endsByCue.TryGetValue(cue, out List<float> ends)) endsByCue[cue] = ends = new List<float>();
            ends.RemoveAll(end => end <= now);
            if (ends.Count >= maxVoices) return false;
            ends.Add(now + length);
            lastStart[cue] = now;
            return true;
        }

        public int Sounding(int cue, float now)
        {
            if (!endsByCue.TryGetValue(cue, out List<float> ends)) return 0;
            int count = 0;
            foreach (float end in ends) if (end > now) count++;
            return count;
        }

        public void Clear()
        {
            endsByCue.Clear();
            lastStart.Clear();
        }
    }
}
