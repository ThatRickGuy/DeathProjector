using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DeathProjector.Models
{
    public class RegionDateDeath
    {
        public int Id { get; set; }
        public string Region { get; set; }
        public DateTime Date { get; set; }
        public int Deaths { get; set; }
    }
}
