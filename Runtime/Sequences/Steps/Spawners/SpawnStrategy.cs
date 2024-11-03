namespace Nexus.Sequences
{
    /// <summary>
    /// Defines how objects should be spawned
    /// </summary>
    public enum SpawnStrategy
    {
        Sequential,      // Spawn at each point in order
        Random,         // Randomly select spawn points
        RoundRobin,     // Distribute evenly across points
        AllAtOnce       // Spawn at all points simultaneously
    }
}