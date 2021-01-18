using System;
using System.Collections.Generic;

namespace LeagueDeck.Models
{
    public class Player
    {
        public Champion Champion { get; }
        public Dictionary<ESpell, Spell> ESpellToSpell { get; }
        public Dictionary<ESpell, DateTime> SpellToTimerEnd { get; }

        public Player(Champion champion, Dictionary<ESpell, Spell> spellDict)
        {
            Champion = champion;
            ESpellToSpell = spellDict;
            SpellToTimerEnd = new Dictionary<ESpell, DateTime>
            {
                { ESpell.Q, DateTime.MinValue },
                { ESpell.W, DateTime.MinValue },
                { ESpell.E, DateTime.MinValue },
                { ESpell.R, DateTime.MinValue },
                { ESpell.SummonerSpell1, DateTime.MinValue },
                { ESpell.SummonerSpell2, DateTime.MinValue }
            };
        }
    }
}