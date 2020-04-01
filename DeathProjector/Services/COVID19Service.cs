using DeathProjector.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DeathProjector.Services
{
    public class COVID19Service
    {
        public static List<OutputModel> GenerateModel(context Context, string Nation, int DaysTillDeath, double DaysTillDouble, 
                                         double CaseFatalityRate, int MaxInfection, DateTime TargetDate, int DeathsOnTargetDate, 
                                         DateTime SocialDistancingDate, double SocialDistancingDoublingRate,
                                         DateTime ShelterInPlaceDate , double ShelterInPlaceDoublingRate,
                                         DateTime LockdownDate, double LockdownDoublingRate,
                                         int ProjectionLength)
        {
            var lReturn = new List<OutputModel>();

            // Get history from Worldometer
            var History = DeathProjector.Externals.worldometers.GetWorldometerData(Context, Nation);

            // Set initial data
            var doubleRate = (Math.Exp(Math.Log(2) / DaysTillDouble) - 1);
            var CurrentDate = TargetDate.AddDays(-1 * DaysTillDeath);
            var CurrentInfected = 0;
            var NewInfections = Math.Round(DeathsOnTargetDate / (CaseFatalityRate / 100), 0);

            // Set the first day
            var id = new OutputModel();
            id.Date = CurrentDate;
            id.ProjectedDeaths = 0;
            id.ProjectedInfections = (int)NewInfections;
            CurrentInfected = (int)Math.Round(id.ProjectedInfections / (doubleRate), 0);
            id.TotalProjectedInfections = (int)Math.Round(id.ProjectedInfections / (doubleRate), 0);
            lReturn.Add(id);

            // Prep for looping!
            CurrentInfected += (int)NewInfections;
            CurrentDate = CurrentDate.AddDays(1);

            while (CurrentDate < TargetDate.AddDays(ProjectionLength + 1))
            {
                // update the doubleRate appropriately
                if (CurrentDate >= SocialDistancingDate) doubleRate = (Math.Exp(Math.Log(2) / SocialDistancingDoublingRate) - 1);
                if (CurrentDate >= ShelterInPlaceDate) doubleRate = (Math.Exp(Math.Log(2) / ShelterInPlaceDoublingRate) - 1);
                if (CurrentDate >= LockdownDate) doubleRate = (Math.Exp(Math.Log(2) / LockdownDoublingRate) - 1);

                id = new OutputModel();
                id.Date = CurrentDate.Date;
                
                // Get projected deaths based off of how many new people got sick DaysTillDeath days ago
                id.ProjectedDeaths = 0;
                var SicknessDate = (from p in lReturn where p.Date == CurrentDate.AddDays(-1 * DaysTillDeath) select p).FirstOrDefault();
                if (SicknessDate != null) id.ProjectedDeaths = (int)( SicknessDate.ProjectedInfections * (CaseFatalityRate / 100));
                
                id.TotalProjectedInfections = (int)Math.Round(CurrentInfected * (1 + doubleRate), 0);
                id.ProjectedInfections = id.TotalProjectedInfections - CurrentInfected;

                NewInfections = id.ProjectedInfections;
                CurrentInfected = id.TotalProjectedInfections;

                // Correct for max infection
                if (CurrentInfected > MaxInfection)
                {
                    CurrentInfected = MaxInfection;
                    NewInfections = 0;
                    id.TotalProjectedInfections = MaxInfection;
                    id.ProjectedInfections = 0;
                }

                // Get Reported Deaths
                var q = (from p in History where p.Date.Date.ToShortDateString() == CurrentDate.ToShortDateString() select p).FirstOrDefault();
                if (q!= null)
                {
                    id.ReportedDeaths = q.Deaths;
                }

                lReturn.Add(id);
                CurrentDate = CurrentDate.AddDays(1);
            }

            return AggregateTotals(lReturn);
        }

        private static List<OutputModel> AggregateTotals(List<OutputModel> input)
        {
            var lReturn =input;

            int TotalProjectedDeaths=0;
            int TotalProjectedInfections=0;
            int TotalReportedDeaths=0;
            foreach (var om in input)
            {
                TotalProjectedDeaths += om.ProjectedDeaths;
                TotalProjectedInfections += om.ProjectedInfections;
                TotalReportedDeaths += om.ReportedDeaths;

                om.TotalProjectedDeaths = TotalProjectedDeaths;
                om.TotalProjectedInfections = TotalProjectedInfections;
                om.TotalReportedDeaths = TotalReportedDeaths;
            }

            return lReturn;
        }
    }

    public class OutputModel
    {
        public DateTime Date { get; set; }
        public int ProjectedInfections { get; set; }
        public int TotalProjectedInfections { get; set; }
        public int ProjectedDeaths { get; set; }
        public int TotalProjectedDeaths { get; set; }
        public int ReportedDeaths { get; set; }
        public int TotalReportedDeaths { get; set; }
    }
}
