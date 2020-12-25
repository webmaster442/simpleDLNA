using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using NMaier.SimpleDlna.Server;
using SkiaSharp;

namespace NMaier.SimpleDlna.Thumbnails
{
  internal sealed class ImageThumbnailLoader : IThumbnailLoader
  {
    public DlnaMediaTypes Handling => DlnaMediaTypes.Image;

    public Stream GetThumbnail(object item, ref int width, ref int height)
    {
      SKImage img;
      var stream = item as Stream;
      if (stream != null)
      {
        img = SKImage.FromEncodedData(stream);
      }
      else
      {
        if (item is FileInfo fi)
        {
          using (var file = File.OpenRead(fi.FullName))
          {
            img = SKImage.FromEncodedData(file);
          }
        }
        else
        {
          throw new NotSupportedException();
        }
      }
      using (img)
      {
        using (var scaled = ThumbnailMaker.ResizeImage(img, width, height, ThumbnailMakerBorder.Borderless))
        {
          width = scaled.Width;
          height = scaled.Height;
          SKData encoded = null;
          try
          {
            encoded = scaled.Encode(SKEncodedImageFormat.Jpeg, 100);
            return encoded.AsStream();
          }
          catch (Exception)
          {
            encoded?.Dispose();
            throw;
          }
        }
      }
    }
  }
}
