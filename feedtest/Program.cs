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
                Console.WriteLine(cache.Update(args[0]));
            }
        }
    }
}
