using System.IO;
using NMaier.SimpleDlna.Server;

namespace NMaier.SimpleDlna.Thumbnails
{
  internal interface IThumbnailLoader
  {
    DlnaMediaTypes Handling { get; }

    Stream GetThumbnail(object item, ref int width, ref int height);
  }
}
