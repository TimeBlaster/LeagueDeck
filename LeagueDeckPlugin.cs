using BarRaider.SdTools;
using Newtonsoft.Json.Linq;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LeagueDeck
{
    [PluginActionId("dev.timeblaster.leaguedeck")]
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
            _info = LeagueInfo.GetInstance();

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

            _isInGame = true;

            var participant = await _info.GetParticipant((int)_settings.Summoner, _cts.Token).ConfigureAwait(false);
            if (participant == null)
                return;

            var spell = participant.GetSummonerSpell(_settings.SummonerSpell);

            await UpdateSummonerSpellImage(spell, participant.ChampionName);
        }

        private async void Connection_OnApplicationDidTerminate(object sender, BarRaider.SdTools.Wrappers.SDEventReceivedEventArgs<BarRaider.SdTools.Events.ApplicationDidTerminate> e)
        {
            if (e.Event.Payload.Application != "League of Legends.exe")
                return;

            _isInGame = false;

            await ResetTimer();
            await Connection.SetDefaultImageAsync();
        }

        #endregion

        #region Overrides

        public override async void KeyPressed(KeyPayload payload)
        {
            if (!_isInGame)
                return;

            _keyPressStart = DateTime.Now;
            _keyPressed = true;

            var participant = await _info.GetParticipant((int)_settings.Summoner, _cts.Token);
            if (participant == null)
                return;

            var spell = participant.GetSummonerSpell(_settings.SummonerSpell);

            var gameData = await _info.GetGameData(_cts.Token);
            var isAram = gameData.GameMode.Equals("ARAM");

            var cooldown = LeagueInfo.GetSummonerSpellCooldown(spell, participant, isAram);
            _endTime = _keyPressStart.AddSeconds(cooldown);

            await UpdateSummonerSpellImage(spell, participant.ChampionName, true);

            Logger.Instance.LogMessage(TracingLevel.INFO, "Key Pressed");
        }

        public async override void KeyReleased(KeyPayload payload)
        {
            _timerEnabled = true;

            if (_longPress)
            {
                await ResetTimer();
            }
            else
            {
                if (_settings.SendMessageInChat && !Utilities.InputRunning)
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
                    await ResetTimer();
                else
                    await Connection.SetTitleAsync(diff.TotalSeconds.ToString("F0"));
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
            Logger.Instance.LogMessage(TracingLevel.INFO, $"Destructor called");

            Connection.OnApplicationDidLaunch -= Connection_OnApplicationDidLaunch;
            Connection.OnApplicationDidTerminate -= Connection_OnApplicationDidTerminate;

            _cts.Cancel();
            _cts.Dispose();
        }

        #endregion

        #region Private Methods

        private async Task UpdateSummonerSpellImage(string spell, string champion, bool grayscaled = false)
        {
            var image = Utilities.GetSummonerSpellImage(spell);

            if (grayscaled)
                image = Utilities.GrayscaleImage(image);

            Utilities.AddChampionToSpellImage(image, champion);
            await Connection.SetImageAsync(image);
        }

        private async Task SendMessageInChat()
        {
            if (!_timerEnabled)
                return;

            var participant = await _info.GetParticipant((int)_settings.Summoner, _cts.Token);
            if (participant == null)
                return;

            var spell = participant.GetSummonerSpell(_settings.SummonerSpell);

            var rest = _endTime - DateTime.Now;
            var gameData = await _info.GetGameData(_cts.Token);
            var inGameEndTime = TimeSpan.FromSeconds(gameData.Time).Add(rest);
            if (inGameEndTime.Seconds == 60)
            {
                //reduce inGameTimer so we dont show 60 seconds in the chat
                //idk if there is a better option ¯\_(ツ)_/¯
                inGameEndTime.Add(new TimeSpan(0, 0, -1));
            }

            Utilities.SendMessageInChat(participant.ChampionName, spell, inGameEndTime);
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
            _timerEnabled = false;

            if (_isInGame)
            {
                var participant = await _info.GetParticipant((int)_settings.Summoner, _cts.Token);
                if (participant == null)
                    return;

                var spell = participant.GetSummonerSpell(_settings.SummonerSpell);

                await UpdateSummonerSpellImage(spell, participant.ChampionName);
            }
            else
            {
                await Connection.SetDefaultImageAsync();
            }

            await Connection.SetTitleAsync(string.Empty);
        }

        #endregion
    }
}