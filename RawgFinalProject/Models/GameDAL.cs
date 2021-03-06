﻿using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Permissions;
using System.Threading.Tasks;

namespace RawgFinalProject.Models
{
    public class GameDAL
    {
        private readonly string _apiKey;

        public GameDAL(string apiKey)
        {
            _apiKey = apiKey;
        }
        public HttpClient GetClient()
        {
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri("https://rawg-video-games-database.p.rapidapi.com");
            client.DefaultRequestHeaders.Add("X-RapidAPI-Key", _apiKey);
            client.DefaultRequestHeaders.Add("User-Agent", "Game Recommendation Station");
            return client;
        }

        public async Task<Game> GetGameByName(string output)
        {
            var client = GetClient(); 
            var response = await client.GetAsync($"games/{output}"); 
            var searchedGames = await response.Content.ReadAsAsync<Game>();

            return searchedGames;
        }
        public async Task<Result> GetResultByName(string output)
        {
            var client = GetClient();
            var response = await client.GetAsync($"games/{output}");
            var searchedGames = await response.Content.ReadAsAsync<Result>();

            return searchedGames;
        }

        public async Task<SearchResult> GetGameSearch(string output)
        {
            var client = GetClient();
            var response = await client.GetAsync($"games?search={output}");
            SearchResult searchedGames = await response.Content.ReadAsAsync<SearchResult>();

            return searchedGames;
        }

        public async Task<SearchResult> GetGameListByGenreAndTag(string apiQuery)
        {
            apiQuery = apiQuery.ToLower();
            var client = GetClient();
            var response = await client.GetAsync($"games?{apiQuery}");
            SearchResult searchedGames = await response.Content.ReadAsAsync<SearchResult>();

            return searchedGames;
        }

    }
}
