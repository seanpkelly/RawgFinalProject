using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RawgFinalProject.Models;

namespace RawgFinalProject.Controllers
{
    public class GameController : Controller
    {
        #region gameDAL Functionality
        private readonly GameDAL _gameDAL;
        private readonly string _apiKey;
        private readonly GameRecommendationDbContext _gameContext;

        public GameController(IConfiguration configuration)
        {
            _apiKey = configuration.GetSection("ApiKeys")["GameAPIKey"];
            _gameDAL = new GameDAL(_apiKey);
            _gameContext = new GameRecommendationDbContext(configuration.GetConnectionString("AzureDbConnection"));
        }
        #endregion

        public async Task<IActionResult> Index()
        {
            Game games = await _gameDAL.GetGamesList();

            return View(games);

        }

        #region Search for Games

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> SearchGameByName(string searchName)
        {
            SearchResult searchResult = await _gameDAL.GetGameSearch(searchName);
            string activeUserId = User.FindFirst(ClaimTypes.NameIdentifier).Value;

            for (int i = 0; i < searchResult.results.Length; i++)
            {
                UserFavorite checkForDupes = _gameContext.UserFavorite.Where(f => f.UserId == activeUserId && f.GameId == searchResult.results[i].id).FirstOrDefault();
                if (checkForDupes != null)
                {
                    searchResult.results[i].isfavorite = true;
                }
            }

            ViewBag.Header = $"Results for {searchName}";

            return View("SearchResults", searchResult);

        }
        [Authorize]
        public async Task<Result> SearchResultById(int id)
        {
            var searchId = await _gameDAL.GetResultByName(id.ToString());
            return searchId;
        }
        [Authorize]
        public async Task<Game> SearchGameById(int id)
        {
            var searchId = await _gameDAL.GetGameByName(id.ToString());
            return searchId;
        }
        [Authorize]
        public async Task<IActionResult> GetGameByDeveloper(string id)
        {
            string query = $"developers={id}";
            var searchId = await _gameDAL.GetGameListByGenreAndTag(query);

            ViewBag.Header = "More Games from this Developer: ";

            return View("SearchResults", searchId);
        }
        [Authorize]
        public async Task<IActionResult> GetGameByPublisher(string id)
        {
            string query = $"publishers={id}";
            var searchId = await _gameDAL.GetGameListByGenreAndTag(query);

            ViewBag.Header = "More Games from this Publisher: ";

            return View("SearchResults", searchId);
        }

        [Authorize]
        public async Task<IActionResult> SeeMoreGamesLikeThis(string id)
        {
            Game game = await _gameDAL.GetGameByName(id);

            string genreQuery = "";
            string tagQuery = "";

            foreach (var genre in game.genres)
            {
                genreQuery += genre.name + ",";
            }
            foreach (var tag in game.tags)
            {
                tagQuery += tag.name + ",";
            }

            tagQuery = tagQuery.Substring(0, tagQuery.Length - 1);

            SearchResult similarGameResults = await _gameDAL.GetGameListByGenreAndTag($"genres={genreQuery}&tags={tagQuery}");

            ViewBag.Header = $"More games like {game.name}.";

            return View("SearchResults", similarGameResults);
        }

        [Authorize]
        public async Task<IActionResult> GameDetails(int id)
        {
            Result searchedGame = await SearchResultById(id);
            Game searchedGame2 = await SearchGameById(id);
            AddToHistory(searchedGame);

            //searchedGame.esrb = (esrb)searchedGame2.esrb_rating;
            return View(searchedGame2);
        }
        #endregion

        #region Favorites CRUD
        [Authorize]
        public async Task<IActionResult> DisplayFavorites() //check performance?
        {
            //Turn into method call: CallIdString
            string activeUserId = User.FindFirst(ClaimTypes.NameIdentifier).Value;

            //Creates list of favorites for the current user
            var favList = await _gameContext.UserFavorite.Where(x => x.UserId == activeUserId).ToListAsync();

            List<Result> convertedFavoritesList = new List<Result>();

            for (int i = 0; i < favList.Count; i++)
            {
                convertedFavoritesList.Add(await SearchResultById(favList[i].GameId));
            }

            var historyList = await _gameContext.UserHistory.Where(x => x.UserId == activeUserId).ToListAsync();
            List<Result> convertedHistoryList = new List<Result>();


            for (int i = 0; i < historyList.Count; i++)
            {
                convertedHistoryList.Add(await SearchResultById(historyList[i].GameId));
            }


            List<List<Result>> favesAndHistory = new List<List<Result>>();
            favesAndHistory.Add(convertedFavoritesList);
            favesAndHistory.Add(convertedHistoryList);
            return View(favesAndHistory);
        }

        [Authorize]
        public IActionResult AddToFavorites(int id)
        {
            //Turn into method call: CallIdString
            string activeUserId = User.FindFirst(ClaimTypes.NameIdentifier).Value;

            UserFavorite f = new UserFavorite();

            f.GameId = id;
            f.UserId = activeUserId;
            f.IsFavorite = true;

            //add code to remove game from history list if its added to favorites

            //check for dupes does not throw an error message or return to search results correctly yet
            UserFavorite checkForDupes = _gameContext.UserFavorite.Where(f => f.UserId == activeUserId && f.GameId == id).FirstOrDefault();

            if (checkForDupes == null)
            {
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

        [Authorize]
        public IActionResult DeleteFavorite(int id)
        {
            //Turn into method call: CallIdString
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
        #endregion

        #region Recommendation Generation Station

        [Authorize]
        public async Task<List<Dictionary<string,double>>> GenerateWeights()
        {
            //test genre names
            //Entire array of genres in API
            string[] genres =
                { "Action", "Indie", "Adventure", "RPG", "Strategy", 
                "Shooter", "Casual", "Simulation", "Puzzle", "Arcade", "Platformer", "Racing",
                "Sports", "Massively Multiplayer", "Family", "Fighting", "Board Games", "Educational", "Card" };

            //test tag names
            //Selected array of tags in API
            string[] tags = { "Singleplayer", "Multiplayer", "Atmospheric", "Great Soundtrack", "RPG", "Co-op", "Story Rich", "Open World", "cooperative", "First-Person", "Sci-fi", 
                "2D", "Third Person", "FPS", "Horror", "Fantasy", "Comedy", "Sandbox", "Survival", "Exploration", "Stealth", "Tactical", "Pixel Graphics", "Action RPG", "Retro",
                "Space", "Zombies", "Point & Click", "Action-Adventure", "Hack and Slash", "Side Scroller", "Survival Horror", "RTS", "Roguelike", "mmo", "Driving", "Puzzle",
                "MMORPG", "Management", "JRPG" };

            //CallIDString method
            string activeUserId = User.FindFirst(ClaimTypes.NameIdentifier).Value;

            //Creates list of favorites for the current user
            var favList = await _gameContext.UserFavorite.Where(x => x.UserId == activeUserId).ToListAsync();

            
            List<Result> convertList = new List<Result>();

            for (int i = 0; i < favList.Count; i++) //8 seconds
            {
                convertList.Add(await SearchResultById(favList[i].GameId));
            }


            //creates a prepopulates a dictionary to log how many occurences of each genre appear in users favorites
            Dictionary<string, int> genreCountDictionary = new Dictionary<string, int>();
            foreach (var g in genres)
            {
                genreCountDictionary.Add(g, 0);
            }

            //creates a prepopulates a dictionary to log how many occurences of each tag appear in users favorites
            Dictionary<string, int> tagCountDictionary = new Dictionary<string, int>(); 
            foreach (var t in tags)
            {
                tagCountDictionary.Add(t, 0);
            }

            //populates tag and genre dictionaries with the number of occurences of each tag/genre for the game
            foreach (Result result in convertList)
            {
                foreach (string key in genreCountDictionary.Keys.ToList())
                {
                    for (int i = 0; i < result.genres.Length; i++)
                    {
                        if (key == result.genres[i].name)
                        {
                            genreCountDictionary[key] += 1;
                        }
                    }
                }
                foreach (string key in tagCountDictionary.Keys.ToList())
                {
                    for (int i = 0; i < result.tags.Length; i++)
                    {
                        if (key == result.tags[i].name)
                        {
                            tagCountDictionary[key] += 1;
                        }
                    }
                }
            }

            //Variables for sorting by genre.count and applying weights to each genre
            int totalGenres = 0;
            Dictionary<string, int> orderedGenreCount = new Dictionary<string, int>();
            Dictionary<string, double> weightedGenres = new Dictionary<string, double>();

            foreach (var item in genreCountDictionary.OrderByDescending(i => i.Value))
            {
                totalGenres += item.Value;
                orderedGenreCount.Add(item.Key, item.Value);
            }
            foreach (var g in orderedGenreCount)
            {
                double value = Math.Round(((double)g.Value / (double)totalGenres),2);
                weightedGenres.Add(g.Key, value);
            }

            //Variables for sorting by tags.count and applying weights to each tag
            int totalTags = 0;
            Dictionary<string, int> orderedTagCount = new Dictionary<string, int>();
            Dictionary<string, double> weightedTags = new Dictionary<string, double>();

            foreach (var item in tagCountDictionary.OrderByDescending(i => i.Value))
            {
                totalTags += item.Value;
                orderedTagCount.Add(item.Key, item.Value);
            }
            foreach (var g in orderedTagCount)
            {
                double value = Math.Round(((double)g.Value / (double)totalTags), 2);
                weightedTags.Add(g.Key, value);
            }

            //List to pass dictionaries to other actions
            List<Dictionary<string, double>> genreAndTagDictionaries = new List<Dictionary<string, double>>();
            genreAndTagDictionaries.Add(weightedGenres);
            genreAndTagDictionaries.Add(weightedTags);

            return genreAndTagDictionaries;
        }

        [Authorize]
        public async Task<IActionResult> GenerateRecommendations()
        {
            //obtains weighted genre and tag values to apply to game list
            List<Dictionary<string, double>> weights = await GenerateWeights();

            //Changing queries into partial strings to API endpoint
            string genreQuery = "";
            string tagQuery = "";
            foreach (string key in weights[0].Keys.ToList())
            {
                if (weights[0][key] != 0)
                {
                    genreQuery += key.Replace(" ", "-").ToLower() + ","; 
                }
            }
            foreach (string key in weights[1].Keys.ToList())
            {
                if (weights[1][key] != 0)
                {
                    tagQuery += key.Replace(" ", "-").ToLower() + ",";
                }
            }

            //This into method?

            SearchResult singlePageResults = new SearchResult();
            List<Result> recommendationResultPool = new List<Result>();

            string activeUserId = User.FindFirst(ClaimTypes.NameIdentifier).Value;


            for (int i = 1; i < 20; i++)
            {
                singlePageResults = await _gameDAL.GetGameListByGenreAndTag($"genres={genreQuery}&tags={tagQuery}&page={i}");
                //singlePageResults = await _gameDAL.GetGameListByGenreAndTag($"genres=sports&page={i}");

                foreach (var result in singlePageResults.results)
                {
                    UserFavorite checkForDupes = _gameContext.UserFavorite.Where(f => f.UserId == activeUserId && f.GameId == result.id).FirstOrDefault();

                    if (checkForDupes == null)
                    {
                        recommendationResultPool.Add(result);
                    }
                }
            }


            //Calculate score through genres and tags present in Favorites List
            List<Result> gameRecs = new List<Result>();
            foreach (Result result in recommendationResultPool)
            {
              double genreRecScore = 0;
              double tagRecScore = 0;
              double totalRecScore = 0;
                foreach (Genre genre in result.genres)
                {
                    genreRecScore += weights[0][genre.name];
                }
                foreach (Tag tag in result.tags)
                {
                    if (weights[1].ContainsKey(tag.name.ToString()))
                    {
                        tagRecScore += weights[1][tag.name];
                    }
                }

                totalRecScore = Math.Round((genreRecScore * 7) + (tagRecScore * 3),2);
                result.recommendationScore = totalRecScore;
                gameRecs.Add(result);

            }

            //Orders recommendations by score
            List<Result> orderedRecs = new List<Result>();
            foreach (var item in gameRecs.OrderByDescending(i => i.recommendationScore))
            {
                orderedRecs.Add(item);
            }

            return View("GenerateRecommendations", orderedRecs);
        }
        #endregion

        public void AddToHistory(Result addToHistory)
        {
            string activeUserId = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            UserHistory history = new UserHistory();

            history.GameId = addToHistory.id;
            history.UserId = activeUserId;

            UserHistory checkForDupes = _gameContext.UserHistory.Where(h => h.UserId == activeUserId && h.GameId == history.GameId).FirstOrDefault();

            if (checkForDupes == null)
            {
                if (ModelState.IsValid)
                {
                    _gameContext.UserHistory.Add(history);
                    _gameContext.SaveChanges();
                }
            }
        }

        [Authorize]
        public async Task<IActionResult> DisplayHistory()
        {
            //Turn into method call: CallIdString
            string activeUserId = User.FindFirst(ClaimTypes.NameIdentifier).Value;

            //Creates list of history for the current user
            var historyList = await _gameContext.UserHistory.Where(x => x.UserId == activeUserId).ToListAsync();

            List<Result> convertList = new List<Result>();

            for (int i = 0; i < historyList.Count; i++)
            {
                convertList.Add(await SearchResultById(historyList[i].GameId));
            }

            return View("DisplayHistory", convertList);
        }

        [Authorize]
        public async Task<IActionResult> DisplayWishlist() //check performance?
        {
            //Turn into method call: CallIdString
            string activeUserId = User.FindFirst(ClaimTypes.NameIdentifier).Value;

            //Creates list of favorites for the current user
            var wishList = await _gameContext.WishList.Where(x => x.UserId == activeUserId).ToListAsync();

            List<Result> convertedWishlist = new List<Result>();

            for (int i = 0; i < wishList.Count; i++)
            {
                convertedWishlist.Add(await SearchResultById(wishList[i].GameId));
            }

            //List<List<Result>> favesAndHistory = new List<List<Result>>();
            //favesAndHistory.Add(convertedWishlist);
            //favesAndHistory.Add(convertedHistoryList);
            return View(convertedWishlist);
        }

        [Authorize]
        public IActionResult AddToWishlist(int id)
        {
            //Turn into method call: CallIdString
            string activeUserId = User.FindFirst(ClaimTypes.NameIdentifier).Value;

            WishList f = new WishList();

            f.GameId = id;
            f.UserId = activeUserId;

            //check for dupes does not throw an error message or return to search results correctly yet
            WishList checkForDupes = _gameContext.WishList.Where(f => f.UserId == activeUserId && f.GameId == id).FirstOrDefault();

            if (checkForDupes == null)
            {
                if (ModelState.IsValid)
                {
                    _gameContext.WishList.Add(f);
                    _gameContext.SaveChanges();
                }

                return RedirectToAction("DisplayWishlist");
            }
            else
            {
                ViewBag.Error = "This game is already on your wishlist!";
                return RedirectToAction("GenerateRecommendations"); //redirect to a different page depending on the page that sent you here?
            }

        }

        [Authorize]
        public IActionResult DeleteWishlist(int id)
        {
            //Turn into method call: CallIdString
            string activeUserId = User.FindFirst(ClaimTypes.NameIdentifier).Value;

            var gameToDelete = _gameContext.WishList.Find(id);

            WishList deleteItem = _gameContext.WishList.Where(uf => uf.UserId == activeUserId && uf.GameId == id).FirstOrDefault();

            if (deleteItem != null)
            {
                _gameContext.WishList.Remove(deleteItem);
                _gameContext.SaveChanges();
            }

            return RedirectToAction("DisplayWishlist");
        }

        public async Task<IActionResult> IndieGames()
        {
            var searchId = await _gameDAL.GetGameListByGenreAndTag("genres=indie");

            return View(searchId);
        }
    }
}
