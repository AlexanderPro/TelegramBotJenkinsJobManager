using System;

namespace TelegramBotJenkinsJobManager.Extensions
{
    public static class DateTimeExtensions
    {
        public static DateTime FromUnixTimeMilliseconds(this long unixTime)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var timeSpan = TimeSpan.FromMilliseconds(unixTime);
            return epoch.Add(timeSpan).ToLocalTime();
        }
    }
}
