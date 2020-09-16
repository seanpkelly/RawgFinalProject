using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RawgFinalProject.Models;

namespace RawgFinalProject.Controllers
{
    public class GameController : Controller
    {
        private readonly GameDAL _gameDAL;
        private readonly string _apiKey;
        private readonly GameRecommendationDbContext _gameContext;

        public GameController(IConfiguration configuration)
        {
            _apiKey = configuration.GetSection("ApiKeys")["GameAPIKey"];
            _gameDAL = new GameDAL(_apiKey);
            _gameContext = new GameRecommendationDbContext(configuration.GetConnectionString("AzureDbConnection"));
        }

        public async Task<IActionResult> Index()
        {
            Game games = await _gameDAL.GetGamesList();

            return View(games);

        }

        [HttpPost]
        public async Task<IActionResult> SearchGameByName(string searchName)
        {
            List<Result> searchResult = await _gameDAL.GetGameSearch(searchName);

            return View("SearchResults", searchResult);

        }

        public async Task<IActionResult> DisplayFavorites()
        {
            string activeUserId = User.FindFirst(ClaimTypes.NameIdentifier).Value;

            //Creates list of favorites for the current user
            var favList = await _gameContext.UserFavorite.Where(x => x.UserId == activeUserId).ToListAsync();

            List<Game> convertList = new List<Game>();

            for (int i = 0; i < favList.Count; i++)
            {
               convertList.Add(await SearchGameById(favList[i].GameId));
            }

            return View(convertList);
        }

        public async Task<IActionResult> GameDetails(int id)
        {
           Game searchedGame = await SearchGameById(id);
            AddToHistory(searchedGame);
            return View(searchedGame);
        }
       
        public void AddToHistory(Game addToHistory)
        {
            string activeUserId = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            UserHistory h = new UserHistory();

            h.GameId = addToHistory.id;
            h.UserId = activeUserId;

            if (ModelState.IsValid)
            {
                _gameContext.UserHistory.Add(h);
                _gameContext.SaveChanges();
            }
        }

        public async Task<IActionResult> AddToFavorites(int id)
        {
            string activeUserId = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            UserFavorite f = new UserFavorite();

            UserFavorite duplicateTest = _gameContext.UserFavorite.Where(f => f.UserId == activeUserId && f.Id == id).FirstOrDefault();

            if (duplicateTest == null)
            {
                f.GameId = id;
                f.UserId = activeUserId;

                if (ModelState.IsValid)
                {
                    _gameContext.UserFavorite.Add(f);
                    _gameContext.SaveChanges();
                }

                return RedirectToAction("DisplayFavorites");
            }
            else
            {
                ViewBag.Error = "This game is already a favorite!";
                return RedirectToAction("SearchResults");
            }

        }

        public async Task<IActionResult> DeleteFavorite(int id)
        {
            string activeUserId = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            var gameToDelete = _gameContext.UserFavorite.Find(id);
            UserFavorite deleteItem = _gameContext.UserFavorite.Where(uf => uf.UserId == activeUserId && uf.GameId == id).FirstOrDefault();

            if (deleteItem != null)
            {
                _gameContext.UserFavorite.Remove(deleteItem);
                _gameContext.SaveChanges();
            }

            return RedirectToAction("DisplayFavorites");
        }

        public async Task<Game> SearchGameById(int id)
        {
            var searchId = await _gameDAL.GetGameByName(id.ToString());

            return searchId;
        }

        public async Task<List<Dictionary<string,double>>> GenerateWeights()
        {
            string[] genres = { "Action", "Indie", "Adventure", "RPG", "Strategy", 
                "Shooter", "Casual", "Simulation", "Puzzle", "Arcade", "Platformer", "Racing", "Sports", 
                "Massively Multiplayer", "Family", "Fighting", "Board Game", "Educational", "Card" };

            string[] tags = { "Singleplayer", "Multiplayer", "Atmospheric", "Great Soundtrack", "RPG", "Co-op", "Story Rich", "Open World", "cooperative", "First-Person", "Sci-fi", 
                "2D", "Third Person", "FPS", "Horror", "Fantasy", "Comedy", "Sandbox", "Survival", "Exploration", "Stealth", "Tactical", "Pixel Graphics", "Action RPG", "Retro",
                "Space", "Zombies", "Point & Click", "Action-Adventure", "Hack and Slash", "Side Scroller", "Survival Horror", "RTS", "Roguelike", "mmo", "Driving", "Puzzle",
                "MMORPG", "Management", "JRPG" };

            string activeUserId = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            //Creates list of favorites for the current user
            var favList = await _gameContext.UserFavorite.Where(x => x.UserId == activeUserId).ToListAsync();

            List<Game> convertList = new List<Game>();

            for (int i = 0; i < favList.Count; i++)
            {
                convertList.Add(await SearchGameById(favList[i].GameId));
            }



            Dictionary<string, int> genreCountDictionary = new Dictionary<string, int>();  //creates a prepopulates a dictionary to log how many occurences of each genre appear in users favorites
            foreach (var g in genres)
            {
                genreCountDictionary.Add(g, 0);
            }

            Dictionary<string, int> tagCountDictionary = new Dictionary<string, int>(); //creates a prepopulates a dictionary to log how many occurences of each tag appear in users favorites
            foreach (var t in tags)
            {
                tagCountDictionary.Add(t, 0);
            }



            foreach (Game game in convertList)  //populates tag and genre dictionaries with the number of occurences of each tag/genre for the game
            {
                foreach (string key in genreCountDictionary.Keys.ToList())
                {
                    for (int i = 0; i < game.genres.Length; i++)
                    {
                        if (key == game.genres[i].name)
                        {
                            genreCountDictionary[key] += 1;
                        }
                    }
                }
                foreach (string key in tagCountDictionary.Keys.ToList())
                {
                    for (int i = 0; i < game.tags.Length; i++)
                    {
                        if (key == game.tags[i].name)
                        {
                            tagCountDictionary[key] += 1;
                        }
                    }
                }

            }


            int totalGenres = 0;

            Dictionary<string, int> orderedGenreCount = new Dictionary<string, int>();
            foreach (var item in genreCountDictionary.OrderByDescending(i => i.Value))
            {
                totalGenres += item.Value;
                orderedGenreCount.Add(item.Key, item.Value);
            }

            Dictionary<string, double> weightedGenres = new Dictionary<string, double>();

            foreach (var g in orderedGenreCount)
            {
                double value = Math.Round(((double)g.Value / (double)totalGenres),2);
                weightedGenres.Add(g.Key, value);
            }

            //do the stuff for tags
            int totalTags = 0;

            Dictionary<string, int> orderedTagCount = new Dictionary<string, int>();
            foreach (var item in tagCountDictionary.OrderByDescending(i => i.Value))
            {
                totalTags += item.Value;
                orderedTagCount.Add(item.Key, item.Value);
            }

            Dictionary<string, double> weightedTags = new Dictionary<string, double>();

            foreach (var g in orderedTagCount)
            {
                double value = Math.Round(((double)g.Value / (double)totalTags), 2);
                weightedTags.Add(g.Key, value);
            }

            //send the weighted genre and tag dictionaries to the recommendations method

            List<Dictionary<string, double>> genreAndTagDictionaries = new List<Dictionary<string, double>>();

            genreAndTagDictionaries.Add(weightedGenres);
            genreAndTagDictionaries.Add(weightedTags);

            return genreAndTagDictionaries;
        }

        public async Task<IActionResult> GenerateRecommendations()//List<Dictionary<string,double>> weights)
        {
            List<Dictionary<string, double>> weights = await GenerateWeights();  //obtains weighted genre and tag values to apply to our game list

            //genres = action,rpg,adventure,sports,indie,simulation & tags = singleplayer
            string genreQuery = "";
            foreach (string key in weights[0].Keys.ToList())
            {
                if (weights[0][key] != 0)
                {
                    genreQuery += key.ToLower() + ",";
                }
            }

            string tagQuery = "";
            foreach (string key in weights[1].Keys.ToList())
            {
                if (weights[1][key] != 0)
                {
                    tagQuery += key.ToLower() + ",";
                }
            }

            //build api endpoint string and call List<Results> = GetGameListByGenreAndTag in DAL (foreach loops)
            List<Result> recommendationResultPool = await _gameDAL.GetGameListByGenreAndTag($"genres={genreQuery}&tags={tagQuery}");

            List<Game> recommendationGamePool = new List<Game>();

            for (int i = 0; i < recommendationResultPool.Count; i++)
            {
                recommendationGamePool.Add(await SearchGameById(recommendationResultPool[i].id));
            }

            double genreRecScore = 0; 
            double tagRecScore = 0;
            double totalRecScore = 0;
            List<RecommendedGame> gameRecs = new List<RecommendedGame>();

            //foreach loop the list of results and create the recommendation score (foreach through the recommended games and apply weighted scores as necessary)
            foreach (Game game in recommendationGamePool)
            {
                genreRecScore = 0;
                tagRecScore = 0;
                totalRecScore = 0;
                foreach (Genre genre in game.genres)
                {
                    genreRecScore += weights[0][genre.name];
                }

                foreach (Tag tag in game.tags)
                {
                    bool containsKey = weights[1].ContainsKey(tag.name.ToString());
                    if (containsKey == true)
                    {
                        tagRecScore += weights[1][tag.name];
                    }
                }

                totalRecScore = Math.Round((genreRecScore * 5) + (tagRecScore * 5),2);

                gameRecs.Add(new RecommendedGame(game.id, totalRecScore));

            }

            List<RecommendedGame> orderedRecs = new List<RecommendedGame>();

            foreach (var item in gameRecs.OrderByDescending(i => i.recommendationScore))
            {
                orderedRecs.Add(item);
            }

            List<Game> orderedGameRecs = new List<Game>();

            for (int i = 0; i < orderedRecs.Count; i++)
            {
                orderedGameRecs.Add(await SearchGameById(orderedRecs[i].id));
                orderedGameRecs[i].recommendationScore = orderedRecs[i].recommendationScore;
            }




            //return list of recommended games to the recommendations view

            //api example:
            //https://api.rawg.io/api/games?genres=action,rpg,adventure,sports,indie,simulation&tags=singleplayer

            return View("GenerateRecommendations", orderedGameRecs); //include list of recommended games as parameter
        }

    }
}
