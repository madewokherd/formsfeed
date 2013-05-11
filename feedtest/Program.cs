using System;
using System.Threading;
using Argotic.Common;
using Argotic.Syndication;
using FormsFeed.Cache;

namespace feedtest
{
    class Program
    {
        static void Usage()
        {
            Console.Write(@"usage: feedtest <command> [<args>]

feedtest update [-f] url
    Refresh items from the specified url.
    -f to refresh even if the next update timestamp is in the future.

feedtest list-items [-r] url
    List the ids of all locally known items from the specified feed url.
    -r to refresh the url first.

feedtest show-item url [id]
    Show detailed informatino for the specified item id.
");
        }

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Usage();
                return;
            }

            Cache cache = new Cache(".");

            string command = args[0].ToLowerInvariant();

            if (command == "update")
            {
                bool force = false;
                int i;
                for (i = 1; i < args.Length; i++)
                {
                    FeedBasicInfo info;
                    if (args[i] == "-f")
                    {
                        force = true;
                        continue;
                    }
                    DateTime last_update_timestamp = DateTime.MinValue;
                    if (cache.TryGetFeedBasicInfo(args[i], out info))
                    {
                        last_update_timestamp = info.lastchecked;
                    }
                    if (cache.Update(args[i], force))
                    {
                        if (DateTime.Compare(info.expiration, DateTime.UtcNow) <= 0)
                            Console.WriteLine("Updated.", info.expiration.ToLocalTime());
                        else
                            Console.WriteLine("Updated. Will not check again until {0}", info.expiration.ToLocalTime());
                    }
                    else
                    {
                        if (DateTime.Compare(last_update_timestamp, info.timestamp) != 0)
                            Console.WriteLine("Updated.");
                        else
                            Console.WriteLine("Skipped check because it is not yet {0}", info.expiration.ToLocalTime());
                    }
                    return;
                }
                // No uri specified.
                Usage();
            }
            else if (command == "list-items")
            {
                bool refresh = false;
                int i;
                for (i = 1; i < args.Length; i++)
                {
                    if (args[i] == "-r")
                    {
                        refresh = true;
                        continue;
                    }
                    if (refresh)
                        cache.Update(args[i]);
                    foreach (var item in cache.GetFeedItems(args[i]))
                    {
                        Console.WriteLine("{0} {1}", item.id, item.title);
                    }
                    return;
                }
                // No uri specified.
                Usage();
            }
            else if (command == "show-item")
            {
                string uri, id;

                if (args.Length == 1)
                {
                    Usage();
                    return;
                }

                DetailedInfo info;

                uri = args[1];
                if (args.Length >= 3)
                    id = args[2];
                else
                    id = "";

                if (cache.TryGetDetailedInfo(uri, id, out info))
                {
                    Console.WriteLine("Title: {0}", info.title);
                    Console.WriteLine("Author: {0}", info.author);
                    Console.WriteLine("Date: {0}", info.timestamp);
                    foreach (var item in info.contents)
                        Console.WriteLine("{0} {1}", item.Item1, item.Item2);
                }
                else
                {
                    Console.WriteLine("No such item");
                }
            }
            else
                Usage();
        }
    }
}
