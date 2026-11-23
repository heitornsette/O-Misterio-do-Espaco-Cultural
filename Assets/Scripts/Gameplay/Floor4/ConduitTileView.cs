using Misterio.Puzzles.Floor4;
using UnityEngine;

namespace Misterio.Gameplay
{
    /// <summary>
    /// Draws one conduit tile. Colour carries the state (idle / powered) and the shape carries the
    /// connection, so the puzzle is readable without relying on hue alone.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class ConduitTileView : MonoBehaviour
    {
        [SerializeField] SpriteRenderer body;
        [SerializeField] SpriteRenderer socketNorth;
        [SerializeField] SpriteRenderer socketEast;
        [SerializeField] SpriteRenderer socketSouth;
        [SerializeField] SpriteRenderer socketWest;

        static readonly Color IdleColor = new Color(0.42f, 0.52f, 0.72f, 1f);
        static readonly Color PoweredColor = new Color(0.62f, 0.90f, 1f, 1f);
        static readonly Color SourceColor = new Color(1f, 0.86f, 0.55f, 1f);
        static readonly Color TargetColor = new Color(0.80f, 1f, 0.72f, 1f);

        public void Apply(ConduitPiece piece, bool powered, bool isSource, bool isTarget)
        {
            if (body == null) body = GetComponent<SpriteRenderer>();

            bool empty = piece.BaseMask == 0;
            if (body != null)
            {
                body.enabled = !empty;
                if (!empty)
                {
                    if (isSource) body.color = SourceColor;
                    else if (isTarget) body.color = TargetColor;
                    else body.color = powered ? PoweredColor : IdleColor;
                }
            }

            SetSocket(socketNorth, piece.HasNorth, powered);
            SetSocket(socketEast, piece.HasEast, powered);
            SetSocket(socketSouth, piece.HasSouth, powered);
            SetSocket(socketWest, piece.HasWest, powered);
        }

        void SetSocket(SpriteRenderer socket, bool connected, bool powered)
        {
            if (socket == null) return;
            socket.enabled = connected;
            if (!connected) return;
            socket.color = powered ? PoweredColor : IdleColor;
        }
    }
}
