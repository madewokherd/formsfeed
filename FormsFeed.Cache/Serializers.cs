using System;
using System.Collections.Generic;
using CSharpTest.Net.Serialization;

namespace FormsFeed
{
    internal class Serializers :
        ISerializer<FeedBasicInfo>,
        ISerializer<DetailedInfo>,
        ISerializer<Tuple<string, string>>,
        ISerializer<List<Tuple<string, string>>>,
        IComparer<Tuple<string, string>>
    {
        private PrimitiveSerializer ps;

        internal Serializers()
        {
            ps = new PrimitiveSerializer();
        }

        FeedBasicInfo ISerializer<FeedBasicInfo>.ReadFrom(System.IO.Stream stream)
        {
            FeedBasicInfo result = new FeedBasicInfo();
            stream.ReadByte(); //version
            result.uri = (ps as ISerializer<string>).ReadFrom(stream);
            result.lastchecked = (ps as ISerializer<DateTime>).ReadFrom(stream);
            result.timestamp = (ps as ISerializer<DateTime>).ReadFrom(stream);
            result.expiration = (ps as ISerializer<DateTime>).ReadFrom(stream);
            result.etag = (ps as ISerializer<string>).ReadFrom(stream);
            result.autofetch = (ps as ISerializer<bool>).ReadFrom(stream);
            result.title = (ps as ISerializer<string>).ReadFrom(stream);
            return result;
        }

        public void WriteTo(FeedBasicInfo value, System.IO.Stream stream)
        {
            stream.WriteByte(0); //version
            (ps as ISerializer<string>).WriteTo(value.uri, stream);
            (ps as ISerializer<DateTime>).WriteTo(value.lastchecked, stream);
            (ps as ISerializer<DateTime>).WriteTo(value.timestamp, stream);
            (ps as ISerializer<DateTime>).WriteTo(value.expiration, stream);
            (ps as ISerializer<string>).WriteTo(value.etag, stream);
            (ps as ISerializer<bool>).WriteTo(value.autofetch, stream);
            (ps as ISerializer<string>).WriteTo(value.title, stream);
        }

        DetailedInfo ISerializer<DetailedInfo>.ReadFrom(System.IO.Stream stream)
        {
            DetailedInfo result = new DetailedInfo();
            stream.ReadByte(); //version
            result.title = (ps as ISerializer<string>).ReadFrom(stream);
            result.author = (ps as ISerializer<string>).ReadFrom(stream);
            result.timestamp = (ps as ISerializer<DateTime>).ReadFrom(stream);
            result.contents = (this as ISerializer<List<Tuple<string, string>>>).ReadFrom(stream);
            return result;
        }

        public void WriteTo(DetailedInfo value, System.IO.Stream stream)
        {
            stream.WriteByte(0); //version
            (ps as ISerializer<string>).WriteTo(value.title, stream);
            (ps as ISerializer<string>).WriteTo(value.author, stream);
            (ps as ISerializer<DateTime>).WriteTo(value.timestamp, stream);
            WriteTo(value.contents, stream);
        }

        Tuple<string, string> ISerializer<Tuple<string, string>>.ReadFrom(System.IO.Stream stream)
        {
            string s1, s2;
            s1 = (ps as ISerializer<string>).ReadFrom(stream);
            s2 = (ps as ISerializer<string>).ReadFrom(stream);
            return Tuple.Create(s1, s2);
        }

        public void WriteTo(Tuple<string, string> value, System.IO.Stream stream)
        {
            (ps as ISerializer<string>).WriteTo(value.Item1, stream);
            (ps as ISerializer<string>).WriteTo(value.Item2, stream);
        }

        public int Compare(Tuple<string, string> x, Tuple<string, string> y)
        {
            int c;
            if (y == null)
            {
                if (x == null)
                    return 0;
                return 1;
            }
            if (x == null)
                return -1;
            c = string.Compare(x.Item1, y.Item1, StringComparison.Ordinal);
            if (c == 0)
                c = string.Compare(x.Item2, y.Item2, StringComparison.Ordinal);
            return c;
        }

        List<Tuple<string, string>> ISerializer<List<Tuple<string, string>>>.ReadFrom(System.IO.Stream stream)
        {
            int length = (ps as ISerializer<int>).ReadFrom(stream);
            List<Tuple<string, string>> result = new List<Tuple<string, string>>();

            result.Capacity = length;

            for (int i = 0; i < length; i++)
                result.Add((this as ISerializer<Tuple<string, string>>).ReadFrom(stream));

            return result;
        }

        public void WriteTo(List<Tuple<string, string>> value, System.IO.Stream stream)
        {
            (ps as ISerializer<int>).WriteTo(value.Count, stream);

            foreach (var item in value)
                WriteTo(item, stream);
        }
    }
}
