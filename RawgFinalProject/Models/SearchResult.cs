using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
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
        public string name { get; set; }
        public int playtime { get; set; }
        //public Platform[] platforms { get; set; }
        public Store[] stores { get; set; }
        public string released { get; set; }
        public bool tba { get; set; }
        public string background_image { get; set; }
        public float rating { get; set; }
        public int rating_top { get; set; }
        public Rating[] ratings { get; set; }
        public int ratings_count { get; set; }
        public int reviews_text_count { get; set; }
        public int added { get; set; }
        public Added_By_Status added_by_status { get; set; }
        public object metacritic { get; set; }
        public int suggestions_count { get; set; }
        public int id { get; set; }
        public object score { get; set; }
        public Clip clip { get; set; }
        public Tag[] tags { get; set; }
        public object user_game { get; set; }
        public int reviews_count { get; set; }
        public string saturated_color { get; set; }
        public string dominant_color { get; set; }
        public Short_Screenshots[] short_screenshots { get; set; }
        //public Parent_Platforms[] parent_platforms { get; set; }
        public Genre[] genres { get; set; }
        public double recommendationScore { get; set; }
        public string description { get; set; }
        public object esrb { get; set; }
        public bool isfavorite { get; set; }
        public double userrating { get; set; }
        public int favoritecount { get; set; }
    }

    public class Added_By_Status
    {
        public int yet { get; set; }
        public int owned { get; set; }
        public int beaten { get; set; }
        public int toplay { get; set; }
        public int dropped { get; set; }
        public int playing { get; set; }
    }

    public class Clip
    {
        public string clip { get; set; }
        public Clips clips { get; set; }
        public string video { get; set; }
        public string preview { get; set; }
    }
    [DataContract]
    public class Clips
    {
        [DataMember(Name ="320")]
        public string _320 { get; set; }

        [DataMember(Name ="640")]
        public string _640 { get; set; }

        [DataMember(Name ="full")]
        public string full { get; set; }
    }

    //public class Platform
    //{
    //    public Platform1 platform { get; set; }
    //}

    //public class Platform1
    //{
    //    public int id { get; set; }
    //    public string name { get; set; }
    //    public string slug { get; set; }
    //}

    public class Store
    {
        public Store1 store { get; set; }
    }

    public class Store1
    {
        public int id { get; set; }
        public string name { get; set; }
        public string slug { get; set; }
    }

    public class Rating
    {
        public int id { get; set; }
        public string title { get; set; }
        public int count { get; set; }
        public float percent { get; set; }
    }

    public class Tag
    {
        public int id { get; set; }
        public string name { get; set; }
        public string slug { get; set; }
        public string language { get; set; }
        public int games_count { get; set; }
        public string image_background { get; set; }
    }

    public class Short_Screenshots
    {
        public int id { get; set; }
        public string image { get; set; }
    }

    //public class Parent_Platforms
    //{
    //    public Platform2 platform { get; set; }
    //}

    //public class Platform2
    //{
    //    public int id { get; set; }
    //    public string name { get; set; }
    //    public string slug { get; set; }
    //}

    public class Genre
    {
        public int id { get; set; }
        public string name { get; set; }
        public string slug { get; set; }
    }

}
