using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Net;
using CSharpTest.Net.Collections;
using CSharpTest.Net.Serialization;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using HtmlAgilityPack;
using System.IO.Compression;
using System.Globalization;
using System.Net.Mime;

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

        static Cache()
        {
            // HACK
            HtmlEntity.EntityName[39] = "apos";
            HtmlEntity.EntityValue["apos"] = 39;
        }

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

        private static string hash_string(string data)
        {
            SHA1 sha = SHA1.Create();
            UTF8Encoding utf = new UTF8Encoding();
            return Convert.ToBase64String(sha.ComputeHash(utf.GetBytes(data)));
        }

        private static bool ParseRfc822DateTime(string str, out DateTime result)
        {
            string[] tokens = str.Split(' ');
            int pos = 0;
            while (pos < tokens.Length && string.IsNullOrWhiteSpace(tokens[pos]))
                pos++;
            if (tokens[pos].EndsWith(","))
            {
                // Day of week; this can only result in rejecting the date so I don't care
                pos++;
                while (pos < tokens.Length && string.IsNullOrWhiteSpace(tokens[pos]))
                    pos++;
            }

            result = DateTime.MinValue;

            if (pos >= tokens.Length)
                return false;
            int day;
            if (!(int.TryParse(tokens[pos], out day)))
                return false;
            pos++;
            while (pos < tokens.Length && string.IsNullOrWhiteSpace(tokens[pos]))
                pos++;

            if (pos >= tokens.Length)
                return false;
            int month;
            string[] months = DateTimeFormatInfo.InvariantInfo.AbbreviatedMonthNames;
            for (month = 0; month < 12; month++)
            {
                if (months[month] == tokens[pos])
                    break;
            }
            if (month == 12)
                return false;
            month++;
            pos++;
            while (pos < tokens.Length && string.IsNullOrWhiteSpace(tokens[pos]))
                pos++;

            if (pos >= tokens.Length)
                return false;
            int year;
            if (!(int.TryParse(tokens[pos], out year)))
                return false;
            // Why does a standard published in 1999 allow 2-digit years?
            if (year < 98)
                year = year + 2000;
            else if (year < 100)
                year = year + 1900;
            pos++;
            while (pos < tokens.Length && string.IsNullOrWhiteSpace(tokens[pos]))
                pos++;

            int hour, minute, second;
            if (pos >= tokens.Length)
                return false;
            string[] hour_token = tokens[pos].Split(':');
            if (hour_token.Length == 2)
            {
                if (!int.TryParse(hour_token[0], out hour))
                    return false;
                if (!int.TryParse(hour_token[1], out minute))
                    return false;
                second = 0;
            }
            else if (hour_token.Length == 3)
            {
                if (!int.TryParse(hour_token[0], out hour))
                    return false;
                if (!int.TryParse(hour_token[1], out minute))
                    return false;
                if (!int.TryParse(hour_token[2], out second))
                    return false;
            }
            else
                return false;
            pos++;
            while (pos < tokens.Length && string.IsNullOrWhiteSpace(tokens[pos]))
                pos++;

            TimeZoneInfo tzi = TimeZoneInfo.Utc;
            string tz = "";
            if (pos < tokens.Length)
            {
                tz = tokens[pos];
                if (tz == "UT" || tz == "GMT" || tz == "Z")
                    tzi = TimeZoneInfo.Utc;
                // We can't trust the EST/EDT distinction to be correct, so use a standard TimeZoneInfo
                else if (tz == "EST" || tz == "EDT")
                {
                    tzi = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
                }
                else if (tz == "CST" || tz == "CDT")
                {
                    tzi = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
                }
                else if (tz == "MST" || tz == "MDT")
                {
                    tzi = TimeZoneInfo.FindSystemTimeZoneById("Mountain Standard Time");
                }
                else if (tz == "PST" || tz == "PDT")
                {
                    tzi = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
                }
                else if (tz.Length == 1)
                {
                    int ofs;
                    char c = tz[0];
                    if (c >= 'A' && c <= 'I')
                        ofs = c - 'A' + 1;
                    else if (c >= 'K' && c <= 'M')
                        ofs = c - 'K' + 10;
                    else if (c >= 'N' && c <= 'Y')
                        ofs = -1 - (c - 'N');
                    else
                        return false;
                    tzi = TimeZoneInfo.CreateCustomTimeZone(tz, new TimeSpan(ofs, 0, 0), tz, tz);
                }
                else if (tz.StartsWith("+") || tz.StartsWith("-"))
                {
                    int offset;
                    if (!int.TryParse(tz.Substring(1), out offset))
                        return false;
                    int hours, minutes;
                    hours = offset / 100;
                    minutes = offset % 100;
                    if (tz.StartsWith("-"))
                    {
                        hours = -hours;
                        minutes = -minutes;
                    }
                    tzi = TimeZoneInfo.CreateCustomTimeZone(tz, new TimeSpan(hours, minutes, 0), tz, tz);
                }
                else
                    return false;
                pos++;
            }
            while (pos < tokens.Length && string.IsNullOrWhiteSpace(tokens[pos]))
                pos++;

            if (pos < tokens.Length)
                return false;

            try
            {
                DateTime localtime = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Unspecified);
                if (tzi.IsAmbiguousTime(localtime))
                {
                    TimeSpan[] offsets = tzi.GetAmbiguousTimeOffsets(localtime);
                    if (tz[1] == 'S')
                        result = DateTime.SpecifyKind(localtime - tzi.BaseUtcOffset, DateTimeKind.Utc);
                    else
                    {
                        if (offsets[0].Equals(tzi.BaseUtcOffset))
                            result = DateTime.SpecifyKind(localtime - offsets[1], DateTimeKind.Utc);
                        else
                            result = DateTime.SpecifyKind(localtime - offsets[0], DateTimeKind.Utc);
                    }
                }
                else
                {
                    result = TimeZoneInfo.ConvertTimeToUtc(localtime, tzi);
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                return false;
            }
            return true;
        }

        private static void ParseXmlSyndication(ref FeedBasicInfo feed, ref DetailedInfo feed_info, ref DetailedInfo item_info, DateTime minDate, DateTime maxDate)
        {
            HtmlNode rootnode = (HtmlNode)item_info.original_resource;
            bool in_link = false;
            DateTime updated = DateTime.MinValue;
            DateTime published = DateTime.MinValue;
            foreach (var itemnode in rootnode.ChildNodes)
            {
                string tagname = itemnode.Name.ToLowerInvariant();
                if (tagname.Contains(":"))
                    tagname = tagname.Split(':')[1];
                if (in_link)
                {
                    // HACK
                    if (itemnode.NodeType == HtmlNodeType.Text)
                        item_info.contents.Add(Tuple.Create("content-uri", HtmlEntity.DeEntitize(itemnode.InnerHtml.Trim())));
                    else if (itemnode.NodeType == HtmlNodeType.Comment && itemnode.OuterHtml.StartsWith("<![CDATA[") && itemnode.OuterHtml.EndsWith("]]>"))
                        item_info.contents.Add(Tuple.Create("content-uri", itemnode.OuterHtml.Substring(9, itemnode.OuterHtml.Length - 12)));
                    in_link = false;
                }
                if (tagname == "title")
                {
                    item_info.title = GetNodeTextContent(itemnode);
                }
                else if (tagname == "link")
                {
                    if (itemnode.Attributes.Contains("href"))
                        AddAtomLink(item_info, itemnode);
                    else
                        in_link = true;
                }
                else if (tagname == "description")
                {
                    item_info.contents.Add(Tuple.Create("description", GetNodeTextContent(itemnode)));
                }
                else if (tagname == "author")
                {
                    AddAtomAuthor(ref item_info, itemnode);
                }
                else if (tagname == "creator")
                {
                    item_info.author = GetNodeTextContent(itemnode);
                    item_info.contents.Add(Tuple.Create("author-name", item_info.author));
                }
                else if (tagname == "category")
                {
                    AddAtomCategory(item_info, itemnode);
                }
                else if (tagname == "comments")
                {
                    item_info.contents.Add(Tuple.Create("comments-uri", GetNodeTextContent(itemnode)));
                }
                else if (tagname == "source")
                {
                    item_info.contents.Add(Tuple.Create(string.Format("source-url:{0}", GetNodeTextContent(itemnode)), GetNodeAttr(itemnode, "url")));
                }
                else if (tagname == "enclosure")
                {
                    item_info.contents.Add(Tuple.Create(string.Format("enclosure-url:{0}", GetNodeAttr(itemnode, "type")), GetNodeAttr(itemnode, "url")));
                }
                else if (tagname == "pubdate" || tagname == "published")
                {
                    if (DateTime.TryParse(GetNodeTextContent(itemnode), out published) ||
                        ParseRfc822DateTime(GetNodeTextContent(itemnode), out published))
                        published = published.ToUniversalTime();
                }
                else if (tagname == "guid")
                {
                    item_info.id = GetNodeTextContent(itemnode);
                }
                else if (tagname == "content")
                {
                    item_info.contents.Add(Tuple.Create("content-html", GetAtomTextHtml(itemnode)));
                }
                else if (tagname == "id")
                {
                    item_info.id = GetNodeTextContent(itemnode);
                }
                else if (tagname == "updated")
                {
                    if (DateTime.TryParse(GetNodeTextContent(itemnode), out updated))
                        updated = updated.ToUniversalTime();
                }
                else if (itemnode.NodeType == HtmlNodeType.Element)
                {
                    if (itemnode.Attributes.Count != 0)
                        item_info.contents.Add(Tuple.Create(string.Format("xml:{0}", itemnode.Name), itemnode.OuterHtml));
                    else
                        item_info.contents.Add(Tuple.Create(string.Format("xml:{0}", itemnode.Name), itemnode.InnerHtml));
                }
            }
            if (published != DateTime.MinValue &&
                DateTime.Compare(minDate, published) <= 0 &&
                DateTime.Compare(maxDate, published) >= 0)
                item_info.timestamp = published;
            else if (updated != DateTime.MinValue &&
                DateTime.Compare(minDate, updated) <= 0 &&
                DateTime.Compare(maxDate, updated) >= 0)
                item_info.timestamp = updated;
            else if ((published != DateTime.MinValue && DateTime.Compare(published, minDate) < 0) ||
                (updated != DateTime.MinValue && DateTime.Compare(updated, minDate) < 0))
                item_info.timestamp = minDate;
            else
                item_info.timestamp = maxDate;
            if (string.IsNullOrWhiteSpace(item_info.id))
            {
                item_info.id = string.Format("sha1:{0}", hash_string(rootnode.OuterHtml));
            }
        }

        private void CreateErrorItem(string feed_uri, Exception e)
        {
            DetailedInfo error_item_info = new DetailedInfo();
            error_item_info.feed_uri = feed_uri;
            error_item_info.contents = new List<Tuple<string, string>>();
            error_item_info.timestamp = DateTime.UtcNow;
            error_item_info.id = "ERROR";
            error_item_info.title = string.Format("Error fetching {0}", feed_uri);
            error_item_info.contents.Add(new Tuple<string, string>("description", string.Format("Error fetching {0}<br/>{1}", feed_uri, text_to_html(e.ToString()))));
            detailed_infos[Tuple.Create(error_item_info.feed_uri, error_item_info.id)] = error_item_info;
            GetTag("(unread)").Add(error_item_info);
        }

        public bool Update(string uri, bool force)
        {
            ContentType content_type = null;
            DateTime minDate = DateTime.MinValue;
            DateTime maxDate;
            lock (GetFeedLock(uri))
            {
                FeedBasicInfo info = GetBasicInfoSafe(uri);
                if (!force && DateTime.Compare(DateTime.UtcNow, info.expiration) < 0)
                    return false;
                DateTime previous_check = info.lastchecked;
                if (info.lastchecked != default(DateTime) &&
                    DateTime.Compare(info.lastchecked, info.timestamp) > 0)
                    minDate = info.lastchecked;
                else if (info.timestamp != default(DateTime))
                    minDate = info.timestamp;
                info.lastchecked = DateTime.UtcNow;
                WebRequest request = WebRequest.Create(uri);
                if (request is HttpWebRequest)
                {
                    var headers = request.Headers;
                    if (info.timestamp != default(DateTime))
                        (request as HttpWebRequest).IfModifiedSince = info.timestamp;
                    if (info.etag != null && info.etag != "")
                        headers.Add("If-None-Match", info.etag);
                    (request as HttpWebRequest).UserAgent = "formsfeed";
                }
                WebResponse response;
                try
                {
                    response = request.GetResponse();
                    maxDate = DateTime.UtcNow;
                }
                catch (Exception e)
                {
                    maxDate = DateTime.UtcNow;
                    if (e is WebException)
                    {
                        WebResponse r = ((WebException)e).Response;
                        if (r is HttpWebResponse && ((HttpWebResponse)r).StatusCode == HttpStatusCode.NotModified)
                        {
                            HttpWebResponse httpresponse = (HttpWebResponse)r;
                            info.expiration = DateTime.MinValue;
                            var headers = httpresponse.Headers;
                            for (int i = 0; i < headers.Count; i++)
                            {
                                string key = headers.GetKey(i);
                                if (key == "Last-Modified" && DateTime.TryParse(headers.Get(i), out info.timestamp))
                                {
                                    info.timestamp = info.timestamp.ToUniversalTime();
                                    if (DateTime.Compare(info.timestamp, maxDate) < 0)
                                        maxDate = info.timestamp;
                                }
                                else if (key == "ETag")
                                    info.etag = headers.Get(i);
                                else if (key == "Expires" && DateTime.TryParse(headers.Get(i), out info.expiration))
                                    info.expiration = info.expiration.ToUniversalTime();
                            }
                            feed_infos[info.uri] = info;
                            return false;
                        }
                    }
                    Console.WriteLine(e);

                    CreateErrorItem(info.uri, e);

                    info.lastchecked = previous_check;
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
                        {
                            info.timestamp = info.timestamp.ToUniversalTime();
                            if (DateTime.Compare(info.timestamp, maxDate) < 0)
                                maxDate = info.timestamp;
                        }
                        else if (key == "ETag")
                            info.etag = headers.Get(i);
                        else if (key == "Expires" && DateTime.TryParse(headers.Get(i), out info.expiration))
                            info.expiration = info.expiration.ToUniversalTime();
                    }
                    if (!string.IsNullOrWhiteSpace(response.ContentType))
                    {
                        content_type = new ContentType(response.ContentType);
                    }
                }

                var memoryStream = new MemoryStream();
                response.GetResponseStream().CopyTo(memoryStream);

                // Type detection
                memoryStream.Seek(0, SeekOrigin.Begin);
                byte[] buffer = new byte[2];
                memoryStream.Read(buffer, 0, 2);
                if (buffer[0] == 0x1f && buffer[1] == 0x8b)
                {
                    // gzip
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    var newMemoryStream = new MemoryStream();
                    var gzipStream = new GZipStream(memoryStream, CompressionMode.Decompress);
                    gzipStream.CopyTo(newMemoryStream);
                    memoryStream = newMemoryStream;
                }

                memoryStream.Seek(0, SeekOrigin.Begin);

                HtmlDocument doc = new HtmlDocument();

                if (content_type == null || string.IsNullOrWhiteSpace(content_type.CharSet))
                {
                    doc.Load(memoryStream, true);
                }
                else
                {
                    doc.Load(memoryStream, Encoding.GetEncoding(content_type.CharSet), true);
                }

                HtmlNode root = null;

                foreach (var node in doc.DocumentNode.ChildNodes)
                {
                    if (node.Name == "rss" || node.Name == "feed" || node.Name == "rdf:rdf" || node.Name == "html")
                    {
                        root = node;
                        break;
                    }
                }

                if (root == null)
                {
                    throw new Exception("unrecognized xml type");
                }

                DetailedInfo feed_detailed_info = new DetailedInfo();
                feed_detailed_info.feed_uri = uri;
                feed_detailed_info.contents = new List<Tuple<string, string>>();
                feed_detailed_info.original_resource = doc;
                LinkedList<DetailedInfo> items = new LinkedList<DetailedInfo>();
                HashSet<Tuple<string, string>> item_keys = new HashSet<Tuple<string, string>>();

                if (root.Name == "rss")
                {
                    foreach (var rssnode in root.ChildNodes)
                    {
                        if (rssnode.Name == "channel")
                        {
                            bool in_link = false;
                            foreach (var channelnode in rssnode.ChildNodes)
                            {
                                if (in_link)
                                {
                                    // HACK
                                    if (channelnode.NodeType == HtmlNodeType.Text)
                                        feed_detailed_info.contents.Add(Tuple.Create("content-uri", HtmlEntity.DeEntitize(channelnode.InnerHtml.Trim())));
                                    else if (channelnode.NodeType == HtmlNodeType.Comment && channelnode.OuterHtml.StartsWith("<![CDATA[") && channelnode.OuterHtml.EndsWith("]]>"))
                                        feed_detailed_info.contents.Add(Tuple.Create("content-uri", channelnode.OuterHtml.Substring(9, channelnode.OuterHtml.Length - 12)));
                                    in_link = false;
                                }
                                if (channelnode.Name == "item")
                                {
                                    DetailedInfo iteminfo = new DetailedInfo();
                                    iteminfo.feed_uri = info.uri;
                                    iteminfo.contents = new List<Tuple<string, string>>();
                                    iteminfo.original_resource = channelnode;
                                    ParseXmlSyndication(ref info, ref feed_detailed_info, ref iteminfo, minDate, maxDate);
                                    if (!detailed_infos.ContainsKey(Tuple.Create(iteminfo.feed_uri, iteminfo.id)) &&
                                        !item_keys.Contains(Tuple.Create(iteminfo.feed_uri, iteminfo.id)))
                                    {
                                        items.AddLast(iteminfo);
                                        item_keys.Add(Tuple.Create(iteminfo.feed_uri, iteminfo.id));
                                    }
                                }
                                else if (channelnode.Name == "title")
                                {
                                    feed_detailed_info.title = GetNodeTextContent(channelnode);
                                }
                                else if (channelnode.Name == "link")
                                {
                                    in_link = true;
                                }
                                else if (channelnode.Name == "description")
                                {
                                    feed_detailed_info.contents.Add(Tuple.Create("description", GetNodeTextContent(channelnode)));
                                }
                                else if (channelnode.Name == "category")
                                {
                                    feed_detailed_info.contents.Add(Tuple.Create("category", GetNodeTextContent(channelnode)));
                                }
                                else if (channelnode.Name == "image")
                                {
                                    foreach (var imagenode in channelnode.ChildNodes)
                                    {
                                        if (imagenode.Name == "url")
                                            feed_detailed_info.contents.Add(Tuple.Create("image-uri", GetNodeTextContent(channelnode)));
                                    }
                                }
                                else if (channelnode.NodeType == HtmlNodeType.Element)
                                {
                                    if (channelnode.Attributes.Count != 0)
                                        feed_detailed_info.contents.Add(Tuple.Create(string.Format("rss:{0}", channelnode.Name), channelnode.OuterHtml));
                                    else
                                        feed_detailed_info.contents.Add(Tuple.Create(string.Format("rss:{0}", channelnode.Name), channelnode.InnerHtml));
                                }
                            }
                        }
                        else if (rssnode.Name == "item")
                        {
                            DetailedInfo iteminfo = new DetailedInfo();
                            iteminfo.feed_uri = info.uri;
                            iteminfo.contents = new List<Tuple<string, string>>();
                            iteminfo.original_resource = rssnode;
                            ParseXmlSyndication(ref info, ref feed_detailed_info, ref iteminfo, minDate, maxDate);
                            if (!detailed_infos.ContainsKey(Tuple.Create(iteminfo.feed_uri, iteminfo.id)) &&
                                !item_keys.Contains(Tuple.Create(iteminfo.feed_uri, iteminfo.id)))
                            {
                                items.AddLast(iteminfo);
                                item_keys.Add(Tuple.Create(iteminfo.feed_uri, iteminfo.id));
                            }
                        }
                        else if (rssnode.NodeType == HtmlNodeType.Element)
                                Console.Error.WriteLine("Unknown rss tag {0}", rssnode.Name);
                    }
                    if (string.IsNullOrWhiteSpace(feed_detailed_info.title))
                        feed_detailed_info.title = uri;
                    if (string.IsNullOrWhiteSpace(feed_detailed_info.author))
                        feed_detailed_info.author = feed_detailed_info.title;
                    LinkedListNode<DetailedInfo> node = items.First;
                    while (node != null)
                    {
                        if (string.IsNullOrWhiteSpace(node.Value.author))
                        {
                            DetailedInfo new_info = node.Value;
                            new_info.author = feed_detailed_info.author;
                            node.Value = new_info;
                        }
                        node = node.Next;
                    }
                }
                else if (root.Name == "rdf:rdf")
                {
                    foreach (var rdfnode in root.ChildNodes)
                    {
                        if (rdfnode.Name == "channel")
                        {
                            bool in_link = false;
                            foreach (var channelnode in rdfnode.ChildNodes)
                            {
                                if (in_link)
                                {
                                    // HACK
                                    if (channelnode.NodeType == HtmlNodeType.Text)
                                        feed_detailed_info.contents.Add(Tuple.Create("content-uri", HtmlEntity.DeEntitize(channelnode.OuterHtml.Trim())));
                                    else if (channelnode.NodeType == HtmlNodeType.Comment && channelnode.OuterHtml.StartsWith("<![CDATA[") && channelnode.OuterHtml.EndsWith("]]>"))
                                        feed_detailed_info.contents.Add(Tuple.Create("content-uri", channelnode.OuterHtml.Substring(9, channelnode.OuterHtml.Length - 12)));
                                    in_link = false;
                                }
                                if (channelnode.Name == "title")
                                {
                                    feed_detailed_info.title = GetNodeTextContent(channelnode);
                                }
                                else if (channelnode.Name == "link")
                                {
                                    in_link = true;
                                }
                                else if (channelnode.Name == "description")
                                {
                                    feed_detailed_info.contents.Add(Tuple.Create("description", GetNodeTextContent(channelnode)));
                                }
                                else if (channelnode.Name == "category")
                                {
                                    feed_detailed_info.contents.Add(Tuple.Create("category", GetNodeTextContent(channelnode)));
                                }
                                else if (channelnode.Name == "image")
                                {
                                    foreach (var imagenode in channelnode.ChildNodes)
                                    {
                                        if (imagenode.Name == "url")
                                            feed_detailed_info.contents.Add(Tuple.Create("image-uri", GetNodeTextContent(channelnode)));
                                    }
                                }
                                else if (channelnode.NodeType == HtmlNodeType.Element)
                                {
                                    if (channelnode.Attributes.Count != 0)
                                        feed_detailed_info.contents.Add(Tuple.Create(string.Format("rdf:{0}", channelnode.Name), channelnode.OuterHtml));
                                    else
                                        feed_detailed_info.contents.Add(Tuple.Create(string.Format("rdf:{0}", channelnode.Name), channelnode.InnerHtml));
                                }
                            }
                        }
                        else if (rdfnode.Name == "item")
                        {
                            DetailedInfo iteminfo = new DetailedInfo();
                            iteminfo.feed_uri = info.uri;
                            iteminfo.contents = new List<Tuple<string, string>>();
                            iteminfo.original_resource = rdfnode;
                            ParseXmlSyndication(ref info, ref feed_detailed_info, ref iteminfo, minDate, maxDate);
                            if (!detailed_infos.ContainsKey(Tuple.Create(iteminfo.feed_uri, iteminfo.id)) &&
                                !item_keys.Contains(Tuple.Create(iteminfo.feed_uri, iteminfo.id)))
                            {
                                items.AddLast(iteminfo);
                                item_keys.Add(Tuple.Create(iteminfo.feed_uri, iteminfo.id));
                            }
                        }
                        else if (rdfnode.NodeType == HtmlNodeType.Element)
                        {
                            Console.Error.WriteLine("Unknown rdf tag {0}", rdfnode.Name);
                        }
                    }
                    if (string.IsNullOrWhiteSpace(feed_detailed_info.title))
                        feed_detailed_info.title = uri;
                    if (string.IsNullOrWhiteSpace(feed_detailed_info.author))
                        feed_detailed_info.author = feed_detailed_info.title;
                    LinkedListNode<DetailedInfo> node = items.First;
                    while (node != null)
                    {
                        if (string.IsNullOrWhiteSpace(node.Value.author))
                        {
                            DetailedInfo new_info = node.Value;
                            new_info.author = feed_detailed_info.author;
                            node.Value = new_info;
                        }
                        node = node.Next;
                    }
                }
                else if (root.Name == "feed")
                {
                    foreach (var feednode in root.ChildNodes)
                    {
                        if (feednode.Name == "entry")
                        {
                            DetailedInfo iteminfo = new DetailedInfo();
                            iteminfo.feed_uri = info.uri;
                            iteminfo.contents = new List<Tuple<string, string>>();
                            iteminfo.original_resource = feednode;
                            ParseXmlSyndication(ref info, ref feed_detailed_info, ref iteminfo, minDate, maxDate);
                            if (!detailed_infos.ContainsKey(Tuple.Create(iteminfo.feed_uri, iteminfo.id)) &&
                                !item_keys.Contains(Tuple.Create(iteminfo.feed_uri, iteminfo.id)))
                            {
                                items.AddLast(iteminfo);
                                item_keys.Add(Tuple.Create(iteminfo.feed_uri, iteminfo.id));
                            }
                        }
                        else if (feednode.Name == "title")
                        {
                            feed_detailed_info.title = GetNodeTextContent(feednode);
                        }
                        else if (feednode.Name == "link")
                        {
                            AddAtomLink(feed_detailed_info, feednode);
                        }
                        else if (feednode.Name == "author")
                        {
                            AddAtomAuthor(ref feed_detailed_info, feednode);
                        }
                        else if (feednode.Name == "category")
                        {
                            AddAtomCategory(feed_detailed_info, feednode);
                        }
                        else if (feednode.Name == "logo")
                        {
                            foreach (var imagenode in feednode.ChildNodes)
                            {
                                if (imagenode.Name == "url")
                                    feed_detailed_info.contents.Add(Tuple.Create("image-uri", GetNodeTextContent(feednode)));
                            }
                        }
                        else if (feednode.Name == "subtitle")
                        {
                            feed_detailed_info.contents.Add(Tuple.Create("subtitle-html", GetAtomTextHtml(feednode)));
                        }
                        else if (feednode.NodeType == HtmlNodeType.Element)
                        {
                            if (feednode.Attributes.Count != 0)
                                feed_detailed_info.contents.Add(Tuple.Create(string.Format("atom:{0}", feednode.Name), feednode.OuterHtml));
                            else
                                feed_detailed_info.contents.Add(Tuple.Create(string.Format("atom:{0}", feednode.Name), feednode.InnerHtml));
                        }
                    }
                    if (string.IsNullOrWhiteSpace(feed_detailed_info.title))
                        feed_detailed_info.title = uri;
                    if (string.IsNullOrWhiteSpace(feed_detailed_info.author))
                        feed_detailed_info.author = feed_detailed_info.title;
                    LinkedListNode<DetailedInfo> node = items.First;
                    while (node != null)
                    {
                        if (string.IsNullOrWhiteSpace(node.Value.author))
                        {
                            DetailedInfo new_info = node.Value;
                            new_info.author = feed_detailed_info.author;
                            node.Value = new_info;
                        }
                        node = node.Next;
                    }
                }
                else if (root.Name == "html")
                {
                    throw new Exception ("html not supported yet");
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
            }

            return true;
        }

        private static void AddAtomCategory(DetailedInfo detailed_info, HtmlNode node)
        {
            string term = null, scheme = null, label = null;
            foreach (var subnode in node.ChildNodes)
            {
                if (subnode.Name == "term")
                    term = GetNodeTextContent(subnode);
                else if (subnode.Name == "scheme")
                    scheme = GetNodeTextContent(subnode);
                else if (subnode.Name == "label")
                    label = GetNodeTextContent(subnode);
            }
            if (term == null)
            {
                string nodetext = GetNodeTextContent(node);
                if (string.IsNullOrWhiteSpace(nodetext))
                    return;
                if (node.Attributes.Contains("domain"))
                    detailed_info.contents.Add(Tuple.Create(string.Format("category:{0}", GetNodeAttr(node, "domain")), GetNodeTextContent(node)));
                else
                    detailed_info.contents.Add(Tuple.Create("category", GetNodeTextContent(node)));
            }
            if (scheme != null)
                detailed_info.contents.Add(Tuple.Create(string.Format("category:{0}", scheme), term));
            else
                detailed_info.contents.Add(Tuple.Create("category", term));
            if (label != null)
                detailed_info.contents.Add(Tuple.Create("category-label", label));
        }

        private static void AddAtomLink(DetailedInfo detailed_info, HtmlNode node)
        {
            string rel;
            if (node.Attributes.Contains("rel"))
                rel = GetNodeAttr(node, "rel");
            else
                rel = "alternate";
            detailed_info.contents.Add(Tuple.Create(string.Format("link:{0}-uri", rel), GetNodeAttr(node, "href")));
            if (node.Attributes.Contains("title"))
                detailed_info.contents.Add(Tuple.Create(string.Format("link:{0}-title", rel), GetNodeAttr(node, "title")));
        }

        private static void AddAtomAuthor(ref DetailedInfo detailed_info, HtmlNode node)
        {
            string node_text = "";
            bool seen_tags = false;
            foreach (var subnode in node.ChildNodes)
            {
                if (subnode.Name == "name")
                {
                    detailed_info.author = GetNodeTextContent(subnode);
                    detailed_info.contents.Add(Tuple.Create("author-name", GetNodeTextContent(subnode)));
                    seen_tags = true;
                }
                else if (subnode.Name == "uri")
                {
                    detailed_info.contents.Add(Tuple.Create("author-uri", GetNodeTextContent(subnode)));
                    seen_tags = true;
                }
                else if (subnode.Name == "email")
                {
                    detailed_info.contents.Add(Tuple.Create("author-email", GetNodeTextContent(subnode)));
                    seen_tags = true;
                }
                else if (subnode.NodeType == HtmlNodeType.Text)
                    node_text = node_text + HtmlEntity.DeEntitize(subnode.InnerText.Trim());
                else if (subnode.NodeType == HtmlNodeType.Comment && subnode.OuterHtml.StartsWith("<![CDATA[") && subnode.OuterHtml.EndsWith("]]>"))
                    node_text = node_text + subnode.OuterHtml.Substring(9, subnode.OuterHtml.Length - 12);
            }
            if (!seen_tags)
            {
                detailed_info.contents.Add(Tuple.Create("author-email", node_text));
                if (node_text.Contains("(") && node_text.EndsWith(")"))
                {
                    detailed_info.author = node_text.Split('(')[1].TrimEnd(')');
                    detailed_info.contents.Add(Tuple.Create("author-name", detailed_info.author));
                }
            }
        }

        private static string GetNodeTextContent(HtmlNode node)
        {
            string result = string.Empty;
            foreach (var subnode in node.ChildNodes)
            {
                if (subnode.NodeType == HtmlNodeType.Text)
                    result = result + HtmlEntity.DeEntitize(subnode.InnerText.Trim());
                else if (subnode.NodeType == HtmlNodeType.Element)
                    result = result + subnode.OuterHtml;
                else if (subnode.NodeType == HtmlNodeType.Comment && subnode.OuterHtml.StartsWith("<![CDATA[") && subnode.OuterHtml.EndsWith("]]>"))
                    result = result + subnode.OuterHtml.Substring(9, subnode.OuterHtml.Length - 12);
            }
            return result;
        }

        private static string GetNodeAttr(HtmlNode node, string name)
        {
            return HtmlEntity.DeEntitize(node.Attributes[name].Value);
        }

        private static string GetAtomTextHtml(HtmlNode itemnode)
        {
            if (!itemnode.Attributes.Contains("type"))
                return text_to_html(GetNodeTextContent(itemnode));
            string texttype = GetNodeAttr(itemnode, "type");
            if (texttype == "text")
                return text_to_html(GetNodeTextContent(itemnode));
            else if (texttype == "html")
                return GetNodeTextContent(itemnode);
            else
                return itemnode.InnerHtml;
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
            content = content.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\n", "<br/>");
            return content;
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

        public delegate void ProgressCallback(int total);

        private static void NoProgressCallback(int total)
        {
        }

        public void UpdateAll(bool force, ProgressCallback cb)
        {
            ParallelOptions options = new ParallelOptions();
            var subs = new List<string>(GetExpiredSubscriptions());
            cb(subs.Count);
            Parallel.ForEach(GetExpiredSubscriptions(), options, feed_uri =>
            {
                try
                {
                    Update(feed_uri, false);
                }
                catch (Exception e)
                {
                    lock (Console.Out)
                    {
                        Console.WriteLine(feed_uri);
                        Console.WriteLine(e);
                        CreateErrorItem(feed_uri, e);
                    }
                }
                cb(subs.Count);
            });
        }

        public void UpdateAll(bool force)
        {
            UpdateAll(force, new ProgressCallback(NoProgressCallback));
        }

        public void UpdateAll(ProgressCallback cb)
        {
            UpdateAll(false, cb);
        }

        public void UpdateAll()
        {
            UpdateAll(false, new ProgressCallback(NoProgressCallback));
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

        public IEnumerable<string> GetTagNames()
        {
            foreach (var file in Directory.GetFiles(basepath, "*.tag"))
            {
                yield return Path.GetFileNameWithoutExtension(file);
            }
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
            HtmlDocument doc = new HtmlDocument();
            doc.Load(filename);

            HtmlNode root = null;

            foreach (var node in doc.DocumentNode.ChildNodes)
            {
                if (node.Name == "opml")
                {
                    root = node;
                    break;
                }
            }

            if (root == null)
                return;

            HtmlNode body = null;

            foreach (var node in root.ChildNodes)
            {
                if (node.Name == "body")
                {
                    body = node;
                    break;
                }
            }

            if (body == null)
                return;

            foreach (var node in body.ChildNodes)
            {
                if (node.Name != "outline")
                    continue;
                string feed_uri = GetNodeAttr(node, "xmlUrl");
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
