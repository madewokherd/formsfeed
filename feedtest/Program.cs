using System;
using System.Threading;
using Argotic.Common;
using Argotic.Syndication;
using FormsFeed.Cache;

namespace feedtest
{
    class Program
    {
        static void Main(string[] args)
        {
            Cache cache = new Cache(".");

            using (cache)
            {
                GenericSyndicationFeed feed = GenericSyndicationFeed.Create(new Uri(args[0]));

                Console.WriteLine(feed.Format);
                Console.WriteLine(feed.Title);
                Console.WriteLine(feed.Description);
                Console.WriteLine(feed.Language);
                Console.WriteLine(feed.LastUpdatedOn);

                foreach (var item in feed.Items)
                {
                    Console.WriteLine(item.Title);
                }

                System.Threading.Thread.Sleep(20000);
            }
        }
    }
}
