using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace LeagueDeck
{
    public class LeagueInfo
    {
        #region Events

        public class UpdateEventArgs : EventArgs
        {
            public int Progress { get; set; }

            public UpdateEventArgs(int progress)
            {
                Progress = progress;
            }
        }

        public event EventHandler<UpdateEventArgs> OnUpdateStarted;
        public event EventHandler<UpdateEventArgs> OnUpdateCompleted;
        public event EventHandler<UpdateEventArgs> OnUpdateProgress;

        #endregion

        #region vars

        private static LeagueInfo _instance;

        private int _progress = 0;
        private int _total = 0;

        private string _activePlayerName;
        private List<SummonerData> _participants;

        private LeagueData _data;
        private string _latestVersion;

        private const string cInGameApiBaseUrl = "https://127.0.0.1:2999/liveclientdata";
        private const string cVersionsUrl = "https://ddragon.leagueoflegends.com/api/versions.json";
        private const string cChampionsDataUrl = "http://ddragon.leagueoflegends.com/cdn/{0}/data/en_US/champion.json";
        private const string cChampionDataUrl = "http://ddragon.leagueoflegends.com/cdn/{0}/data/en_US/champion/{1}.json";
        private const string cChampionImageUrl = "http://ddragon.leagueoflegends.com/cdn/{0}/img/champion/{1}.png";
        private const string cSummonerSpellDataUrl = "http://ddragon.leagueoflegends.com/cdn/{0}/data/en_US/summoner.json";
        private const string cSpellImageUrl = "http://ddragon.leagueoflegends.com/cdn/{0}/img/spell/{1}.png";

        private readonly string _championImageFolder = Path.Combine(Environment.CurrentDirectory, "Images", "Champions");
        private readonly string _spellImageFolder = Path.Combine(Environment.CurrentDirectory, "Images", "Spells");
        private readonly string _summonerSpellImageFolder = Path.Combine(Environment.CurrentDirectory, "Images", "SummonerSpells");
        private readonly string _leagueDataFolder = Path.Combine(Environment.CurrentDirectory, "LeagueData");

        #endregion

        #region ctor

        private LeagueInfo(CancellationToken ct)
        {
            Task.Run(async () =>
            {
                var version = await GetLatestVersion(ct);

                Directory.CreateDirectory(_leagueDataFolder);
                var path = Path.Combine(_leagueDataFolder, $"{version}.json");

                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    _data = JsonConvert.DeserializeObject<LeagueData>(json);
                }
                else
                {
                    await UpdateData(version, path, ct);
                }
            });
        }

        #endregion

        #region Public Methods

        public static LeagueInfo GetInstance(CancellationToken ct)
        {
            if (_instance == null)
                _instance = new LeagueInfo(ct);

            return _instance;
        }

        public static double GetSpellCooldown(Spell spell, SummonerData participant)
        {
            // assuming the player always levels when possible
            // TODO: check for special level behaviour(Elise, Jayce, Karma, ...)
            // TODO: basic abilities
            double cooldown;
            if (participant.Level < 11)
                cooldown = spell.Cooldown[0];
            else if (participant.Level < 16)
                cooldown = spell.Cooldown[1];
            else
                cooldown = spell.Cooldown[2];

            int abilityHaste = 0;
            // TODO: get Ability Haste from items

            // TODO: check for runes

            return cooldown / (1 + (abilityHaste / 100));
        }

        public static double GetSummonerSpellCooldown(Spell spell, SummonerData participant, bool isAram)
        {
            double cooldown;
            // SummonerSpellData contains no info about tp cooldown
            if (spell.Id == "SummonerTeleport")
                cooldown = 430.588 - 10.588 * participant.Level;
            else
                cooldown = spell.Cooldown[0];

            if (isAram)
                cooldown *= 0.6;

            int summonerSpellHaste = 0;

            // TODO: check for runes

            // check for Ionian Boots of Lucidity
            if (participant.Items.Any(x => x.Id == 3158))
                summonerSpellHaste += 10;

            return cooldown / (1 + (summonerSpellHaste / 100));
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
            var url = cInGameApiBaseUrl + "/gamestats";
            var response = await Utilities.GetApiResponse(url, ct);
            var gameData = JsonConvert.DeserializeObject<GameData>(response);

            return gameData;
        }

        public Champion GetChampion(string championName)
        {
            return _data.Champions.FirstOrDefault(x => x.Name == championName);
        }

        public Image GetChampionImage(string championName)
        {
            return Image.FromFile(Path.Combine(_championImageFolder, $"{championName}.png"));
        }

        public Spell GetSummonerSpell(string spellName)
        {
            return _data.SummonerSpells.FirstOrDefault(x => x.Name == spellName);
        }

        public Image GetSummonerSpellImage(string spellName)
        {
            return Image.FromFile(Path.Combine(_summonerSpellImageFolder, $"{spellName}.png"));
        }

        public Spell GetSpell(SummonerData participant, ESpell spell)
        {
            switch (spell)
            {
                case ESpell.Q:
                case ESpell.W:
                case ESpell.E:
                case ESpell.R:
                    var champion = GetChampion(participant.ChampionName);
                    return champion.Spells[(int)spell];

                case ESpell.SummonerSpell1:
                case ESpell.SummonerSpell2:
                    var spellName = participant.GetSummonerSpell(spell);
                    return GetSummonerSpell(spellName);

                default:
                    throw new ArgumentOutOfRangeException(nameof(spell));
            }
        }

        public Image GetSpellImage(string spellName)
        {
            return Image.FromFile(Path.Combine(_spellImageFolder, $"{spellName}.png"));
        }

        #endregion

        #region Private Methods

        private async Task UpdateData(string version, string path, CancellationToken ct)
        {
            OnUpdateStarted?.Invoke(this, new UpdateEventArgs(0));

            var champions = await GetChampions(ct);
            var summonerSpells = await GetSummonerSpells(ct);

            using (var wc = new WebClient())
            {
                foreach (var champion in champions)
                {
                    var url = string.Format(cChampionImageUrl, version, champion.Id);
                    var imgPath = Path.Combine(_championImageFolder, $"{champion.Id}.png");
                    await wc.DownloadFileTaskAsync(url, imgPath);

                    _progress++;
                    OnUpdateProgress?.Invoke(this, new UpdateEventArgs((int)((double)_progress / _total * 100)));

                    foreach (var spell in champion.Spells)
                    {
                        url = string.Format(cSpellImageUrl, version, spell.Id);
                        imgPath = Path.Combine(_spellImageFolder, $"{spell.Id}.png");
                        await wc.DownloadFileTaskAsync(url, imgPath);

                        _progress++;
                        OnUpdateProgress?.Invoke(this, new UpdateEventArgs((int)((double)_progress / _total * 100)));
                    }
                }

                foreach (var summonerSpell in summonerSpells)
                {
                    var url = string.Format(cSpellImageUrl, version, summonerSpell.Id);
                    var imgPath = Path.Combine(_summonerSpellImageFolder, $"{summonerSpell.Id}.png");
                    await wc.DownloadFileTaskAsync(url, imgPath);

                    _progress++;
                    OnUpdateProgress?.Invoke(this, new UpdateEventArgs((int)((double)_progress / _total * 100)));
                }
            }

            _data = new LeagueData
            {
                Champions = champions,
                SummonerSpells = summonerSpells,
            };

            var json = JsonConvert.SerializeObject(_data);
            File.WriteAllText(path, json);

            OnUpdateCompleted?.Invoke(this, new UpdateEventArgs(1));
        }

        private async Task<string> GetLatestVersion(CancellationToken ct)
        {
            if (string.IsNullOrEmpty(_latestVersion))
            {
                var versionsJson = await Utilities.GetApiResponse(cVersionsUrl, ct);
                var versions = JsonConvert.DeserializeObject<List<string>>(versionsJson);

                _latestVersion = versions.FirstOrDefault();
            }

            return _latestVersion;
        }

        private async Task<List<Champion>> GetChampions(CancellationToken ct)
        {
            var championList = new List<Champion>();

            var latestVersion = await GetLatestVersion(ct);
            var url = string.Format(cChampionsDataUrl, latestVersion);

            var championsJson = await Utilities.GetApiResponse(url, ct);
            var champions = JsonConvert.DeserializeObject<JObject>(championsJson);
            var data = champions.GetValue("data", StringComparison.OrdinalIgnoreCase);
            var children = data.Children<JProperty>();

            // 6 requests:
            // data, square, q, w, e, r
            _total += children.Count() * 6;

            foreach (var x in children)
            {
                var id = x.Name;
                url = string.Format(cChampionDataUrl, latestVersion, id);

                var championJson = await Utilities.GetApiResponse(url, ct);
                var champion = JsonConvert.DeserializeObject<JObject>(championJson);

                var detailedData = champion.GetValue("data", StringComparison.OrdinalIgnoreCase).First.First;

                var name = detailedData["name"].Value<string>();
                var spells = JsonConvert.DeserializeObject<List<Spell>>(detailedData["spells"].ToString());

                championList.Add(new Champion
                {
                    Id = id,
                    Name = name,
                    Spells = spells,
                });

                _progress++;
                OnUpdateProgress?.Invoke(this, new UpdateEventArgs((int)((double)_progress / _total * 100)));
            }

            return championList;
        }

        private async Task<List<Spell>> GetSummonerSpells(CancellationToken ct)
        {
            var summonerSpellList = new List<Spell>();

            var latestVersion = await GetLatestVersion(ct);
            var url = string.Format(cSummonerSpellDataUrl, latestVersion);

            var summonerSpellsJson = await Utilities.GetApiResponse(url, ct);
            var summonerSpells = JsonConvert.DeserializeObject<JObject>(summonerSpellsJson);
            var data = summonerSpells.GetValue("data", StringComparison.OrdinalIgnoreCase);
            var children = data.Children<JProperty>().Select(x => x.First);

            // 1 request:
            // image
            _total += children.Count();

            foreach (var summonerSpell in children)
            {
                var spell = JsonConvert.DeserializeObject<Spell>(summonerSpell.ToString());
                summonerSpellList.Add(spell);
            }

            return summonerSpellList;
        }

        private async Task<List<SummonerData>> GetParticipants(CancellationToken ct)
        {
            try
            {
                var url = cInGameApiBaseUrl + "/playerlist";
                var json = await Utilities.GetApiResponse(url, ct);
                _participants = JsonConvert.DeserializeObject<List<SummonerData>>(json);
            }
            catch
            { }

            return _participants;
        }

        private async Task<string> GetActivePlayer(CancellationToken ct)
        {
            if (string.IsNullOrEmpty(_activePlayerName))
            {
                var url = cInGameApiBaseUrl + "/activeplayername";
                var name = await Utilities.GetApiResponse(url, ct);

                //Remove " from beginning and end
                _activePlayerName = name.Substring(1, name.Length - 2);
            }

            return _activePlayerName;
        }

        #endregion
    }
}
