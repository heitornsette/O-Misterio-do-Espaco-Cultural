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

        /// <summary>How far above the object the interaction marker floats.</summary>
        [SerializeField, Min(0.02f)] float markerHeight = 0.30f;
        public virtual float MarkerHeight => markerHeight;

        protected virtual void OnEnable()
        {
            if (!All.Contains(this)) All.Add(this);
        }

        protected virtual void OnDisable() => All.Remove(this);

        public abstract void Interact(GuardActor actor);

        public virtual void SetHighlighted(bool highlighted) { }

        /// <summary>
        /// Drops entries that Unity destroyed, without clearing the live ones. The registry is
        /// rebuilt lazily because OnEnable order is not guaranteed relative to a floor's Awake, and
        /// clearing it there would silently unregister objects that had already enabled.
        /// </summary>
        /// <summary>
        /// Replaces the registry contents. Used by editor tooling, where MonoBehaviour lifecycle
        /// callbacks such as OnEnable are not driven by the player loop.
        /// </summary>
        public static void RebuildRegistry(IEnumerable<Interactable> items)
        {
            All.Clear();
            if (items == null) return;
            foreach (var item in items)
                if (item != null) All.Add(item);
        }

        public static void PruneRegistry()
        {
            for (int i = All.Count - 1; i >= 0; i--)
                if (All[i] == null) All.RemoveAt(i);
        }
    }
}
