namespace DrawDecks.Domain;

/// <summary>Specified seeded PRNG for reproducible gameplay; never use for security tokens.</summary>
internal sealed class DeterministicRandom
{
    private ulong _state;
    public DeterministicRandom(ulong seed) => _state = seed == 0 ? 0x9E3779B97F4A7C15UL : seed;

    public ulong NextUInt64()
    {
        _state ^= _state >> 12;
        _state ^= _state << 25;
        _state ^= _state >> 27;
        return _state * 2685821657736338717UL;
    }

    public int Next(int exclusiveMax)
    {
        if (exclusiveMax <= 0) throw new ArgumentOutOfRangeException(nameof(exclusiveMax));
        return (int)(NextUInt64() % (uint)exclusiveMax);
    }
}
