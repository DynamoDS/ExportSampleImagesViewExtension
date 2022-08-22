using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Dialogs;
using MessageBox = System.Windows.Forms.MessageBox;

namespace ExportSampleImages
{
    public static class Utilities
    {

        /// <summary>
        ///     Returns a list of files of given path and extension
        /// </summary>
        /// <param name="path"></param>
        /// <param name="extension"></param>
        /// <returns></returns>
        public static IEnumerable<string> GetAllFilesOfExtension(string path, string extension = ".dyn")
        {
            var files = Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories)
                .Where(x => extension.Equals(Path.GetExtension(x).ToLowerInvariant()));

            return files;
        }

        public static string GetFolder()
        {
            var folder = "";

            using (var fbd = new FolderBrowserDialog())
            {
                var result = fbd.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrEmpty(fbd.SelectedPath)) folder = fbd.SelectedPath;
            }

            return folder;
        }


        /// <summary>
        ///     Resize logic for bitmap images.
        ///     Will resize the image up/down to the smaller/bigger dimension
        /// </summary>
        /// <param name="sourceImg">The image to match the size to</param>
        /// <param name="targetImg">The image to be resized</param>
        /// <returns></returns>
        public static Bitmap Resize(Bitmap sourceImg, Bitmap targetImg)
        {
            var scaleFactor = Math.Min(sourceImg.Width / (float) targetImg.Width,
                sourceImg.Height / (float) targetImg.Height);
            var newWidth = (int) (targetImg.Width * scaleFactor);
            var newHeight = (int) (targetImg.Height * scaleFactor);
            var newImage = new Bitmap(newWidth, newHeight);

            using (var graphics = Graphics.FromImage(newImage))
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.DrawImage(targetImg, new Rectangle(0, 0, newWidth, newHeight));
                return newImage;
            }
        }

        /// <summary>
        ///     Saves a bitmap image to png
        /// </summary>
        /// <param name="image"></param>
        /// <param name="path"></param>
        /// <param name="name"></param>
        public static void SaveBitmapToPng(Bitmap image, string path, string name)
        {
            if (image == null) return;
            image.Save(Path.Combine(path, name + ".png"), ImageFormat.Png);
        }

        /// <summary>
        ///     Combines 2 bitmap images with transparent background
        /// </summary>
        /// <param name="background"></param>
        /// <param name="foreground"></param>
        /// <returns></returns>
        public static Bitmap OverlayImages(string background, string foreground)
        {
            Bitmap finalImage;

            using (var baseImage = (Bitmap) Image.FromFile(background))
            {
                using (var overlayImage = (Bitmap) Image.FromFile(foreground))
                {
                    var resizedImage = Resize(baseImage, overlayImage);

                    finalImage = new Bitmap(baseImage.Width, baseImage.Height, PixelFormat.Format32bppArgb);

                    GetCurrentDPI(out var dpiX, out var dpiY);

                    finalImage.SetResolution(dpiX, dpiY);
                    var graphics = Graphics.FromImage(finalImage);

                    graphics.CompositingMode = CompositingMode.SourceOver;
                    graphics.DrawImage(baseImage, 0, 0);
                    graphics.DrawImage(resizedImage,
                        Convert.ToInt32((baseImage.Width - resizedImage.Width) * (float) 0.5),
                        Convert.ToInt32((baseImage.Height - resizedImage.Height) *
                                        (float) 0.5)); // Center the overlaid image
                }
            }

            return finalImage;
        }


        /// <summary>
        ///     Retrieve the current system DPI settings
        ///     Uses reflection, does not need a Control
        /// </summary>
        private static void GetCurrentDPI(out int dpiX, out int dpiY)
        {
            var dpiXProperty =
                typeof(SystemParameters).GetProperty("DpiX", BindingFlags.NonPublic | BindingFlags.Static);
            var dpiYProperty =
                typeof(SystemParameters).GetProperty("Dpi", BindingFlags.NonPublic | BindingFlags.Static);

            dpiX = (int) dpiXProperty.GetValue(null, null);
            dpiY = (int) dpiYProperty.GetValue(null, null);
        }
    }
}