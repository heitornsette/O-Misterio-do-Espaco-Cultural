using UnityEngine;

namespace Misterio.Gameplay
{
    /// <summary>A piece of the archive waiting to be collected.</summary>
    public sealed class PieceInteractable : Interactable
    {
        Floor3Controller floor;
        int index;

        public void Bind(Floor3Controller owner, int pieceIndex)
        {
            floor = owner;
            index = pieceIndex;
        }

        public override void Interact(GuardActor actor)
        {
            if (floor == null) return;
            floor.Collect(index);
        }
    }
}
