using UnityEngine;

namespace EspacoCultural.Gameplay
{
    /// <summary>
    /// Grid achatado sobre o piso (células 48×30 px, 13 colunas a partir de x = 8).
    /// Coordenadas de célula: x = coluna (0 à esquerda), y = linha (0 junto à parede do fundo).
    /// </summary>
    public class FloorGrid
    {
        public readonly int originX, originY, cols, rows, cellW, cellH;

        public FloorGrid(int originY, int rows, int originX = 8, int cols = 13, int cellW = 48, int cellH = 30)
        {
            this.originX = originX; this.originY = originY;
            this.cols = cols; this.rows = rows;
            this.cellW = cellW; this.cellH = cellH;
        }

        public bool Inside(Vector2Int c) => c.x >= 0 && c.y >= 0 && c.x < cols && c.y < rows;

        /// <summary>Canto superior esquerdo da célula, em pixels de tela.</summary>
        public Vector2 TopLeft(Vector2Int c) => new Vector2(originX + c.x * cellW, originY + c.y * cellH);

        /// <summary>Ponto onde ficam os pés de quem está na célula (centro horizontal, perto da borda de baixo).</summary>
        public Vector2 Feet(Vector2Int c) => new Vector2(originX + c.x * cellW + cellW * 0.5f, originY + c.y * cellH + cellH - 7);
    }
}
