using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RawgFinalProject.Models
{
    public class Game
    {
        public int count { get; set; }
        public string next { get; set; }
        public string previous { get; set; }
        public Result[] results { get; set; }
        public string seo_title { get; set; }
        public string seo_description { get; set; }
        public string seo_keywords { get; set; }
        public string seo_h1 { get; set; }
        public bool noindex { get; set; }
        public bool nofollow { get; set; }
        public string description { get; set; }
        public Filters filters { get; set; }
        public string[] nofollow_collections { get; set; }
    }

    public class Filters
    {
        public Year[] years { get; set; }
    }

    public class Year
    {
        public int from { get; set; }
        public int to { get; set; }
        public string filter { get; set; }
        public int decade { get; set; }
        public Year1[] years { get; set; }
        public bool nofollow { get; set; }
        public int count { get; set; }
    }

    public class Year1
    {
        public int year { get; set; }
        public int count { get; set; }
        public bool nofollow { get; set; }
    }

    public class Result
    {
        public int id { get; set; }
        public string slug { get; set; }
        public string name { get; set; }
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
        public int? metacritic { get; set; }
        public int playtime { get; set; }
        public int suggestions_count { get; set; }
        public object user_game { get; set; }
        public int reviews_count { get; set; }
        public string saturated_color { get; set; }
        public string dominant_color { get; set; }
        public Platform[] platforms { get; set; }
        public Parent_Platforms[] parent_platforms { get; set; }
        public Genre[] genres { get; set; }
        public Store[] stores { get; set; }
        public Clip clip { get; set; }
        public Tag[] tags { get; set; }
        public Short_Screenshots[] short_screenshots { get; set; }
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

    public class Clips
    {
        public string _320 { get; set; }
        public string _640 { get; set; }
        public string full { get; set; }
    }

    public class Rating
    {
        public int id { get; set; }
        public string title { get; set; }
        public int count { get; set; }
        public float percent { get; set; }
    }

    public class Platform
    {
        public Platform1 platform { get; set; }
        public string released_at { get; set; }
        public Requirements_En requirements_en { get; set; }
        public Requirements_Ru requirements_ru { get; set; }
    }

    public class Platform1
    {
        public int id { get; set; }
        public string name { get; set; }
        public string slug { get; set; }
        public object image { get; set; }
        public object year_end { get; set; }
        public int? year_start { get; set; }
        public int games_count { get; set; }
        public string image_background { get; set; }
    }

    public class Requirements_En
    {
        public string minimum { get; set; }
        public string recommended { get; set; }
    }

    public class Requirements_Ru
    {
        public string minimum { get; set; }
        public string recommended { get; set; }
    }

    public class Parent_Platforms
    {
        public Platform2 platform { get; set; }
    }

    public class Platform2
    {
        public int id { get; set; }
        public string name { get; set; }
        public string slug { get; set; }
    }

    public class Genre
    {
        public int id { get; set; }
        public string name { get; set; }
        public string slug { get; set; }
        public int games_count { get; set; }
        public string image_background { get; set; }
    }

    public class Store
    {
        public int id { get; set; }
        public Store1 store { get; set; }
        public string url_en { get; set; }
        public string url_ru { get; set; }
    }

    public class Store1
    {
        public int id { get; set; }
        public string name { get; set; }
        public string slug { get; set; }
        public string domain { get; set; }
        public int games_count { get; set; }
        public string image_background { get; set; }
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
}
