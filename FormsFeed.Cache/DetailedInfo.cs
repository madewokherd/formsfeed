using System;
using System.Collections.Generic;
using Argotic.Syndication;

namespace FormsFeed.Cache
{
    public struct DetailedInfo
    {
        // Detailed information for a feed or item/entry
        public string feed_uri;
        public string id;
        public string title;
        public string author;
        public DateTime timestamp;
        public List<Tuple<string, string>> contents;
        public object argotic_resource;
    }
}
