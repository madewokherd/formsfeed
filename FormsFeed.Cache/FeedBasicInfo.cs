using System;

namespace FormsFeed.Cache
{
    [Serializable]
    internal class FeedBasicInfo
    {
        public Uri uri;             // URL of the feed
        public DateTime timestamp;  // http last modified time
        public string etag;         // http last retrieved string
        public bool unread;         // true if the feed contains items not marked as read
        public bool autofetch;      // if true, fetching "all" feeds will include this one
        public string dbfilename;   // location of a database file containing more detailed information
    }
}
