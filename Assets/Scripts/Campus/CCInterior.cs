using System.Collections.Generic;
using EspacoCultural.Audio;
using EspacoCultural.Jogo;
using UnityEngine;

namespace EspacoCultural.Campus
{
    /// <summary>
    /// Centro de Convivência por dentro (uma tela): a zeladora do turno da noite (empresta a chave do
    /// registro da fonte e dá dicas) e a máquina de café (zera o cansaço, uma ficha por noite).
    /// </summary>
    public class CCInterior : CampusScene
    {
        protected override float WorldWidth => 640;
        protected override Rect WalkArea => new Rect(24, 10, 570, 88);
        protected override string MapHere => "cc";
        protected override string DefaultSpawn => "cc_entrada";
        protected override Dictionary<string, Vector2> SpawnPoints => new Dictionary<string, Vector2>
        {
            { "cc_entrada", new Vector2(70, 40) },
        };
        protected override float GlobalIntensity => 0.7f;

        private const float CoffeeX = 600, ZeladoraX = 452;

        protected override void BuildWorld()
        {
            Background("fundo_int_cc", 0);
            PointLight("Cafe", new Vector2(CoffeeX + 10, 130), 130, new Color(1f, 0.45f, 0.4f), 1.2f);
            PointLight("Luminaria1", new Vector2(150, 190), 150, WindowWarm, 0.8f);
            PointLight("Luminaria2", new Vector2(265, 190), 150, WindowWarm, 0.8f);
            PointLight("PostesFora", new Vector2(40, 180), 120, new Color(1f, 0.5f, 0.45f), 0.8f);

            var z = Prop("Zeladora", "zeladora", new Vector2(ZeladoraX, 92), 121);
            if (z != null) z.flipX = true;
            AddSpot("Zeladora", new Vector2(ZeladoraX, 90), () => "conversar com a Dona Socorro (zeladora)", TalkZeladora);
            AddSpot("Cafe", new Vector2(CoffeeX - 10, 88), () => GameState.Flag("cafe") ? "máquina de café (sem ficha)" : "tomar um café", Coffee);
            AddSpot("Saida", new Vector2(40, 40), () => "voltar para a praça", () => GoTo("Campus", "cc_porta"), null, 40);
        }

        private void TalkZeladora()
        {
            if (!GameState.Flag("fonte_desligada") && !GameState.Has("chave_registro"))
            {
                SayThen("DONA SOCORRO (ZELADORA)", () =>
                    {
                        GameState.Give("chave_registro");
                        GameAudio.Sfx("sfx_pegar");
                        hud.Toast("Você recebeu a chave do registro.", 2f);
                    },
                    "Boa noite, seu guarda! Vi uma ema passar correndo pela praça, acredita? Ia atrás de um bichinho voando.",
                    "Uma estátua na fonte? Valha! Pra tirar sem molhar, tem que fechar o registro.",
                    "Toma a chave do cadeado. Depois me devolve, viu?");
                return;
            }
            if (!GameState.Flag("cafe"))
            {
                Say("DONA SOCORRO (ZELADORA)", "Tá com cara de sono, hein? A máquina de café ali aceita uma ficha. Tem uma no seu bolso, não tem?");
                return;
            }
            Say("DONA SOCORRO (ZELADORA)", "Dizem que o ginásio guarda os fusíveis de tudo quanto é prédio. E a biblioteca tem catálogo até de exposição!");
        }

        private void Coffee()
        {
            if (GameState.Flag("cafe")) { hud.Toast("A ficha do café já foi. Agora é só no fim do turno.", 2f); return; }
            GameState.Set("cafe");
            GameState.Fatigue = 0f;
            GameState.AddFatigue(0f);
            GameAudio.Sfx("sfx_sucesso", 0.6f);
            hud.Popup("CAFÉ", "Um cafezinho quente da máquina. O cansaço sumiu!\n\n(Só tinha uma ficha: use bem.)");
        }
    }
}
