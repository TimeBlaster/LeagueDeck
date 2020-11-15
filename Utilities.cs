using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using WindowsInput;

namespace LeagueDeck
{
    public static class Utilities
    {
        #region vars

        private static readonly ColorMatrix grayscaleMatrix = new ColorMatrix(
            new float[][] {
                new float[] { .3f, .3f, .3f, 0, 0 },
                new float[] { .59f, .59f, .59f, 0, 0 },
                new float[] { .11f, .11f, .11f, 0, 0 },
                new float[] { 0, 0, 0, 1, 0 },
                new float[] { 0, 0, 0, 0, 1 }
            });

        public static bool InputRunning { get; private set; }

        public const string cInGameApiBaseUrl = "https://127.0.0.1:2999/liveclientdata";

        #endregion

        #region Public Methods

        public static Image GetSummonerSpellImage(string spell)
        {
            // Format: "{Namespace}.{Folder}.{filename}.{Extension}"
            var path = $"LeagueDeck.SummonerSpells.{spell}.png";

            var assembly = typeof(LeagueDeckPlugin).GetTypeInfo().Assembly;

            var resourcePath = assembly.GetManifestResourceNames()
                .FirstOrDefault(str => str.EndsWith(path));

            // fallback for new summoner spells
            if (string.IsNullOrEmpty(resourcePath))
            {
                path = $"LeagueDeck.SummonerSpells.Missing.png";
                resourcePath = assembly.GetManifestResourceNames()
                .Single(str => str.EndsWith(path));
            }

            using (Stream stream = assembly.GetManifestResourceStream(resourcePath))
            {
                return Image.FromStream(stream);
            }
        }

        public static void AddChampionToSpellImage(Image spellImage, string champion)
        {
            // Format: "{Namespace}.{Folder}.{filename}.{Extension}"
            var path = $"LeagueDeck.Champions.{champion}.png";

            var assembly = typeof(LeagueDeckPlugin).GetTypeInfo().Assembly;

            var resourcePath = assembly.GetManifestResourceNames()
                .FirstOrDefault(str => str.EndsWith(path));

            // fallback for new champions
            if (string.IsNullOrEmpty(resourcePath))
            {
                path = $"LeagueDeck.Champions.Missing.png";
                resourcePath = assembly.GetManifestResourceNames()
                .Single(str => str.EndsWith(path));
            }

            using (Stream stream = assembly.GetManifestResourceStream(resourcePath))
            {
                var championImage = Image.FromStream(stream);

                using (var g = Graphics.FromImage(spellImage))
                {
                    var bounds = new Rectangle(0, 0, 32, 32);
                    g.DrawImage(championImage, bounds);
                }
            }
        }

        public static Image GrayscaleImage(Image source)
        {
            Bitmap grayscaled = new Bitmap(source.Width, source.Height);

            using (Graphics g = Graphics.FromImage(grayscaled))
            {
                using (ImageAttributes attributes = new ImageAttributes())
                {
                    attributes.SetColorMatrix(grayscaleMatrix);
                    g.DrawImage(source, new Rectangle(0, 0, source.Width, source.Height), 0, 0, source.Width, source.Height, GraphicsUnit.Pixel, attributes);
                }
            }
            return grayscaled;
        }

        public static void SendMessageInChat(string message)
        {
            InputRunning = true;

            InputSimulator iis = new InputSimulator();

            // open chat
            iis.Keyboard.KeyDown(WindowsInput.Native.VirtualKeyCode.RETURN);

            // enter message
            iis.Keyboard.TextEntry(message);

            // fixes the chat not closing, thanks Timmy
            Thread.Sleep(10);

            // send message
            iis.Keyboard.KeyDown(WindowsInput.Native.VirtualKeyCode.RETURN);

            InputRunning = false;
        }

        public static async Task<string> GetApiResponse(string relativeUrl, CancellationToken ct)
        {
            var requestUrl = cInGameApiBaseUrl + relativeUrl;

            HttpWebResponse response = null;
            while (response == null)
            {
                try
                {
                    if (ct.IsCancellationRequested)
                        return string.Empty;

                    var request = (HttpWebRequest)WebRequest.Create(requestUrl);
                    response = (HttpWebResponse)await request.GetResponseAsync();
                }
                catch
                {
                    await Task.Delay(5000, ct);
                }
            }

            using (var stream = response.GetResponseStream())
            {
                using (var sr = new StreamReader(stream))
                {
                    return sr.ReadToEnd();
                }
            };
        }

        #endregion
    }
}
