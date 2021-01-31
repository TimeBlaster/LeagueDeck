using LeagueDeck.Models.Api;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LeagueDeck.Models
{
    public class AllGameDataCache : AllGameData
    {
        public DateTime DateTimeAllGameDataUpdate =>
            new DateTime[]
            {
                ActivePlayerUpdate,
                ParticipantsUpdate,
                GameEventsUpdate,
                GameDataUpdate,
            }.Min();

        public DateTime ActivePlayerUpdate { get; private set; } = DateTime.MinValue;
        public new ActivePlayer ActivePlayer
        {
            get => base.ActivePlayer;
            internal set
            {
                ActivePlayerUpdate = DateTime.Now;
                base.ActivePlayer = value;
            }
        }

        public DateTime ParticipantsUpdate { get; private set; } = DateTime.MinValue;
        public new List<Participant> Participants
        {
            get => base.Participants;
            internal set
            {
                ParticipantsUpdate = DateTime.Now;
                base.Participants = value;
            }
        }

        public DateTime GameEventsUpdate { get; private set; } = DateTime.MinValue;
        public new GameEvents GameEvents
        {
            get => base.GameEvents;
            internal set
            {
                GameEventsUpdate = DateTime.Now;
                base.GameEvents = value;
            }
        }

        public DateTime GameDataUpdate { get; private set; } = DateTime.MinValue;
        public new GameData GameData
        {
            get => base.GameData;
            internal set
            {
                GameDataUpdate = DateTime.Now;
                base.GameData = value;
            }
        }

        public void Update(AllGameData allGameData)
        {
            ActivePlayer = allGameData.ActivePlayer;
            Participants = allGameData.Participants;
            GameEvents = allGameData.GameEvents;
            GameData = allGameData.GameData;
        }
    }
}
