using System.Collections.Generic;
using EspacoCultural.Audio;
using EspacoCultural.Jogo;
using UnityEngine;

namespace EspacoCultural.Campus
{
    /// <summary>
    /// Ginásio por dentro (uma tela): caixa de achados e perdidos (bola de borracha para o acervo),
    /// quadro de luz com os fusíveis reserva (o guarda leva um de cada vez; o certo para o salão é o de 25 A,
    /// circuito EC-4) e a mariposa rondando um refletor (pista opcional).
    /// </summary>
    public class GinasioInterior : CampusScene
    {
        protected override float WorldWidth => 640;
        protected override Rect WalkArea => new Rect(24, 8, 580, 100);
        protected override string MapHere => "gi";
        protected override string DefaultSpawn => "gin_entrada";
        protected override Dictionary<string, Vector2> SpawnPoints => new Dictionary<string, Vector2>
        {
            { "gin_entrada", new Vector2(56, 40) },
        };
        protected override float GlobalIntensity => 0.55f;

        private const float BoxX = 566, PanelX = 606, MothX = 320;
        private static readonly int[] Amps = { 10, 16, 25, 32 };

        protected override void BuildWorld()
        {
            Background("fundo_int_gin", 0);
            PointLight("Refletor1", new Vector2(200, 275), 230, CoolWhite, 0.9f);
            PointLight("Refletor2", new Vector2(MothX, 298), 240, CoolWhite, 1.0f);
            PointLight("Refletor3", new Vector2(438, 275), 230, CoolWhite, 0.9f);
            PointLight("Quadro", new Vector2(PanelX, 180), 60, new Color(0.6f, 1f, 0.7f), 0.6f);

            var moth = CampusArt.Tex("mariposa_voo_sheet_16");
            if (moth != null) StartCoroutine(MothRoutine());

            AddSpot("Achados", new Vector2(BoxX, 96), () => GameState.Has("bola") ? "achados e perdidos" : "mexer nos achados e perdidos", UseBox);
            AddSpot("Quadro", new Vector2(PanelX, 108), () => "quadro de luz (fusíveis reserva)", UsePanel);
            AddSpot("Mariposa", new Vector2(MothX, 60), () => "olhar para cima", () =>
                Clue("mariposa", "Uma mariposa clara voa em volta do refletor aceso. Uma pena cinza está presa na rede " +
                                 "logo abaixo... A Moema esteve aqui correndo atrás dela?"), null, 40);
            AddSpot("Saida", new Vector2(36, 40), () => "sair do ginásio", () => GoTo("Campus", "gin_porta"), null, 34);
        }

        private System.Collections.IEnumerator MothRoutine()
        {
            var frames = CampusArt.Sheet("mariposa_voo_sheet_16", 8);
            if (frames.Length == 0) yield break;
            var sr = PropSprite("Mariposa", frames[0], new Vector2(MothX, 292), false);
            sr.sortingOrder = 3000;
            float t = 0;
            while (sr != null)
            {
                t += Time.deltaTime;
                sr.transform.position = new Vector3(Mathf.Round(MothX + Mathf.Cos(t * 1.7f) * 26), Mathf.Round(285 + Mathf.Sin(t * 2.9f) * 12), 0);
                sr.sprite = frames[Mathf.FloorToInt(t * 12) % frames.Length];
                sr.flipX = Mathf.Sin(t * 1.7f) > 0;
                yield return null;
            }
        }

        private void UseBox()
        {
            if (GameState.Has("bola") || GameState.Flag("resolvido3"))
            {
                hud.Toast("Um tênis sem par, uma garrafinha, um apito... nada mais que sirva.", 2.5f);
                return;
            }
            GameState.Give("bola");
            GameAudio.Sfx("sfx_pegar");
            hud.Popup("BOLA DE BORRACHA", "Uma bolinha de borracha vermelha, dos achados e perdidos.\n\n" +
                "Emas são curiosas: se rolar perto de uma, ela vai querer bicar. (No acervo, E arremessa.)");
        }

        private void UsePanel()
        {
            int carrying = GameState.FuseCarried();
            var opts = new List<string>();
            foreach (var a in Amps) opts.Add(carrying == a ? $"Fusível de {a} A  (na sua mão: devolver)" : $"Fusível de {a} A");
            opts.Add("(fechar)");
            string header =
                "Etiqueta da tampa — reservas por circuito:\n" +
                "GIN-1 quadra: 16 A   ·   GIN-2 vestiários: 32 A\n" +
                "BIB-2 leitura: 10 A   ·   EC-4 salão principal do Espaço Cultural: 25 A";
            if (GameState.A4Fuse) header += "\n(O do EC-4 já foi instalado.)";
            choice.Show("QUADRO DE LUZ", header, opts.ToArray(), i =>
            {
                if (i < 0 || i >= Amps.Length) return;
                int a = Amps[i];
                if (carrying == a)
                {
                    GameState.Take("fusivel_" + a);
                    hud.Toast($"Você devolveu o fusível de {a} A.", 1.8f);
                    return;
                }
                if (carrying > 0) GameState.Take("fusivel_" + carrying);
                GameState.Give("fusivel_" + a);
                GameAudio.Sfx("sfx_pegar");
                hud.Toast(carrying > 0 ? $"Trocou o de {carrying} A pelo de {a} A." : $"Você pegou o fusível de {a} A.", 2f);
            });
        }
    }
}
