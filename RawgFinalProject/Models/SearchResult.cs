using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RawgFinalProject.Models
{

    public class SearchResult
    {
        public int count { get; set; }
        public string next { get; set; }
        public object previous { get; set; }
        public Result[] results { get; set; }
        public bool user_platforms { get; set; }
    }

    public class Result
    {
        public string slug { get; set; }
        public int id { get; set; }
        public string name { get; set; }
        public object metacritic { get; set; }
        public string score { get; set; }
        public string background_image { get; set; }
    }
}
