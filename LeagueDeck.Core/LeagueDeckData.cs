using LeagueDeck.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LeagueDeck.Core
{
    public class PlayerEventArgs : EventArgs
    {
        public int Index { get; set; }
        public Player Player { get; }

        public PlayerEventArgs(int index, Player player)
        {
            Index = index;
            Player = player;
        }
    }

    public class LeagueDeckData
    {
        #region vars

        private const string cConfigFileName = "config.json";

        private readonly string _leagueDeckDataFolder = Path.Combine(Environment.CurrentDirectory, "LeagueDeckData");

        private static LeagueDeckData _instance;

        private CancellationTokenSource _updateGameDataCts;

        public IReadOnlyDictionary<string, Image> IdToImage => _idToImage;
        private Dictionary<string, Image> _idToImage = new Dictionary<string, Image>();

        public IReadOnlyDictionary<int, Player> IndexToEnemyPlayer => _indexToEnemyPlayer;
        private Dictionary<int, Player> _indexToEnemyPlayer = new Dictionary<int, Player>();

        private Dictionary<string, Player> _summonerNameToPlayer = new Dictionary<string, Player>();

        public Task UpdateTask { get; private set; }
        public Task UpdatePlayerListTask { get; private set; }
        public Task LoadGameDataTask { get; private set; }

        public ChampionAssetController ChampionAssetController { get; private set; }
        public SpellAssetController SpellAssetController { get; private set; }
        public ItemAssetController ItemAssetController { get; private set; }

        public event EventHandler<PlayerEventArgs> OnTabOrderChanged;

        #endregion

        #region ctor

        private LeagueDeckData(CancellationToken ct)
        {
            Task.Run(async () => await LoadData(ct));
        }

        public static LeagueDeckData GetInstance(CancellationToken ct)
        {
            if (_instance == null)
                _instance = new LeagueDeckData(ct);

            return _instance;
        }

        #endregion

        #region Public Methods

        public void StartGameService(CancellationToken ct)
        {
            if (_updateGameDataCts != null)
                return;

            _updateGameDataCts = new CancellationTokenSource();
            Task.Run(async () =>
            {
                while (!_updateGameDataCts.IsCancellationRequested && !ct.IsCancellationRequested)
                {
                    await UpdatePlayerList(ct);
                    await Task.Delay(200, ct);
                }
            });
        }

        public async Task UpdatePlayerList(CancellationToken ct)
        {
            if (UpdatePlayerListTask == null)
            {
                UpdatePlayerListTask = Task.Run(async () =>
                {
                    await LoadData(ct);

                    var enemies = await LiveClientApiController.GetEnemies(ct);
                    if (enemies == null)
                        return;

                    for (int i = 0; i < enemies.Count; i++)
                    {
                        if (!_summonerNameToPlayer.TryGetValue(enemies[i].SummonerName, out var newEnemy))
                        {
                            newEnemy = CreatePlayer(enemies[i]);
                            _summonerNameToPlayer[enemies[i].SummonerName] = newEnemy;
                        }

                        if (_indexToEnemyPlayer.TryGetValue(i, out var enemy))
                        {
                            if (enemy.Name != newEnemy.Name)
                            {
                                var args = new PlayerEventArgs(i, newEnemy);
                                OnTabOrderChanged?.Invoke(this, args);
                            }
                        }
                        _indexToEnemyPlayer[i] = newEnemy;
                    }
                });
            }

            await UpdatePlayerListTask;
            UpdatePlayerListTask = null;
        }

        public void ResetGameService()
        {
            if (_updateGameDataCts != null)
            {
                _updateGameDataCts.Cancel();
                _updateGameDataCts = null;
            }

            UpdatePlayerListTask = null;
            _indexToEnemyPlayer.Clear();
            _summonerNameToPlayer.Clear();
            _idToImage.Clear();
        }

        public async Task LoadData(CancellationToken ct, bool forceUpdate = false)
        {
            if (UpdateTask == null)
            {
                UpdateTask = Task.Run(async () =>
                {
                    ChampionAssetController = new ChampionAssetController();
                    SpellAssetController = new SpellAssetController();
                    ItemAssetController = new ItemAssetController();

                    var assetLoader = new AssetLoader();
                    assetLoader.Add(ChampionAssetController);
                    assetLoader.Add(SpellAssetController);
                    assetLoader.Add(ItemAssetController);

                    Directory.CreateDirectory(_leagueDeckDataFolder);
                    var path = Path.Combine(_leagueDeckDataFolder, cConfigFileName);

                    LeagueDeckConfig config;
                    if (!File.Exists(path))
                    {
                        config = LeagueDeckConfig.CreateDefaultConfig();
                        var json = JsonConvert.SerializeObject(config);
                        File.WriteAllText(path, json);
                    }
                    else
                    {
                        var json = File.ReadAllText(path);
                        config = JsonConvert.DeserializeObject<LeagueDeckConfig>(json);
                    }

                    forceUpdate |= (config.LeagueDeckVersion == null || config.LeagueDeckVersion < Utilities.GetLeagueDeckVersion());
                    await assetLoader.UpdateData(ct, force: forceUpdate);

                    await assetLoader.LoadData(ct);
                }, ct);
            }

            await UpdateTask;
        }

        private Player CreatePlayer(Models.Api.Participant participant)
        {
            var champion = ChampionAssetController.GetChampion(participant.ChampionName);
            _idToImage[champion.Id] = ChampionAssetController.GetImage(champion.Id);

            var spellDict = new Dictionary<ESpell, Spell>();

            var summonerSpell1 = SpellAssetController.GetAsset(participant.SummonerSpells.Spell1.Id);
            spellDict[ESpell.SummonerSpell1] = summonerSpell1;
            if (!IdToImage.ContainsKey(summonerSpell1.Id))
                _idToImage[summonerSpell1.Id] = SpellAssetController.GetImage(summonerSpell1.Id);

            var summonerSpell2 = SpellAssetController.GetAsset(participant.SummonerSpells.Spell2.Id);
            spellDict[ESpell.SummonerSpell2] = summonerSpell2;
            if (!IdToImage.ContainsKey(summonerSpell2.Id))
                _idToImage[summonerSpell2.Id] = SpellAssetController.GetImage(summonerSpell2.Id);

            var spells = champion.Spells;

            var q = spells[0];
            spellDict[ESpell.Q] = q;
            if (!IdToImage.ContainsKey(q.Id))
                _idToImage[q.Id] = SpellAssetController.GetImage(q.Id);

            var w = spells[1];
            spellDict[ESpell.W] = w;
            if (!IdToImage.ContainsKey(w.Id))
                _idToImage[w.Id] = SpellAssetController.GetImage(w.Id);

            var e = spells[2];
            spellDict[ESpell.E] = e;
            if (!IdToImage.ContainsKey(e.Id))
                _idToImage[e.Id] = SpellAssetController.GetImage(e.Id);

            var r = spells[3];
            spellDict[ESpell.R] = r;
            if (!IdToImage.ContainsKey(r.Id))
                _idToImage[r.Id] = SpellAssetController.GetImage(r.Id);

            var player = new Player(participant.SummonerName, champion, spellDict);
            return player;
        }

        #region Cooldowns

        public double GetSpellCooldown(Spell spell, Models.Api.Participant participant)
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
                var item = ItemAssetController.GetAsset(itemId.ToString());
                abilityHaste += item.AbilityHaste;
            }

            // TODO: check for runes

            return cooldown / (1 + (abilityHaste / 100));
        }

        public double GetUltimateCooldown(Spell spell, Models.Api.Participant participant, int cloudDrakes = 0)
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
                var item = ItemAssetController.GetAsset(itemId.ToString());
                abilityHaste += item.AbilityHaste;
            }

            // TODO: check for runes

            return cooldown / (1 + (abilityHaste / 100));
        }

        public double GetSummonerSpellCooldown(Spell spell, Models.Api.Participant participant, bool isAram)
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

        #endregion

        #endregion
    }
}
