using System.Collections.Generic;
using UnityEngine;

namespace Misterio.Puzzles.Floor2
{
    public enum PaletteFamily { Cool, Warm }

    /// <summary>One fictional regional painting used by the gallery puzzle.</summary>
    public sealed class GalleryPainting
    {
        public string Id;
        public string Title;
        public string Author;
        public int Year;
        public PaletteFamily Palette;
        public string Theme;
        public string Symbol;   // non-colour cue, required by the accessibility rules

        public string YearText => Year.ToString();
    }

    /// <summary>
    /// Logic for the gallery: the player reads the plaques, then hangs each painting on the correct
    /// hook. The solution is unique and comes from relative clues (never from guessing), so the
    /// puzzle is real deduction with only three canvases.
    /// </summary>
    public sealed class GalleryPuzzle
    {
        readonly GalleryPainting[] paintings;
        readonly int[] solution;          // slot index -> painting index
        readonly int[] assignment;        // slot index -> painting index, -1 when empty
        readonly string[] clues;

        public IReadOnlyList<GalleryPainting> Paintings => paintings;
        public IReadOnlyList<string> Clues => clues;
        public int SlotCount => solution.Length;

        public GalleryPuzzle(GalleryPainting[] paintings, int[] solution, string[] clues)
        {
            if (paintings == null || solution == null) throw new System.ArgumentNullException();
            if (paintings.Length != solution.Length)
                throw new System.ArgumentException("Every painting needs exactly one slot.");
            this.paintings = paintings;
            this.solution = (int[])solution.Clone();
            this.clues = clues ?? new string[0];
            assignment = new int[solution.Length];
            for (int i = 0; i < assignment.Length; i++) assignment[i] = -1;
        }

        public int PaintingAt(int slot) =>
            slot >= 0 && slot < assignment.Length ? assignment[slot] : -1;

        public int SlotOf(int paintingIndex)
        {
            for (int i = 0; i < assignment.Length; i++)
                if (assignment[i] == paintingIndex) return i;
            return -1;
        }

        public bool IsPlaced(int paintingIndex) => SlotOf(paintingIndex) >= 0;

        public int PlacedCount()
        {
            int count = 0;
            foreach (int value in assignment) if (value >= 0) count++;
            return count;
        }

        /// <summary>Hangs a painting. If it was already hanging somewhere, it moves.</summary>
        public bool Place(int paintingIndex, int slot)
        {
            if (paintingIndex < 0 || paintingIndex >= paintings.Length) return false;
            if (slot < 0 || slot >= assignment.Length) return false;
            int current = SlotOf(paintingIndex);
            if (current == slot) return false;
            if (current >= 0) assignment[current] = -1;
            assignment[slot] = paintingIndex;
            return true;
        }

        public void Remove(int slot)
        {
            if (slot >= 0 && slot < assignment.Length) assignment[slot] = -1;
        }

        public bool IsCorrectAt(int slot) =>
            slot >= 0 && slot < assignment.Length && assignment[slot] == solution[slot];

        public bool IsSlotEmpty(int slot) =>
            slot >= 0 && slot < assignment.Length && assignment[slot] < 0;

        public bool IsSolved()
        {
            for (int i = 0; i < assignment.Length; i++)
                if (assignment[i] != solution[i]) return false;
            return true;
        }

        public int CorrectCount()
        {
            int count = 0;
            for (int i = 0; i < assignment.Length; i++)
                if (assignment[i] == solution[i]) count++;
            return count;
        }

        public void Reset()
        {
            for (int i = 0; i < assignment.Length; i++) assignment[i] = -1;
        }
    }
}
