using UnityEngine;

namespace SuperBot.Controls
{
    /// <summary>Per-frame input snapshot. Valid for the whole frame after MobileInputRouter.Update.</summary>
    public sealed class PlayerInputState
    {
        public Vector2 Move;
        public Vector2 LookDelta;
        public bool JumpPressed;
        public bool JumpHeld;
        public bool DashPressed;
        public bool AttackPressed;

        public void ClearFrame()
        {
            Move = Vector2.zero;
            LookDelta = Vector2.zero;
            JumpPressed = false;
            JumpHeld = false;
            DashPressed = false;
            AttackPressed = false;
        }
    }

    public enum ButtonAction
    {
        Jump,
        Dash,
        Attack
    }
}
