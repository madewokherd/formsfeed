using System;
using System.Collections.Generic;
using Argotic.Syndication;

namespace FormsFeed
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

        public string get_content_uri()
        {
            foreach (var item in contents)
            {
                if (item.Item1 == "content-uri" || item.Item1 == "link:alternate-uri")
                    return item.Item2;
            }
            return null;
        }

        public string get_content_html()
        {
            string summary = null;
            foreach (var item in contents)
            {
                if (item.Item1 == "description")
                    return item.Item2;
                else if (item.Item1 == "summary")
                    summary = item.Item2;
            }
            return summary;
        }
    }
}
