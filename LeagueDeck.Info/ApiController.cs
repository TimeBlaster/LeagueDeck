using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace LeagueDeck.Core
{
    public static class ApiController
    {
        private const string cInGameApiBaseUrl = "https://127.0.0.1:2999/liveclientdata";

        public static async Task<List<Models.Api.Participant>> GetEnemies(CancellationToken ct)
        {
            var participants = await GetParticipants(ct);
            if (participants == null)
                return null;

            var activePlayerName = await GetActivePlayerName(ct);

            var activePlayer = participants.FirstOrDefault(x => x.SummonerName == activePlayerName);
            if (activePlayer == null)
                return null;

            var team = activePlayer.Team;
            var enemies = participants.Where(x => x.Team != team).ToList();

            return enemies;
        }

        private static async Task<string> GetActivePlayerName(CancellationToken ct)
        {
            try
            {
                var url = cInGameApiBaseUrl + "/activeplayername";
                var name = await GetApiResponse(url, ct);

                //Remove " from beginning and end
                return name.Substring(1, name.Length - 2);

            }
            catch (Exception e)
            {
                // TODO: Log
                return null;
            }
        }

        public static async Task<List<Models.Api.Participant>> GetParticipants(CancellationToken ct)
        {
            List<Models.Api.Participant> participants = null;
            while (participants == null || participants.Count == 0)
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

            return participants;
        }

        public static async Task<Models.Api.Participant> GetParticipant(string name, CancellationToken ct)
        {
            var participants = await GetParticipants(ct);
            if (participants == null)
                return null;

            var participant = participants.FirstOrDefault(x => x.SummonerName == name);

            return participant;
        }

        public static async Task<Models.Api.GameData> GetGameData(CancellationToken ct)
        {
            try
            {
                var url = cInGameApiBaseUrl + "/gamestats";
                var response = await GetApiResponse(url, ct);
                var gameData = JsonConvert.DeserializeObject<Models.Api.GameData>(response);

                return gameData;
            }
            catch (Exception e)
            {
                // TODO: Log
                return null;
            }
        }

        public static async Task<List<Models.Api.GameEvent>> GetEventData(CancellationToken ct)
        {
            try
            {
                var url = cInGameApiBaseUrl + "/eventdata";
                var response = await GetApiResponse(url, ct);
                var eventData = JsonConvert.DeserializeObject<JObject>(response);
                var events = eventData["Events"].ToObject<List<Models.Api.GameEvent>>();

                return events;
            }
            catch (Exception e)
            {
                // TODO: Log
                return null;
            }
        }

        public static async Task<Models.Api.ActivePlayer> GetActivePlayer(CancellationToken ct)
        {
            try
            {
                var url = cInGameApiBaseUrl + "/activeplayer";
                var response = await GetApiResponse(url, ct);
                var activePlayer = JsonConvert.DeserializeObject<Models.Api.ActivePlayer>(response);

                return activePlayer;
            }
            catch (Exception e)
            {
                // TODO: Log
                return null;
            }
        }

        public static async Task<string> GetApiResponse(string url, CancellationToken ct)
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
