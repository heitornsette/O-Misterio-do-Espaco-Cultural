using UnityEngine;

namespace Misterio.Gameplay
{
    /// <summary>
    /// A seam that lets an automated playthrough drive the game through exactly the same code paths
    /// a player uses. When <see cref="Active"/> is false nothing here is read and the real Input
    /// System is used, so this costs nothing in a normal session.
    /// It exists so a playtest can press keys, walk and interact for real instead of calling the
    /// puzzle APIs directly, which is the only way to catch problems in the input handling itself.
    /// </summary>
    public static class PlaytestInput
    {
        public static bool Active { get; private set; }

        /// <summary>Movement axis for the current frame, as the left stick would report it.</summary>
        public static Vector2 Move;

        /// <summary>True on the frame the interact key is pressed.</summary>
        public static bool InteractPressed;

        /// <summary>True on the frame the pause key is pressed.</summary>
        public static bool PausePressed;

        /// <summary>One-shot cursor step for the conduit panel.</summary>
        public static Vector2Int CursorStep;

        /// <summary>True on the frame the turn key is pressed.</summary>
        public static bool TurnPressed;

        /// <summary>One-shot undo/reset keys used by the statue puzzle.</summary>
        public static bool UndoPressed;
        public static bool ResetPressed;

        // One-shot presses are stamped with the frame they were issued on. Coroutines run after
        // Update but before LateUpdate, so clearing unconditionally in LateUpdate would erase a
        // press before the gameplay Update of the next frame could read it. A press is cleared only
        // once a full frame has passed.
        static int pressedFrame = -1;

        public static void Enable()
        {
            Active = true;
            Clear();
        }

        public static void Disable()
        {
            Active = false;
            Clear();
        }

        /// <summary>
        /// Clears the one-shot presses. Called from LateUpdate, after every Update has had the
        /// chance to consume them; clearing earlier raced the gameplay scripts.
        /// Movement is deliberately not cleared: steering re-asserts it every frame.
        /// </summary>
        public static void ClearOneShots()
        {
            if (pressedFrame < 0 || Time.frameCount <= pressedFrame) return;
            InteractPressed = false;
            PausePressed = false;
            CursorStep = Vector2Int.zero;
            TurnPressed = false;
            UndoPressed = false;
            ResetPressed = false;
            pressedFrame = -1;
        }

        /// <summary>Marks that a one-shot was issued on this frame.</summary>
        public static void MarkPress() => pressedFrame = Time.frameCount;

        /// <summary>Full reset, used when the playtest starts or stops.</summary>
        public static void Clear()
        {
            Move = Vector2.zero;
            InteractPressed = false;
            PausePressed = false;
            CursorStep = Vector2Int.zero;
            TurnPressed = false;
            UndoPressed = false;
            ResetPressed = false;
            pressedFrame = -1;
        }
    }
}
