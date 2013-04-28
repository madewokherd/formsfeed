﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Net;
using CSharpTest.Net.Collections;
using CSharpTest.Net.Serialization;
using Argotic.Syndication;

namespace FormsFeed.Cache
{
    public class Cache : IDisposable
    {
        private string basepath;
        private FileStream lockfile;
        internal BPlusTree<string, FeedBasicInfo> feed_infos;
        internal BPlusTree<Tuple<string, string>, DetailedInfo> detailed_infos;

        public Cache(string path)
        {
            Serializers s = new Serializers();

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
            return result;
        }

        public bool Update(string uri, bool force)
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
                    items.AddLast(iteminfo);
                }
            }

            //FIXME: Tag all new items as unread, apply any applicable filters?

            LinkedListNode<DetailedInfo> link = items.First;
            while (link != null) {
                DetailedInfo iteminfo = link.Value;
                if (iteminfo.timestamp.CompareTo(previous_check) < 0)
                    iteminfo.timestamp = previous_check;
                else if (iteminfo.timestamp.CompareTo(DateTime.UtcNow) > 0)
                    iteminfo.timestamp = DateTime.UtcNow;

                detailed_infos[Tuple.Create(iteminfo.feed_uri, iteminfo.id)] = iteminfo;

                link = link.Next;
            }

            detailed_infos[Tuple.Create(feed_detailed_info.feed_uri, "")] = feed_detailed_info;

            feed_infos[info.uri] = info;
            return true;
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
    }
}
