using System;
using System.IO;
using System.Collections.Generic;
using System.Net;
using CSharpTest.Net.Collections;
using CSharpTest.Net.Serialization;

namespace FormsFeed.Cache
{
    public class Cache : IDisposable
    {
        private string basepath;
        private FileStream lockfile;
        internal BPlusTree<string, FeedBasicInfo> feed_infos;

        public Cache(string path)
        {
            this.basepath = path;
            this.lockfile = new FileStream(
                Path.Combine(basepath, "lock"),
                FileMode.OpenOrCreate,
                FileAccess.ReadWrite,
                FileShare.None);

            BPlusTree<string, FeedBasicInfo>.OptionsV2 options = new BPlusTree<string, FeedBasicInfo>.OptionsV2(
                new PrimitiveSerializer(),
                new Serializers());
            options.FileName = Path.Combine(basepath, "feeds");
            options.CreateFile = CreatePolicy.IfNeeded;
            options.FileBlockSize = 512;
            this.feed_infos = new BPlusTree<string, FeedBasicInfo>(options);
        }

        public void Dispose()
        {
            if (lockfile != null)
                lockfile.Close();
            if (feed_infos != null)
                feed_infos.Dispose();
        }

        private FeedBasicInfo GetBasicInfoSafe(string uri)
        {
            FeedBasicInfo result;
            if (!feed_infos.TryGetValue(uri, out result))
            {
                result = new FeedBasicInfo();
                result.uri = uri;
                result.timestamp = DateTime.MinValue;
                feed_infos[uri] = result;
            }
            else
                Console.WriteLine(result.uri);
            return result;
        }

        public bool Update(string uri, bool force)
        {
            FeedBasicInfo info = GetBasicInfoSafe(uri);
            if (!force && DateTime.Compare(DateTime.UtcNow, info.expiration) < 0)
            {
                Console.WriteLine("content expired");
                return false;
            }
            WebRequest request = WebRequest.Create(uri);
            if (request is HttpWebRequest)
            {
                var headers = request.Headers;
                if (info.timestamp != default(DateTime))
                    (request as HttpWebRequest).IfModifiedSince = info.timestamp;
                if (info.etag != null && info.etag != "")
                    headers.Add("If-None-Match", info.etag);
            }
            info.lastchecked = DateTime.UtcNow;
            WebResponse response;
            try
            {
                response = request.GetResponse();
            }
            catch (WebException e)
            {
                if (e.Response is HttpWebResponse && ((HttpWebResponse)e.Response).StatusCode == HttpStatusCode.NotModified)
                    return false;
                throw;
            }
            if (response is HttpWebResponse)
            {
                HttpWebResponse httpresponse = (HttpWebResponse)response;
                info.timestamp = DateTime.MinValue;
                info.etag = null;
                info.expiration = DateTime.MinValue;
                var headers = httpresponse.Headers;
                for (int i = 0; i < headers.Count; i++)
                {
                    string key = headers.GetKey(i);
                    if (key == "Last-Modified")
                        info.timestamp = DateTime.Parse(headers.Get(i)).ToUniversalTime();
                    else if (key == "ETag")
                        info.etag = headers.Get(i);
                    else if (key == "Expires")
                        info.expiration = DateTime.Parse(headers.Get(i)).ToUniversalTime();
                }
            }

            feed_infos[info.uri] = info;
            return true;
        }

        public bool Update(string uri)
        {
            return Update(uri, false);
        }
    }
}
