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

        /// <summary>
        /// Returns true if both paths exist
        /// </summary>
        /// <param name="path1"></param>
        /// <param name="path2"></param>
        /// <returns></returns>
        public static bool AreValidPaths(string path1, string path2)
        {
            return Directory.Exists(path1) && Directory.Exists(path2);
        }


        /// <summary>
        ///     Resize logic for bitmap images.
        ///     Match target to source Image 
        /// </summary>
        /// <param name="sourceImg">The image to match the size to</param>
        /// <param name="targetImg">The image to be resized</param>
        /// <returns></returns>
        public static Bitmap Resize(Bitmap sourceImg, Bitmap targetImg, double scale = 1.0)
        {
            var scaleFactor = Math.Max(sourceImg.Width / (float) targetImg.Width,
                sourceImg.Height / (float) targetImg.Height);   // Take the smaller of the two ratios
            var newWidth = (int) (targetImg.Width * scaleFactor * scale);
            var newHeight = (int) (targetImg.Height * scaleFactor * scale);
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
        ///     Saves a bitmap image to jpg
        /// </summary>
        /// <param name="image"></param>
        /// <param name="path"></param>
        /// <param name="name"></param>
        public static void SaveBitmapToJpg(Bitmap image, string path, string name)
        {
            if (image == null) return;
            image.Save(Path.Combine(path, name + ".jpg"), ImageFormat.Jpeg);
        }

        /// <summary>
        ///     Combines 2 bitmap images with transparent background
        /// </summary>
        /// <param name="background"></param>
        /// <param name="foreground"></param>
        /// <returns></returns>
        public static Bitmap OverlayImages(string background, string foreground, double scale = 1.0)
        {
            Bitmap finalImage; 

            GetCurrentDPI(out var dpiX, out var dpiY);

            using (var baseImage = (Bitmap) Image.FromFile(background))
            {
                using (var overlayImage = (Bitmap) Image.FromFile(foreground))
                {
                    overlayImage.SetResolution(dpiX, dpiY);
                    var resizedImage = Resize(overlayImage, baseImage, scale);  // Resize the 3D background 

                    finalImage = new Bitmap(resizedImage.Width, resizedImage.Height, PixelFormat.Format32bppArgb);


                    finalImage.SetResolution(dpiX, dpiY);
                    using (var graphics = Graphics.FromImage(finalImage))
                    {
                        graphics.CompositingMode = CompositingMode.SourceOver;
                        graphics.DrawImage(resizedImage, 0, 0);
                        graphics.DrawImage(overlayImage,
                            Convert.ToInt32((resizedImage.Width - overlayImage.Width) * (float) 0.5),
                            Convert.ToInt32((resizedImage.Height - overlayImage.Height) *
                                            (float) 0.5)); // Center the overlaid image
                    }
                }
            }

            return finalImage;
        }

        public static Bitmap PrepareImages(string image, double scale = 1.0)
        {
            Bitmap finalImage;

            GetCurrentDPI(out var dpiX, out var dpiY);

            using (var graphImage = (Bitmap)Image.FromFile(image))
            {
                graphImage.SetResolution(dpiX, dpiY);

                finalImage = new Bitmap(graphImage.Width, graphImage.Height, PixelFormat.Format32bppArgb);
                finalImage.SetResolution(dpiX, dpiY);
                using (var graphics = Graphics.FromImage(finalImage))
                {
                    graphics.Clear(Color.White);
                    graphics.DrawImage(graphImage, 0, 0);
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