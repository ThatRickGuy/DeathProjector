using DeathProjector.Models;
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

        public static List<WorldometerModel> GetWorldometerData(context Context, string Nation)
        {
            var lReturn = new List<WorldometerModel>();

            if (Nation == "WI") return GetWisconsin();

            // Check memory cache first
            if (cache.ContainsKey(Nation) &&
                cacheDate.ContainsKey(Nation) &&
                DateTime.Now < cacheDate[Nation].AddHours(1))
            {
                lReturn = cache[Nation];
            }
            else if (cacheDate.ContainsKey(Nation) &&  
                     DateTime.Now < cacheDate[Nation].AddHours(1))
            {
                // if we're still in the cache window, check database next
                lReturn = (from p in Context.RegionDateDeaths where p.Region == Nation select new WorldometerModel() { Date = p.Date, Deaths = p.Deaths }).ToList();
                cache[Nation] = lReturn;
            }

            if (lReturn.Count == 0  && (Nation == "US" || Nation =="Italy"))
            {
                // nothing in memory or database, pull fresh from the server for USA or Italy
                // States are wonky because they are stored on the USA page, so they're handled below
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
                cacheDate[Nation] = DateTime.Now;

                // as long as we're pulling the US, cache state date
                if (Nation == "US") ParseAndStoreStateData(Context, Content);

            } else if (lReturn.Count == 0 && !(Nation == "US" || Nation == "Italy"))
            {
                // Nothing stored, not USA or Italy, must be a state
                string Content;
                using (WebClient client = new WebClient())
                {
                    Content = client.DownloadString("https://www.worldometers.info/coronavirus/country/" + Nation + "/");
                }
                ParseAndStoreStateData(Context, Content);
            }

                cache[Nation] = lReturn;
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

        private static void ParseAndStoreStateData(context Context, string Content)
        {
            var StateTablesStart = Content.IndexOf("usa_table_countries_today");
            var BodyStart = Content.IndexOf("<tbody>", StateTablesStart);
            var BodyEnd = Content.IndexOf("</tbody>", BodyStart);

            var searchIndex = BodyStart;
            while (searchIndex < BodyEnd)
            {
                var rdd = new Models.RegionDateDeath();
                //start of row
                searchIndex = Content.IndexOf("<tr ", searchIndex);
                //start of cell
                searchIndex = Content.IndexOf("<td ", searchIndex);
                //State Name
                var StartOfValue = Content.IndexOf(">", searchIndex)+1;
                var EndOfValue = Content.IndexOf("<", StartOfValue);
                rdd.Date = DateTime.Now.Date;
                rdd.Region = Content.Substring(StartOfValue, EndOfValue - StartOfValue).Trim();

                //Total Cases
                StartOfValue = Content.IndexOf("<td", EndOfValue);
                //New Cases
                StartOfValue = Content.IndexOf("<td", StartOfValue);
                //Total Deaths
                StartOfValue = Content.IndexOf("<td", StartOfValue);
                StartOfValue = Content.IndexOf(">", StartOfValue)+1;
                EndOfValue = Content.IndexOf("<", StartOfValue)+1;
                var previouslyReportedDeaths = (from p in Context.RegionDateDeaths where p.Region == rdd.Region select p.Deaths).Sum();
                var currentlyReportedDeaths = int.Parse(Content.Substring(StartOfValue, EndOfValue - StartOfValue-1).Trim().Replace(",",string.Empty));
                rdd.Deaths = currentlyReportedDeaths -= previouslyReportedDeaths;
                Context.RegionDateDeaths.Add(rdd);

                searchIndex = EndOfValue;
            }
            Context.SaveChanges();

            var DistinctRegions = (from p in Context.RegionDateDeaths orderby p.Region select p.Region).Distinct().ToList();
            foreach (var Region in DistinctRegions)
            {
                var q = (from p in Context.RegionDateDeaths where p.Region == Region orderby p.Date select new WorldometerModel() {Date = p.Date, Deaths = p.Deaths }).ToList();
                if (cache.ContainsKey(Region))
                {
                    cache[Region] = q;
                    cacheDate[Region] = DateTime.Now;
                } else
                {
                    cache.Add(Region, q);
                    cacheDate.Add(Region, DateTime.Now);
                }
            }

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
            lReturn.Add(new WorldometerModel() { Date = new DateTime(2020, 3, 26, 0, 0, 0, 0), Deaths = 3 }); //10
            lReturn.Add(new WorldometerModel() { Date = new DateTime(2020, 3, 27, 0, 0, 0, 0), Deaths = 4 }); //14
            lReturn.Add(new WorldometerModel() { Date = new DateTime(2020, 3, 28, 0, 0, 0, 0), Deaths = 1 }); //15
            lReturn.Add(new WorldometerModel() { Date = new DateTime(2020, 3, 29, 0, 0, 0, 0), Deaths = 2 }); //17
            lReturn.Add(new WorldometerModel() { Date = new DateTime(2020, 3, 30, 0, 0, 0, 0), Deaths = 0 }); //17
            lReturn.Add(new WorldometerModel() { Date = new DateTime(2020, 3, 31, 0, 0, 0, 0), Deaths = 8 }); //25
            lReturn.Add(new WorldometerModel() { Date = new DateTime(2020, 4, 1, 0, 0, 0, 0), Deaths = 0 }); //25
            lReturn.Add(new WorldometerModel() { Date = new DateTime(2020, 4, 2, 0, 0, 0, 0), Deaths = 6 }); //31
            lReturn.Add(new WorldometerModel() { Date = new DateTime(2020, 4, 3, 0, 0, 0, 0), Deaths = 15 }); //46
            lReturn.Add(new WorldometerModel() { Date = new DateTime(2020, 4, 4, 0, 0, 0, 0), Deaths = 10 }); //56
            lReturn.Add(new WorldometerModel() { Date = new DateTime(2020, 4, 5, 0, 0, 0, 0), Deaths = 12 }); //68
            lReturn.Add(new WorldometerModel() { Date = new DateTime(2020, 4, 6, 0, 0, 0, 0), Deaths = 9 });

            return lReturn;
        }
    }

    public class WorldometerModel
    {
        public DateTime Date { get; set; }
        public int Deaths { get; set; }
    }
}
