using System;
using System.Collections.Generic;
using HtmlAgilityPack;

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
        public object original_resource;

        public string get_content_uri()
        {
            foreach (var item in contents)
            {
                if (item.Item1 == "content-uri" || item.Item1 == "link:alternate-uri")
                    return item.Item2;
            }
            return null;
        }

        private string get_content_raw_html()
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

        public string get_content_html()
        {
            string raw_html = get_content_raw_html();
            if (string.IsNullOrWhiteSpace(raw_html))
                return null;
            // Fixup the base tag
            var doc = new HtmlDocument();
            doc.LoadHtml(raw_html);
            HtmlNode html_node = null;
            HtmlNode head_node = null;
            HtmlNode base_node = null;
            HtmlNode body_node = null;
            foreach (var node in doc.DocumentNode.ChildNodes)
            {
                string node_name = node.Name.ToLowerInvariant();
                if (node_name == "html")
                    html_node = node;
                if (node_name == "head")
                    head_node = node;
                if (node_name == "body")
                    body_node = node;
            }
            if (html_node != null)
            {
                foreach (var node in html_node.ChildNodes)
                {
                    string node_name = node.Name.ToLowerInvariant();
                    if (node_name == "head")
                        head_node = node;
                    if (node_name == "body")
                        body_node = node;
                }
            }
            if (head_node != null)
            {
                foreach (var node in head_node.ChildNodes)
                {
                    if (node.Name.ToLowerInvariant() == "base")
                        base_node = node;
                }
            }
            if (body_node == null)
            {
                body_node = doc.CreateElement("body");
                body_node.AppendChildren(doc.DocumentNode.ChildNodes);
                html_node = null;
            }
            if (head_node == null)
            {
                head_node = doc.CreateElement("head");
                html_node = null;
            }
            if (base_node == null)
            {
                base_node = doc.CreateElement("base");
                head_node.AppendChild(base_node);
            }
            base_node.SetAttributeValue("href", this.feed_uri);
            if (html_node == null)
            {
                html_node = doc.CreateElement("html");
                html_node.AppendChild(head_node);
                html_node.AppendChild(body_node);
            }
            return html_node.OuterHtml;
        }
    }
}
