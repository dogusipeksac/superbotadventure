namespace SuperBot.Core
{
    /// <summary>Implemented by UI that takes over what happens after the player dies (e.g. revive offer).</summary>
    public interface IDeathHandler
    {
        void HandlePlayerDeath();
    }
}
