using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EspacoCultural.Gameplay
{
    /// <summary>
    /// Movimento do guarda em grid (provisório — o Levi pode trocar depois).
    /// Setas/WASD andam uma célula por vez (segurar continua andando); E, Espaço ou Enter interagem.
    /// Quem decide se o passo é possível é o andar (FloorBase.TryStep), assim cada puzzle tem suas regras.
    /// As sheets "frente" olham para a direita; andar para a esquerda espelha o sprite.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        public FloorBase floor;
        public GridActor actor;

        [Header("Animações")]
        public Texture2D idleFront, idleBack, walkFront, walkBack;
        public float walkFps = 12f, idleFps = 6f;
        public int footRowFront = 113, footRowBack = 120;

        [Header("Ritmo")]
        public float stepTime = 0.2f;

        public Vector2Int Facing { get; private set; } = new Vector2Int(0, 1);
        public bool Busy { get; private set; }
        public bool FacingLeft => flipLeft;

        private bool flipLeft;

        private void Update()
        {
            if (floor == null || actor == null || Busy || actor.Moving) return;
            if (floor.InputLocked) { Idle(); return; }

            var k = Keyboard.current;
            if (k == null) return;

            Vector2Int d = ReadDirection(k);
            if (d != Vector2Int.zero)
            {
                Face(d);
                if (floor.TryStep(this, d) == StepResult.Blocked) Idle();
                return;
            }

            if (k.eKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame)
                floor.Interact(this, actor.cell, Facing);

            if (!Busy && !actor.Moving) Idle();
        }

        private static Vector2Int ReadDirection(Keyboard k)
        {
            if (k.upArrowKey.isPressed || k.wKey.isPressed) return new Vector2Int(0, -1);
            if (k.downArrowKey.isPressed || k.sKey.isPressed) return new Vector2Int(0, 1);
            if (k.leftArrowKey.isPressed || k.aKey.isPressed) return new Vector2Int(-1, 0);
            if (k.rightArrowKey.isPressed || k.dKey.isPressed) return new Vector2Int(1, 0);
            return Vector2Int.zero;
        }

        public void Face(Vector2Int d)
        {
            Facing = d;
            if (d.x < 0) flipLeft = true;
            else if (d.x > 0) flipLeft = false;
        }

        /// <summary>Dá um passo na direção d. Use sheet para trocar a animação (ex.: empurrar).</summary>
        public void Step(Vector2Int d, Texture2D sheet = null, float duration = -1f, Action arrived = null)
        {
            Face(d);
            var target = actor.cell + d;
            bool back = d.y < 0 && sheet == null;
            SetFoot(back);
            actor.anim.Play(sheet != null ? sheet : (back ? walkBack : walkFront), walkFps);
            actor.anim.FlipX = flipLeft;
            actor.MoveTo(target, (duration > 0 ? duration : stepTime) * EspacoCultural.Jogo.GameState.SlowFactor, () =>
            {
                arrived?.Invoke();
                floor.OnPlayerArrived(this, target);
            });
        }

        public void Idle()
        {
            if (actor == null || actor.anim == null) return;
            bool back = Facing.y < 0;
            SetFoot(back);
            actor.anim.Play(back ? idleBack : idleFront, idleFps);
            actor.anim.FlipX = flipLeft;
        }

        /// <summary>Toca uma animação uma vez, travando o controle até terminar.</summary>
        public void PlayOnce(Texture2D sheet, float fps, Action done = null, bool back = false)
        {
            Busy = true;
            SetFoot(back);
            actor.anim.Play(sheet, fps, false, () =>
            {
                Busy = false;
                Idle();
                done?.Invoke();
            }, 0, true);
            actor.anim.FlipX = flipLeft;
        }

        /// <summary>Volta para a célula sem animação (reinício de puzzle).</summary>
        public void Teleport(Vector2Int c, Vector2Int facing)
        {
            Busy = false;
            actor.Snap(c);
            Face(facing);
            Idle();
        }

        private void SetFoot(bool back)
        {
            int row = back ? footRowBack : footRowFront;
            if (actor.footRow == row) return;
            actor.footRow = row;
            actor.SetFeet(actor.FeetPos);
        }
    }
}
