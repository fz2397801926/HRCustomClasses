﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HREngine.Bots
{

    public class BehaviorRush : Behavior
    {
        PenalityManager penman = PenalityManager.Instance;

        public override int getPlayfieldValue(Playfield p)
        {
            if (p.value >= -2000000) return p.value;
            int retval = 0;
            retval -= p.evaluatePenality;
            retval += p.owncards.Count * 1;

            retval += p.ownHeroHp + p.ownHeroDefence;
            retval += -(p.enemyHeroHp + p.enemyHeroDefence);

            retval += p.ownMaxMana * 15 - p.enemyMaxMana * 15;

            if (p.ownWeaponAttack >= 1)
            {
                retval += p.ownWeaponAttack * p.ownWeaponDurability;
            }

            if (!p.enemyHeroFrozen)
            {
                retval -= p.enemyWeaponDurability * p.enemyWeaponAttack;
            }
            else
            {
                if (p.enemyHeroName != HeroEnum.mage && p.enemyHeroName != HeroEnum.priest)
                {
                    retval += 11;
                }
            }

            retval += p.owncarddraw * 5;
            retval -= p.enemycarddraw * 15;

            bool useAbili = false;
            bool usecoin = false;
            foreach (Action a in p.playactions)
            {
                if (a.heroattack && p.enemyHeroHp <= p.attackFaceHP) retval++;
                if (a.useability) useAbili = true;
                if (p.ownHeroName == HeroEnum.warrior && a.heroattack && useAbili) retval -= 1;
                if (a.useability && a.handcard.card.name == CardDB.cardName.lesserheal && ((a.enemytarget >= 10 && a.enemytarget <= 20) || a.enemytarget == 200)) retval -= 5;
                if (!a.cardplay) continue;
                if ((a.handcard.card.name == CardDB.cardName.thecoin || a.handcard.card.name == CardDB.cardName.innervate)) usecoin = true;
                if (a.handcard.card.name == CardDB.cardName.flamestrike && a.numEnemysBeforePlayed <= 2) retval -= 20;
            }
            if (usecoin && useAbili && p.ownMaxMana <= 2) retval -= 40;
            //if (usecoin && p.mana >= 1) retval -= 20;

            foreach (Minion m in p.ownMinions)
            {
                retval += m.Hp * 1;
                retval += m.Angr * 2;
                retval += m.handcard.card.rarity;
                if (m.windfury) retval += m.Angr;
                if (m.taunt) retval += 1;
                if (!m.taunt && m.stealth && penman.specialMinions.ContainsKey(m.name)) retval += 20;
                if (m.handcard.card.name == CardDB.cardName.silverhandrecruit && m.Angr == 1 && m.Hp == 1) retval -= 5;
                if (m.handcard.card.name == CardDB.cardName.direwolfalpha || m.handcard.card.name == CardDB.cardName.flametonguetotem || m.handcard.card.name == CardDB.cardName.stormwindchampion || m.handcard.card.name == CardDB.cardName.raidleader) retval += 10;
            }

            foreach (Minion m in p.enemyMinions)
            {
                retval -= this.getEnemyMinionValue(m, p);
            }

            retval -= p.enemySecretCount;
            retval -= p.lostDamage;//damage which was to high (like killing a 2/1 with an 3/3 -> => lostdamage =2
            retval -= p.lostWeaponDamage;
            if (p.ownMinions.Count == 0) retval -= 20;
            if (p.enemyMinions.Count >= 4) retval -= 20;
            if (p.enemyHeroHp <= 0) retval = 10000;
            //soulfire etc
            int deletecardsAtLast = 0;
            foreach (Action a in p.playactions)
            {
                if (!a.cardplay) continue;
                if (a.handcard.card.name == CardDB.cardName.soulfire || a.handcard.card.name == CardDB.cardName.doomguard || a.handcard.card.name == CardDB.cardName.succubus) deletecardsAtLast = 1;
                if (deletecardsAtLast == 1 && !(a.handcard.card.name == CardDB.cardName.soulfire || a.handcard.card.name == CardDB.cardName.doomguard || a.handcard.card.name == CardDB.cardName.succubus)) retval -= 20;
            }
            if (p.enemyHeroHp >= 1 && p.guessingHeroHP <= 0)
            {
                retval += p.owncarddraw * 500;
                retval -= 1000;
            }
            if (p.ownHeroHp <= 0) retval = -10000;

            p.value = retval;
            return retval;
        }

        public override int getEnemyMinionValue(Minion m, Playfield p)
        {
            int retval = 0;
            if (p.enemyMinions.Count >= 4 || m.taunt || (penman.priorityTargets.ContainsKey(m.name) && !m.silenced) || m.Angr >= 5)
            {
                retval += m.Hp;
                if (!m.frozen && !(m.handcard.card.name == CardDB.cardName.ancientwatcher && !m.silenced))
                {
                    retval += m.Angr * 2;
                    if (m.windfury) retval += 2 * m.Angr;
                }
                if (m.taunt) retval += 5;
                if (m.divineshild) retval += m.Angr;
                if (m.frozen) retval -= 1; // because its bad for enemy :D
                if (m.poisonous) retval += 4;
                retval += m.handcard.card.rarity;
            }


            if (penman.priorityTargets.ContainsKey(m.name) && !m.silenced) retval += penman.priorityTargets[m.name];
            if (m.Angr >= 4) retval += 20;
            if (m.Angr >= 7) retval += 50;
            if (m.name == CardDB.cardName.nerubianegg && m.Angr <= 3 && !m.taunt) retval = 0;
            return retval;
        }


    }

}
