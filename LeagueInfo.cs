using BarRaider.SdTools;
using LeagueDeck.ApiResponse;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
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
        private const string cItemImageUrl = "https://ddragon.bangingheads.net/cdn/{0}/img/item/{1}.png";

        private readonly string _championImageFolder = Path.Combine(Environment.CurrentDirectory, "Images", "Champions");
        private readonly string _spellImageFolder = Path.Combine(Environment.CurrentDirectory, "Images", "Spells");
        private readonly string _summonerSpellImageFolder = Path.Combine(Environment.CurrentDirectory, "Images", "SummonerSpells");
        private readonly string _itemImageFolder = Path.Combine(Environment.CurrentDirectory, "Images", "Items");
        private readonly string _leagueDataFolder = Path.Combine(Environment.CurrentDirectory, "LeagueData");
        private readonly string _missingChampionImageId = "MissingChampion";
        private readonly string _missingSpellImageId = "MissingSpell";
        private readonly string _missingSummonerSpellImageId = "MissingSummonerSpell";
        private readonly string _missingItemImageId = "MissingItem";

        private Dictionary<ESummoner, Dictionary<ESpell, Spell>> SummonerToSpells = new Dictionary<ESummoner, Dictionary<ESpell, Spell>>();
        private Dictionary<ESummoner, Champion> SummonerToChampion = new Dictionary<ESummoner, Champion>();
        private Dictionary<string, Image> IdToImage = new Dictionary<string, Image>();

        public Task UpdateTask { get; private set; }
        public Task LoadGameDataTask { get; private set; }

        public List<Champion> GetChampions() => _data.Champions;
        public List<Spell> GetSummonerSpells() => _data.SummonerSpells;
        public List<Item> GetItems() => _data.Items;

        #endregion

        #region ctor

        private LeagueInfo(CancellationToken ct)
        {
            UpdateTask = Task.Run(async () =>
            {
                OnUpdateStarted?.Invoke(this, new UpdateEventArgs(0));

                var version = await GetLatestVersion(ct);

                Directory.CreateDirectory(_leagueDataFolder);
                var path = Path.Combine(_leagueDataFolder, $"{version}.json");

                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    _data = JsonConvert.DeserializeObject<LeagueData>(json);

                    if (_data.LeagueDeckVersion == null || _data.LeagueDeckVersion < GetLeagueDeckVersion())
                    {
                        Logger.Instance.LogMessage(TracingLevel.DEBUG, "LeagueDeckVersionUpdated");
                        await UpdateData(version, path, ct).ContinueWith(x => OnUpdateCompleted?.Invoke(this, new UpdateEventArgs(1)));
                    }
                }
                else
                {
                    await UpdateData(version, path, ct).ContinueWith(x => OnUpdateCompleted?.Invoke(this, new UpdateEventArgs(1)));
                }

            });

            Logger.Instance.LogMessage(TracingLevel.DEBUG, "LeagueInfo - Constructor");
        }

        private Version GetLeagueDeckVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            return new Version(fvi.FileVersion);
        }

        #endregion

        #region Public Methods

        public static LeagueInfo GetInstance(CancellationToken ct)
        {
            if (_instance == null)
                _instance = new LeagueInfo(ct);

            return _instance;
        }

        public async Task LoadGameData(CancellationToken ct)
        {
            if (LoadGameDataTask == null)
            {
                LoadGameDataTask = Task.Run(async () =>
                {
                    await UpdateTask;

                    Logger.Instance.LogMessage(TracingLevel.DEBUG, "Loading Game Data -  initiated");

                    var participants = await GetParticipants(ct);
                    var activePlayerName = await GetActivePlayerName(ct);

                    var activePlayer = participants.FirstOrDefault(x => x.SummonerName == activePlayerName);
                    if (activePlayer == null)
                        return;

                    var team = activePlayer.Team;
                    var enemies = participants.Where(x => x.Team != team).ToList();

                    for (int i = 0; i < enemies.Count(); i++)
                    {
                        var enemy = enemies[i];

                        var champion = GetChampion(enemy.ChampionName);
                        SummonerToChampion[(ESummoner)i] = champion;
                        IdToImage[champion.Id] = GetChampionImage(champion.Id);

                        var spellDict = new Dictionary<ESpell, Spell>();

                        var summonerSpell1 = GetSummonerSpell(enemy.SummonerSpells.Spell1.Id);
                        spellDict[ESpell.SummonerSpell1] = summonerSpell1;
                        if (!IdToImage.ContainsKey(summonerSpell1.Id))
                            IdToImage[summonerSpell1.Id] = GetSummonerSpellImage(summonerSpell1.Id);

                        var summonerSpell2 = GetSummonerSpell(enemy.SummonerSpells.Spell2.Id);
                        spellDict[ESpell.SummonerSpell2] = summonerSpell2;
                        if (!IdToImage.ContainsKey(summonerSpell2.Id))
                            IdToImage[summonerSpell2.Id] = GetSummonerSpellImage(summonerSpell2.Id);

                        var spells = champion.Spells;

                        var q = spells[0];
                        spellDict[ESpell.Q] = q;
                        if (!IdToImage.ContainsKey(q.Id))
                            IdToImage[q.Id] = GetSpellImage(q.Id);

                        var w = spells[1];
                        spellDict[ESpell.W] = w;
                        if (!IdToImage.ContainsKey(w.Id))
                            IdToImage[w.Id] = GetSpellImage(w.Id);

                        var e = spells[2];
                        spellDict[ESpell.E] = e;
                        if (!IdToImage.ContainsKey(e.Id))
                            IdToImage[e.Id] = GetSpellImage(e.Id);

                        var r = spells[3];
                        spellDict[ESpell.R] = r;
                        if (!IdToImage.ContainsKey(r.Id))
                            IdToImage[r.Id] = GetSpellImage(r.Id);

                        SummonerToSpells[(ESummoner)i] = spellDict;
                    }

                    Logger.Instance.LogMessage(TracingLevel.DEBUG, "Loading Game Data - completed");
                });
            }

            await LoadGameDataTask;
        }

        public void ClearGameData()
        {
            if (LoadGameDataTask != null)
            {
                LoadGameDataTask = null;
                _activePlayerName = null;
                SummonerToChampion.Clear();
                SummonerToSpells.Clear();
                IdToImage.Clear();
            }
        }

        public double GetSpellCooldown(Spell spell, Summoner participant)
        {
            // assuming the player always levels when possible
            // TODO: check for special level behaviour(Elise, Jayce, Karma, ...)
            double cooldown = 0;
            if (spell.MaxRank >= 1 && participant.Level < 3)
                cooldown = spell.Cooldown[0];
            else if (spell.MaxRank >= 2 && participant.Level < 5)
                cooldown = spell.Cooldown[1];
            else if (spell.MaxRank >= 3 && participant.Level < 7)
                cooldown = spell.Cooldown[2];
            else if (spell.MaxRank >= 4 && participant.Level < 9)
                cooldown = spell.Cooldown[3];
            else if (spell.MaxRank >= 5 && participant.Level < 11)
                cooldown = spell.Cooldown[4];
            else if (spell.MaxRank >= 6)
                cooldown = spell.Cooldown[5];

            double abilityHaste = 0;
            foreach (var itemId in participant.Items.Select(x => x.Id))
            {
                var item = GetItem(itemId);
                abilityHaste += item.AbilityHaste;
            }

            // TODO: check for runes

            return cooldown / (1 + (abilityHaste / 100));
        }

        public double GetUltimateCooldown(Spell spell, Summoner participant, int cloudDrakes = 0)
        {
            // assuming the player always levels when possible
            // TODO: check for special level behaviour(Elise, Jayce, Karma, ...)
            double cooldown = 0;
            if (spell.MaxRank >= 1 && participant.Level < 11)
                cooldown = spell.Cooldown[0];
            else if (spell.MaxRank >= 2 && participant.Level < 16)
                cooldown = spell.Cooldown[1];
            else if (spell.MaxRank >= 3)
                cooldown = spell.Cooldown[2];

            double abilityHaste = 0;

            abilityHaste += cloudDrakes * 10;

            foreach (var itemId in participant.Items.Select(x => x.Id))
            {
                var item = GetItem(itemId);
                abilityHaste += item.AbilityHaste;
            }

            // TODO: check for runes

            return cooldown / (1 + (abilityHaste / 100));
        }

        public double GetSummonerSpellCooldown(Spell spell, Summoner participant, bool isAram)
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

        public async Task<Summoner> GetSummoner(string name, CancellationToken ct)
        {
            var participants = await GetParticipants(ct);
            if (participants == null)
                return null;

            var summoner = participants.FirstOrDefault(x => x.SummonerName == name);

            return summoner;
        }

        public async Task<List<Summoner>> GetEnemies(CancellationToken ct)
        {
            Logger.Instance.LogMessage(TracingLevel.DEBUG, "GetEnemies - initiated");

            var participants = await GetParticipants(ct);
            if (participants == null)
                return null;

            var activePlayerName = await GetActivePlayerName(ct);

            var activePlayer = participants.FirstOrDefault(x => x.SummonerName == activePlayerName);
            if (activePlayer == null)
                return null;

            var team = activePlayer.Team;
            var enemies = participants.Where(x => x.Team != team).ToList();

            Logger.Instance.LogMessage(TracingLevel.DEBUG, "GetEnemies - completed");

            return enemies;
        }

        public async Task<GameData> GetGameData(CancellationToken ct)
        {
            try
            {
                var url = cInGameApiBaseUrl + "/gamestats";
                var response = await Utilities.GetApiResponse(url, ct);
                var gameData = JsonConvert.DeserializeObject<GameData>(response);

                return gameData;
            }
            catch (Exception e)
            {
                Logger.Instance.LogMessage(TracingLevel.DEBUG, "GetGameData - failed to get data:\n" + e.Message);
                return null;
            }
        }

        public async Task<List<GameEvent>> GetEventData(CancellationToken ct)
        {
            try
            {
                var url = cInGameApiBaseUrl + "/eventdata";
                var response = await Utilities.GetApiResponse(url, ct);
                var eventData = JsonConvert.DeserializeObject<JObject>(response);
                var events = eventData["Events"].ToObject<List<GameEvent>>();

                return events;
            }
            catch (Exception e)
            {
                Logger.Instance.LogMessage(TracingLevel.DEBUG, "GetEventData - failed to get data:\n" + e.Message);
                return null;
            }
        }

        public async Task<ActivePlayer> GetActivePlayer(CancellationToken ct)
        {
            try
            {
                var url = cInGameApiBaseUrl + "/activeplayer";
                var response = await Utilities.GetApiResponse(url, ct);
                var activePlayer = JsonConvert.DeserializeObject<ActivePlayer>(response);

                return activePlayer;
            }
            catch(Exception e)
            {
                Logger.Instance.LogMessage(TracingLevel.DEBUG, "GetActivePlayer - failed to get data:\n" + e.Message);
                return null;
            }
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

        internal Champion GetChampion(ESummoner summoner)
        {
            if (!SummonerToChampion.TryGetValue(summoner, out var champion))
            {
                champion = Champion.Default;
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"Champion for Summoner not found: {summoner}");
            }

            return champion;
        }

        public Image GetChampionImage(string championId)
        {
            if (string.IsNullOrWhiteSpace(championId))
                championId = _missingChampionImageId;

            if (!IdToImage.TryGetValue(championId, out var image))
            {
                var path = Path.Combine(_championImageFolder, $"{championId}.png");

                if (!File.Exists(path))
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"GetChampionImage - file not found: {path}");
                    championId = _missingChampionImageId;
                    path = Path.Combine(_championImageFolder, $"{championId}.png");
                }

                image = Image.FromFile(path);
                IdToImage[championId] = image;
            }

            return image;
        }

        public Spell GetSummonerSpell(string spellId)
        {
            var spell = _data.SummonerSpells.FirstOrDefault(x => x.Id == spellId);
            if (spell == null)
            {
                spell = Spell.Default;
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"Spell not found: {spellId}");
            }

            return spell;
        }

        public Image GetSummonerSpellImage(string spellId = null)
        {
            if (string.IsNullOrWhiteSpace(spellId))
                spellId = _missingSummonerSpellImageId;

            if (!IdToImage.TryGetValue(spellId, out var image))
            {
                var path = Path.Combine(_summonerSpellImageFolder, $"{spellId}.png");

                if (!File.Exists(path))
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"GetSummonerSpellImage - file not found: {path}");
                    spellId = _missingSummonerSpellImageId;
                    path = Path.Combine(_summonerSpellImageFolder, $"{spellId}.png");
                }

                image = Image.FromFile(path);
                IdToImage[spellId] = image;
            }

            return image;
        }

        public Spell GetSpell(ESummoner summoner, ESpell spellId)
        {
            if (!SummonerToSpells.TryGetValue(summoner, out var spells))
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"Summoner not found: {summoner} - {spellId}");
                return Spell.Default;
            }

            if (!spells.TryGetValue(spellId, out var spell))
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"Spell not found: {summoner} - {spellId}");
                return Spell.Default;
            }

            return spell;
        }

        public Image GetSpellImage(string spellId)
        {
            if (string.IsNullOrWhiteSpace(spellId))
                spellId = _missingSpellImageId;

            if (!IdToImage.TryGetValue(spellId, out var image))
            {
                var path = Path.Combine(_spellImageFolder, $"{spellId}.png");

                if (!File.Exists(path))
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"GetSpellImage - file not found: {path}");
                    spellId = _missingSpellImageId;
                    path = Path.Combine(_spellImageFolder, $"{spellId}.png");
                }

                image = Image.FromFile(path);
                IdToImage[spellId] = image;
            }

            return image;
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

        public Image GetItemImage(int itemId)
        {
            string id = itemId.ToString();
            if (itemId == -1)
                id = _missingItemImageId;

            if (!IdToImage.TryGetValue(id, out var image))
            {
                var path = Path.Combine(_itemImageFolder, $"{id}.png");

                if (!File.Exists(path))
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"GetItemImage - file not found: {path}");
                    id = _missingItemImageId;
                    path = Path.Combine(_itemImageFolder, $"{id}.png");
                }

                image = Image.FromFile(path);
                IdToImage[id] = image;
            }

            return image;
        }

        #endregion

        #region Private Methods

        private async Task UpdateData(string version, string path, CancellationToken ct)
        {
            Logger.Instance.LogMessage(TracingLevel.DEBUG, "UpdateData - initiated");

            var champions = await GetChampions(ct);
            var summonerSpells = await GetSummonerSpells(ct);
            var items = await GetItems(ct);

            Directory.CreateDirectory(_championImageFolder);
            Directory.CreateDirectory(_spellImageFolder);
            Directory.CreateDirectory(_summonerSpellImageFolder);
            Directory.CreateDirectory(_itemImageFolder);

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

                foreach (var item in items)
                {
                    var url = string.Format(cItemImageUrl, version, item.Id);
                    var imgPath = Path.Combine(_itemImageFolder, $"{item.Id}.png");
                    await wc.DownloadFileTaskAsync(url, imgPath);

                    _progress++;
                    OnUpdateProgress?.Invoke(this, new UpdateEventArgs((int)((double)_progress / _total * 100)));
                }
            }

            var leagueDeckVersion = GetLeagueDeckVersion();

            _data = new LeagueData
            {
                LeagueDeckVersion = leagueDeckVersion,
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

            // 1 request:
            // image
            _total += children.Count();

            foreach (var child in children)
            {
                if (!int.TryParse(child.Name, out int id))
                    continue;

                var item = child.First;

                var name = item["name"].Value<string>();
                var from = item["from"]?.Values<int>();

                var gold = item["gold"];
                var baseCost = gold["base"].Value<int>();
                var totalCost = gold["total"].Value<int>();

                var stats = item["stats"];
                var modifier = stats.Cast<JProperty>().FirstOrDefault(x => x.Name == "AbilityHasteMod");

                var abilityHaste = 0d;
                if (modifier != null)
                    abilityHaste = modifier.First.Value<double>();

                itemList.Add(new Item
                {
                    Id = id,
                    Name = name,
                    AbilityHaste = abilityHaste,
                    ComponentIds = from?.ToList() ?? new List<int>(),
                    BaseCost = baseCost,
                    TotalCost = totalCost,
                });
            }

            return itemList;
        }

        private async Task<List<Summoner>> GetParticipants(CancellationToken ct)
        {
            List<Summoner> participants = null;
            while (participants == null || participants.Count == 0)
            {
                try
                {
                    var url = cInGameApiBaseUrl + "/playerlist";
                    var json = await Utilities.GetApiResponse(url, ct);
                    participants = JsonConvert.DeserializeObject<List<Summoner>>(json);
                }
                catch
                {
                    await Task.Delay(500, ct);
                }
            }

            return participants;
        }

        private async Task<string> GetActivePlayerName(CancellationToken ct)
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
