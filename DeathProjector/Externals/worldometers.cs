using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace DeathProjector.Externals
{
    public class worldometers
    {
        private static Dictionary<string, List<WorldometerModel>> cache = new Dictionary<string, List<WorldometerModel>>();
        private static Dictionary<string, DateTime> cacheDate = new Dictionary<string, DateTime>();

        public static List<WorldometerModel> GetWorldometerData(string Nation)
        {
            var lReturn = new List<WorldometerModel>();

            if (Nation == "WI") return GetWisconsin();

            if (!cache.ContainsKey(Nation) ||
                !cacheDate.ContainsKey(Nation) ||
                DateTime.Now > cacheDate[Nation].AddHours(1))
            {
                string Content;
                using (WebClient client = new WebClient())
                {
                    Content = client.DownloadString("https://www.worldometers.info/coronavirus/country/" + Nation + "/");
                }
                var DailyDeathsStart = Content.IndexOf("text: 'Daily Deaths'");
                var DatesBlockStart = Content.IndexOf("xAxis:", DailyDeathsStart);
                var DatesStart = Content.IndexOf("[", DatesBlockStart) + 1;
                var DatesEnd = Content.IndexOf("]", DatesStart);
                var Dates = StringToDateTimes(Content.Substring(DatesStart, DatesEnd - DatesStart));

                var SeriesStart = Content.IndexOf("series:", DatesEnd);
                var DataBlockStart = Content.IndexOf("data", SeriesStart);
                var DataStart = Content.IndexOf("[", DataBlockStart) + 1;
                var DataEnd = Content.IndexOf("]", DataStart);
                var Values = StringToInts(Content.Substring(DataStart, DataEnd - DataStart));

                if (Dates.Count != Values.Count) throw new DataMisalignedException("Dates (" + Dates.Count + ") and Values (" + Values.Count + ") missmatch!");
                for (int i = 0; i < Dates.Count; i++)
                {
                    lReturn.Add(new WorldometerModel() { Date = Dates[i], Deaths = Values[i] });
                }
                cache[Nation] = lReturn;
                cacheDate[Nation] = DateTime.Now;
            }
            else
            {
                lReturn = cache[Nation];
            }
            return lReturn;
        }

        /// <summary>
        /// Converts from a coma delimited list of enclosed string date parts to a List of DateTimes
        /// </summary>
        /// <param name="input">A coma separated list of enclosed string date parts ("Feb 1","Feb 2",...)</param>
        /// <returns>A list of DateTimes</returns>
        private static List<DateTime> StringToDateTimes(string input)
        {
            var lReturn = new List<DateTime>();

            var sdates = input.Split(",");
            foreach (var sdate in sdates)
            {
                var s = sdate.Trim().TrimStart('\"').TrimEnd('\"') + ", 2020";
                lReturn.Add(DateTime.Parse(s));
            }

            return lReturn;
        }

        /// <summary>
        /// Takes a coma delimited list of integers and returns a list of ints
        /// </summary>
        /// <param name="input">A coma separated list of ints (1,2,3,4,5,...)</param>
        /// <returns>A list of ints</returns>
        private static List<int> StringToInts(string input)
        {
            var lReturn = new List<int>();

            var sints = input.Split(",");
            foreach (var sint in sints)
            {
                lReturn.Add(int.Parse(sint.Trim()));
            }

            return lReturn;
        }

        private static List<WorldometerModel> GetWisconsin()
        {
            var lReturn = new List<WorldometerModel>();

            lReturn.Add(new WorldometerModel() { Date = new DateTime(2020, 3, 19, 0, 0, 0, 0), Deaths = 1 });
            lReturn.Add(new WorldometerModel() { Date = new DateTime(2020, 3, 20, 0, 0, 0, 0), Deaths = 1 });
            lReturn.Add(new WorldometerModel() { Date = new DateTime(2020, 3, 21, 0, 0, 0, 0), Deaths = 0 });
            lReturn.Add(new WorldometerModel() { Date = new DateTime(2020, 3, 22, 0, 0, 0, 0), Deaths = 1 });
            lReturn.Add(new WorldometerModel() { Date = new DateTime(2020, 3, 23, 0, 0, 0, 0), Deaths = 1 });
            lReturn.Add(new WorldometerModel() { Date = new DateTime(2020, 3, 24, 0, 0, 0, 0), Deaths = 1 });
            lReturn.Add(new WorldometerModel() { Date = new DateTime(2020, 3, 25, 0, 0, 0, 0), Deaths = 2 });
            lReturn.Add(new WorldometerModel() { Date = new DateTime(2020, 3, 26, 0, 0, 0, 0), Deaths = 2 });
            lReturn.Add(new WorldometerModel() { Date = new DateTime(2020, 3, 27, 0, 0, 0, 0), Deaths = 7 });
            lReturn.Add(new WorldometerModel() { Date = new DateTime(2020, 3, 28, 0, 0, 0, 0), Deaths = 1 });
            lReturn.Add(new WorldometerModel() { Date = new DateTime(2020, 3, 29, 0, 0, 0, 0), Deaths = 1 });

            return lReturn;
        }
    }

    public class WorldometerModel
    {
        public DateTime Date { get; set; }
        public int Deaths { get; set; }
    }
}
