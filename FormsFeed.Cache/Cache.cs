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
                catch (Exception e)
                {
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
                                    info.timestamp = info.timestamp.ToUniversalTime();
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

                doc.Load(memoryStream);

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
                        if (rssnode.Name != "channel")
                        {
                            if (rssnode.NodeType == HtmlNodeType.Element)
                                Console.Error.WriteLine("Unknown rss tag {0}", rssnode.Name);
                            continue;
                        }
                        bool in_link = false;
                        foreach (var channelnode in rssnode.ChildNodes)
                        {
                            if (in_link)
                            {
                                // HACK
                                if (channelnode.NodeType == HtmlNodeType.Text)
                                    feed_detailed_info.contents.Add(Tuple.Create("content-uri", HtmlEntity.DeEntitize(channelnode.OuterHtml)));
                                in_link = false;
                            }
                            if (channelnode.Name == "item")
                            {
                                DetailedInfo iteminfo = new DetailedInfo();
                                iteminfo.feed_uri = info.uri;
                                iteminfo.contents = new List<Tuple<string, string>>();
                                iteminfo.original_resource = channelnode;
                                foreach (var itemnode in channelnode.ChildNodes)
                                {
                                    if (in_link)
                                    {
                                        // HACK
                                        if (itemnode.NodeType == HtmlNodeType.Text)
                                            iteminfo.contents.Add(Tuple.Create("content-uri", HtmlEntity.DeEntitize(itemnode.OuterHtml)));
                                        in_link = false;
                                    }
                                    if (itemnode.Name == "title")
                                    {
                                        iteminfo.title = GetNodeTextContent(itemnode);
                                    }
                                    else if (itemnode.Name == "link")
                                    {
                                        in_link = true;
                                    }
                                    else if (itemnode.Name == "description")
                                    {
                                        iteminfo.contents.Add(Tuple.Create("description", GetNodeTextContent(itemnode)));
                                    }
                                    else if (itemnode.Name == "author")
                                    {
                                        string author = GetNodeTextContent(itemnode);
                                        iteminfo.contents.Add(Tuple.Create("author-email", GetNodeTextContent(itemnode)));
                                        if (author.Contains("(") && author.EndsWith(")"))
                                        {
                                            iteminfo.author = author.Split('(')[1].TrimEnd(')');
                                        }
                                    }
                                    else if (itemnode.Name == "category")
                                    {
                                        if (itemnode.Attributes.Contains("domain"))
                                            iteminfo.contents.Add(Tuple.Create(string.Format("category:{0}", GetNodeAttr(itemnode, "domain")), GetNodeTextContent(itemnode)));
                                        else
                                            iteminfo.contents.Add(Tuple.Create("category", GetNodeTextContent(itemnode)));
                                    }
                                    else if (itemnode.Name == "comments")
                                    {
                                        iteminfo.contents.Add(Tuple.Create("comments-uri", GetNodeTextContent(itemnode)));
                                    }
                                    else if (itemnode.Name == "source")
                                    {
                                        iteminfo.contents.Add(Tuple.Create(string.Format("source-url:{0}", GetNodeTextContent(itemnode)), GetNodeAttr(itemnode,"url")));
                                    }
                                    else if (itemnode.Name == "enclosure")
                                    {
                                        iteminfo.contents.Add(Tuple.Create(string.Format("enclosure-url:{0}", GetNodeAttr(itemnode, "type")), GetNodeAttr(itemnode, "url")));
                                    }
                                    else if (itemnode.Name.ToLowerInvariant() == "pubdate")
                                    {
                                        if (DateTime.TryParse(GetNodeTextContent(itemnode), out iteminfo.timestamp))
                                            iteminfo.timestamp = iteminfo.timestamp.ToUniversalTime();
                                    }
                                    else if (itemnode.Name == "guid")
                                    {
                                        iteminfo.id = GetNodeTextContent(itemnode);
                                    }
                                    else if (itemnode.NodeType == HtmlNodeType.Element)
                                    {
                                        if (itemnode.Attributes.Count != 0)
                                            iteminfo.contents.Add(Tuple.Create(string.Format("rss:{0}", itemnode.Name), itemnode.OuterHtml));
                                        else
                                            iteminfo.contents.Add(Tuple.Create(string.Format("rss:{0}", itemnode.Name), itemnode.InnerHtml));
                                    }
                                }
                                if (string.IsNullOrWhiteSpace(iteminfo.id))
                                {
                                    iteminfo.id = string.Format("sha1:{0}", hash_string(channelnode.OuterHtml));
                                }
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
                                        feed_detailed_info.contents.Add(Tuple.Create("content-uri", HtmlEntity.DeEntitize(channelnode.OuterHtml)));
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
                            bool in_link = false;
                            foreach (var itemnode in rdfnode.ChildNodes)
                            {
                                if (in_link)
                                {
                                    // HACK
                                    if (itemnode.NodeType == HtmlNodeType.Text)
                                        iteminfo.contents.Add(Tuple.Create("content-uri", HtmlEntity.DeEntitize(itemnode.OuterHtml)));
                                    in_link = false;
                                }
                                if (itemnode.Name == "title")
                                {
                                    iteminfo.title = GetNodeTextContent(itemnode);
                                }
                                else if (itemnode.Name == "link")
                                {
                                    in_link = true;
                                }
                                else if (itemnode.Name == "description")
                                {
                                    iteminfo.contents.Add(Tuple.Create("description", GetNodeTextContent(itemnode)));
                                }
                                else if (itemnode.Name == "author")
                                {
                                    string author = GetNodeTextContent(itemnode);
                                    iteminfo.contents.Add(Tuple.Create("author-email", GetNodeTextContent(itemnode)));
                                    if (author.Contains("(") && author.EndsWith(")"))
                                    {
                                        iteminfo.author = author.Split('(')[1].TrimEnd(')');
                                    }
                                }
                                else if (itemnode.Name == "category")
                                {
                                    if (itemnode.Attributes.Contains("domain"))
                                        iteminfo.contents.Add(Tuple.Create(string.Format("category:{0}", GetNodeAttr(itemnode, "domain")), GetNodeTextContent(itemnode)));
                                    else
                                        iteminfo.contents.Add(Tuple.Create("category", GetNodeTextContent(itemnode)));
                                }
                                else if (itemnode.Name == "comments")
                                {
                                    iteminfo.contents.Add(Tuple.Create("comments-uri", GetNodeTextContent(itemnode)));
                                }
                                else if (itemnode.Name == "source")
                                {
                                    iteminfo.contents.Add(Tuple.Create(string.Format("source-url:{0}", GetNodeTextContent(itemnode)), GetNodeAttr(itemnode,"url")));
                                }
                                else if (itemnode.Name == "enclosure")
                                {
                                    iteminfo.contents.Add(Tuple.Create(string.Format("enclosure-url:{0}", GetNodeAttr(itemnode, "type")), GetNodeAttr(itemnode, "url")));
                                }
                                else if (itemnode.Name.ToLowerInvariant() == "pubdate")
                                {
                                    if (DateTime.TryParse(GetNodeTextContent(itemnode), out iteminfo.timestamp))
                                        iteminfo.timestamp = iteminfo.timestamp.ToUniversalTime();
                                }
                                else if (itemnode.Name == "guid")
                                {
                                    iteminfo.id = GetNodeTextContent(itemnode);
                                }
                                else if (itemnode.NodeType == HtmlNodeType.Element)
                                {
                                    if (itemnode.Attributes.Count != 0)
                                        iteminfo.contents.Add(Tuple.Create(string.Format("rdf:{0}", itemnode.Name), itemnode.OuterHtml));
                                    else
                                        iteminfo.contents.Add(Tuple.Create(string.Format("rdf:{0}", itemnode.Name), itemnode.InnerHtml));
                                }
                            }
                            if (string.IsNullOrWhiteSpace(iteminfo.id))
                            {
                                iteminfo.id = string.Format("sha1:{0}", hash_string(rdfnode.OuterHtml));
                            }
                            if (!detailed_infos.ContainsKey(Tuple.Create(iteminfo.feed_uri, iteminfo.id)) &&
                                !item_keys.Contains(Tuple.Create(iteminfo.feed_uri, iteminfo.id)))
                            {
                                items.AddLast(iteminfo);
                                item_keys.Add(Tuple.Create(iteminfo.feed_uri, iteminfo.id));
                            }
                        }
                        else if (rdfnode.NodeType == HtmlNodeType.Element)
                        {
                            Console.Error.WriteLine("Unknown rss tag {0}", rdfnode.Name);
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
                            foreach (var itemnode in feednode.ChildNodes)
                            {
                                if (itemnode.Name == "title")
                                {
                                    iteminfo.title = GetNodeTextContent(itemnode);
                                }
                                else if (itemnode.Name == "content")
                                {
                                    iteminfo.contents.Add(Tuple.Create("content-html", GetAtomTextHtml(itemnode)));
                                }
                                else if (itemnode.Name == "link")
                                {
                                    AddAtomLink(iteminfo, itemnode);
                                }
                                else if (itemnode.Name == "author")
                                {
                                    AddAtomAuthor(iteminfo, itemnode);
                                }
                                else if (itemnode.Name == "category")
                                {
                                    if (itemnode.Attributes.Contains("domain"))
                                        iteminfo.contents.Add(Tuple.Create(string.Format("category:{0}", GetNodeAttr(itemnode, "domain")), GetNodeTextContent(itemnode)));
                                    else
                                        iteminfo.contents.Add(Tuple.Create("category", GetNodeTextContent(itemnode)));
                                }
                                else if (itemnode.Name == "published")
                                {
                                    if (DateTime.TryParse(GetNodeTextContent(itemnode), out iteminfo.timestamp))
                                        iteminfo.timestamp = iteminfo.timestamp.ToUniversalTime();
                                }
                                else if (itemnode.Name == "id")
                                {
                                    iteminfo.id = GetNodeTextContent(itemnode);
                                }
                                else if (itemnode.Name == "category")
                                {
                                    AddAtomCategory(iteminfo, itemnode);
                                }
                                else if (itemnode.NodeType == HtmlNodeType.Element)
                                {
                                    if (itemnode.Attributes.Count != 0)
                                        iteminfo.contents.Add(Tuple.Create(string.Format("atom:{0}", itemnode.Name), itemnode.OuterHtml));
                                    else
                                        iteminfo.contents.Add(Tuple.Create(string.Format("atom:{0}", itemnode.Name), itemnode.InnerHtml));
                                }
                            }
                            if (string.IsNullOrWhiteSpace(iteminfo.id))
                            {
                                iteminfo.id = string.Format("sha1:{0}", hash_string(feednode.OuterHtml));
                            }
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
                            AddAtomAuthor(feed_detailed_info, feednode);
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

        private void AddAtomCategory(DetailedInfo detailed_info, HtmlNode node)
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
                return;
            if (scheme != null)
                detailed_info.contents.Add(Tuple.Create(string.Format("category:{0}", scheme), term));
            else
                detailed_info.contents.Add(Tuple.Create("category", term));
            if (label != null)
                detailed_info.contents.Add(Tuple.Create("category-label", label));
        }

        private void AddAtomLink(DetailedInfo detailed_info, HtmlNode node)
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

        private void AddAtomAuthor(DetailedInfo detailed_info, HtmlNode node)
        {
            foreach (var subnode in node.ChildNodes)
            {
                if (subnode.Name == "name" && detailed_info.author == null)
                    detailed_info.author = GetNodeTextContent(subnode);
                else if (subnode.Name == "uri")
                    detailed_info.contents.Add(Tuple.Create("author-uri", GetNodeTextContent(subnode)));
                else if (subnode.Name == "email")
                    detailed_info.contents.Add(Tuple.Create("author-email", GetNodeTextContent(subnode)));
            }
        }

        private string GetNodeTextContent(HtmlNode node)
        {
            string result = string.Empty;
            foreach (var subnode in node.ChildNodes)
            {
                if (subnode.NodeType == HtmlNodeType.Text)
                    result = result + HtmlEntity.DeEntitize(subnode.InnerText).Trim();
                else if (subnode.NodeType == HtmlNodeType.Element)
                    result = result + subnode.OuterHtml;
            }
            return result;
        }

        private string GetNodeAttr(HtmlNode node, string name)
        {
            return HtmlEntity.DeEntitize(node.Attributes[name].Value);
        }

        private string GetAtomTextHtml(HtmlNode itemnode)
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
