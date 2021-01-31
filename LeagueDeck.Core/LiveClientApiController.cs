using LeagueDeck.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace LeagueDeck.Core
{
    public static class LiveClientApiController
    {
        private const string cInGameApiBaseUrl = "https://127.0.0.1:2999/liveclientdata";

        private static AllGameDataCache AllGameDataCache = new AllGameDataCache();

        public static async Task<Models.Api.AllGameData> GetAll(CancellationToken ct)
        {
            return await GetAll(ct, DateTime.Now);
        }

        public static async Task<Models.Api.AllGameData> GetAll(CancellationToken ct, DateTime cache)
        {
            if (AllGameDataCache.DateTimeAllGameDataUpdate >= cache)
                return AllGameDataCache;

            try
            {
                var url = cInGameApiBaseUrl + "/allgamedata";
                var json = await GetApiResponse(url, ct);
                var allGameData = JsonConvert.DeserializeObject<Models.Api.AllGameData>(json);
                AllGameDataCache.Update(allGameData);

                return AllGameDataCache;
            }
            catch (Exception e)
            {
                // TODO: Log
                return null;
            }
        }

        public static async Task<List<Models.Api.Participant>> GetEnemies(CancellationToken ct)
        {
            return await GetEnemies(ct, DateTime.Now);
        }

        public static async Task<List<Models.Api.Participant>> GetEnemies(CancellationToken ct, DateTime cache)
        {
            var participants = await GetParticipants(ct, cache);
            if (participants == null)
                return null;

            var activePlayer = await GetActivePlayer(ct, DateTime.MinValue);
            if (activePlayer == null)
                return null;

            var activePlayerParticipant = participants.FirstOrDefault(x => x.SummonerName == activePlayer.Name);
            if (activePlayerParticipant == null)
                return null;

            var enemies = participants.Where(x => x.Team != activePlayerParticipant.Team).ToList();

            return enemies;
        }

        public static async Task<List<Models.Api.Participant>> GetParticipants(CancellationToken ct)
        {
            return await GetParticipants(ct, DateTime.Now);
        }

        public static async Task<List<Models.Api.Participant>> GetParticipants(CancellationToken ct, DateTime cache)
        {
            if (AllGameDataCache.Participants != null && AllGameDataCache.ParticipantsUpdate >= cache)
                return AllGameDataCache.Participants;

            // Sometimes the api returns an empty participant list on game start
            // We prevent failing the initial call by waiting for the list to fill
            // A game without participants doesn't make sense so we can wait here
            List<Models.Api.Participant> participants = null;
            while ((participants == null || participants.Count == 0) && !ct.IsCancellationRequested)
            {
                try
                {
                    var url = cInGameApiBaseUrl + "/playerlist";
                    var json = await GetApiResponse(url, ct);
                    participants = JsonConvert.DeserializeObject<List<Models.Api.Participant>>(json);
                }
                catch
                {
                    await Task.Delay(500, ct);
                }
            }

            AllGameDataCache.Participants = participants;

            return AllGameDataCache.Participants;
        }

        public static async Task<Models.Api.Participant> GetParticipant(string name, CancellationToken ct)
        {
            return await GetParticipant(name, ct, DateTime.Now);
        }

        public static async Task<Models.Api.Participant> GetParticipant(string name, CancellationToken ct, DateTime cache)
        {
            var participants = await GetParticipants(ct, cache);
            if (participants == null)
                return null;

            var participant = participants.FirstOrDefault(x => x.SummonerName == name);

            return participant;
        }

        public static async Task<Models.Api.GameData> GetGameData(CancellationToken ct)
        {
            return await GetGameData(ct, DateTime.Now);
        }

        public static async Task<Models.Api.GameData> GetGameData(CancellationToken ct, DateTime cache)
        {
            if (AllGameDataCache.GameData != null && AllGameDataCache.GameDataUpdate >= cache)
                return AllGameDataCache.GameData;

            try
            {
                var url = cInGameApiBaseUrl + "/gamestats";
                var response = await GetApiResponse(url, ct);
                AllGameDataCache.GameData = JsonConvert.DeserializeObject<Models.Api.GameData>(response);
                return AllGameDataCache.GameData;
            }
            catch (Exception e)
            {
                // TODO: Log
                return null;
            }
        }

        public static async Task<Models.Api.GameEvents> GetEventData(CancellationToken ct)
        {
            return await GetEventData(ct, DateTime.Now);
        }

        public static async Task<Models.Api.GameEvents> GetEventData(CancellationToken ct, DateTime cache)
        {
            if (AllGameDataCache.GameEvents != null && AllGameDataCache.GameEventsUpdate >= cache)
                return AllGameDataCache.GameEvents;

            try
            {
                var url = cInGameApiBaseUrl + "/eventdata";
                var response = await GetApiResponse(url, ct);
                AllGameDataCache.GameEvents = JsonConvert.DeserializeObject<Models.Api.GameEvents>(response);
                return AllGameDataCache.GameEvents;
            }
            catch (Exception e)
            {
                // TODO: Log
                return null;
            }
        }

        public static async Task<Models.Api.ActivePlayer> GetActivePlayer(CancellationToken ct)
        {
            return await GetActivePlayer(ct, DateTime.Now);
        }

        public static async Task<Models.Api.ActivePlayer> GetActivePlayer(CancellationToken ct, DateTime cache)
        {
            if (AllGameDataCache.ActivePlayer != null && AllGameDataCache.ActivePlayerUpdate >= cache)
                return AllGameDataCache.ActivePlayer;

            try
            {
                var url = cInGameApiBaseUrl + "/activeplayer";
                var response = await GetApiResponse(url, ct);
                AllGameDataCache.ActivePlayer = JsonConvert.DeserializeObject<Models.Api.ActivePlayer>(response);
                return AllGameDataCache.ActivePlayer;
            }
            catch (Exception e)
            {
                // TODO: Log
                return null;
            }
        }

        internal static async Task<string> GetApiResponse(string url, CancellationToken ct)
        {
            HttpWebResponse response = null;
            while (response == null)
            {
                try
                {
                    if (ct.IsCancellationRequested)
                        return string.Empty;

                    var request = (HttpWebRequest)WebRequest.Create(url);

                    // accept all SSL certificates
                    request.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;

                    response = (HttpWebResponse)await request.GetResponseAsync();
                }
                catch
                {
                    await Task.Delay(500, ct);
                }
            }

            using (var stream = response.GetResponseStream())
            {
                using (var sr = new StreamReader(stream))
                {
                    return sr.ReadToEnd();
                }
            };
        }
    }
}
