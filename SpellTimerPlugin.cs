using BarRaider.SdTools;
using Newtonsoft.Json.Linq;
using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LeagueDeck
{
    [PluginActionId("dev.timeblaster.leaguedeck.spelltimer")]
    public class SpellTimerPlugin : PluginBase
    {
        #region vars

        private const int cResetTimerKeypressLength = 500;

        private CancellationTokenSource _cts = new CancellationTokenSource();

        private SpellTimerSettings _settings;

        private LeagueInfo _info;
        private bool _isInGame;

        private DateTime _keyPressStart;
        private DateTime _endTime;
        private bool _keyPressed = false;
        private bool _longPress = false;
        private bool _timerEnabled;

        #endregion

        #region ctor

        public SpellTimerPlugin(SDConnection connection, InitialPayload payload)
            : base(connection, payload)
        {
            Logger.Instance.LogMessage(TracingLevel.DEBUG, $"Constructor called");

            LeagueInfo.OnUpdateStarted += LeagueInfo_OnUpdateStarted;
            LeagueInfo.OnUpdateProgress += LeagueInfo_OnUpdateProgress;
            LeagueInfo.OnUpdateCompleted += LeagueInfo_OnUpdateCompleted;

            _info = LeagueInfo.GetInstance(_cts.Token);

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

        #endregion

        #region Events

        private async void Connection_OnApplicationDidLaunch(object sender, BarRaider.SdTools.Wrappers.SDEventReceivedEventArgs<BarRaider.SdTools.Events.ApplicationDidLaunch> e)
        {
            if (e.Event.Payload.Application != "League of Legends.exe")
                return;

            Logger.Instance.LogMessage(TracingLevel.DEBUG, "GameStarted");

            await Task.WhenAll(new[] { _info.LoadGameData(_cts.Token), _info.UpdateTask })
                .ContinueWith(async (x) =>
                {
                    _isInGame = true;
                    await UpdateSpellImage();
                });
        }

        private async void Connection_OnApplicationDidTerminate(object sender, BarRaider.SdTools.Wrappers.SDEventReceivedEventArgs<BarRaider.SdTools.Events.ApplicationDidTerminate> e)
        {
            if (e.Event.Payload.Application != "League of Legends.exe")
                return;

            _isInGame = false;

            await ResetTimer();

            _info.ClearGameData();

            _cts.Cancel();
            _cts = new CancellationTokenSource();

            await Connection.SetDefaultImageAsync();
        }

        private async void LeagueInfo_OnUpdateStarted(object sender, LeagueInfo.UpdateEventArgs e)
        {
            var image = Utilities.GetUpdateImage();
            await Connection.SetImageAsync(image);
        }

        private async void LeagueInfo_OnUpdateProgress(object sender, LeagueInfo.UpdateEventArgs e)
        {
            await Connection.SetTitleAsync($"{e.Progress}%");
        }

        private async void LeagueInfo_OnUpdateCompleted(object sender, LeagueInfo.UpdateEventArgs e)
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

            if (_timerEnabled)
                return;

            var enemies = await _info.GetEnemies(_cts.Token);
            if (enemies == null || enemies.Count - 1 < (int)_settings.Summoner)
                return;

            var participant = enemies[(int)_settings.Summoner];

            var champion = _info.GetChampion(participant.ChampionName);
            var spell = _info.GetSpell(_settings.Summoner, _settings.Spell);

            double cooldown;
            switch (_settings.Spell)
            {
                case ESpell.Q:
                case ESpell.W:
                case ESpell.E:
                    cooldown = _info.GetSpellCooldown(spell, participant);
                    break;

                case ESpell.R:
                    var events = await _info.GetEventData(_cts.Token);
                    var enemySummonerNames = enemies.Select(y => y.SummonerName);
                    var cloudDrakes = events
                        .Where(x => x.Type == EEventType.DragonKill && x.DragonType == "Air")
                        .Count(x => enemySummonerNames.Contains(x.KillerName));

                    cooldown = _info.GetUltimateCooldown(spell, participant, cloudDrakes);
                    break;

                case ESpell.SummonerSpell1:
                case ESpell.SummonerSpell2:
                    var gameData = await _info.GetGameData(_cts.Token);
                    var isAram = gameData.GameMode.Equals("ARAM");

                    cooldown = _info.GetSummonerSpellCooldown(spell, participant, isAram);
                    break;

                default:
                    Logger.Instance.LogMessage(TracingLevel.DEBUG, $"KeyPressed - out of range: {_settings.Spell}");
                    throw new ArgumentOutOfRangeException(nameof(_settings.Spell));
            }

            if (cooldown - _settings.Offset <= 0)
                return;

            _timerEnabled = true;

            _endTime = _keyPressStart.AddSeconds(cooldown - _settings.Offset);

            await UpdateSpellImage(spell, champion, true);
        }

        public async override void KeyReleased(KeyPayload payload)
        {
            if (_longPress)
            {
                await ResetTimer();
            }
            else
            {
                if (_timerEnabled && _settings.SendMessageInChat && !Utilities.InputRunning)
                    await SendMessageInChat();
            }

            _longPress = false;
            _keyPressed = false;
        }

        public override async void OnTick()
        {
            if (_timerEnabled)
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

            LeagueInfo.OnUpdateStarted -= LeagueInfo_OnUpdateStarted;
            LeagueInfo.OnUpdateProgress -= LeagueInfo_OnUpdateProgress;
            LeagueInfo.OnUpdateCompleted -= LeagueInfo_OnUpdateCompleted;

            Connection.OnApplicationDidLaunch -= Connection_OnApplicationDidLaunch;
            Connection.OnApplicationDidTerminate -= Connection_OnApplicationDidTerminate;

            _cts.Cancel();
            _cts.Dispose();
        }

        #endregion

        #region Private Methods

        private async Task UpdateSpellImage()
        {
            var spell = _info.GetSpell(_settings.Summoner, _settings.Spell);
            var champion = _info.GetChampion(_settings.Summoner);

            await UpdateSpellImage(spell, champion);
        }

        private async Task UpdateSpellImage(Spell spell, Champion champion, bool grayscaled = false)
        {
            Logger.Instance.LogMessage(TracingLevel.DEBUG, "Spell Image Update - initiated");

            if (spell == null)
                Logger.Instance.LogMessage(TracingLevel.DEBUG, "Spell Image Update - spell is null");

            if (champion == null)
                Logger.Instance.LogMessage(TracingLevel.DEBUG, "Spell Image Update - champion is null");

            Image spellImage;
            switch (_settings.Spell)
            {
                case ESpell.Q:
                case ESpell.W:
                case ESpell.E:
                case ESpell.R:
                    spellImage = _info.GetSpellImage(spell?.Id);
                    break;

                case ESpell.SummonerSpell1:
                case ESpell.SummonerSpell2:
                    spellImage = _info.GetSummonerSpellImage(spell?.Id);
                    break;

                default:
                    Logger.Instance.LogMessage(TracingLevel.DEBUG, $"Spell Image Update - out of range: {_settings.Spell}");
                    throw new ArgumentOutOfRangeException(nameof(_settings.Spell));
            }

            if (grayscaled)
                spellImage = Utilities.GrayscaleImage(spellImage);

            var championImage = _info.GetChampionImage(champion?.Id);
            var image = Utilities.AddChampionToSpellImage(spellImage, championImage);

            await Connection.SetImageAsync(image);
            Logger.Instance.LogMessage(TracingLevel.DEBUG, "Spell Image Update - completed");
        }

        private async Task SendMessageInChat()
        {
            if (!_timerEnabled)
                return;

            Logger.Instance.LogMessage(TracingLevel.DEBUG, $"Chat Message - initiated");

            var enemies = await _info.GetEnemies(_cts.Token);
            if (enemies == null || enemies.Count - 1 < (int)_settings.Summoner)
                return;

            var participant = enemies[(int)_settings.Summoner];

            var spell = _info.GetSpell(_settings.Summoner, _settings.Spell);

            var rest = _endTime - DateTime.Now;

            string time;
            switch (_settings.ChatFormat)
            {
                case EChatFormat.GameTime:
                    var gameData = await _info.GetGameData(_cts.Token);
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
                message = $"{participant.ChampionName} - {Enum.GetName(typeof(ESpell), _settings.Spell)} - {time}";
            else
                message = $"{participant.ChampionName} - {spell.Name} - {time}";

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

            _timerEnabled = false;

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