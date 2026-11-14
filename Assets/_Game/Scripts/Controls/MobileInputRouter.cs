using UnityEngine;
using UnityEngine.InputSystem;

namespace SuperBot.Controls
{
    /// <summary>
    /// Merges on-screen touch controls with keyboard/mouse/gamepad (editor + controller support) into one
    /// <see cref="PlayerInputState"/>. Touch events arrive from the EventSystem at arbitrary points in the frame,
    /// so they are latched and folded into the state at the start of the next Update.
    /// </summary>
    [DefaultExecutionOrder(-500)]
    public sealed class MobileInputRouter : MonoBehaviour
    {
        [SerializeField] private float gamepadLookSpeed = 600f;

        public static MobileInputRouter Instance { get; private set; }

        public PlayerInputState State { get; } = new PlayerInputState();

        private Vector2 joystickMove;
        private Vector2 pendingLook;
        private bool pendingJump, pendingDash, pendingAttack;
        private bool touchJumpHeld;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnDisable()
        {
            joystickMove = Vector2.zero;
            touchJumpHeld = false;
            pendingLook = Vector2.zero;
            pendingJump = pendingDash = pendingAttack = false;
            State.ClearFrame();
        }

        // ---- API used by on-screen controls ----
        public void SetJoystick(Vector2 value) => joystickMove = Vector2.ClampMagnitude(value, 1f);
        public void AddLook(Vector2 delta) => pendingLook += delta;

        public void Press(ButtonAction action)
        {
            switch (action)
            {
                case ButtonAction.Jump: pendingJump = true; touchJumpHeld = true; break;
                case ButtonAction.Dash: pendingDash = true; break;
                case ButtonAction.Attack: pendingAttack = true; break;
            }
        }

        public void Release(ButtonAction action)
        {
            if (action == ButtonAction.Jump) touchJumpHeld = false;
        }

        private void Update()
        {
            var s = State;
            s.ClearFrame();

            s.Move = joystickMove;
            s.LookDelta = pendingLook;
            s.JumpPressed = pendingJump;
            s.JumpHeld = touchJumpHeld;
            s.DashPressed = pendingDash;
            s.AttackPressed = pendingAttack;
            pendingLook = Vector2.zero;
            pendingJump = pendingDash = pendingAttack = false;

            ReadKeyboardMouse(s);
            ReadGamepad(s);
            s.Move = Vector2.ClampMagnitude(s.Move, 1f);
        }

        private static void ReadKeyboardMouse(PlayerInputState s)
        {
            var kb = Keyboard.current;
            if (kb != null)
            {
                Vector2 k = Vector2.zero;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) k.y += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) k.y -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) k.x += 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) k.x -= 1f;
                if (k.sqrMagnitude > s.Move.sqrMagnitude) s.Move = k.normalized;

                s.JumpPressed |= kb.spaceKey.wasPressedThisFrame;
                s.JumpHeld |= kb.spaceKey.isPressed;
                s.DashPressed |= kb.leftShiftKey.wasPressedThisFrame;
                s.AttackPressed |= kb.eKey.wasPressedThisFrame || kb.jKey.wasPressedThisFrame;
            }

            var mouse = Mouse.current;
            if (mouse != null && mouse.rightButton.isPressed)
                s.LookDelta += mouse.delta.ReadValue();
        }

        private void ReadGamepad(PlayerInputState s)
        {
            var gp = Gamepad.current;
            if (gp == null) return;
            Vector2 stick = gp.leftStick.ReadValue();
            if (stick.sqrMagnitude > s.Move.sqrMagnitude) s.Move = stick;
            s.LookDelta += gp.rightStick.ReadValue() * (gamepadLookSpeed * Time.unscaledDeltaTime);
            s.JumpPressed |= gp.buttonSouth.wasPressedThisFrame;
            s.JumpHeld |= gp.buttonSouth.isPressed;
            s.DashPressed |= gp.buttonWest.wasPressedThisFrame || gp.rightShoulder.wasPressedThisFrame;
            s.AttackPressed |= gp.buttonEast.wasPressedThisFrame;
        }
    }
}
