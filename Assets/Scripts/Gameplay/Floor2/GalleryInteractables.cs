using UnityEngine;

namespace Misterio.Gameplay
{
    /// <summary>A wall hook where a painting can be hung.</summary>
    public sealed class HookInteractable : Interactable
    {
        Floor2Controller floor;
        int slot;

        public void Bind(Floor2Controller owner, int index)
        {
            floor = owner;
            slot = index;
            promptText = "Pendurar quadro";
        }

        public override Vector2 InteractPoint => transform.position;

        public override void Interact(GuardActor actor)
        {
            if (floor == null) return;
            if (floor.Carried >= 0) floor.HangOn(slot);
            else if (floor.IsSlotOccupied(slot)) floor.TakeFromSlot(slot);
        }
    }

    /// <summary>A painting resting on the gallery floor.</summary>
    public sealed class PaintingInteractable : Interactable
    {
        Floor2Controller floor;
        int index;

        public void Bind(Floor2Controller owner, int paintingIndex)
        {
            floor = owner;
            index = paintingIndex;
            promptText = "Pegar quadro";
        }

        public override void Interact(GuardActor actor)
        {
            if (floor == null) return;
            floor.PickUp(index);
        }
    }

    /// <summary>
    /// A readable plaque; shows its clue text in the HUD.
    /// The text is plain and short on purpose: long strings with typographic dashes have already
    /// broken the scene serialisation once, and a clue never needs more than one line.
    /// </summary>
    public sealed class PlaqueInteractable : Interactable
    {
        // Serialised so the value survives being written into the scene by the builder.
        [SerializeField] Floor2Controller floor;
        [SerializeField] int clueIndex;

        public void Bind(Floor2Controller owner, int index)
        {
            floor = owner;
            clueIndex = index;
            promptText = "Ler placa";
        }

        public override void Interact(GuardActor actor)
        {
            if (floor == null) floor = Object.FindAnyObjectByType<Floor2Controller>();
            if (floor == null) return;
            floor.ReadClue(clueIndex);
        }
    }
}
