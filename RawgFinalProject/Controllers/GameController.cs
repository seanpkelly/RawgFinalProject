using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Security.Claims;
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

        public async Task<IActionResult> GenerateWeights()
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

            int maxCount = genreCountDictionary.Values.Max();
            //int threePointCount = 0;
            //int twoPointCount = 0;
            //int onePointCount = 0;

            //foreach (string key in genreCountDictionary.Keys.ToList())
            //{
            //    if (genreCountDictionary[key] < maxCount && genreCountDictionary[key] > threePointCount)
            //    {
            //        threePointCount = genreCountDictionary[key];
            //    }
            //}






            Dictionary<string, int> orderedGenreCount = new Dictionary<string, int>();

            foreach (var item in genreCountDictionary.OrderByDescending(i => i.Value))
            {
                orderedGenreCount.Add(item.Key, item.Value);
            }


            //We need to get a list of Genres/Tags for each weight level (5, 3, 2, 1, 0) so that we can apply the weighted score to the full database to get recommendations

            List<string> maxWeightedGenre = new List<string>();

            

            foreach (string key in genreCountDictionary.Keys.ToList())
            {
                if (genreCountDictionary[key] == maxCount)
                {
                    maxWeightedGenre.Add(key);
                }
            }

            return View(orderedGenreCount);
        }
    }
}
