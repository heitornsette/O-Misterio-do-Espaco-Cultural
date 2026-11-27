using System.Collections.Generic;
using EspacoCultural.Audio;
using EspacoCultural.Jogo;
using UnityEngine;
using UnityEngine.UI;

namespace EspacoCultural.Gameplay
{
    /// <summary>
    /// Andar 2 — Galeria: dedução quadro → prego, com 5 obras e 5 placas.
    /// As etiquetas com autor e ano caíram e foram pisoteadas: sem o catálogo da exposição
    /// (Biblioteca Central) só se vê título, paleta e tema, e o puzzle tem 36 soluções possíveis.
    /// Com o catálogo a solução é única (verificado por força bruta nas 120 permutações):
    ///   A igreja (1921, a mais antiga) · B sertão (mesma autora de A, 13 anos depois)
    ///   C rendeiras (fria, antes de E) · D praia (quente) · E serra (1987, a mais recente).
    /// Com os 5 pendurados, o painel diz QUANTOS estão certos (não quais).
    /// Saídas: porta (12,0) → acervo depois de resolvido; escada de serviço (0,5) → térreo.
    /// </summary>
    public class Andar2Galeria : FloorBase
    {
        private class Painting
        {
            public string id, tex, title, palette, theme, author;
            public int year;
            public Vector2Int floorCell;
            public GridActor floorActor;   // null quando está na mão ou na parede
        }

        private class Slot
        {
            public string letter, clue, answer;
            public int column, wallX;
            public RawImage hung;
            public Painting painting;
        }

        private readonly List<Painting> paintings = new List<Painting>();
        private readonly List<Slot> slots = new List<Slot>();
        private Painting carrying;
        private RawImage carryIcon;
        private static readonly Vector2Int DoorCell = new Vector2Int(12, 0);
        private static readonly Vector2Int ServiceCell = new Vector2Int(0, 5);

        private const int WallTop = 70;

        protected override int FloorNumber => 2;
        protected override string BackgroundName => "fundo_andar2";
        protected override FloorGrid CreateGrid() => new FloorGrid(180, 6);
        protected override Vector2Int DefaultStart => new Vector2Int(0, 2);
        protected override Dictionary<string, (Vector2Int, Vector2Int)> SpawnPoints => new Dictionary<string, (Vector2Int, Vector2Int)>
        {
            { "andar2_entrada", (new Vector2Int(0, 2), new Vector2Int(1, 0)) },
            { "andar2_porta", (new Vector2Int(11, 0), Down) },
        };
        protected override string IntroTitle => "ANDAR 2 — GALERIA DE PINTURAS";
        protected override string IntroText =>
            "Cinco quadros foram derrubados e as etiquetas com autor e ano caíram e foram pisoteadas.\n\n" +
            "Cada placa da parede dá uma pista. Pendure cada quadro no prego certo para destravar a porta do acervo.\n" +
            "Sem autor e ano fica difícil... O catálogo da exposição deve ter tudo: talvez na Biblioteca Central.\n\n" +
            "E de frente para um quadro: pegar   |   embaixo de um prego, olhando para cima: E";

        private bool HasCatalog => GameState.Has("catalogo");

        protected override string ObjectiveText()
        {
            if (Solved) return "A porta destravou!\nSiga pela porta à direita.";
            int n = 0; foreach (var s in slots) if (s.painting != null) n++;
            string t = $"Pendure cada quadro no prego certo.\nNa parede: {n}/{slots.Count}";
            if (!HasCatalog) t += "\nFaltam autor e ano (catálogo?)";
            else if (carrying != null) t += $"\nNa mão: {carrying.title}";
            return t;
        }

        protected override void BuildLevel()
        {
            paintings.Add(new Painting { id = "igreja", tex = "quadro_igreja", title = "Matriz de Aquiraz", author = "C. Bezerra", year = 1921,
                palette = "quente (ocre, branco e azul-claro)", theme = "a igreja matriz e sua praça", floorCell = new Vector2Int(9, 2) });
            paintings.Add(new Painting { id = "praia", tex = "quadro_praia", title = "Jangadas no Mucuripe", author = "R. Lima", year = 1958,
                palette = "quente (laranja do entardecer)", theme = "jangadas voltando à praia", floorCell = new Vector2Int(3, 4) });
            paintings.Add(new Painting { id = "serra", tex = "quadro_serra", title = "Manhã em Guaramiranga", author = "M. Alencar", year = 1987,
                palette = "fria (verdes e cinza da neblina)", theme = "a serra ao amanhecer", floorCell = new Vector2Int(6, 3) });
            paintings.Add(new Painting { id = "sertao", tex = "quadro_sertao", title = "Mandacarus ao Meio-Dia", author = "C. Bezerra", year = 1934,
                palette = "quente (terra e amarelo de sol)", theme = "o sertão seco", floorCell = new Vector2Int(2, 2) });
            paintings.Add(new Painting { id = "rendeiras", tex = "quadro_rendeiras", title = "Rendeiras da Prainha", author = "R. Lima", year = 1972,
                palette = "fria (azul do mar e branco da renda)", theme = "rendeiras trabalhando à sombra", floorCell = new Vector2Int(10, 4) });

            slots.Add(new Slot { letter = "A", column = 1, wallX = 66, answer = "igreja", clue = "A obra mais antiga da sala." });
            slots.Add(new Slot { letter = "B", column = 3, wallX = 160, answer = "sertao", clue = "Mesma autoria da obra da placa A, pintada 13 anos depois." });
            slots.Add(new Slot { letter = "C", column = 4, wallX = 212, answer = "rendeiras", clue = "Paleta fria. Pintada antes da obra da placa E." });
            slots.Add(new Slot { letter = "D", column = 8, wallX = 434, answer = "praia", clue = "Paleta quente." });
            slots.Add(new Slot { letter = "E", column = 11, wallX = 565, answer = "serra", clue = "A obra mais recente da sala." });

            foreach (var s in slots)
            {
                Px.Raw("Prego_" + s.letter, floorLayer, Px.Tex("prego"), s.wallX - 2, WallTop - 6);
                Px.Raw("Placa_" + s.letter, floorLayer, Px.Tex("placa"), s.wallX - 8, WallTop + 44);
                Px.Label("Letra_" + s.letter, floorLayer, fontBody, 8, new Color(0.9f, 0.95f, 1f), s.wallX - 8, WallTop + 45, 16, 10, TextAnchor.UpperCenter).text = s.letter;
                var tag = Px.Box("Vazio_" + s.letter, floorLayer, new Color(1f, 1f, 1f, 0.08f), s.wallX - 24, WallTop, 48, 37);
                var hung = Px.Raw("Quadro_" + s.letter, floorLayer, null, s.wallX - 24, WallTop, 48, 37);
                hung.gameObject.SetActive(false);
                s.hung = hung;
                _ = tag;
            }

            // Estado: resolvido, salvo ou inicial
            if (AlreadySolved)
            {
                foreach (var s in slots) Hang(s, paintings.Find(p => p.id == s.answer));
            }
            else if (GameState.A2Slot != null)
            {
                for (int i = 0; i < slots.Count; i++)
                    if (GameState.A2Slot[i] >= 0) Hang(slots[i], paintings[GameState.A2Slot[i]]);
                for (int i = 0; i < paintings.Count; i++)
                    if (GameState.A2Floor[i] >= 0) PutOnFloor(paintings[i], Cell(GameState.A2Floor[i]));
                if (GameState.A2Carry >= 0) carrying = paintings[GameState.A2Carry];
            }
            else foreach (var p in paintings) PutOnFloor(p, p.floorCell);

            carryIcon = Px.Raw("NaMao", fx, null, 0, 0, 48, 37);
            carryIcon.gameObject.SetActive(false);

            AddStepExit(DoorCell, () => Solved, () =>
            {
                if (!GameState.Flag("visto_cc03")) { GameState.Set("visto_cc03"); GoTo(Misterio.Puzzles.RouteRules.Cutscene03); }
                else GoTo("Andar3", "andar3_entrada");
            });
            ExitSign(ServiceCell, "TÉRREO");
            AddStepExit(ServiceCell, null, GoDown);
        }

        protected override void OnEntered()
        {
            if (HasCatalog && !GameState.Flag("catalogo_lido"))
            {
                GameState.Set("catalogo_lido");
                Hud.Toast("Com o catálogo, as fichas agora mostram autor e ano.", 3f);
            }
        }

        protected override void SaveProgress()
        {
            var slotArr = new int[slots.Count];
            for (int i = 0; i < slots.Count; i++) slotArr[i] = slots[i].painting != null ? paintings.IndexOf(slots[i].painting) : -1;
            var floorArr = new int[paintings.Count];
            for (int i = 0; i < paintings.Count; i++) floorArr[i] = paintings[i].floorActor != null ? Idx(paintings[i].floorCell) : -1;
            GameState.A2Slot = slotArr;
            GameState.A2Floor = floorArr;
            GameState.A2Carry = carrying != null ? paintings.IndexOf(carrying) : -1;
        }

        private void Hang(Slot s, Painting p)
        {
            s.painting = p;
            s.hung.texture = Px.Tex(p.tex);
            s.hung.gameObject.SetActive(true);
        }

        private void PutOnFloor(Painting p, Vector2Int c)
        {
            p.floorCell = c;
            p.floorActor = Spawn("Quadro_" + p.id, p.tex, c, 36);
        }

        private Painting PaintingAt(Vector2Int c)
        {
            foreach (var p in paintings) if (p.floorActor != null && p.floorCell == c) return p;
            return null;
        }

        private Slot SlotFor(Vector2Int cell, Vector2Int facing)
        {
            if (cell.y != 0 || facing != Up) return null;
            foreach (var s in slots) if (s.column == cell.x) return s;
            return null;
        }

        public override bool IsBlocked(Vector2Int c) => PaintingAt(c) != null || (!Solved && c == DoorCell);

        private string Ficha(Painting p)
        {
            string autor = HasCatalog ? $"{p.author}, {p.year}." : "??? (etiqueta pisoteada)";
            return $"Autoria e ano: {autor}\nÓleo sobre tela.\nPaleta {p.palette}.\nTema: {p.theme}.";
        }

        public override void Interact(PlayerController p, Vector2Int cell, Vector2Int facing)
        {
            var slot = SlotFor(cell, facing);
            if (slot != null) { UseSlot(slot); return; }

            var front = cell + facing;
            var onFloor = PaintingAt(front);
            if (onFloor != null && carrying == null)
            {
                sortables.Remove(onFloor.floorActor);
                Destroy(onFloor.floorActor.gameObject);
                onFloor.floorActor = null;
                carrying = onFloor;
                GameAudio.Sfx("sfx_pegar");
                p.PlayOnce(Px.Tex("guarda_pegar_frente_sheet"), 18f, () =>
                    Hud.Popup(onFloor.title.ToUpper(), Ficha(onFloor) + "\n\nAgora encontre o prego certo na parede."));
                Hud.SetObjective(ObjectiveText());
                return;
            }

            if (carrying != null && onFloor == null && Grid.Inside(front) && !IsBlocked(front) && front != p.actor.cell && front != ServiceCell)
            {
                PutOnFloor(carrying, front);
                carrying = null;
                Hud.SetObjective(ObjectiveText());
            }
        }

        private void UseSlot(Slot s)
        {
            if (Solved) { Hud.Popup($"PLACA {s.letter}", s.clue); return; }
            if (carrying != null)
            {
                if (s.painting != null) { Hud.Toast("Já tem um quadro neste prego.\nRetire-o primeiro (E sem nada na mão).", 3f); return; }
                Hang(s, carrying);
                carrying = null;
                GameAudio.Sfx("sfx_encaixe");
                Hud.SetObjective(ObjectiveText());
                CheckWall();
                return;
            }
            if (s.painting != null)
            {
                carrying = s.painting;
                s.painting = null;
                s.hung.gameObject.SetActive(false);
                Hud.Toast($"Você tirou \"{carrying.title}\" da parede.", 2f);
                Hud.SetObjective(ObjectiveText());
                return;
            }
            Hud.Popup($"PLACA {s.letter}", s.clue + "\n\nTraga um quadro e aperte E aqui para pendurar.");
        }

        private void CheckWall()
        {
            foreach (var s in slots) if (s.painting == null) return;
            int right = 0;
            foreach (var s in slots) if (s.painting.id == s.answer) right++;
            if (right == slots.Count)
            {
                MarkSolved("*clique magnético* Tudo certo por aqui.\nA porta do acervo destravou!");
                return;
            }
            GameAudio.Sfx("sfx_erro");
            Hud.Toast($"O painel da porta acende: {right} de {slots.Count} obras no lugar certo.\nReleia as placas e as fichas.", 4f);
        }

        protected override string ContextHint(Vector2Int cell, Vector2Int facing)
        {
            var s = SlotFor(cell, facing);
            if (s != null)
            {
                string action = Solved ? "E ler" : carrying != null ? (s.painting == null ? "E pendurar" : "prego ocupado") : (s.painting != null ? "E retirar" : "E ler");
                return $"PLACA {s.letter}: {s.clue}   [{action}]";
            }
            if (cell.y == 0)
                foreach (var sl in slots) if (sl.column == cell.x) return $"Aperte CIMA para usar o prego {sl.letter}.";
            var front = cell + facing;
            var p = PaintingAt(front);
            if (p != null && carrying == null) return $"E pegar \"{p.title}\"";
            if (carrying != null) return $"Na mão: {carrying.title} — leve até um prego   (E no chão vazio: largar)";
            return null;
        }

        protected override void LateUpdate()
        {
            base.LateUpdate();
            if (carryIcon == null || Player == null) return;
            bool show = carrying != null;
            if (carryIcon.gameObject.activeSelf != show) carryIcon.gameObject.SetActive(show);
            if (!show) return;
            carryIcon.texture = Px.Tex(carrying.tex);
            var f = Player.actor.FeetPos;
            Px.Place(carryIcon.rectTransform, f.x - 24, f.y - 140, 48, 37);
        }
    }
}
