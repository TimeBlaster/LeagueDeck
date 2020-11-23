using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
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

        private const string cUpdateImageName = "updating@2x.png";
        private static readonly string _pluginImageFolder = Path.Combine(Environment.CurrentDirectory, "Images", "Plugin");
        private static Image _updateImage;

        public static bool InputRunning { get; private set; }

        #endregion

        #region Public Methods

        public static Image GetUpdateImage()
        {
            if (_updateImage == null)
                _updateImage = Image.FromFile(Path.Combine(_pluginImageFolder, cUpdateImageName));

            return _updateImage;
        }

        public static void AddChampionToSpellImage(Image spellImage, Image championImage)
        {
            using (var g = Graphics.FromImage(spellImage))
            {
                var bounds = new Rectangle(0, 0, 32, 32);
                g.DrawImage(championImage, bounds);
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

            Thread.Sleep(20);

            // enter message
            iis.Keyboard.TextEntry(message);

            // fixes the chat not closing, thanks Timmy
            Thread.Sleep(20);

            // send message
            iis.Keyboard.KeyDown(WindowsInput.Native.VirtualKeyCode.RETURN);

            InputRunning = false;
        }

        public static async Task<string> GetApiResponse(string url, CancellationToken ct)
        {
            HttpWebResponse response = null;
            while (response == null)
            {
                try
                {
                    if (ct.IsCancellationRequested)
                        return string.Empty;

                    var request = (HttpWebRequest)WebRequest.Create(url);

                    // accept all SSL certificates
                    request.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;

                    response = (HttpWebResponse)await request.GetResponseAsync();
                }
                catch
                {
                    await Task.Delay(500, ct);
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
