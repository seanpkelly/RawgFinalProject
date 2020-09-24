using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        public async Task<IActionResult> SearchGames()
        {
            return View();
        }

        [Authorize]
        public string GetActiveUser()
        {
            string activeUserId = User.FindFirst(ClaimTypes.NameIdentifier).Value;

            return activeUserId;
        }
        [Authorize]
        public IActionResult ClearUserRating(int id)
        {
            string activeUserId = GetActiveUser();
            UserFavorite favorite = _gameContext.UserFavorite.Where(f => f.UserId == activeUserId && f.GameId == id).FirstOrDefault();

            favorite.UserRating = -1;

            _gameContext.Entry(favorite).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
            _gameContext.Update(favorite);
            _gameContext.SaveChanges();

            return RedirectToAction("DisplayFavorites");
        }

        #region Search for Games

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> SearchGameByName(string searchName)
        {
            SearchResult searchResult = await _gameDAL.GetGameSearch(searchName);
            string activeUserId = GetActiveUser();

            for (int i = 0; i < searchResult.results.Length; i++)
            {
                UserFavorite checkForDupes = _gameContext.UserFavorite.Where(f => f.UserId == activeUserId && f.GameId == searchResult.results[i].id).FirstOrDefault();
                if (checkForDupes != null)
                {
                    searchResult.results[i].isfavorite = true;
                }
            }

            ViewBag.Header = $"Results for {searchName}";
            ViewBag.NoResults = "No results found.  Please try again.";

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
                genreQuery += genre.name.ToLower() + ",";
            }
            foreach (var tag in game.tags)
            {
                tagQuery += tag.name.Replace(" ", "-").ToLower() + ",";
            }

            //tagQuery = tagQuery.Substring(0, tagQuery.Length - 1);

            SearchResult similarGameResults = new SearchResult();

            if (genreQuery == "" && tagQuery != "")
            {
                similarGameResults = await _gameDAL.GetGameListByGenreAndTag($"tags={tagQuery}");

            }
            else if (genreQuery != "" && tagQuery == "")
            {
                similarGameResults = await _gameDAL.GetGameListByGenreAndTag($"genres={genreQuery}");
            }
            else if (genreQuery == "" && tagQuery == "")
            {
                ViewBag.NoResults = "No Results Found";
                ViewBag.Header = $"More games like {game.name}.";
                return View("SearchResults", similarGameResults);
            }
            else
            {
                similarGameResults = await _gameDAL.GetGameListByGenreAndTag($"genres={genreQuery}&tags={tagQuery}");
            }


            ViewBag.Header = $"More games like {game.name}.";

            return View("SearchResults", similarGameResults);
        }

        [Authorize]
        public async Task<IActionResult> GameDetails(int id)
        {
            Game searchedGame = await SearchGameById(id);
            
            Result searchResult = await _gameDAL.GetResultByName(id.ToString());
            string activeUserId = GetActiveUser();


            UserFavorite checkForDupes = _gameContext.UserFavorite.Where(f => f.UserId == activeUserId && f.GameId == searchedGame.id).FirstOrDefault();
            if (checkForDupes != null)
            {
                searchedGame.isFavorite = true;
            }
            else
            {
                AddToHistory(searchResult);
            }



            return View(searchedGame);
        }
        #endregion

        #region Favorites CRUD
        [Authorize]
        public async Task<IActionResult> DisplayFavorites() //check performance?
        {
            //Turn into method call: CallIdString
            string activeUserId = GetActiveUser();

            //Creates list of favorites for the current user
            var favList = await _gameContext.UserFavorite.Where(x => x.UserId == activeUserId).ToListAsync();

            List<Result> convertedFavoritesList = new List<Result>();

            DateTime time1 = DateTime.Now;

            for (int i = 0; i < favList.Count; i++)
            {
                convertedFavoritesList.Add(await SearchResultById(favList[i].GameId));
                convertedFavoritesList[i].userrating = favList[i].UserRating;
                convertedFavoritesList[i].favoritecount = favList[i].FavoriteCount;
            }

            DateTime time2 = DateTime.Now;

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
            string activeUserId = GetActiveUser();

            UserFavorite f = new UserFavorite();

            f.GameId = id;
            f.UserId = activeUserId;
            f.IsFavorite = true;
            f.UserRating = -1;


            //remove game from history list if its added to favorites
            DeleteHistory(id);
            DeleteWishlist(id);

            //check for dupes does not throw an error message or return to search results correctly yet
            UserFavorite checkForDupes = _gameContext.UserFavorite.Where(f => f.UserId == activeUserId && f.GameId == id).FirstOrDefault();

            if (checkForDupes == null)
            {
                if (ModelState.IsValid)
                {
                    _gameContext.UserFavorite.Add(f);
                    _gameContext.SaveChanges();
                }

                ////iterate favorite counter here///////////////////////////////////////////////////////

                List<UserFavorite> favorite = _gameContext.UserFavorite.Where(f => f.GameId == id).ToList();
                int count = favorite.Max(m => m.FavoriteCount) +1;

                foreach (var fav in favorite)
                {
                    fav.FavoriteCount = count;
                    _gameContext.Entry(fav).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                    _gameContext.Update(fav);
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
        public IActionResult AddUserRating(double userrating, int id)
        {
            string activeUserId = GetActiveUser();
            UserFavorite favorite = _gameContext.UserFavorite.Where(f => f.UserId == activeUserId && f.GameId == id).FirstOrDefault();

            favorite.UserRating = userrating;

            _gameContext.Entry(favorite).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
            _gameContext.Update(favorite);
            _gameContext.SaveChanges();

            return RedirectToAction("DisplayFavorites");

        }

        [Authorize]
        public IActionResult DeleteHistory(int id)
        {
            //Turn into method call: CallIdString
            string activeUserId = GetActiveUser();

            var gameToDelete = _gameContext.UserFavorite.Find(id);

            UserHistory deleteItem = _gameContext.UserHistory.Where(h => h.UserId == activeUserId && h.GameId == id).FirstOrDefault();

            if (deleteItem != null)
            {
                _gameContext.UserHistory.Remove(deleteItem);
                _gameContext.SaveChanges();
            }

            return RedirectToAction("DisplayFavorites");
        }

        [Authorize]
        public IActionResult ClearSuggestions()
        {
            //Turn into method call: CallIdString
            string activeUserId = GetActiveUser();

            List<UserHistory> deleteItem = _gameContext.UserHistory.Where(h => h.UserId == activeUserId).ToList();

            foreach (var delete in deleteItem)
            {
                if (deleteItem != null)
                    {
                        _gameContext.UserHistory.Remove(delete);
                        _gameContext.SaveChanges();
                    }
            }

            return RedirectToAction("DisplayFavorites");
        }

        [Authorize]
        public IActionResult DeleteFavorite(int id)
        {
            //Turn into method call: CallIdString
            string activeUserId = GetActiveUser();

            var gameToDelete = _gameContext.UserFavorite.Find(id);

            UserFavorite deleteItem = _gameContext.UserFavorite.Where(uf => uf.UserId == activeUserId && uf.GameId == id).FirstOrDefault();

            if (deleteItem != null)
            {
                _gameContext.UserFavorite.Remove(deleteItem);
                _gameContext.SaveChanges();
            }

            List<UserFavorite> favorite = _gameContext.UserFavorite.Where(f => f.GameId == id).ToList();

            if (favorite.Count > 0)
            {
                int count = favorite.Max(m => m.FavoriteCount) - 1;

                foreach (var fav in favorite)
                {
                    fav.FavoriteCount = count;
                    _gameContext.Entry(fav).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                    _gameContext.Update(fav);
                    _gameContext.SaveChanges();
                }
            }
            

            return RedirectToAction("DisplayFavorites");
        }
        #endregion

        #region Questionnaire
        [Authorize]
        public IActionResult ResetQuestionnaire()
        {
            string activeUserId = GetActiveUser();
            Questionnaire resetQuestionnaire = _gameContext.Questionnaire.Where(q => q.UserId == activeUserId).FirstOrDefault();

            resetQuestionnaire.Genres = "";
            resetQuestionnaire.Tags = "";

            List<string> emptyGenreTag = new List<string>();
            emptyGenreTag.Add("");
            emptyGenreTag.Add("");
            _gameContext.Entry(resetQuestionnaire).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
            _gameContext.Update(resetQuestionnaire);
            _gameContext.SaveChanges();

            return View("Questionnaire", emptyGenreTag);
        }

        [Authorize]
        public IActionResult Questionnaire()
        {
            string activeUserId = GetActiveUser();

            //Creates list of favorites for the current user
            List<string> questionnaireAnswers = new List<string>();

            Questionnaire q = (_gameContext.Questionnaire.Where(x => x.UserId == activeUserId).FirstOrDefault());

            if (q != null)
            {
                questionnaireAnswers.Add(q.Genres);
                questionnaireAnswers.Add(q.Tags);
            }
            else
            {
                questionnaireAnswers.Add("");
                questionnaireAnswers.Add("");
            }

            return View(questionnaireAnswers);
        }

        [Authorize]
        public async Task<IActionResult> GenerateQuestionnaireRecommendations(Microsoft.AspNetCore.Http.IFormCollection form)
        {

            string genre = form["genre"];
            string tag = form["tag"];

            //save questionnaire results here

            string activeUserId = GetActiveUser();

            if (genre == null)
            {
                genre = "";
            }
            if (tag == null)
            {
                tag = "";
            }

            Questionnaire qToUpdate = _gameContext.Questionnaire.Where(q => q.UserId == activeUserId).FirstOrDefault();

            if (qToUpdate != null)
            {
                qToUpdate.Genres = genre;
                qToUpdate.Tags = tag;

                _gameContext.Entry(qToUpdate).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                _gameContext.Update(qToUpdate);
                _gameContext.SaveChanges();
            }
            else
            {
                Questionnaire q = new Questionnaire();

                q.UserId = activeUserId;
                q.Genres = genre;
                q.Tags = tag;
                if (ModelState.IsValid)
                {
                    _gameContext.Questionnaire.Add(q);
                    _gameContext.SaveChanges();
                }
            }

            List<Result> recommendationResultPool = await GenerateQuestionnaireResults(genre, tag);

            if (recommendationResultPool.Count > 0)
            {
                return View("QuestionnaireResults", recommendationResultPool);
            }
            else
            {
                ViewBag.NoResults = "No results found.  Please try again.";
                return View("QuestionnaireResults", recommendationResultPool);
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<List<Result>> GenerateQuestionnaireResults(string genreQuery, string tagQuery)
        {
            SearchResult singlePageResults = new SearchResult();
            List<Result> recommendationResultPool = new List<Result>();

            genreQuery = genreQuery.Replace("RPG", "role-playing-games-rpg");
            genreQuery = genreQuery.Replace("Massively Multiplayer", "Massively-Multiplayer");
            genreQuery = genreQuery.Replace("Board Games", "Board-Games");

            tagQuery = tagQuery.Replace(" ", "-");

            try
            {
                for (int i = 1; i < 10; i++)
                {
                    singlePageResults = await _gameDAL.GetGameListByGenreAndTag($"genres={genreQuery}&tags={tagQuery}&page={i}");
                    foreach (var result in singlePageResults.results)
                    {
                        string activeUserId = GetActiveUser();
                        UserFavorite checkForDupes = _gameContext.UserFavorite.Where(f => f.UserId == activeUserId && f.GameId == result.id).FirstOrDefault();

                        if (checkForDupes != null)
                        {
                            result.isfavorite = checkForDupes.IsFavorite;
                        }
                        recommendationResultPool.Add(result);
                    }
                }
            }
            catch (Exception)
            {
                List<Result> emptyList = new List<Result>();
                return emptyList;
            }


            return recommendationResultPool;
        }
        #endregion

        #region Recommendation Generation Station

        [Authorize]
        public async Task<List<Result>> ConvertToResult(List<UserFavorite> favList)
        {
            List<Result> convertList = new List<Result>();

            for (int i = 0; i < favList.Count; i++) //8 seconds
            {
                convertList.Add(await SearchResultById(favList[i].GameId));
            }

            return convertList;
        }

        [Authorize]
        public Dictionary<string,int> CountGenreOccurences(List<Result> convertList, Dictionary<string,int> genreCountDictionary)
        {
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
            }
            return genreCountDictionary;
        }

        [Authorize]
        public Dictionary<string, int> CountTagOccurences(List<Result> convertList, Dictionary<string, int> tagCountDictionary)
        {
            foreach (Result result in convertList)
            {
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
            return tagCountDictionary;
        }

        [Authorize]
        public Dictionary<string, double> CalculateWeights(Dictionary<string, int> countDictionary)
        {
            int totalGenres = 0;
            Dictionary<string, int> orderedCount = new Dictionary<string, int>();
            Dictionary<string, double> weightedDictionary = new Dictionary<string, double>();

            foreach (var item in countDictionary.OrderByDescending(i => i.Value))
            {
                totalGenres += item.Value;
                orderedCount.Add(item.Key, item.Value);
            }
            foreach (var g in orderedCount)
            {
                double value = Math.Round(((double)g.Value / (double)totalGenres), 2);
                weightedDictionary.Add(g.Key, value);
            }

            return weightedDictionary;
        }

        [Authorize]
        public async Task<List<Dictionary<string,double>>> GenerateWeights()
        {

            Dictionary<string, int> genreCountDictionary = PopulateGenreDictionary();
            Dictionary<string, int> tagCountDictionary = PopulateTagDictionary();

            string activeUserId = GetActiveUser();

            var favList = await _gameContext.UserFavorite.Where(x => x.UserId == activeUserId).ToListAsync();

            List<Result> convertList = await ConvertToResult(favList);

            genreCountDictionary = CountGenreOccurences(convertList, genreCountDictionary);
            tagCountDictionary = CountTagOccurences(convertList, tagCountDictionary);

            Dictionary<string, double> weightedGenres = CalculateWeights(genreCountDictionary);
            Dictionary<string, double> weightedTags = CalculateWeights(tagCountDictionary);

            List<Dictionary<string, double>> genreAndTagDictionaries = new List<Dictionary<string, double>>();
            genreAndTagDictionaries.Add(weightedGenres);
            genreAndTagDictionaries.Add(weightedTags);

            return genreAndTagDictionaries;
        }

        [Authorize]
        public string CreateQuery(Dictionary<string, double> dictionary)
        {
            string query = "";
            foreach (string key in dictionary.Keys.ToList())
            {
                if (dictionary[key] != 0)
                {
                    query += key.Replace(" ", "-").ToLower() + ",";
                }
            }
            return query;
        }
      

        [Authorize]
        public async Task<List<Result>> GenerateResultPool(string genreQuery, string tagQuery)
        {
            SearchResult singlePageResults = new SearchResult();
            List<Result> recommendationResultPool = new List<Result>();

            string activeUserId = GetActiveUser();

            genreQuery.Replace("RPG", "role-playing-games-rpg");

            for (int i = 1; i < 15; i++)
            {
                //&tags ={ tagQuery}
                singlePageResults = await _gameDAL.GetGameListByGenreAndTag($"genres={genreQuery}&page={i}");

                foreach (var result in singlePageResults.results)
                {
                    UserFavorite checkForDupes = _gameContext.UserFavorite.Where(f => f.UserId == activeUserId && f.GameId == result.id).FirstOrDefault();

                    if (checkForDupes == null)
                    {
                        recommendationResultPool.Add(result);
                    }
                }
            }
            return recommendationResultPool;
        }

        [Authorize]
        public List<Result> GenerateScores(List<Result> recommendationResultPool, List<Dictionary<string, double>> weights)
        {
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

                totalRecScore = Math.Round((genreRecScore * 7) + (tagRecScore * 3), 2);
                result.recommendationScore = totalRecScore;
                gameRecs.Add(result);
            }

            //Orders recommendations by score
            List<Result> orderedRecs = new List<Result>();
            foreach (var item in gameRecs.OrderByDescending(i => i.recommendationScore))
            {
                orderedRecs.Add(item);
            }

            return orderedRecs;
        }
 
        [Authorize]
        public async Task<IActionResult> GenerateRecommendations()
        {
            List<Dictionary<string, double>> weights = await GenerateWeights();

            string genreQuery = CreateQuery(weights[0]);
            string tagQuery = CreateQuery(weights[1]);

            List<Result> recommendationResultPool = await GenerateResultPool(genreQuery, tagQuery);

            List<Result> orderedRecs = GenerateScores(recommendationResultPool, weights);

            return View("GenerateRecommendations", orderedRecs);
        }

        [Authorize]
        public Dictionary<string, int> PopulateGenreDictionary()
        {
            string[] genres =
                { "Action", "Indie", "Adventure", "RPG", "Strategy",
                "Shooter", "Casual", "Simulation", "Puzzle", "Arcade", "Platformer", "Racing",
                "Sports", "Massively Multiplayer", "Family", "Fighting", "Board Games", "Educational", "Card" };

            Dictionary<string, int> genreCountDictionary = new Dictionary<string, int>();
            foreach (var g in genres)
            {
                genreCountDictionary.Add(g, 0);
            }

            return genreCountDictionary;
        }

        [Authorize]
        public Dictionary<string, int> PopulateTagDictionary()
        {
            string[] tags = { "Singleplayer", "Multiplayer", "Atmospheric", "Great Soundtrack", "RPG", "Co-op", "Story Rich", "Open World", "cooperative", "First-Person", "Sci-fi",
                "2D", "Third Person", "FPS", "Horror", "Fantasy", "Comedy", "Sandbox", "Survival", "Exploration", "Stealth", "Tactical", "Pixel Graphics", "Action RPG", "Retro",
                "Space", "Zombies", "Point & Click", "Action-Adventure", "Hack and Slash", "Side Scroller", "Survival Horror", "RTS", "Roguelike", "mmo", "Driving", "Puzzle",
                "MMORPG", "Management", "JRPG" };

            Dictionary<string, int> tagCountDictionary = new Dictionary<string, int>();
            foreach (var t in tags)
            {
                tagCountDictionary.Add(t, 0);
            }

            return tagCountDictionary;
        }
        #endregion

        #region History
        [Authorize]
        public void AddToHistory(Result addToHistory)
        {
            string activeUserId = GetActiveUser();
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
            string activeUserId = GetActiveUser();

            //Creates list of history for the current user
            var historyList = await _gameContext.UserHistory.Where(x => x.UserId == activeUserId).ToListAsync();

            List<Result> convertList = new List<Result>();

            for (int i = 0; i < historyList.Count; i++)
            {
                convertList.Add(await SearchResultById(historyList[i].GameId));
            }

            return View("DisplayHistory", convertList);
        }
        #endregion

        #region Wishlist
        [Authorize]
        public async Task<IActionResult> DisplayWishlist() //check performance?
        {
            //Turn into method call: CallIdString
            string activeUserId = GetActiveUser();

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
            string activeUserId = GetActiveUser();

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
            string activeUserId = GetActiveUser();

            var gameToDelete = _gameContext.WishList.Find(id);

            WishList deleteItem = _gameContext.WishList.Where(uf => uf.UserId == activeUserId && uf.GameId == id).FirstOrDefault();

            if (deleteItem != null)
            {
                _gameContext.WishList.Remove(deleteItem);
                _gameContext.SaveChanges();
            }

            return RedirectToAction("DisplayWishlist");
        }
        #endregion

        #region Indie Games
        [Authorize]

        public async Task<IActionResult> IndieGames()
        {
            var indieGames = await _gameDAL.GetGameListByGenreAndTag("genres=indie");

            return View(indieGames);
        }
        #endregion

    }
}
