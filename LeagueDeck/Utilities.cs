using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using WindowsInput;

namespace LeagueDeck
{
    public static class Utilities
    {
        #region vars

        public const string cLeagueOfLegendsProcessName = "League of Legends.exe";

        private static readonly string _pluginImageFolder = Path.Combine(Environment.CurrentDirectory, "Images", "Plugin");

        private static readonly ColorMatrix grayscaleMatrix = new ColorMatrix(
            new float[][] {
                new float[] { .3f, .3f, .3f, 0, 0 },
                new float[] { .59f, .59f, .59f, 0, 0 },
                new float[] { .11f, .11f, .11f, 0, 0 },
                new float[] { 0, 0, 0, 1, 0 },
                new float[] { 0, 0, 0, 0, 1 }
            });

        private const string cUpdateImageName = "updating@2x.png";
        private const string cCheckMarkImageName = "check@2x.png";

        private static Image _updateImage;
        private static Image _checkMarkImage;

        public static bool InputRunning { get; private set; }

        #endregion

        #region Public Methods

        public static Image GetUpdateImage()
        {
            if (_updateImage == null)
                _updateImage = Image.FromFile(Path.Combine(_pluginImageFolder, cUpdateImageName));

            lock (_updateImage)
            {
                return (Image)_updateImage.Clone();
            }
        }

        public static Image GetCheckMarkImage()
        {
            if (_checkMarkImage == null)
                _checkMarkImage = Image.FromFile(Path.Combine(_pluginImageFolder, cCheckMarkImageName));

            lock (_checkMarkImage)
            {
                return (Image)_checkMarkImage.Clone();
            }
        }

        public static Image AddChampionToSpellImage(Image spellImage, Image championImage)
        {
            Bitmap image = (Bitmap)spellImage.Clone();

            using (var g = Graphics.FromImage(image))
            {
                var bounds = new Rectangle(0, 0, 32, 32);
                g.DrawImage(championImage, bounds);
            }

            return image;
        }

        public static Image AddCheckMarkToImage(Image source)
        {
            Bitmap image = (Bitmap)source.Clone();

            using (var g = Graphics.FromImage(image))
            {
                var bounds = new Rectangle(0, 0, source.Width, source.Height);
                var check = GetCheckMarkImage();
                g.DrawImage(check, bounds);
            }

            return image;
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
            iis.Keyboard.KeyPress(WindowsInput.Native.VirtualKeyCode.RETURN);

            Thread.Sleep(20);

            // enter message
            iis.Keyboard.TextEntry(message);

            // fixes the chat not closing, thanks Timmy
            Thread.Sleep(20);

            // send message
            iis.Keyboard.KeyPress(WindowsInput.Native.VirtualKeyCode.RETURN);

            InputRunning = false;
        }

        #endregion
    }
}
