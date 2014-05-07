using EventStore.Common.Utils;

namespace EventStore.Projections.Core.Utils
{
    public static class EncodingExtensions
    {
        public static string FromUtf8(this byte[] self)
        {
            return self != null ? Helper.UTF8NoBom.GetString(self): null;
        }

        public static byte[] ToUtf8(this string self)
        {
            return self != null ? Helper.UTF8NoBom.GetBytes(self) : null;
        }

        public static string Apply(this string format, params object[] args)
        {
            return string.Format(format, args);
        }
    }
}
