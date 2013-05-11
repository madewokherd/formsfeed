using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FormsFeed.Cache
{
    public class Tag
    {
        internal Tag(Cache cache, string name)
        {
            throw new NotImplementedException();
        }

        private void Add(IEnumerable<DetailedInfo> infos, bool RemoveContents)
        {
            throw new NotImplementedException();
        }

        private void Add(DetailedInfo info, bool RemoveContents)
        {
            DetailedInfo[] infos = new DetailedInfo[1];
            infos[0] = info;
            Add(infos, RemoveContents);
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

        public void Remove(Tuple<string, string> key)
        {
            throw new NotImplementedException();
        }

        public void Remove(IEnumerable<Tuple<string, string>> keys)
        {
            throw new NotImplementedException();
        }

        public void Remove(DetailedInfo info)
        {
            throw new NotImplementedException();
        }

        public void Remove(IEnumerable<DetailedInfo> infos)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<DetailedInfo> GetSummaries()
        {
            throw new NotImplementedException();
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

        string Name
        {
            get
            {
                throw new NotImplementedException();
            }
        }
    }
}
