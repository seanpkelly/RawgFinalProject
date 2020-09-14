using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
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
            _gameContext = new GameRecommendationDbContext();
        }

        public async Task<IActionResult> Index()
        {
            Game games = await _gameDAL.GetGamesList();

            return View(games);

        }

        [HttpPost]
        public async Task<IActionResult> SearchGameByName(string searchName)
        {
            string searchNameSlug = searchName.Replace(" ", "-").ToLower();

            var searchResult = await _gameDAL.GetGameByName(searchNameSlug);

            List<Game> searchedGames = new List<Game>();

            searchedGames.Add(searchResult);

            return View("SearchResults", searchedGames);

        }

        public async Task<IActionResult> AddToFavorites(int id)
        {
           string activeUserId = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            UserFavorite f = new UserFavorite();

            f.GameId = id;
            f.UserId = activeUserId;

            if (ModelState.IsValid)
            {
                _gameContext.UserFavorite.Add(f);
                _gameContext.SaveChanges();
            }
          
            return RedirectToAction("DisplayFavorites");
        }

        public async Task<IActionResult> Delete(int id)
        {
            string activeUserId = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            var gameToDelete = _gameContext.UserFavorite.Find(id);
            //UsersFavorite deleteItem = _context.UsersFavorite.Where(uf => uf.UserId == loginUserId && uf.FavoriteId == favoriteid).FirstOrDefault();

            if (gameToDelete != null)
            {
                _gameContext.UserFavorite.Remove(gameToDelete);
                _gameContext.SaveChanges();
            }

            return RedirectToAction("DisplayFavorites");
        }
    }
}
