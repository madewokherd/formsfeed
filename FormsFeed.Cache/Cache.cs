using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Net;
using CSharpTest.Net.Collections;
using CSharpTest.Net.Serialization;
using Argotic.Syndication;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;

namespace FormsFeed
{
    public class Cache : IDisposable
    {
        internal string basepath;
        private FileStream lockfile;
        internal BPlusTree<string, FeedBasicInfo> feed_infos;
        internal BPlusTree<Tuple<string, string>, DetailedInfo> detailed_infos;
        private ConcurrentDictionary<string, Tag> loaded_tags;
        private ConcurrentDictionary<string, object> feed_locks;

        public Cache(string path)
        {
            Serializers s = new Serializers();

            Directory.CreateDirectory(path);

            this.basepath = path;
            this.lockfile = new FileStream(
                Path.Combine(basepath, "lock"),
                FileMode.OpenOrCreate,
                FileAccess.ReadWrite,
                FileShare.None);

            {
                BPlusTree<string, FeedBasicInfo>.OptionsV2 options = new BPlusTree<string, FeedBasicInfo>.OptionsV2(new PrimitiveSerializer(), s);
                options.FileName = Path.Combine(basepath, "feeds");
                options.CreateFile = CreatePolicy.IfNeeded;
                options.FileBlockSize = 512;
                this.feed_infos = new BPlusTree<string, FeedBasicInfo>(options);
            }

            {
                BPlusTree<Tuple<string, string>, DetailedInfo>.OptionsV2 options = new BPlusTree<Tuple<string, string>, DetailedInfo>.OptionsV2(s, s);
                options.FileName = Path.Combine(basepath, "items");
                options.CreateFile = CreatePolicy.IfNeeded;
                options.KeyComparer = s;
                this.detailed_infos = new BPlusTree<Tuple<string, string>, DetailedInfo>(options);
            }

            loaded_tags = new ConcurrentDictionary<string, Tag>();
            feed_locks = new ConcurrentDictionary<string, object>();

            // Process any "new" items we failed to process before
            Dictionary<string, List<DetailedInfo>> tags = new Dictionary<string, List<DetailedInfo>>();
            Tag new_tag = GetTag("(new)");
            List<DetailedInfo> summaries = new List<DetailedInfo>(new_tag.GetSummaries());

            foreach (var summary in summaries)
            {
                DetailedInfo info;
                if (TryGetDetailedInfo(summary.feed_uri, summary.id, out info))
                {
                    ProcessItem(info, tags);
                }
            }

            foreach (var kvp in tags)
            {
                GetTag(kvp.Key).Add(kvp.Value);
            }

            new_tag.Remove(summaries);
        }

        public Cache()
            : this(DefaultPath)
        {
        }

        public static string DefaultPath
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "FormsFeed");
            }
        }

        private object GetFeedLock(string uri)
        {
            object result;
            if (feed_locks.TryGetValue(uri, out result))
                return result;
            feed_locks.TryAdd(uri, new object());
            return feed_locks[uri];
        }

        public void Dispose()
        {
            if (lockfile != null)
                lockfile.Close();
            if (feed_infos != null)
                feed_infos.Dispose();
            if (detailed_infos != null)
                detailed_infos.Dispose();
        }

        private FeedBasicInfo GetBasicInfoSafe(string uri)
        {
            FeedBasicInfo result;
            if (!feed_infos.TryGetValue(uri, out result))
            {
                lock (GetFeedLock(uri))
                {
                    result = new FeedBasicInfo();
                    result.uri = uri;
                    result.timestamp = DateTime.MinValue;
                    feed_infos[uri] = result;
                }
            }
            return result;
        }

        public bool Update(string uri, bool force)
        {
            lock (GetFeedLock(uri))
            {
                FeedBasicInfo info = GetBasicInfoSafe(uri);
                if (!force && DateTime.Compare(DateTime.UtcNow, info.expiration) < 0)
                    return false;
                DateTime previous_check = info.lastchecked;
                info.lastchecked = DateTime.UtcNow;
                WebRequest request = WebRequest.Create(uri);
                if (request is HttpWebRequest)
                {
                    var headers = request.Headers;
                    if (info.timestamp != default(DateTime))
                        (request as HttpWebRequest).IfModifiedSince = info.timestamp;
                    if (info.etag != null && info.etag != "")
                        headers.Add("If-None-Match", info.etag);
                }
                WebResponse response;
                try
                {
                    response = request.GetResponse();
                }
                catch (WebException e)
                {
                    if (e.Response is HttpWebResponse && ((HttpWebResponse)e.Response).StatusCode == HttpStatusCode.NotModified)
                    {
                        HttpWebResponse httpresponse = (HttpWebResponse)e.Response;
                        info.expiration = DateTime.MinValue;
                        var headers = httpresponse.Headers;
                        for (int i = 0; i < headers.Count; i++)
                        {
                            string key = headers.GetKey(i);
                            if (key == "Last-Modified" && DateTime.TryParse(headers.Get(i), out info.timestamp))
                                info.timestamp = info.timestamp.ToUniversalTime();
                            else if (key == "ETag")
                                info.etag = headers.Get(i);
                            else if (key == "Expires" && DateTime.TryParse(headers.Get(i), out info.expiration))
                                info.expiration = info.expiration.ToUniversalTime();
                        }
                        feed_infos[info.uri] = info;
                        return false;
                    }
                    throw;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    // FIXME: Store exception
                    info.expiration = DateTime.UtcNow.AddMinutes(15);
                    feed_infos[info.uri] = info;
                    return false;
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
                        if (key == "Last-Modified" && DateTime.TryParse(headers.Get(i), out info.timestamp))
                            info.timestamp = info.timestamp.ToUniversalTime();
                        else if (key == "ETag")
                            info.etag = headers.Get(i);
                        else if (key == "Expires" && DateTime.TryParse(headers.Get(i), out info.expiration))
                            info.expiration = info.expiration.ToUniversalTime();
                    }
                }

                GenericSyndicationFeed feed = new GenericSyndicationFeed();
                feed.Load(response.GetResponseStream());

                DetailedInfo feed_detailed_info = new DetailedInfo();
                feed_detailed_info.feed_uri = uri;
                feed_detailed_info.title = feed.Title;
                feed_detailed_info.author = feed.Title;
                feed_detailed_info.argotic_resource = feed;
                feed_detailed_info.contents = new List<Tuple<string, string>>();

                if (feed.Description != null)
                    feed_detailed_info.contents.Add(Tuple.Create("description", feed.Description));

                FillCategories(feed_detailed_info, feed.Categories);

                if (feed.Resource is AtomFeed)
                {
                    AtomFeed atomfeed = (AtomFeed)feed.Resource;
                    FillAuthors(feed_detailed_info, atomfeed.Authors);
                    FillContributors(feed_detailed_info, atomfeed.Contributors);
                    if (atomfeed.Icon != null)
                        feed_detailed_info.contents.Add(Tuple.Create("icon-uri", atomfeed.Icon.Uri.ToString()));
                    FillLinks(feed_detailed_info, atomfeed.Links);
                    if (atomfeed.Logo != null)
                        feed_detailed_info.contents.Add(Tuple.Create("logo-uri", atomfeed.Logo.Uri.ToString()));
                    if (atomfeed.Subtitle != null)
                        AddAtomText(feed_detailed_info, "subtitle-html", atomfeed.Subtitle);
                }
                else if (feed.Resource is RssFeed)
                {
                    RssFeed rssfeed = (RssFeed)feed.Resource;
                    RssChannel channel = rssfeed.Channel;

                    if (channel.Image != null)
                        feed_detailed_info.contents.Add(Tuple.Create("image-uri", channel.Image.Url.ToString()));
                    if (channel.Link != null)
                        feed_detailed_info.contents.Add(Tuple.Create("link-uri", channel.Link.ToString()));
                    if (channel.TimeToLive > 0)
                    {
                        DateTime expiration = DateTime.UtcNow.AddMinutes(channel.TimeToLive);
                        if (DateTime.Compare(expiration, info.expiration) > 0)
                            info.expiration = expiration;
                    }
                    if (channel.SkipDays != null)
                    {
                        DateTime expiration = DateTime.UtcNow;
                        bool delayed = false;
                        int i = 0;
                        while (channel.SkipDays.Contains(expiration.DayOfWeek))
                        {
                            expiration = new DateTime(expiration.Year, expiration.Month, expiration.Day, 0, 0, 0, DateTimeKind.Utc);
                            expiration = expiration.AddDays(1.0);
                            delayed = true;
                            if (i == 7)
                            {
                                delayed = false;
                                break;
                            }
                            i++;
                        }
                        if (delayed && DateTime.Compare(expiration, info.expiration) > 0)
                            info.expiration = expiration;
                    }
                    if (channel.SkipHours != null)
                    {
                        DateTime expiration = DateTime.UtcNow;
                        bool delayed = false;
                        int i = 0;
                        while (channel.SkipHours.Contains(expiration.Hour))
                        {
                            expiration = new DateTime(expiration.Year, expiration.Month, expiration.Day, expiration.Hour, 0, 0, DateTimeKind.Utc);
                            expiration = expiration.AddHours(1.0);
                            delayed = true;
                            if (i == 24)
                            {
                                delayed = false;
                                break;
                            }
                            i++;
                        }
                        if (delayed && DateTime.Compare(expiration, info.expiration) > 0)
                            info.expiration = expiration;
                    }
                }

                LinkedList<DetailedInfo> items = new LinkedList<DetailedInfo>();
                HashSet<Tuple<string, string>> seen_keys = new HashSet<Tuple<string, string>>();
                bool duplicate_keys = false;

                if (feed.Resource is AtomFeed)
                {
                    foreach (var item in ((AtomFeed)feed.Resource).Entries)
                    {
                        DetailedInfo iteminfo = new DetailedInfo();
                        iteminfo.feed_uri = info.uri;
                        iteminfo.id = item.Id.Uri.ToString();
                        if (detailed_infos.ContainsKey(Tuple.Create(iteminfo.feed_uri, iteminfo.id)))
                            continue;
                        iteminfo.contents = new List<Tuple<string, string>>();
                        iteminfo.title = item.Title.Content; // FIXME: remove any html/xml
                        iteminfo.author = feed_detailed_info.author;
                        iteminfo.timestamp = item.PublishedOn;
                        iteminfo.argotic_resource = item;

                        FillCategories(iteminfo, (new GenericSyndicationItem(item)).Categories);

                        FillAuthors(iteminfo, item.Authors);
                        FillContributors(iteminfo, item.Contributors);
                        FillLinks(iteminfo, item.Links);
                        if (item.Content != null)
                        {
                            if (item.Content.Source != null)
                                iteminfo.contents.Add(Tuple.Create("content-uri", item.Content.Source.ToString()));
                            else
                                iteminfo.contents.Add(Tuple.Create("content", item.Content.Content)); // FIXME: escape text?
                        }
                        if (item.Summary != null)
                        {
                            AddAtomText(iteminfo, "summary", item.Summary);
                        }

                        if (DateTime.Compare(iteminfo.timestamp, previous_check) < 0)
                            iteminfo.timestamp = previous_check;
                        else if (DateTime.Compare(iteminfo.timestamp, DateTime.UtcNow) > 0)
                            iteminfo.timestamp = DateTime.UtcNow;

                        if (seen_keys.Contains(Tuple.Create(iteminfo.feed_uri, iteminfo.id)))
                            duplicate_keys = true;
                        else
                            seen_keys.Add(Tuple.Create(iteminfo.feed_uri, iteminfo.id));

                        items.AddLast(iteminfo);
                    }
                }
                else if (feed.Resource is RssFeed)
                {
                    foreach (var item in ((RssFeed)feed.Resource).Channel.Items)
                    {
                        DetailedInfo iteminfo = new DetailedInfo();
                        iteminfo.feed_uri = info.uri;
                        if (item.Guid != null)
                            iteminfo.id = item.Guid.Value.ToString();
                        else if (item.Link != null)
                            iteminfo.id = item.Link.ToString();
                        else
                            iteminfo.id = item.Title;
                        if (detailed_infos.ContainsKey(Tuple.Create(iteminfo.feed_uri, iteminfo.id)))
                            continue;
                        iteminfo.contents = new List<Tuple<string, string>>();
                        iteminfo.title = item.Title;
                        iteminfo.author = feed_detailed_info.author;
                        iteminfo.timestamp = item.PublicationDate;
                        iteminfo.argotic_resource = item;

                        FillCategories(iteminfo, (new GenericSyndicationItem(item)).Categories);

                        if (item.Author != null)
                        {
                            iteminfo.author = item.Author;
                            iteminfo.contents.Add(Tuple.Create("author", item.Author));
                        }
                        if (item.Comments != null)
                            iteminfo.contents.Add(Tuple.Create("comments-uri", item.Comments.ToString()));
                        if (item.Description != null)
                            iteminfo.contents.Add(Tuple.Create("description", item.Description));
                        foreach (var enclosure in item.Enclosures)
                        {
                            iteminfo.contents.Add(Tuple.Create("enclosure", enclosure.Url.ToString()));
                        }
                        if (item.Link != null)
                            iteminfo.contents.Add(Tuple.Create("content-uri", item.Link.ToString()));

                        if (DateTime.Compare(iteminfo.timestamp, previous_check) < 0)
                            iteminfo.timestamp = previous_check;
                        else if (DateTime.Compare(iteminfo.timestamp, DateTime.UtcNow) > 0)
                            iteminfo.timestamp = DateTime.UtcNow;

                        if (seen_keys.Contains(Tuple.Create(iteminfo.feed_uri, iteminfo.id)))
                            duplicate_keys = true;
                        else
                            seen_keys.Add(Tuple.Create(iteminfo.feed_uri, iteminfo.id));

                        items.AddLast(iteminfo);
                    }
                }
                else
                {
                    foreach (var item in feed.Items)
                    {
                        DetailedInfo iteminfo = new DetailedInfo();
                        iteminfo.feed_uri = info.uri;
                        iteminfo.id = item.Title;
                        if (detailed_infos.ContainsKey(Tuple.Create(iteminfo.feed_uri, iteminfo.id)))
                            continue;
                        iteminfo.contents = new List<Tuple<string, string>>();
                        iteminfo.title = item.Title;
                        iteminfo.author = feed_detailed_info.author;
                        iteminfo.timestamp = item.PublishedOn;
                        iteminfo.argotic_resource = item;

                        FillCategories(iteminfo, item.Categories);
                        if (item.Summary != null)
                            iteminfo.contents.Add(Tuple.Create("summary", item.Summary));

                        if (DateTime.Compare(iteminfo.timestamp, previous_check) < 0)
                            iteminfo.timestamp = previous_check;
                        else if (DateTime.Compare(iteminfo.timestamp, DateTime.UtcNow) > 0)
                            iteminfo.timestamp = DateTime.UtcNow;

                        if (seen_keys.Contains(Tuple.Create(iteminfo.feed_uri, iteminfo.id)))
                            duplicate_keys = true;
                        else
                            seen_keys.Add(Tuple.Create(iteminfo.feed_uri, iteminfo.id));

                        items.AddLast(iteminfo);
                    }
                }

                if (duplicate_keys)
                {
                    LinkedList<DetailedInfo> new_items = new LinkedList<DetailedInfo>();
                    SHA1 sha = SHA1.Create();
                    UTF8Encoding utf = new UTF8Encoding ();
                    foreach (var item in items)
                    {
                        DetailedInfo new_item = item;
                        sha.Initialize();
                        byte[] buffer = utf.GetBytes(item.title);
                        sha.TransformBlock(buffer, 0, buffer.Length, null, 0);
                        foreach (var kvp in item.contents)
                        {
                            buffer = utf.GetBytes(kvp.Item1);
                            sha.TransformBlock(buffer, 0, buffer.Length, null, 0);
                            buffer = utf.GetBytes(kvp.Item2);
                            sha.TransformBlock(buffer, 0, buffer.Length, null, 0);
                        }
                        new_item.id = item.id + ":" + Convert.ToBase64String(sha.Hash);
                        new_items.AddLast(new_item);
                    }
                    items = new_items;
                }

                if (items.First != null)
                {
                    GetTag("(new)").Add(items);

                    detailed_infos.AddRange(GetInfosAsKvp(items));

                    detailed_infos[Tuple.Create(feed_detailed_info.feed_uri, "")] = feed_detailed_info;

                    Dictionary<string, List<DetailedInfo>> tags = new Dictionary<string,List<DetailedInfo>>();

                    foreach (var item in items)
                    {
                        ProcessItem(item, tags);
                    }

                    foreach (var kvp in tags)
                    {
                        GetTag(kvp.Key).Add(kvp.Value);
                    }

                    GetTag("(new)").Remove(items);
                }

                feed_infos[info.uri] = info;

                //FIXME: Tag all new items as unread, apply any applicable filters?
            }

            return true;
        }

        private void MarkItemToTag(string tag, DetailedInfo item, Dictionary<string, List<DetailedInfo>> tags)
        {
            List<DetailedInfo> itemlist;
            if (!tags.TryGetValue(tag, out itemlist))
            {
                itemlist = new List<DetailedInfo>();
                tags[tag] = itemlist;
            }
            itemlist.Add(item);
        }

        private void ProcessItem(DetailedInfo item, Dictionary<string, List<DetailedInfo>> tags)
        {
            MarkItemToTag("(unread)", item, tags);
        }

        private IEnumerable<KeyValuePair<Tuple<string, string>, DetailedInfo>> GetInfosAsKvp(IEnumerable<DetailedInfo> infos)
        {
            foreach (DetailedInfo info in infos)
            {
                yield return new KeyValuePair<Tuple<string, string>, DetailedInfo>(
                    Tuple.Create(info.feed_uri, info.id), info);
            }
        }

        internal static string text_to_html(string text)
        {
            string content = text;
            content = content.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
            content = string.Format("<pre>{0}</pre>", content);
            return content;
        }

        private void AddAtomText(DetailedInfo feed_detailed_info, string key, AtomTextConstruct text)
        {
            string content = text.Content;
            if (text.TextType == AtomTextConstructType.Text)
            {
                content = text_to_html(content);
            }
            feed_detailed_info.contents.Add(Tuple.Create(key, content));
        }

        private void FillLinks(DetailedInfo feed_detailed_info, System.Collections.ObjectModel.Collection<AtomLink> collection)
        {
            foreach (var item in collection)
                feed_detailed_info.contents.Add(Tuple.Create(string.Format("link:{0}-uri", item.Relation), item.Uri.ToString()));
        }

        private void FillContributors(DetailedInfo feed_detailed_info, System.Collections.ObjectModel.Collection<AtomPersonConstruct> collection)
        {
            foreach (var person in collection)
                feed_detailed_info.contents.Add(Tuple.Create("contributor", PersonToString(person)));
        }

        private void FillCategories(DetailedInfo feed_detailed_info, System.Collections.ObjectModel.Collection<GenericSyndicationCategory> collection)
        {
            foreach (var item in collection)
                feed_detailed_info.contents.Add(Tuple.Create(string.Format("category:{0}", item.Scheme), item.Term));
        }

        private string PersonToString(AtomPersonConstruct person)
        {
            string stringvalue = person.Name;
            if (person.EmailAddress != null)
                stringvalue = string.Format("{0}\nemail={1}", stringvalue, person.EmailAddress);
            if (person.Uri != null)
                stringvalue = string.Format("{0}\nuri={1}", stringvalue, person.Uri.ToString());
            return stringvalue;
        }

        private void FillAuthors(DetailedInfo feed_detailed_info, System.Collections.ObjectModel.Collection<AtomPersonConstruct> collection)
        {
            if (collection.Count != 0)
                feed_detailed_info.author = collection[0].Name;
            foreach (var person in collection)
                feed_detailed_info.contents.Add(Tuple.Create("author", PersonToString(person)));
        }

        public bool Update(string uri)
        {
            return Update(uri, false);
        }

        private IEnumerable<string> GetExpiredSubscriptions()
        {
            List<string> result = new List<string>();
            foreach (var kvp in feed_infos)
            {
                if (kvp.Value.autofetch && DateTime.Compare(kvp.Value.expiration, DateTime.UtcNow) <= 0)
                    result.Add(kvp.Key);
            }
            return result;
        }

        public void UpdateAll(bool force)
        {
            ParallelOptions options = new ParallelOptions();
            options.MaxDegreeOfParallelism = 16;
            Parallel.ForEach(GetExpiredSubscriptions(), options, feed_uri =>
            {
                try
                {
                    Update(feed_uri, false);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            });
        }

        public void UpdateAll()
        {
            UpdateAll(false);
        }

        public bool TryGetFeedBasicInfo(string uri, out FeedBasicInfo info)
        {
            return feed_infos.TryGetValue(uri, out info);
        }

        public IEnumerable<DetailedInfo> GetFeedItems(string uri)
        {
            List<DetailedInfo> result = new List<DetailedInfo>();

            foreach (var kvp in detailed_infos.EnumerateFrom(Tuple.Create(uri, "")))
            {
                DetailedInfo info;
                if (kvp.Key.Item1 != uri)
                    break;
                if (kvp.Key.Item2 == "") // Information for entire feed
                    continue;
                info = kvp.Value;
                info.feed_uri = uri;
                info.id = kvp.Key.Item2;
                result.Add(info);
            }

            return result;
        }

        public bool TryGetDetailedInfo(string uri, string id, out DetailedInfo result)
        {
            if (detailed_infos.TryGetValue(Tuple.Create(uri, id), out result))
            {
                result.feed_uri = uri;
                result.id = id;
                return true;
            }
            return false;
        }

        public bool TryGetDetailedInfo(string uri, out DetailedInfo result)
        {
            return TryGetDetailedInfo(uri, "", out result);
        }

        public Tag GetTag(string name)
        {
            Tag result;
            if (loaded_tags.TryGetValue(name, out result))
                return result;
            loaded_tags.TryAdd(name, new Tag(this, name));
            result = loaded_tags[name];
            result.Load();
            return result;
        }

        public void SetSubscribed(string feed_uri, bool subscribed)
        {
            FeedBasicInfo info = GetBasicInfoSafe(feed_uri);
            if (info.autofetch != subscribed)
            {
                lock (GetFeedLock(feed_uri))
                {
                    info = GetBasicInfoSafe(feed_uri);
                    info.autofetch = subscribed;
                    feed_infos[feed_uri] = info;
                }
            }
        }

        public void Subscribe(string feed_uri)
        {
            SetSubscribed(feed_uri, true);
        }

        public void ImportOpml(string filename)
        {
            FileStream f = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
            OpmlDocument doc = new OpmlDocument();
            doc.Load(f);
            foreach (var outline in doc.Outlines)
            {
                string feed_uri = outline.Attributes["xmlUrl"];
                Subscribe(feed_uri);
            }
        }

        public IEnumerable<string> GetSubscriptions()
        {
            List<string> result = new List<string>();
            foreach (var kvp in feed_infos)
            {
                if (kvp.Value.autofetch)
                    result.Add(kvp.Key);
            }
            return result;
        }
    }
}
