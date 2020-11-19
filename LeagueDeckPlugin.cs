using BarRaider.SdTools;
using Newtonsoft.Json.Linq;
using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace LeagueDeck
{
    [PluginActionId("dev.timeblaster.leaguedeck.spelltimer")]
    public class LeagueDeckPlugin : PluginBase
    {
        #region vars

        private const int cResetTimerKeypressLength = 500;

        private CancellationTokenSource _cts = new CancellationTokenSource();

        private LeagueDeckSettings _settings;

        private LeagueInfo _info;
        private bool _isInGame;

        private DateTime _keyPressStart;
        private DateTime _endTime;
        private bool _keyPressed = false;
        private bool _longPress = false;
        private bool _timerEnabled;

        #endregion

        #region ctor

        public LeagueDeckPlugin(SDConnection connection, InitialPayload payload)
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
                this._settings = LeagueDeckSettings.CreateDefaultSettings();
            else
                this._settings = payload.Settings.ToObject<LeagueDeckSettings>();
        }

        #endregion

        #region Events

        private async void Connection_OnApplicationDidLaunch(object sender, BarRaider.SdTools.Wrappers.SDEventReceivedEventArgs<BarRaider.SdTools.Events.ApplicationDidLaunch> e)
        {
            if (e.Event.Payload.Application != "League of Legends.exe")
                return;

            Logger.Instance.LogMessage(TracingLevel.DEBUG, "GameStarted");

            while (_info.Updating)
            {
                if (_cts.IsCancellationRequested)
                    return;

                await Task.Delay(500, _cts.Token);
            }

            _isInGame = true;

            await UpdateSpellImage();
        }

        private async void Connection_OnApplicationDidTerminate(object sender, BarRaider.SdTools.Wrappers.SDEventReceivedEventArgs<BarRaider.SdTools.Events.ApplicationDidTerminate> e)
        {
            if (e.Event.Payload.Application != "League of Legends.exe")
                return;

            _isInGame = false;

            await ResetTimer();

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

            var participant = await _info.GetParticipant((int)_settings.Summoner, _cts.Token);
            if (participant == null)
                return;

            var champion = _info.GetChampion(participant.ChampionName);
            var spell = _info.GetSpell(participant, _settings.Spell);

            double cooldown;
            switch (_settings.Spell)
            {
                case ESpell.Q:
                case ESpell.W:
                case ESpell.E:
                case ESpell.R:
                    cooldown = _info.GetSpellCooldown(spell, participant);
                    break;

                case ESpell.SummonerSpell1:
                case ESpell.SummonerSpell2:
                    var gameData = await _info.GetGameData(_cts.Token);
                    var isAram = gameData.GameMode.Equals("ARAM");

                    cooldown = _info.GetSummonerSpellCooldown(spell, participant, isAram);
                    break;

                default:
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
            var participant = await _info.GetParticipant((int)_settings.Summoner, _cts.Token);
            if (participant == null)
                return;

            var champion = _info.GetChampion(participant.ChampionName);
            var spell = _info.GetSpell(participant, _settings.Spell);

            await UpdateSpellImage(spell, champion);
        }

        private async Task UpdateSpellImage(Spell spell, Champion champion, bool grayscaled = false)
        {
            Logger.Instance.LogMessage(TracingLevel.DEBUG, "Spell Image Update - initiated");

            Image spellImage;
            switch (_settings.Spell)
            {
                case ESpell.Q:
                case ESpell.W:
                case ESpell.E:
                case ESpell.R:
                    spellImage = _info.GetSpellImage(spell.Id);
                    break;

                case ESpell.SummonerSpell1:
                case ESpell.SummonerSpell2:
                    spellImage = _info.GetSummonerSpellImage(spell.Id);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(_settings.Spell));
            }

            if (grayscaled)
                spellImage = Utilities.GrayscaleImage(spellImage);

            var championImage = _info.GetChampionImage(champion?.Id);
            Utilities.AddChampionToSpellImage(spellImage, championImage);

            await Connection.SetImageAsync(spellImage);
            Logger.Instance.LogMessage(TracingLevel.DEBUG, "Spell Image Update - completed");
        }

        private async Task SendMessageInChat()
        {
            if (!_timerEnabled)
                return;

            Logger.Instance.LogMessage(TracingLevel.DEBUG, $"Chat Message - initiated");

            var participant = await _info.GetParticipant((int)_settings.Summoner, _cts.Token);
            if (participant == null)
                return;

            var spell = _info.GetSpell(participant, _settings.Spell);

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
                    throw new ArgumentOutOfRangeException(nameof(_settings.ChatFormat));
            }

            var message = $"{participant.ChampionName} - {spell.Name} - {time}";
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