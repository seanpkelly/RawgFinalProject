using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RawgFinalProject.Models
{
    public class RecommendedGame
    {
        public RecommendedGame(int id, double recommendationScore)
        {
            this.id = id;
            this.recommendationScore = recommendationScore;
        }

        public int id { get; set; }
        public double recommendationScore { get; set; }
    }
}
