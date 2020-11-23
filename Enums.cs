namespace LeagueDeck
{
    public enum EChatFormat
    {
        GameTime = 0,
        RemainingSeconds = 1,
        RemainingMinutesAndSeconds = 2,
    }

    public enum ESpell
    {
        Q = 0,
        W = 1,
        E = 2,
        R = 3,
        SummonerSpell1 = 4,
		SummonerSpell2 = 5,
    }

    public enum ESummoner
    {
        Summoner1 = 0,
        Summoner2 = 1,
        Summoner3 = 2,
        Summoner4 = 3,
        Summoner5 = 4,
    }

    public enum EEventType
    {
        GameStart,
        MinionsSpawning,
        FirstBrick,
        FirstBlood,
        ChampionKill,
        Multikill,
        Ace,
        DragonKill,
        HeraldKill,
        BaronKill,
        TurretKilled,
        InhibKilled,
    }
}
