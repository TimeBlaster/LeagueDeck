using BarRaider.SdTools;
using LeagueDeck.Settings;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

namespace LeagueDeck
{
    [PluginActionId("dev.timeblaster.leaguedeck.timeroffset")]
    public class TimerOffsetAction : PluginBase
    {
        #region vars

        private TimerOffsetSettings _settings;
        private bool _toggled;
        private DateTime _keyPressStart;
        private const double cResetToggleLength = 5000;

        #endregion

        #region ctor

        public TimerOffsetAction(SDConnection connection, InitialPayload payload)
            : base(connection, payload)
        {
            if (payload.Settings == null || payload.Settings.Count == 0)
                this._settings = TimerOffsetSettings.CreateDefaultSettings();
            else
                this._settings = payload.Settings.ToObject<TimerOffsetSettings>();
        }

        #endregion

        #region Overrides

        public override void KeyPressed(KeyPayload payload)
        {
            _toggled = !_toggled;
        }

        public override void KeyReleased(KeyPayload payload)
        {
        }

        public override void OnTick()
        {
            CheckIfResetNeeded();
        }

        public override async void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            Tools.AutoPopulateSettings(_settings, payload.Settings);
            await SaveSettings();
        }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload) { }

        private Task SaveSettings()
        {
            return Connection.SetSettingsAsync(JObject.FromObject(_settings));
        }

        public override void Dispose()
        {

        }

        #endregion

        #region Private Methods

        private void CheckIfResetNeeded()
        {
            if (!_toggled)
                return;

            if ((DateTime.Now - _keyPressStart).TotalMilliseconds > cResetToggleLength)
            {
                _toggled = false;
            }
        }

        #endregion
    }
}