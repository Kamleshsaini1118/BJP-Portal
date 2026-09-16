using System;

namespace StockPortalApp.Helpers
{
    /// <summary>
    /// Gives the current date/time in India Standard Time (IST, UTC+5:30),
    /// regardless of how the machine running the app is configured. Use this
    /// instead of IndiaTime.Today / DateTime.Now anywhere a "today" default
    /// or a logged timestamp needs to reflect India time specifically.
    /// </summary>
    public static class IndiaTime
    {
        private static readonly TimeZoneInfo Ist = ResolveIst();

        private static TimeZoneInfo ResolveIst()
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"); } // Windows ID
            catch (TimeZoneNotFoundException)
            {
                try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata"); } // IANA ID (Linux/macOS)
                catch
                {
                    // Fallback: build it manually. IST has no daylight saving, always UTC+5:30.
                    return TimeZoneInfo.CreateCustomTimeZone("India Standard Time", new TimeSpan(5, 30, 0), "India Standard Time", "IST");
                }
            }
        }

        public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Ist);

        public static DateTime Today => Now.Date;
    }
}