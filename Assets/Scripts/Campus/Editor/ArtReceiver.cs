using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace EspacoCultural.Campus.EditorTools
{
    /// <summary>
    /// Receptor de arte (só no Editor): escuta em http://localhost:8765/ e salva em
    /// &lt;projeto&gt;/ArteRecebida/&lt;nome&gt;.png as imagens enviadas por POST (corpo = PNG em base64
    /// ou data URL), por exemplo a partir da página do PixelLab. Serve para trazer as artes geradas
    /// para o projeto sem passar pela pasta Downloads. Liga sozinho ao abrir o projeto;
    /// menu "Espaço Cultural › Arte › Receptor do PixelLab" liga/desliga.
    /// </summary>
    [InitializeOnLoad]
    public static class ArtReceiver
    {
        private const int Port = 8765;
        private const string PrefKey = "EspacoCultural.ArtReceiver.On";
        private static HttpListener listener;
        private static Thread thread;

        static ArtReceiver()
        {
            if (EditorPrefs.GetBool(PrefKey, true)) Start();
            AssemblyReloadEvents.beforeAssemblyReload += Stop;
            EditorApplication.quitting += Stop;
        }

        [MenuItem("Espaço Cultural/Arte/Receptor do PixelLab (ligar ou desligar)")]
        private static void Toggle()
        {
            bool on = listener == null;
            EditorPrefs.SetBool(PrefKey, on);
            if (on) Start(); else Stop();
            Debug.Log(on ? "[Arte] Receptor ligado em http://localhost:8765/" : "[Arte] Receptor desligado.");
        }

        private static string OutDir => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "ArteRecebida"));

        private static void Start()
        {
            if (listener != null) return;
            try
            {
                listener = new HttpListener();
                listener.Prefixes.Add($"http://localhost:{Port}/");
                listener.Prefixes.Add($"http://127.0.0.1:{Port}/");
                listener.Start();
                thread = new Thread(Loop) { IsBackground = true, Name = "ArtReceiver" };
                thread.Start();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Arte] Não deu para ligar o receptor: " + e.Message);
                listener = null;
            }
        }

        private static void Stop()
        {
            try { listener?.Stop(); listener?.Close(); } catch { }
            listener = null;
        }

        private static void Loop()
        {
            while (listener != null && listener.IsListening)
            {
                HttpListenerContext ctx;
                try { ctx = listener.GetContext(); }
                catch { break; }
                try { Handle(ctx); }
                catch (Exception e) { Reply(ctx, 500, "erro: " + e.Message); }
            }
        }

        private static void Handle(HttpListenerContext ctx)
        {
            var req = ctx.Request;
            if (req.HttpMethod == "OPTIONS") { Reply(ctx, 204, ""); return; }
            if (req.HttpMethod == "GET") { Reply(ctx, 200, "ok"); return; }

            string name = req.QueryString["name"];
            if (string.IsNullOrEmpty(name)) { Reply(ctx, 400, "falta ?name="); return; }
            foreach (char c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');

            string body;
            using (var r = new StreamReader(req.InputStream, Encoding.UTF8)) body = r.ReadToEnd();
            // Formulário HTML (envio por navegação, que o navegador não bloqueia): data=<data URL>
            if (body.StartsWith("data=")) body = WebUtility.UrlDecode(body.Substring(5));
            int comma = body.IndexOf(',');
            if (body.StartsWith("data:") && comma > 0) body = body.Substring(comma + 1);
            byte[] bytes = Convert.FromBase64String(body.Trim());

            Directory.CreateDirectory(OutDir);
            string path = Path.Combine(OutDir, name.EndsWith(".png") ? name : name + ".png");
            File.WriteAllBytes(path, bytes);
            Reply(ctx, 200, "salvo " + path + " (" + bytes.Length + " bytes)");
        }

        private static void Reply(HttpListenerContext ctx, int status, string text)
        {
            var res = ctx.Response;
            res.StatusCode = status;
            res.AddHeader("Access-Control-Allow-Origin", "*");
            res.AddHeader("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
            res.AddHeader("Access-Control-Allow-Headers", "*");
            res.AddHeader("Access-Control-Allow-Private-Network", "true");
            var buf = Encoding.UTF8.GetBytes(text);
            res.ContentLength64 = buf.Length;
            res.OutputStream.Write(buf, 0, buf.Length);
            res.OutputStream.Close();
        }
    }
}
