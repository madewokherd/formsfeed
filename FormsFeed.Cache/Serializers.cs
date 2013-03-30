using System;
using CSharpTest.Net.Serialization;

namespace FormsFeed.Cache
{
    internal class Serializers : ISerializer<FeedBasicInfo>
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
            result.unread = (ps as ISerializer<bool>).ReadFrom(stream);
            result.autofetch = (ps as ISerializer<bool>).ReadFrom(stream);
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
            (ps as ISerializer<bool>).WriteTo(value.unread, stream);
            (ps as ISerializer<bool>).WriteTo(value.autofetch, stream);
        }
    }
}
