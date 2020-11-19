using BarRaider.SdTools;
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

        public static event EventHandler<UpdateEventArgs> OnUpdateStarted;
        public static event EventHandler<UpdateEventArgs> OnUpdateCompleted;
        public static event EventHandler<UpdateEventArgs> OnUpdateProgress;

        #endregion

        #region vars

        private static LeagueInfo _instance;

        private int _progress = 0;
        private int _total = 0;

        private string _activePlayerName;

        private LeagueData _data;
        private string _latestVersion;

        private const string cInGameApiBaseUrl = "https://127.0.0.1:2999/liveclientdata";
        private const string cVersionsUrl = "https://ddragon.bangingheads.net/api/versions.json";
        private const string cChampionsDataUrl = "https://ddragon.bangingheads.net/cdn/{0}/data/en_US/champion.json";
        private const string cChampionDataUrl = "https://ddragon.bangingheads.net/cdn/{0}/data/en_US/champion/{1}.json";
        private const string cChampionImageUrl = "https://ddragon.bangingheads.net/cdn/{0}/img/champion/{1}.png";
        private const string cSummonerSpellDataUrl = "https://ddragon.bangingheads.net/cdn/{0}/data/en_US/summoner.json";
        private const string cSpellImageUrl = "https://ddragon.bangingheads.net/cdn/{0}/img/spell/{1}.png";
        private const string cItemDataUrl = "https://ddragon.bangingheads.net/cdn/{0}/data/en_US/item.json";

        private readonly string _championImageFolder = Path.Combine(Environment.CurrentDirectory, "Images", "Champions");
        private readonly string _spellImageFolder = Path.Combine(Environment.CurrentDirectory, "Images", "Spells");
        private readonly string _summonerSpellImageFolder = Path.Combine(Environment.CurrentDirectory, "Images", "SummonerSpells");
        private readonly string _leagueDataFolder = Path.Combine(Environment.CurrentDirectory, "LeagueData");
        private readonly string _missingChampionImagePath = Path.Combine(Environment.CurrentDirectory, "Images", "Champions", "Missing.png");
        private readonly string _missingSpellImagePath = Path.Combine(Environment.CurrentDirectory, "Images", "Spells", "Missing.png");
        private readonly string _missingSummonerSpellImagePath = Path.Combine(Environment.CurrentDirectory, "Images", "SummonerSpells", "Missing.png");

        public bool Updating { get; private set; }

        #endregion

        #region ctor

        private LeagueInfo(CancellationToken ct)
        {
            Logger.Instance.LogMessage(TracingLevel.DEBUG, "LeagueInfo - Constructor");

            OnUpdateStarted?.Invoke(this, new UpdateEventArgs(0));
            Updating = true;

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

                Updating = false;
                OnUpdateCompleted?.Invoke(this, new UpdateEventArgs(1));
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

        public double GetSpellCooldown(Spell spell, SummonerData participant)
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

            double abilityHaste = 0;
            foreach (var itemId in participant.Items.Select(x => x.Id))
            {
                var item = GetItem(itemId);
                abilityHaste += item.AbilityHaste;
            }

            // TODO: check for runes

            return cooldown / (1 + (abilityHaste / 100));
        }

        public double GetSummonerSpellCooldown(Spell spell, SummonerData participant, bool isAram)
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

            // check for Ionian Boots of Lucidity
            if (participant.Items.Any(x => x.Id == 3158))
                summonerSpellHaste += 10;

            // TODO: check for runes

            return cooldown / (1 + (summonerSpellHaste / 100));
        }

        public async Task<SummonerData> GetParticipant(int index, CancellationToken ct)
        {
            Logger.Instance.LogMessage(TracingLevel.DEBUG, "GetParticipant - initiated");

            var participants = await GetParticipants(ct);
            if (participants == null)
                return null;

            var activePlayerName = await GetActivePlayer(ct);

            var activePlayer = participants.FirstOrDefault(x => x.SummonerName == activePlayerName);
            if (activePlayer == null)
                return null;

            var team = activePlayer.Team;
            var enemyTeamParticipants = participants.Where(x => x.Team != team);
            var participant = enemyTeamParticipants?.ElementAtOrDefault(index);

            Logger.Instance.LogMessage(TracingLevel.DEBUG, $"Participant Found - {participant != null}");

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
            var champion = _data.Champions.FirstOrDefault(x => x.Name == championName);
            if (champion == null)
            {
                champion = Champion.Default;
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"Champion not found: {championName}");
            }

            return champion;
        }

        public Image GetChampionImage(string championName)
        {
            var path = Path.Combine(_championImageFolder, $"{championName}.png");

            if (!File.Exists(path))
                path = _missingChampionImagePath;

            return Image.FromFile(path);
        }

        public Spell GetSummonerSpell(string spellName)
        {
            var spell = _data.SummonerSpells.FirstOrDefault(x => x.Name == spellName);
            if (spell == null)
            {
                spell = Spell.Default;
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"Spell not found: {spellName}");
            }

            return spell;
        }

        public Image GetSummonerSpellImage(string spellName)
        {
            var path = Path.Combine(_summonerSpellImageFolder, $"{spellName}.png");

            if (!File.Exists(path))
                path = _missingSummonerSpellImagePath;

            return Image.FromFile(path);
        }

        public Spell GetSpell(SummonerData participant, ESpell spellId)
        {
            Spell spell;
            switch (spellId)
            {
                case ESpell.Q:
                case ESpell.W:
                case ESpell.E:
                case ESpell.R:
                    var champion = GetChampion(participant.ChampionName);
                    spell = champion.Spells.ElementAtOrDefault((int)spellId);
                    break;

                case ESpell.SummonerSpell1:
                case ESpell.SummonerSpell2:
                    var spellName = participant.GetSummonerSpell(spellId);
                    spell = GetSummonerSpell(spellName);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(spellId));
            }

            if (spell == null)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"Spell not found: {participant.ChampionName} - {spellId}");
                return Spell.Default;
            }

            return spell;
        }

        public Image GetSpellImage(string spellName)
        {
            var path = Path.Combine(_spellImageFolder, $"{spellName}.png");

            if (!File.Exists(path))
                path = _missingSpellImagePath;

            return Image.FromFile(path);
        }

        public Item GetItem(int itemId)
        {
            var item = _data.Items.FirstOrDefault(x => x.Id == itemId);
            if (item == null)
            {
                item = Item.Default;
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"Item not found: {itemId}");
            }

            return item;
        }

        #endregion

        #region Private Methods

        private async Task UpdateData(string version, string path, CancellationToken ct)
        {
            Logger.Instance.LogMessage(TracingLevel.DEBUG, "UpdateData - initiated");

            var champions = await GetChampions(ct);
            var summonerSpells = await GetSummonerSpells(ct);
            var items = await GetItems(ct);

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
                Items = items,
            };

            var json = JsonConvert.SerializeObject(_data);
            File.WriteAllText(path, json);

            Logger.Instance.LogMessage(TracingLevel.DEBUG, "UpdateData - completed");
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

        private async Task<List<Item>> GetItems(CancellationToken ct)
        {
            var itemList = new List<Item>();

            var latestVersion = await GetLatestVersion(ct);
            var url = string.Format(cItemDataUrl, latestVersion);

            var itemJson = await Utilities.GetApiResponse(url, ct);
            var items = JsonConvert.DeserializeObject<JObject>(itemJson);
            var data = items.GetValue("data", StringComparison.OrdinalIgnoreCase);
            var children = data.Children<JProperty>();

            foreach (var child in children)
            {
                if (!int.TryParse(child.Name, out int id))
                    continue;

                var item = child.First;

                var name = item["name"].Value<string>();

                var stats = item["stats"];
                var modifier = stats.Cast<JProperty>().FirstOrDefault(x => x.Name == "AbilityHasteMod");

                var abilityHaste = 0d;
                if (modifier != null)
                    abilityHaste = modifier.First.Value<double>();

                itemList.Add(new Item
                {
                    Id = id,
                    Name = name,
                    AbilityHaste = abilityHaste
                });
            }

            return itemList;
        }

        private async Task<List<SummonerData>> GetParticipants(CancellationToken ct)
        {
            List<SummonerData> participants = null;
            while (participants == null || participants.Count == 0)
            {
                try
                {
                    var url = cInGameApiBaseUrl + "/playerlist";
                    var json = await Utilities.GetApiResponse(url, ct);
                    participants = JsonConvert.DeserializeObject<List<SummonerData>>(json);
                }
                catch
                {
                    await Task.Delay(500, ct);
                }
            }

            return participants;
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
