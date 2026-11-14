using UnityEngine;

namespace SuperBot.Core
{
    public interface IDamageable
    {
        void TakeHit(Vector3 source);
    }

    /// <summary>Something the player can land on to trigger an effect (crates, enemies).</summary>
    public interface IStompable
    {
        /// <returns>Upward bounce velocity for the player, or 0 for no bounce.</returns>
        float OnStomped(Vector3 stomperPosition);
    }
}
