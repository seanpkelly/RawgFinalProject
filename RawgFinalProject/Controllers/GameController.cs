using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
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

        //[HttpPost]
        //public async Task<IActionResult> SearchGameByName(string searchName)
        //{
        //    string searchNameSlug = searchName;

        //    var searchResult = await _gameDAL.GetGameByName(searchNameSlug);

        //    List<Game> searchedGames = new List<Game>();

        //    searchedGames.Add(searchResult);

        //    AddToHistory(searchResult);

        //    return View("SearchResults", searchedGames);

        //}

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

    }
}
