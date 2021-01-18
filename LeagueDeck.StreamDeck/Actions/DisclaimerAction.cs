using BarRaider.SdTools;
using LeagueDeck.Settings;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

namespace LeagueDeck
{
    [PluginActionId("dev.timeblaster.leaguedeck.disclaimer")]
    public class DisclaimerAction : PluginBase
    {
        #region vars

        private DisclaimerSettings _settings;
        private bool _longPress;
        private bool _keyPressed;
        private DateTime _keyPressStart;
        private const double cResetTimerKeypressLength = 500;

        #endregion

        #region ctor

        public DisclaimerAction(SDConnection connection, InitialPayload payload)
            : base(connection, payload)
        {
            if (payload.Settings == null || payload.Settings.Count == 0)
                this._settings = DisclaimerSettings.CreateDefaultSettings();
            else
                this._settings = payload.Settings.ToObject<DisclaimerSettings>();
        }

        #endregion

        #region Overrides

        public override void KeyPressed(KeyPayload payload)
        {
            _keyPressed = true;
        }

        public override void KeyReleased(KeyPayload payload)
        {
            if (!_longPress)
            {
                Utilities.SendMessageInChat(_settings.Message);
            }

            _longPress = false;
            _keyPressed = false;
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
            if (!_keyPressed)
                return;

            if ((DateTime.Now - _keyPressStart).TotalMilliseconds > cResetTimerKeypressLength)
            {
                _longPress = true;
            }
        }

        #endregion
    }
}