using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using CSharpTest.Net.Serialization;

namespace FormsFeed
{
    public class Tag
    {
        class TaggedItem
        {
            internal DetailedInfo info;
            internal long valid_ofs;
        }

        class KeyAndDetailedInfoSerializer : ISerializer<DetailedInfo>
        {
            internal Tag parent;
            public DetailedInfo ReadFrom(Stream stream)
            {
                string feed_uri = (parent.ps as ISerializer<string>).ReadFrom(stream);
                string id = (parent.ps as ISerializer<string>).ReadFrom(stream);
                DetailedInfo result = (parent.ls as ISerializer<DetailedInfo>).ReadFrom(stream);
                result.feed_uri = feed_uri;
                result.id = id;
                return result;
            }

            public void WriteTo(DetailedInfo value, Stream stream)
            {
                (parent.ps as ISerializer<string>).WriteTo(value.feed_uri, stream);
                (parent.ps as ISerializer<string>).WriteTo(value.id, stream);
                (parent.ls as ISerializer<DetailedInfo>).WriteTo(value, stream);
            }
        }

        private Dictionary<Tuple<string, string>, TaggedItem> tagged_items;

        long num_invalid_items;

        bool loaded;

        string filename;

        FileStream f;

        PrimitiveSerializer ps;

        Serializers ls;

        KeyAndDetailedInfoSerializer ks;

        internal Tag(Cache cache, string name)
        {
            tagged_items = new Dictionary<Tuple<string, string>, TaggedItem>();
            filename = Path.Combine(cache.basepath, name + ".tag");
            ps = new PrimitiveSerializer();
            ls = new Serializers();
            ks = new KeyAndDetailedInfoSerializer();
            ks.parent = this;
        }

        internal void Load()
        {
            if (!loaded)
            {
                lock (this)
                {
                    if (!loaded)
                    {
                        f = new FileStream(
                            filename,
                            FileMode.OpenOrCreate,
                            FileAccess.ReadWrite,
                            FileShare.Delete);

                        byte[] buffer = new byte[1024];
                        long last_valid_ofs = 0;

                        while (true)
                        {
                            TaggedItem res = new TaggedItem();
                            res.valid_ofs = f.Position;
                            bool valid = f.ReadByte() != 0;
                            if (f.Read(buffer, 0, 4) != 4)
                                break;
                            int len = ps.FromByteArray<int>(buffer);
                            if (!valid)
                            {
                                f.Seek(len, SeekOrigin.Current);
                                num_invalid_items += 1;
                                continue;
                            }
                            if (len > buffer.Length)
                                buffer = new byte[len * 2];
                            if (f.Read(buffer, 0, len) != len)
                            {
                                f.SetLength(res.valid_ofs);
                                break;
                            }
                            res.info = ks.FromByteArray<DetailedInfo>(buffer);
                            tagged_items.Add(Tuple.Create(res.info.feed_uri, res.info.id), res);
                            last_valid_ofs = f.Position;
                        }

                        f.SetLength(last_valid_ofs);

                        loaded = true;
                    }
                }
            }
        }

        private void AddOneUnsafe(DetailedInfo info, bool RemoveContents)
        {
            // this must be locked, f must be seeked to the end, and f must be flushed after all items are written
            var key = Tuple.Create(info.feed_uri, info.id);
            if (tagged_items.ContainsKey(key))
                return;
            if (RemoveContents)
            {
                info.contents = new List<Tuple<string,string>>();;
            }
            byte[] serialized_contents = ks.ToByteArray<DetailedInfo>(info);
            TaggedItem tagged_item = new TaggedItem();
            tagged_item.valid_ofs = f.Position;
            f.WriteByte(1);
            (ps as ISerializer<int>).WriteTo(serialized_contents.Length, f);
            f.Write(serialized_contents, 0, serialized_contents.Length);
            tagged_item.info = info;
            tagged_items[key] = tagged_item;
        }

        private void Add(IEnumerable<DetailedInfo> infos, bool RemoveContents)
        {
            lock (this)
            {
                f.Seek(0, SeekOrigin.End);
                foreach (DetailedInfo info in infos)
                    AddOneUnsafe(info, RemoveContents);
                f.Flush();
            }
        }

        private void Add(DetailedInfo info, bool RemoveContents)
        {
            lock (this)
            {
                f.Seek(0, SeekOrigin.End);
                AddOneUnsafe(info, RemoveContents);
                f.Flush();
            }
        }

        public void Add(IEnumerable<DetailedInfo> infos)
        {
            Add(infos, true);
        }

        public void Add(DetailedInfo info)
        {
            Add(info, true);
        }

        public void AddWithContents(IEnumerable<DetailedInfo> infos)
        {
            Add(infos, false);
        }

        public void AddWithContents(DetailedInfo info)
        {
            Add(info, false);
        }

        private bool RemoveOneUnsafe(Tuple<string, string> key)
        {
            TaggedItem item;
            if (tagged_items.TryGetValue(key, out item))
            {
                f.Seek(item.valid_ofs, SeekOrigin.Begin);
                f.WriteByte(0);
                tagged_items.Remove(key);
                num_invalid_items += 1;
                return true;
            }
            return false;
        }

        public bool Remove(Tuple<string, string> key)
        {
            bool result;
            lock (this)
            {
                result = RemoveOneUnsafe(key);
                // FIXME: Rewrite file if there are too many invalid items.
                f.Flush();
            }
            return result;
        }

        public void Remove(IEnumerable<Tuple<string, string>> keys)
        {
            lock (this)
            {
                foreach (var key in keys)
                {
                    RemoveOneUnsafe(key);
                }
                // FIXME: Rewrite file if there are too many invalid items.
                f.Flush();
            }
        }

        public bool Remove(DetailedInfo info)
        {
            bool result;
            lock (this)
            {
                result = RemoveOneUnsafe(Tuple.Create(info.feed_uri, info.id));
                // FIXME: Rewrite file if there are too many invalid items.
                f.Flush();
            }
            return result;
        }

        public void Remove(IEnumerable<DetailedInfo> infos)
        {
            lock (this)
            {
                foreach (var info in infos)
                {
                    RemoveOneUnsafe(Tuple.Create(info.feed_uri, info.id));
                }
                // FIXME: Rewrite file if there are too many invalid items.
                f.Flush();
            }
        }

        public IEnumerable<DetailedInfo> GetSummaries()
        {
            ICollection<TaggedItem> values;
            lock (this)
            {
                values = tagged_items.Values;
            }
            foreach (var item in values)
            {
                yield return item.info;
            }
        }

        public bool TryGetSummary(string feed_uri, string id, out DetailedInfo info)
        {
            throw new NotImplementedException();
        }

        public bool TryGetSummary(Tuple<string, string> key, out DetailedInfo info)
        {
            throw new NotImplementedException();
        }

        public bool Contains(string feed_uri, string id)
        {
            DetailedInfo dummy;
            return TryGetSummary(feed_uri, id, out dummy);
        }

        public bool Contains(Tuple<string, string> key)
        {
            DetailedInfo dummy;
            return TryGetSummary(key, out dummy);
        }

        public IEnumerable<DetailedInfo> Intersect(IEnumerable<Tuple<string, string>> keys)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<DetailedInfo> Intersect(IEnumerable<DetailedInfo> keys)
        {
            throw new NotImplementedException();
        }

        string Name
        {
            get
            {
                throw new NotImplementedException();
            }
        }
    }
}
