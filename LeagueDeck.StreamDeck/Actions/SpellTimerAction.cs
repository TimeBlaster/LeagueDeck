using BarRaider.SdTools;
using LeagueDeck.Core;
using LeagueDeck.Models;
using LeagueDeck.Settings;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LeagueDeck
{
    [PluginActionId("dev.timeblaster.leaguedeck.spelltimer")]
    public class SpellTimerAction : PluginBase
    {
        #region vars

        private const string cAram = "ARAM";
        private const string cCloudDrake = "Air";

        private const int cResetTimerKeypressLength = 500;

        private CancellationTokenSource _cts = new CancellationTokenSource();

        private SpellTimerSettings _settings;

        private LeagueDeckData _info;
        private bool _isInGame;

        private DateTime _keyPressStart;
        private DateTime _endTime;
        private bool _keyPressed = false;
        private bool _longPress = false;
        private bool _isTimerEnabled;
        private bool _first;

        #endregion

        #region ctor

        public SpellTimerAction(SDConnection connection, InitialPayload payload)
            : base(connection, payload)
        {
            Logger.Instance.LogMessage(TracingLevel.DEBUG, $"Constructor called");

            var updateProgressReporter = UpdateProgressReporter.GetInstance();
            updateProgressReporter.OnUpdateStarted += LeagueInfo_OnUpdateStarted;
            updateProgressReporter.OnUpdateProgress += LeagueInfo_OnUpdateProgress;
            updateProgressReporter.OnUpdateCompleted += LeagueInfo_OnUpdateCompleted;

            _info = LeagueDeckData.GetInstance(_cts.Token);
            _info.OnTabOrderChanged += LeagueDeckdata_OnTabOrderChanged;

            Connection.OnApplicationDidLaunch += Connection_OnApplicationDidLaunch;
            Connection.OnApplicationDidTerminate += Connection_OnApplicationDidTerminate;

            if (payload.Settings == null || payload.Settings.Count == 0)
                this._settings = SpellTimerSettings.CreateDefaultSettings();
            else
                this._settings = payload.Settings.ToObject<SpellTimerSettings>();

            if (_info.UpdateTask != null)
            {
                Task.Run(async () =>
                {
                    var image = Utilities.GetUpdateImage();
                    await Connection.SetImageAsync(image);

                    await _info.UpdateTask;

                    await Connection.SetDefaultImageAsync();
                    await Connection.SetTitleAsync(string.Empty);
                });
            }
        }

        private async void LeagueDeckdata_OnTabOrderChanged(object sender, PlayerEventArgs e)
        {
            if (e.Index != (int)_settings.Summoner)
                return;

            if (!e.Player.SpellToTimerEnd.TryGetValue(_settings.Spell, out var timerEnd))
            {
                // TODO log
                return;
            }

            _endTime = timerEnd;

            var diff = _endTime - DateTime.Now;
            if (diff.TotalSeconds <= 0)
            {
                _isTimerEnabled = false;
                await Connection.SetTitleAsync(string.Empty);
            }
            else
            {
                _isTimerEnabled = true;
                string title = _settings.ShowMinutesAndSeconds ? $"{diff.Minutes}:{diff.Seconds:00}" : $"{diff.TotalSeconds:F0}";
                await Connection.SetTitleAsync(title);
            }

            await UpdateSpellImage(e.Player);
        }

        #endregion

        #region Events

        private async void Connection_OnApplicationDidLaunch(object sender, BarRaider.SdTools.Wrappers.SDEventReceivedEventArgs<BarRaider.SdTools.Events.ApplicationDidLaunch> e)
        {
            if (e.Event.Payload.Application != Utilities.cLeagueOfLegendsProcessName)
                return;

            Logger.Instance.LogMessage(TracingLevel.DEBUG, "GameStarted");

            await Task.WhenAll(new[] { _info.UpdateTask, _info.UpdatePlayerList(_cts.Token) })
                .ContinueWith(async (x) =>
                {
                    _isInGame = true;
                    _info.StartGameService(_cts.Token);
                    await UpdateSpellImage();
                });
        }

        private async void Connection_OnApplicationDidTerminate(object sender, BarRaider.SdTools.Wrappers.SDEventReceivedEventArgs<BarRaider.SdTools.Events.ApplicationDidTerminate> e)
        {
            if (e.Event.Payload.Application != Utilities.cLeagueOfLegendsProcessName)
                return;

            _isInGame = false;

            await ResetTimer();

            _info.ResetGameService();

            _cts.Cancel();
            _cts = new CancellationTokenSource();

            await Connection.SetDefaultImageAsync();
        }

        private async void LeagueInfo_OnUpdateStarted(object sender, ProgressEventArgs e)
        {
            var image = Utilities.GetUpdateImage();
            await Connection.SetImageAsync(image);
        }

        private async void LeagueInfo_OnUpdateProgress(object sender, ProgressEventArgs e)
        {
            await Connection.SetTitleAsync($"{(int)e.Percentage}%");
        }

        private async void LeagueInfo_OnUpdateCompleted(object sender, ProgressEventArgs e)
        {
            await Connection.SetDefaultImageAsync();
            await Connection.SetTitleAsync(string.Empty);
        }

        #endregion

        #region Overrides

        public override async void KeyPressed(KeyPayload payload)
        {
            if (!_isInGame)
                return;

            _keyPressed = true;
            _keyPressStart = DateTime.Now;

            if (_isTimerEnabled)
                return;

            if (!_info.IndexToEnemyPlayer.TryGetValue((int)_settings.Summoner, out var player))
            {
                Debug.WriteLine($"Player not found: {_settings.Summoner}");
                return;
            }

            var participant = await LiveClientApiController.GetParticipant(player.Name, _cts.Token);
            if (participant == null)
                return;

            if (!player.ESpellToSpell.TryGetValue(_settings.Spell, out var spell))
            {
                Debug.WriteLine($"Spell not found: {player}.{_settings.Spell}");
                return;
            }

            double cooldown;
            switch (_settings.Spell)
            {
                case ESpell.Q:
                case ESpell.W:
                case ESpell.E:
                    cooldown = _info.GetSpellCooldown(spell, participant);
                    break;

                case ESpell.R:
                    var enemies = await LiveClientApiController.GetEnemies(_cts.Token);
                    if (enemies == null || enemies.Count - 1 < (int)_settings.Summoner)
                        return;

                    var enemySummonerNames = enemies.Select(y => y.SummonerName);

                    var events = await LiveClientApiController.GetEventData(_cts.Token);
                    var cloudDrakes = events.Events
                        .Where(x => x.Type == Models.Api.EEventType.DragonKill && x.DragonType == cCloudDrake)
                        .Count(x => enemySummonerNames.Contains(x.KillerName));

                    cooldown = _info.GetUltimateCooldown(spell, participant, cloudDrakes);
                    break;

                case ESpell.SummonerSpell1:
                case ESpell.SummonerSpell2:
                    var gameData = await LiveClientApiController.GetGameData(_cts.Token, DateTime.MinValue);
                    var isAram = gameData.GameMode.Equals(cAram);

                    cooldown = _info.GetSummonerSpellCooldown(spell, participant, isAram);
                    break;

                default:
                    Logger.Instance.LogMessage(TracingLevel.DEBUG, $"KeyPressed - out of range: {_settings.Spell}");
                    throw new ArgumentOutOfRangeException(nameof(_settings.Spell));
            }

            if (cooldown - _settings.Offset <= 0)
                return;

            _first = true;
            _isTimerEnabled = true;

            _endTime = _keyPressStart.AddSeconds(cooldown - _settings.Offset);
            player.SpellToTimerEnd[_settings.Spell] = _endTime;

            await UpdateSpellImage(spell, player.Champion);
        }

        public async override void KeyReleased(KeyPayload payload)
        {
            if (_longPress)
            {
                await ResetTimer();
            }
            else
            {
                if (_isTimerEnabled && _first && _settings.SendMessageInChat && !Utilities.InputRunning)
                {
                    _first = false;
                    await SendMessageInChat(EChatFormat.GameTime);
                }
                else if (_isTimerEnabled && _settings.SendMessageInChat && !Utilities.InputRunning)
                    await SendMessageInChat(_settings.ChatFormat);
            }

            _longPress = false;
            _keyPressed = false;
        }

        public override async void OnTick()
        {
            if (_isTimerEnabled)
            {
                var diff = _endTime - DateTime.Now;

                if (diff.TotalSeconds <= 0)
                {
                    await ResetTimer();
                }
                else
                {
                    string title = _settings.ShowMinutesAndSeconds ? $"{diff.Minutes}:{diff.Seconds:00}" : $"{diff.TotalSeconds:F0}";
                    await Connection.SetTitleAsync(title);
                }
            }

            await CheckIfResetNeeded();
        }

        public override void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            Tools.AutoPopulateSettings(_settings, payload.Settings);
            SaveSettings();
        }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload) { }

        private Task SaveSettings()
        {
            return Connection.SetSettingsAsync(JObject.FromObject(_settings));
        }

        public override void Dispose()
        {
            Logger.Instance.LogMessage(TracingLevel.DEBUG, $"Destructor called");

            var updateProgressReporter = UpdateProgressReporter.GetInstance();
            updateProgressReporter.OnUpdateStarted -= LeagueInfo_OnUpdateStarted;
            updateProgressReporter.OnUpdateProgress -= LeagueInfo_OnUpdateProgress;
            updateProgressReporter.OnUpdateCompleted -= LeagueInfo_OnUpdateCompleted;

            Connection.OnApplicationDidLaunch -= Connection_OnApplicationDidLaunch;
            Connection.OnApplicationDidTerminate -= Connection_OnApplicationDidTerminate;

            _cts.Cancel();
            _cts.Dispose();
        }

        #endregion

        #region Private Methods

        private async Task UpdateSpellImage()
        {
            if (!_info.IndexToEnemyPlayer.TryGetValue((int)_settings.Summoner, out var player))
            {
                // TODO log
                return;
            }

            await UpdateSpellImage(player);
        }

        private async Task UpdateSpellImage(Player player)
        {
            if (!player.ESpellToSpell.TryGetValue(_settings.Spell, out var spell))
            {
                // TODO log
                return;
            }

            await UpdateSpellImage(spell, player.Champion);
        }

        private async Task UpdateSpellImage(Spell spell, Champion champion)
        {
            Logger.Instance.LogMessage(TracingLevel.DEBUG, "Spell Image Update - initiated");

            if (spell == null)
                Logger.Instance.LogMessage(TracingLevel.DEBUG, "Spell Image Update - spell is null");

            if (champion == null)
                Logger.Instance.LogMessage(TracingLevel.DEBUG, "Spell Image Update - champion is null");

            Image spellImage = _info.SpellAssetController.GetImage(spell?.Id);

            if (_isTimerEnabled)
                spellImage = Utilities.GrayscaleImage(spellImage);

            var championImage = _info.ChampionAssetController.GetImage(champion?.Id);
            var image = Utilities.AddChampionToSpellImage(spellImage, championImage);

            await Connection.SetImageAsync(image);
            Logger.Instance.LogMessage(TracingLevel.DEBUG, "Spell Image Update - completed");
        }

        private async Task SendMessageInChat(EChatFormat chatFormat)
        {
            if (!_isTimerEnabled)
                return;

            Logger.Instance.LogMessage(TracingLevel.DEBUG, $"Chat Message - initiated");

            if (!_info.IndexToEnemyPlayer.TryGetValue((int)_settings.Summoner, out var enemy))
            {
                // TODO log
                return;
            }

            if (!enemy.ESpellToSpell.TryGetValue(_settings.Spell, out var spell))
            {
                // TODO log
                return;
            }

            var rest = _endTime - DateTime.Now;

            string time;
            switch (chatFormat)
            {
                case EChatFormat.GameTime:
                    var gameData = await LiveClientApiController.GetGameData(_cts.Token);
                    var inGameEndTime = TimeSpan.FromSeconds(gameData.Time).Add(rest);
                    if (inGameEndTime.Seconds == 60)
                    {
                        //reduce inGameTimer so we dont show 60 seconds in the chat
                        //idk if there is a better option ¯\_(ツ)_/¯
                        inGameEndTime.Add(new TimeSpan(0, 0, -1));
                    }

                    time = $"{inGameEndTime.Minutes}:{inGameEndTime.Seconds:00}";
                    break;

                case EChatFormat.RemainingSeconds:
                    time = $"{rest.TotalSeconds:F0}";
                    break;

                case EChatFormat.RemainingMinutesAndSeconds:
                    time = $"{rest.Minutes}:{rest.Seconds:00}";
                    break;

                default:
                    Logger.Instance.LogMessage(TracingLevel.DEBUG, $"Chat Message - out of range: {_settings.ChatFormat}");
                    throw new ArgumentOutOfRangeException(nameof(_settings.ChatFormat));
            }

            string message;
            if (!_settings.ShowAbilityName && _settings.Spell != ESpell.SummonerSpell1 && _settings.Spell != ESpell.SummonerSpell2)
                message = $"{enemy.Champion.Name} - {Enum.GetName(typeof(ESpell), _settings.Spell)} - {time}";
            else
                message = $"{enemy.Champion.Name} - {spell.Name} - {time}";

            Utilities.SendMessageInChat(message);
            Logger.Instance.LogMessage(TracingLevel.DEBUG, $"Chat Message - completed");
        }

        private async Task CheckIfResetNeeded()
        {
            if (!_keyPressed)
                return;

            if ((DateTime.Now - _keyPressStart).TotalMilliseconds > cResetTimerKeypressLength)
            {
                await ResetTimer();
                _longPress = true;
            }
        }

        private async Task ResetTimer()
        {
            Logger.Instance.LogMessage(TracingLevel.DEBUG, $"Timer Reset - initiated");

            _endTime = DateTime.MinValue;
            _isTimerEnabled = false;

            if (_isInGame)
            {
                await UpdateSpellImage();
            }
            else
            {
                await Connection.SetDefaultImageAsync();
            }

            await Connection.SetTitleAsync(string.Empty);
            Logger.Instance.LogMessage(TracingLevel.DEBUG, $"Timer Reset - completed");
        }

        #endregion
    }
}