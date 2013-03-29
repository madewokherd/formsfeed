using System;
using System.IO;
using System.Collections.Generic;

namespace FormsFeed.Cache
{
    public class Cache : IDisposable
    {
        private string basepath;
        private FileStream lockfile;

        public Cache(string path)
        {
            this.basepath = path;
            this.lockfile = new FileStream(
                System.IO.Path.Combine(basepath, "lock"),
                FileMode.OpenOrCreate,
                FileAccess.ReadWrite,
                FileShare.None);
        }

        public void Dispose()
        {
            if (lockfile != null)
                lockfile.Close();
        }
    }
}
