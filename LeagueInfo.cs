using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LeagueDeck
{
    public class LeagueInfo
    {
        #region vars

        private string _activePlayerName;
        private List<SummonerData> _participants;

        private static LeagueInfo _instance;

        #endregion

        #region Public Methods

        public static LeagueInfo GetInstance()
        {
            if (_instance == null)
                _instance = new LeagueInfo();

            return _instance;
        }

        public async Task<List<SummonerData>> GetParticipants(CancellationToken ct)
        {
            try
            {
                var json = await Utilities.GetApiResponse("/playerlist", ct);
                _participants = JsonConvert.DeserializeObject<List<SummonerData>>(json);
            }
            catch
            { }

            return _participants;
        }

        public async Task<string> GetActivePlayer(CancellationToken ct)
        {
            if (string.IsNullOrEmpty(_activePlayerName))
            {
                var name = await Utilities.GetApiResponse("/activeplayername", ct);

                //Remove " from beginning and end
                _activePlayerName = name.Substring(1, name.Length - 2);
            }

            return _activePlayerName;
        }

        public async Task<SummonerData> GetParticipant(int index, CancellationToken ct)
        {
            var participants = await GetParticipants(ct);
            if (participants == null)
                return null;

            var activePlayerName = await GetActivePlayer(ct);

            var team = participants.FirstOrDefault(x => x.SummonerName == activePlayerName).Team;
            var enemyTeamParticipants = participants.Where(x => x.Team != team);
            var participant = enemyTeamParticipants.ElementAtOrDefault(index);

            return participant;
        }

        public async Task<GameData> GetGameData(CancellationToken ct)
        {
            var response = await Utilities.GetApiResponse("/gamestats", ct);
            var gameData = JsonConvert.DeserializeObject<GameData>(response);

            return gameData;
        }

        public static double GetSummonerSpellCooldown(string spell, SummonerData participant, bool isAram)
        {
            double cooldown = 0;
            switch (spell)
            {
                // Teleport is weird
                case "Teleport":
                    cooldown = 430.588;
                    cooldown -= participant.Level * 10.588;
                    break;

                case "Flash":
                    cooldown = 300;
                    break;

                case "Heal":
                case "Clarity":
                    cooldown = 240;
                    break;

                case "Cleanse":
                case "Exhaust":
                case "Ghost":
                    cooldown = 210;
                    break;

                case "Barrier":
                case "Ignite":
                    cooldown = 180;
                    break;

                case "Smite":
                    cooldown = 90;
                    break;

                case "Mark":
                    cooldown = 80;
                    break;

                case "Hexflash":
                    cooldown = 20;
                    break;

                case "Garrison":
                case "Revive":
                case "Clairvoyance":
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(spell));
            }

            if (isAram)
                cooldown *= 0.6;

            int summonerSpellHaste = 0;

            // check for Ionian Boots of Lucidity
            if (participant.Items.Any(x => x.Id == 3158))
                summonerSpellHaste += 10;

            return cooldown / (1 + (summonerSpellHaste / 100));
        }

        #endregion
    }
}
