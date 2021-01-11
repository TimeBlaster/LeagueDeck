using BarRaider.SdTools;
using LeagueDeck.ApiResponse;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LeagueDeck
{
    [PluginActionId("dev.timeblaster.leaguedeck.buyitem")]
    public class BuyItemPlugin : PluginBase
    {
        #region vars

        private CancellationTokenSource _cts = new CancellationTokenSource();

        private BuyItemSettings _settings;

        private LeagueInfo _info;
        private bool _isInGame;

        private bool _keyPressed = false;

        const int cResetTimerKeypressLength = 500;

        #endregion

        #region ctor

        public BuyItemPlugin(SDConnection connection, InitialPayload payload)
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
                this._settings = BuyItemSettings.CreateDefaultSettings();
            else
                this._settings = payload.Settings.ToObject<BuyItemSettings>();

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

            Task.Run(async () =>
            {
                await _info.UpdateTask;
                _settings.Items = _info.GetItems();
                await SaveSettings();
                UpdateItemImage(_settings.ItemId);

                if (_settings.DisplayFormat == EBuyItemDisplayFormat.TotalCost)
                {
                    var item = _info.GetItem(_settings.ItemId);
                    await Connection.SetTitleAsync($"{item.TotalCost:F0}");
                }
            });
        }

        #endregion

        #region Events

        private async void Connection_OnApplicationDidLaunch(object sender, BarRaider.SdTools.Wrappers.SDEventReceivedEventArgs<BarRaider.SdTools.Events.ApplicationDidLaunch> e)
        {
            if (e.Event.Payload.Application != "League of Legends.exe")
                return;

            Logger.Instance.LogMessage(TracingLevel.DEBUG, "GameStarted");

            await Task.WhenAll(new[] { _info.LoadGameData(_cts.Token), _info.UpdateTask })
                .ContinueWith(x =>
                {
                    _isInGame = true;
                    UpdateItemImage(_settings.ItemId);
                });
        }

        private async void Connection_OnApplicationDidTerminate(object sender, BarRaider.SdTools.Wrappers.SDEventReceivedEventArgs<BarRaider.SdTools.Events.ApplicationDidTerminate> e)
        {
            if (e.Event.Payload.Application != "League of Legends.exe")
                return;

            _isInGame = false;

            _info.ClearGameData();

            _cts.Cancel();
            _cts = new CancellationTokenSource();

            switch (_settings.DisplayFormat)
            {
                case EBuyItemDisplayFormat.RemainingCost:
                    await Connection.SetTitleAsync(string.Empty);
                    break;

                case EBuyItemDisplayFormat.None:
                case EBuyItemDisplayFormat.TotalCost:
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(_settings.DisplayFormat));
            }

            UpdateItemImage(_settings.ItemId);
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

            // TODO: buy item

            _keyPressed = true;
        }

        public async override void KeyReleased(KeyPayload payload)
        {
            _keyPressed = false;
        }

        public override async void OnTick()
        {
            if (!_isInGame)
                return;

            var activePlayer = await _info.GetActivePlayer(_cts.Token);
            if (activePlayer == null)
                return;

            var participant = await _info.GetSummoner(activePlayer.Name, _cts.Token);
            if (participant == null)
                return;

            var inventory = participant.Items.ToList();
            var itemOwned = inventory.Any(x => x.Id == _settings.ItemId);

            var remaining = GetGoldNecessary(activePlayer, inventory, _settings.ItemId);
            var canPurchase = remaining <= 0;

            switch (_settings.DisplayFormat)
            {
                case EBuyItemDisplayFormat.RemainingCost:
                    if (!canPurchase)
                        await Connection.SetTitleAsync($"{remaining:F0}");
                    else
                        await Connection.SetTitleAsync(string.Empty);
                    break;

                case EBuyItemDisplayFormat.None:
                case EBuyItemDisplayFormat.TotalCost:
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(_settings.DisplayFormat));
            }

            UpdateItemImage(_settings.ItemId, !canPurchase, itemOwned);
        }

        public override async void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            Tools.AutoPopulateSettings(_settings, payload.Settings);
            await SaveSettings();
            UpdateItemImage(_settings.ItemId);

            switch (_settings.DisplayFormat)
            {
                case EBuyItemDisplayFormat.TotalCost:
                    var item = _info.GetItem(_settings.ItemId);
                    await Connection.SetTitleAsync($"{item.TotalCost:F0}");
                    break;

                case EBuyItemDisplayFormat.None:
                case EBuyItemDisplayFormat.RemainingCost:
                    await Connection.SetTitleAsync(string.Empty);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(_settings.DisplayFormat));
            }
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

        private async void UpdateItemImage(int itemId, bool grayscale = false, bool checkMark = false)
        {
            var image = _info.GetItemImage(itemId);

            if (grayscale)
                image = Utilities.GrayscaleImage(image);

            if (checkMark)
                image = Utilities.AddCheckMarkToImage(image);

            await Connection.SetImageAsync(image);
        }

        private int GetRemainingCost(List<ApiResponse.Item> inventory, int itemId)
        {
            var item = _info.GetItem(itemId);

            var invItem = inventory.FirstOrDefault(x => x.Id == item.Id);

            if (invItem != null)
            {
                inventory.Remove(invItem);
                return 0;
            }
            else
            {
                var cost = item.BaseCost;
                foreach (var componentId in item.ComponentIds)
                {
                    cost += GetRemainingCost(inventory, componentId);
                }
                return cost;
            }
        }

        private double GetGoldNecessary(ActivePlayer player, List<ApiResponse.Item> inventory, int itemId)
        {
            var remaining = GetRemainingCost(inventory, itemId);
            return remaining - player.CurrentGold;
        }

        #endregion
    }
}