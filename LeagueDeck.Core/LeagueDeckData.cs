using LeagueDeck.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace LeagueDeck.Core
{
    public class LeagueDeckData
    {
        #region vars

        private const string cConfigFileName = "config.json";

        private readonly string _leagueDeckDataFolder = Path.Combine(Environment.CurrentDirectory, "LeagueDeckData");

        private static LeagueDeckData _instance;

        public IReadOnlyDictionary<string, Image> IdToImage => _idToImage;
        private Dictionary<string, Image> _idToImage = new Dictionary<string, Image>();

        public IReadOnlyDictionary<string, Player> SummonerNameToPlayer => _summonerNameToPlayer;
        private Dictionary<string, Player> _summonerNameToPlayer = new Dictionary<string, Player>();

        public Task UpdateTask { get; private set; }
        public Task LoadGameDataTask { get; private set; }

        public ChampionAssetController ChampionAssetController { get; private set; }
        public SpellAssetController SpellAssetController { get; private set; }
        public ItemAssetController ItemAssetController { get; private set; }

        #endregion

        #region ctor

        private LeagueDeckData(CancellationToken ct)
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

                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    var config = JsonConvert.DeserializeObject<LeagueDeckConfig>(json);

                    if (config.LeagueDeckVersion == null || config.LeagueDeckVersion < GetLeagueDeckVersion())
                    {
                        await assetLoader.UpdateData(ct, force: true);
                    }
                }
                else
                {
                    await assetLoader.UpdateData(ct);
                }

                await assetLoader.LoadData(ct);
            }, ct);
        }

        public static LeagueDeckData GetInstance(CancellationToken ct)
        {
            if (_instance == null)
                _instance = new LeagueDeckData(ct);

            return _instance;
        }

        #endregion

        #region Private Methods

        private Version GetLeagueDeckVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            return new Version(fvi.FileVersion);
        }

        #endregion

        #region Public Methods

        public async Task LoadGameData(CancellationToken ct)
        {
            if (LoadGameDataTask == null)
            {
                LoadGameDataTask = Task.Run(async () =>
                {
                    await UpdateTask;

                    var participants = await ApiController.GetParticipants(ct);

                    foreach(var participant in participants)
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

                        var player = new Player(champion, spellDict);
                        if (!SummonerNameToPlayer.ContainsKey(participant.SummonerName))
                            _summonerNameToPlayer[participant.SummonerName] = player;
                    }
                });
            }

            await LoadGameDataTask;
        }

        public void ClearGameData()
        {
            if (LoadGameDataTask != null)
            {
                LoadGameDataTask = null;
                _summonerNameToPlayer.Clear();
                _idToImage.Clear();
            }
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
