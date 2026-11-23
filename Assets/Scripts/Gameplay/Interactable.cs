using System.Collections.Generic;
using UnityEngine;

namespace Misterio.Gameplay
{
    /// <summary>
    /// Anything the guard can press E on. Registers itself so the actor can find the closest
    /// target without physics queries.
    /// </summary>
    public abstract class Interactable : MonoBehaviour
    {
        public static readonly List<Interactable> All = new List<Interactable>();

        [SerializeField, Min(0.05f)] float interactRadius = 0.35f;
        [SerializeField] protected string promptText = "Interagir";

        public float InteractRadius => interactRadius;
        public virtual Vector2 InteractPoint => transform.position;
        public virtual bool IsAvailable => isActiveAndEnabled;
        public string Prompt => promptText;

        protected virtual void OnEnable()
        {
            if (!All.Contains(this)) All.Add(this);
        }

        protected virtual void OnDisable() => All.Remove(this);

        public abstract void Interact(GuardActor actor);

        public virtual void SetHighlighted(bool highlighted) { }

        /// <summary>Clears the registry; used when a floor is rebuilt or a scene reloads.</summary>
        public static void ClearRegistry() => All.Clear();
    }
}
