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
        }

        public override void Interact(GuardActor actor)
        {
            if (floor == null) return;
            floor.PickUp(index);
        }
    }

    /// <summary>A readable plaque; shows its clue text in the HUD.</summary>
    public sealed class PlaqueInteractable : Interactable
    {
        [SerializeField, TextArea] string plaqueText = "Placa do acervo.";
        HudView hud;

        public void Bind(HudView target, string text)
        {
            hud = target;
            plaqueText = text;
        }

        public override void Interact(GuardActor actor)
        {
            hud?.ShowHint(plaqueText, 8f);
        }
    }
}
