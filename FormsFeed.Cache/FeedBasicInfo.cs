using System;

namespace FormsFeed
{
    public struct FeedBasicInfo
    {
        public string uri;          // URL of the feed
        public DateTime lastchecked;// timestamp of last successful check
        public DateTime timestamp;  // http last modified time
        public DateTime expiration; // earliest we've been asked to recheck
        public string etag;         // http last retrieved string
        public bool autofetch;      // if true, fetching "all" feeds will include this one
        public string title;
    }
}
