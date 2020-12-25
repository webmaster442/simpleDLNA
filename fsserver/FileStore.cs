using LiteDB;
using NMaier.SimpleDlna.Server;
using NMaier.SimpleDlna.Utilities;
using System;
using System.Data.Common;
using System.IO;
using System.Reflection;

namespace NMaier.SimpleDlna.FileMediaServer
{
  internal sealed class CoverItem
  {
    public string Key { get; set; }
    public long Size { get; set; }
    public DateTime Time { get; set; }
    public Cover Cover { get; set; }
  }

  internal sealed class FileStore : Logging, IDisposable
  {
    private static readonly object globalLock = new object();

    public FileInfo StoreFile { get; }

    private readonly LiteDatabase db;
    private ILiteCollection<BaseFile> files;
    private ILiteCollection<CoverItem> covers;

    internal FileStore(FileInfo storeFile)
    {
      StoreFile = storeFile;
      db = new LiteDatabase(StoreFile.FullName);

      covers = db.GetCollection<CoverItem>();
      files = db.GetCollection<BaseFile>();


      covers.EnsureIndex(x => x.Key);
      covers.EnsureIndex(x => x.Time);
      covers.EnsureIndex(x => x.Size);

      files.EnsureIndex(x => x.Item.FullName);
      files.EnsureIndex(x => x.Item.Length);
      files.EnsureIndex(x => x.Item.LastWriteTimeUtc);
    }

    public void Dispose()
    {
      db?.Dispose();
    }

    internal bool HasCover(BaseFile file)
    {
      if (db == null)
      {
        return false;
      }
      var info = file.Item;
      lock (globalLock)
      {
        try
        {
          var cover = covers
            .Query()
            .Where(x => x.Key == info.FullName
                && x.Size == info.Length
                && x.Time == info.LastWriteTimeUtc)
            .FirstOrDefault();

          return cover != null;
        }
        catch (DbException ex)
        {
          Error("Failed to lookup file cover existence from store", ex);
          return false;
        }
      }
    }

    internal Cover MaybeGetCover(BaseFile file)
    {
      if (db == null)
      {
        return null;
      }

      var info = file.Item;
      lock (globalLock)
      {
        try
        {
          var cover = covers
            .Query()
            .Where(x => x.Key == info.FullName
                && x.Size == info.Length
                && x.Time == info.LastWriteTimeUtc)
            .FirstOrDefault();

          return cover.Cover;
        }
        catch (Exception ex)
        {
          Fatal("Failed to deserialize a cover", ex);
          throw;
        }
      }
    }

    internal BaseFile MaybeGetFile(FileServer server, FileInfo info, DlnaMime type)
    {
      if (db == null)
      {
        return null;
      }
      lock (globalLock)
      {
        try
        {
          var file = files
            .Query()
            .Where(x => x.Item.FullName == info.FullName
                && x.Item.Length == info.Length
                && x.Item.LastWriteTimeUtc == info.LastAccessTimeUtc)
            .FirstOrDefault();

          return file;
        }
        catch (Exception ex)
        {
          Debug("Failed to deserialize an item", ex);
          return null;
        }
      }
    }

    internal void MaybeStoreFile(BaseFile file)
    {
      if (db == null)
      {
        return;
      }
      if (!file.GetType().Attributes.HasFlag(TypeAttributes.Serializable))
      {
        return;
      }
      try
      {
        var cover = 
        files.Insert(file);
        covers.Insert(new CoverItem
        {
          Cover =file.MaybeGetCover(),
          Key = file.Item.FullName,
          Size = file.Item.Length,
          Time = file.Item.LastWriteTimeUtc
        });
      }
      catch (Exception ex)
      {
        Error("Failed to serialize an object of type " + file.GetType(), ex);
        throw;
      }
    }
  }
}
