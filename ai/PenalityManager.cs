﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HREngine.Bots
{

  public class PenalityManager
    {
        //todo acolyteofpain
        //todo better aoe-penality

        ComboBreaker cb ;

        public Dictionary<CardDB.cardName, int> priorityDatabase = new Dictionary<CardDB.cardName, int>();
        Dictionary<CardDB.cardName, int> HealTargetDatabase = new Dictionary<CardDB.cardName, int>();
        Dictionary<CardDB.cardName, int> HealHeroDatabase = new Dictionary<CardDB.cardName, int>();
        Dictionary<CardDB.cardName, int> HealAllDatabase = new Dictionary<CardDB.cardName, int>();

        public Dictionary<CardDB.cardName, int> DamageTargetDatabase = new Dictionary<CardDB.cardName, int>();
        public Dictionary<CardDB.cardName, int> DamageTargetSpecialDatabase = new Dictionary<CardDB.cardName, int>();
        Dictionary<CardDB.cardName, int> DamageAllDatabase = new Dictionary<CardDB.cardName, int>();
        Dictionary<CardDB.cardName, int> DamageHeroDatabase = new Dictionary<CardDB.cardName, int>();
        Dictionary<CardDB.cardName, int> DamageRandomDatabase = new Dictionary<CardDB.cardName, int>();
        Dictionary<CardDB.cardName, int> DamageAllEnemysDatabase = new Dictionary<CardDB.cardName, int>();

        Dictionary<CardDB.cardName, int> enrageDatabase = new Dictionary<CardDB.cardName, int>();
        Dictionary<CardDB.cardName, int> silenceDatabase = new Dictionary<CardDB.cardName, int>();

        Dictionary<CardDB.cardName, int> heroAttackBuffDatabase = new Dictionary<CardDB.cardName, int>();
        Dictionary<CardDB.cardName, int> attackBuffDatabase = new Dictionary<CardDB.cardName, int>();
        Dictionary<CardDB.cardName, int> healthBuffDatabase = new Dictionary<CardDB.cardName, int>();
        Dictionary<CardDB.cardName, int> tauntBuffDatabase = new Dictionary<CardDB.cardName, int>();

        Dictionary<CardDB.cardName, int> cardDrawBattleCryDatabase = new Dictionary<CardDB.cardName, int>();
        Dictionary<CardDB.cardName, int> cardDiscardDatabase = new Dictionary<CardDB.cardName, int>();
        Dictionary<CardDB.cardName, int> destroyOwnDatabase = new Dictionary<CardDB.cardName, int>();
        Dictionary<CardDB.cardName, int> destroyDatabase = new Dictionary<CardDB.cardName, int>();
        Dictionary<CardDB.cardName, int> buffingMinionsDatabase = new Dictionary<CardDB.cardName, int>();
        Dictionary<CardDB.cardName, int> buffing1TurnDatabase = new Dictionary<CardDB.cardName, int>();
        Dictionary<CardDB.cardName, int> heroDamagingAoeDatabase = new Dictionary<CardDB.cardName, int>();

        Dictionary<CardDB.cardName, int> returnHandDatabase = new Dictionary<CardDB.cardName, int>();
        public Dictionary<CardDB.cardName, int> priorityTargets = new Dictionary<CardDB.cardName, int>();

        public Dictionary<CardDB.cardName, int> specialMinions = new Dictionary<CardDB.cardName, int>(); //minions with cardtext, but no battlecry


        private static PenalityManager instance;

        public static PenalityManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new PenalityManager();
                }
                return instance;
            }
        }

        private PenalityManager()
        {
            setupHealDatabase();
            setupEnrageDatabase();
            setupDamageDatabase();
            setupPriorityList();
            setupsilenceDatabase(); 
            setupAttackBuff(); 
            setupHealthBuff(); 
            setupCardDrawBattlecry(); 
            setupDiscardCards(); 
            setupDestroyOwnCards();
            setupSpecialMins();
            setupEnemyTargetPriority();
            setupHeroDamagingAOE();
            setupBuffingMinions();
        }

        public void setCombos()
        {
            this.cb = ComboBreaker.Instance;
        }
        
        public int getAttackWithMininonPenality(Minion m, Playfield p, int target, bool lethal)
        {
            int pen = 0;
            pen = getAttackSecretPenality(m,p,target);
            if (!lethal && m.name == CardDB.cardName.bloodimp) pen = 50;
            if (m.name == CardDB.cardName.leeroyjenkins)
            {
                if (target >= 10 && target <= 19)
                {
                    Minion t = p.enemyMinions[target - 10];
                    if (t.name == CardDB.cardName.whelp) return 500;
                }
                
            }
            return pen;
        }

        public int getAttackWithHeroPenality(int target, Playfield p)
        {
            int retval = 0;

            //no penality, but a bonus, if he has weapon on hand!
            if (target == 200 && p.ownWeaponName == CardDB.cardName.gorehowl && p.ownWeaponAttack >= 3)
            {
                return 10;
            }
            if (p.ownWeaponDurability >= 1)
            {
                bool hasweapon = false;
                foreach (Handmanager.Handcard c in p.owncards)
                {
                    if (c.card.type == CardDB.cardtype.WEAPON) hasweapon = true;
                }
                if (p.ownWeaponAttack == 1 && p.ownHeroName == HeroEnum.thief) hasweapon = true;
                if (hasweapon) retval = -p.ownWeaponAttack - 1; // so he doesnt "lose" the weapon in evaluation :D
            }
            if (p.ownWeaponAttack == 1 && p.ownHeroName == HeroEnum.thief) retval += -1;
            return retval;
        }

        public int getPlayCardPenality(CardDB.Card card, int target, Playfield p, int choice, bool lethal)
        {
            int retval = 0;
            CardDB.cardName name = card.name;
            //there is no reason to buff HP of minon (because it is not healed)

            int abuff = getAttackBuffPenality(card, target, p, choice, lethal);
            int tbuff = getTauntBuffPenality(name, target, p, choice);
            if (name == CardDB.cardName.markofthewild && ( (abuff >= 500 && tbuff == 0) || (abuff == 0 && tbuff >= 500)) )
            {
                retval = 0;
            }
            else
            {
                retval += abuff + tbuff;
            }
            retval += getHPBuffPenality(card, target, p, choice);
            retval += getSilencePenality( name,  target,  p,  choice, lethal);
            retval += getDamagePenality( name,  target,  p,  choice, lethal);
            retval += getHealPenality( name,  target,  p,  choice, lethal);
            retval += getCardDrawPenality( name,  target,  p,  choice);
            retval += getCardDrawofEffectMinions( card,  p);
            retval += getCardDiscardPenality( name,  p);
            retval += getDestroyOwnPenality(name, target, p, lethal);
            
            retval += getDestroyPenality( name,  target,  p);
            retval += getSpecialCardComboPenalitys( card,  target,  p, lethal, choice);
            retval += playSecretPenality( card,  p);
            retval += getPlayCardSecretPenality(card, p);
            if(!lethal) retval += cb.getPenalityForDestroyingCombo(card, p);

            return retval;
        }

        private int getAttackBuffPenality(CardDB.Card card, int target, Playfield p, int choice, bool lethal)
        {
            CardDB.cardName name = card.name;
            int pen = 0;
            //buff enemy?

            if (!lethal && (card.name == CardDB.cardName.savageroar || card.name == CardDB.cardName.bloodlust))
            {
                int targets = 0;
                foreach (Minion m in p.ownMinions)
                {
                    if (m.Ready) targets++;
                }
                if ((p.ownHeroReady || p.ownHeroNumAttackThisTurn == 0) && card.name == CardDB.cardName.savageroar) targets++;

                if (targets <= 2)
                {
                    return 20;
                }
            }

            if (!this.attackBuffDatabase.ContainsKey(name)) return 0;
            if (target >= 10 && target <= 19)
            {
                if (card.type == CardDB.cardtype.MOB && p.ownMinions.Count == 0) return 0;
                //allow it if you have biggamehunter
                foreach (Handmanager.Handcard hc in p.owncards)
                {
                    if (hc.card.name == CardDB.cardName.biggamehunter) return pen;
                    if (hc.card.name == CardDB.cardName.shadowworddeath) return pen;
                }
                if (card.name == CardDB.cardName.crueltaskmaster)
                {
                    int maxhp = 0;
                    Minion m = p.enemyMinions[target - 10];

                    if (m.Hp == 1)
                    {
                        return 0;
                    }

                    if (m.Angr >= 4 || m.Hp >= 5)
                    {
                        maxhp++;
                    }
                    if (maxhp >= 1)
                    {
                        foreach (Handmanager.Handcard hc in p.owncards)
                        {
                            if (hc.card.name == CardDB.cardName.execute) return 0;
                        }
                    }
                    pen = 30;
                }
                else
                {
                    pen = 500;
                }
            }
            if (target >= 0 && target <= 9)
            {
                Minion m = p.ownMinions[target];
                if (!m.Ready)
                {
                    return 20;
                }
                if (m.Hp == 1 && !m.divineshild && !this.buffing1TurnDatabase.ContainsKey(name))
                {
                    return 10;
                }
            }
            return pen;
        }

        private int getHPBuffPenality(CardDB.Card card, int target, Playfield p, int choice)
        {
            CardDB.cardName name = card.name;
            int pen = 0;
            //buff enemy?
            if (!this.healthBuffDatabase.ContainsKey(name)) return 0;
            if (target >= 10 && target <= 19 && !this.tauntBuffDatabase.ContainsKey(name))
            {
                pen = 500;
            }

            return pen;
        }


        private int getTauntBuffPenality(CardDB.cardName name, int target, Playfield p, int choice)
        {
            int pen = 0;
            //buff enemy?
            if (!this.tauntBuffDatabase.ContainsKey(name)) return 0;
            if (name == CardDB.cardName.markofnature && choice != 2) return 0;

            if (target >= 10 && target <= 19)
            {
                //allow it if you have black knight
                foreach (Handmanager.Handcard hc in p.owncards)
                {
                    if (hc.card.name == CardDB.cardName.theblackknight) return 0;
                }

                // allow taunting if target is priority and others have taunt
                bool enemyhasTaunts=false;
                foreach (Minion mnn in p.enemyMinions)
                {
                    if (mnn.taunt)
                    {
                        enemyhasTaunts = true;
                        break;
                    }
                }
                Minion m= p.enemyMinions[target-10];
                if (enemyhasTaunts && this.priorityDatabase.ContainsKey(m.name))
                {
                    return 0;
                }

                pen = 500;
            }

            return pen;
        }

        private int getSilencePenality(CardDB.cardName name, int target, Playfield p, int choice, bool lethal)
        {
            int pen = 0;
            if (name == CardDB.cardName.keeperofthegrove && choice != 2) return 0; // look at damage penality in this case

            if (target >= 0 && target <= 9)
            {
                if (this.silenceDatabase.ContainsKey(name))
                {
                    // no pen if own is enrage
                    Minion m = p.ownMinions[target];

                    if ((!m.silenced && (m.name == CardDB.cardName.ancientwatcher || m.name == CardDB.cardName.ragnarosthefirelord)) || m.Angr < m.handcard.card.Attack || m.maxHp < m.handcard.card.Health || (m.frozen && !m.playedThisTurn && m.numAttacksThisTurn == 0))
                    {
                        return 0;
                    }
                    

                    pen += 500;
                }
            }

            if (target == -1)
            {
                if (name == CardDB.cardName.ironbeakowl || name == CardDB.cardName.spellbreaker)
                {

                    return 20;
                }
            }

            if (target >= 10 && target <= 19)
            {
                if (this.silenceDatabase.ContainsKey(name))
                {
                    // no pen if own is enrage
                    Minion m = p.enemyMinions[target - 10];//

                    if ( !m.silenced && (m.name == CardDB.cardName.ancientwatcher || m.name == CardDB.cardName.ragnarosthefirelord) )
                    {
                        return 500;
                    }

                    if (lethal)
                    {
                        //during lethal we only silence taunt, or if its a mob (owl/spellbreaker) + we can give him charge
                        if (m.taunt || (name == CardDB.cardName.ironbeakowl && (p.ownMinions.Find(x => x.name == CardDB.cardName.tundrarhino) != null || p.ownMinions.Find(x => x.name == CardDB.cardName.warsongcommander) != null || p.owncards.Find(x => x.card.name == CardDB.cardName.charge) != null)) || (name == CardDB.cardName.spellbreaker && p.owncards.Find(x => x.card.name == CardDB.cardName.charge) != null)) return 0;
                       
                        return 500;
                    }
                    if (m.handcard.card.name == CardDB.cardName.venturecomercenary && !m.silenced && (m.Angr <= m.handcard.card.Attack && m.maxHp <= m.handcard.card.Health))
                    {
                        return 30;
                    }

                    if (priorityDatabase.ContainsKey(m.name) && !m.silenced)
                    {
                        return -10;
                    }

                    //silence nothing
                    if (m.Angr <= m.handcard.card.Attack && m.maxHp <= m.handcard.card.Health && !m.taunt && !m.windfury && !m.divineshild && !m.poisonous && m.enchantments.Count == 0 && !this.specialMinions.ContainsKey(name))
                    {
                        return 30;
                    }

                    

                    pen = 0;
                }
            }

            return pen;

        }

        private int getDamagePenality(CardDB.cardName name, int target, Playfield p, int choice, bool lethal)
        {
            int pen = 0;

            if (name == CardDB.cardName.shieldslam && p.ownHeroDefence == 0) return 500;
            if (name == CardDB.cardName.savagery && p.ownheroAngr == 0) return 500;
            if (name == CardDB.cardName.keeperofthegrove && choice != 1) return 0; // look at silence penality

            if (this.DamageAllDatabase.ContainsKey(name) || (p.auchenaiseelenpriesterin && HealAllDatabase.ContainsKey(name))) // aoe penality
            {
                int maxhp = 0;
                foreach (Minion m in p.enemyMinions)
                {
                    if (m.Angr >= 4 || m.Hp >= 5)
                    {
                        maxhp++;
                    }
                }
                if (maxhp >= 1)
                {
                    foreach (Handmanager.Handcard hc in p.owncards)
                    {
                        if (hc.card.name == CardDB.cardName.execute) return 0;
                    }
                }

                if( p.enemyMinions.Count <=1 || p.enemyMinions.Count +1 <= p.ownMinions.Count || p.ownMinions.Count >=3)
                {
                    return 30;
                }
            }

            if (this.DamageAllEnemysDatabase.ContainsKey(name)) // aoe penality
            {
                int maxhp = 0;
                foreach (Minion m in p.enemyMinions)
                {
                    if (m.Angr >= 4 || m.Hp >= 5)
                    {
                        maxhp++;
                    }
                }
                if (maxhp >= 4)
                {
                    foreach (Handmanager.Handcard hc in p.owncards)
                    {
                        if (hc.card.name == CardDB.cardName.execute) return 0;
                    }
                }

                if (name == CardDB.cardName.holynova)
                {
                    int targets = p.enemyMinions.Count;
                    foreach (Minion m in p.ownMinions)
                    {
                        if (m.wounded) targets++;
                    }
                    if (targets <= 2)
                    {
                        return 20;
                    }

                }
                if (p.enemyMinions.Count <= 2)
                {
                    return 20 * p.enemyMinions.Count;
                }
            }

            if (target == 100)
            {
                if (DamageTargetDatabase.ContainsKey(name) || DamageTargetSpecialDatabase.ContainsKey(name) || (p.auchenaiseelenpriesterin && HealTargetDatabase.ContainsKey(name)))
                {
                    pen = 500;
                }
            }

            if (!lethal && target == 200)
            {
                if (name == CardDB.cardName.baneofdoom)
                {
                    pen = 500;
                }
            }

            if (target >= 0 && target <= 9)
            {
                if (DamageTargetDatabase.ContainsKey(name) || (p.auchenaiseelenpriesterin && HealTargetDatabase.ContainsKey(name)))
                {
                    // no pen if own is enrage
                    Minion m = p.ownMinions[target];

                    //standard ones :D (mostly carddraw
                    if (enrageDatabase.ContainsKey(m.name) && !m.wounded)
                    {
                        return pen;
                    }

                    // no pen if we have battlerage for example
                    int dmg = 0;
                    if (DamageTargetDatabase.ContainsKey(name))
                    {
                        dmg = DamageTargetDatabase[name];
                    }
                    else
                    {
                        dmg = HealTargetDatabase[name];
                    }
                    if (m.handcard.card.deathrattle) return 10;
                    if (m.Hp > dmg)
                    {
                        if (m.name == CardDB.cardName.acolyteofpain && p.owncards.Count <= 3) return 0; 
                        foreach (Handmanager.Handcard hc in p.owncards)
                        {
                            if (hc.card.name == CardDB.cardName.battlerage) return pen;
                            if (hc.card.name == CardDB.cardName.rampage) return pen;
                        }
                    }
                    

                    pen = 500;
                }

                //special cards
                if (DamageTargetSpecialDatabase.ContainsKey(name) )
                {
                    int dmg  = DamageTargetSpecialDatabase[name];
                    Minion m = p.ownMinions[target];
                    if (name == CardDB.cardName.crueltaskmaster && m.Hp>=2) return 0;
                    if (name == CardDB.cardName.demonfire && (TAG_RACE)m.handcard.card.race == TAG_RACE.DEMON) return 0;
                    if (name == CardDB.cardName.earthshock && m.Hp >= 2 )
                    {
                        if (priorityDatabase.ContainsKey(m.name) && !m.silenced)
                        {
                            return 500;
                        }

                        if ((!m.silenced && (m.name == CardDB.cardName.ancientwatcher || m.name == CardDB.cardName.ragnarosthefirelord)) || m.Angr < m.handcard.card.Attack || m.maxHp < m.handcard.card.Health || (m.frozen && !m.playedThisTurn && m.numAttacksThisTurn == 0))
                        return 0;
                    }
                    if (name == CardDB.cardName.earthshock)//dont silence other own minions
                    {
                        return 500;
                    }

                    // no pen if own is enrage
                    if (enrageDatabase.ContainsKey(m.name) && !m.wounded)
                    {
                        return pen;
                    }

                    // no pen if we have battlerage for example
                    
                    if (m.Hp > dmg)
                    {
                        foreach (Handmanager.Handcard hc in p.owncards)
                        {
                            if (hc.card.name == CardDB.cardName.battlerage) return pen;
                            if (hc.card.name == CardDB.cardName.rampage) return pen;
                        }
                    }

                    pen = 500;
                }
            }
            if (target >= 10 && target <= 19)
            {
                if (DamageTargetSpecialDatabase.ContainsKey(name) || DamageTargetDatabase.ContainsKey(name))
                {
                    Minion m = p.enemyMinions[target-10];
                    if(name==CardDB.cardName.soulfire && m.maxHp <=2) pen=10;

                    if (name == CardDB.cardName.baneofdoom && m.Hp >= 3) pen = 10;

                    if (name == CardDB.cardName.shieldslam && (m.Hp <= 4 || m.Angr <= 4)) pen = 20;
                }
            }

            return pen;
        }

        private int getHealPenality(CardDB.cardName name, int target, Playfield p, int choice, bool lethal)
        {
            ///Todo healpenality for aoe heal
            ///todo auchenai soulpriest
            if (p.auchenaiseelenpriesterin) return 0;
            if (name == CardDB.cardName.ancientoflore && choice != 2) return 0;
            int pen = 0;
            int heal = 0;
            /*if (HealHeroDatabase.ContainsKey(name))
            {
                heal = HealHeroDatabase[name];
                if (target == 200) pen = 500; // dont heal enemy
                if ((target == 100 || target == -1) && p.ownHeroHp + heal > 30) pen = p.ownHeroHp + heal - 30;
            }*/

            if (HealTargetDatabase.ContainsKey(name))
            {
                heal = HealTargetDatabase[name];
                if (target == 200) return 500; // dont heal enemy
                if ((target == 100) && p.ownHeroHp == 30) return 150;
                if ((target == 100) && p.ownHeroHp + heal > 30) pen = p.ownHeroHp + heal - 30;
                Minion m = new Minion();

                if (target >= 0 && target < 10)
                {
                    m = p.ownMinions[target];
                    int wasted=0;
                    if (m.Hp == m.maxHp) return 500;
                    if (m.Hp + heal-1 > m.maxHp) wasted = m.Hp + heal - m.maxHp;
                    pen=wasted;
                    
                    if (m.taunt && wasted <= 2 && m.Hp < m.maxHp) pen -= 5; // if we heal a taunt, its good :D

                    if (m.Hp + heal <= m.maxHp) pen = -1;
                }

                if (target >= 10 && target < 20)
                {
                    m = p.enemyMinions[target-10];
                    if (m.Hp == m.maxHp) return 500;
                    // no penality if we heal enrage enemy
                    if (enrageDatabase.ContainsKey(m.name))
                    {
                        return pen;
                    }
                    // no penality if we have heal-trigger :D
                    int i = 0;
                    foreach (Minion mnn in p.ownMinions)
                    {
                        if (mnn.name == CardDB.cardName.northshirecleric) i++;
                        if (mnn.name == CardDB.cardName.lightwarden) i++;
                    }
                    foreach (Minion mnn in p.enemyMinions)
                    {
                        if (mnn.name == CardDB.cardName.northshirecleric) i--;
                        if (mnn.name == CardDB.cardName.lightwarden) i--;
                    }
                    if (i >= 1) return pen;

                    // no pen if we have slam

                    foreach (Handmanager.Handcard hc in p.owncards)
                    {
                        if (hc.card.name == CardDB.cardName.slam && m.Hp < 2) return pen;
                        if (hc.card.name == CardDB.cardName.backstab) return pen;
                    }

                    pen = 500;
                }
 

            }

            return pen;
        }

        private int getCardDrawPenality(CardDB.cardName name, int target, Playfield p, int choice)
        {
            // penality if carddraw is late or you have enough cards
            int pen = 0;
            if (!cardDrawBattleCryDatabase.ContainsKey(name)) return 0;
            if (name == CardDB.cardName.ancientoflore && choice != 1) return 0;
            if (name == CardDB.cardName.wrath && choice != 2) return 0;
            if (name == CardDB.cardName.nourish && choice != 2) return 0;
            int carddraw = cardDrawBattleCryDatabase[name];
            if (name == CardDB.cardName.harrisonjones)
            {
                carddraw = p.enemyWeaponDurability;
                if (carddraw == 0 && (p.enemyHeroName != HeroEnum.mage && p.enemyHeroName != HeroEnum.warlock && p.enemyHeroName!= HeroEnum.priest)) return 5;
            }
            if (name == CardDB.cardName.divinefavor)
            {
                carddraw = p.enemyAnzCards + p.enemycarddraw - (p.owncards.Count);
                if (carddraw == 0) return 500;
            }
            
            if (name == CardDB.cardName.battlerage)
            {
                carddraw = 0;
                foreach (Minion mnn in p.ownMinions)
                {
                    if (mnn.wounded) carddraw++;
                }
                if (carddraw == 0) return 500;
            }

            if (name == CardDB.cardName.slam)
            {
                Minion m = new Minion();
                if (target >= 0 && target <= 9)
                {
                    m = p.ownMinions[target];
                }
                if (target >= 10 && target <= 19)
                {
                    m = p.enemyMinions[target-10];
                }
                carddraw=0;
                if (m.Hp >= 3) carddraw = 1;
                if (carddraw == 0) return 4;
            }

            if (name == CardDB.cardName.mortalcoil)
            {
                Minion m = new Minion();
                if (target >= 0 && target <= 9)
                {
                    m = p.ownMinions[target];
                }
                if (target >= 10 && target <= 19)
                {
                    m = p.enemyMinions[target - 10];
                }
                carddraw = 0;
                if (m.Hp == 1) carddraw = 1;
                if (carddraw == 0) return 3;
            }

            if (name == CardDB.cardName.lifetap )
            {
                return Math.Max(-carddraw + 2*p.playactions.Count + p.ownMaxMana - p.mana,0);
            }

            if (p.owncards.Count + carddraw > 10) return 15 * (p.owncarddraw + p.owncards.Count - 10);
            if (p.owncards.Count + p.cardsPlayedThisTurn > 5) return 5;

            return -carddraw + 2*p.playactions.Count + p.ownMaxMana - p.mana;
            /*pen = -carddraw + p.ownMaxMana - p.mana;
            return pen;*/
        }

        private int getCardDrawofEffectMinions(CardDB.Card card, Playfield p)
        {
            int pen = 0;
            int carddraw=0;
            if (card.type == CardDB.cardtype.SPELL)
            {
                foreach (Minion mnn in p.ownMinions)
                {
                    if(mnn.name == CardDB.cardName.gadgetzanauctioneer ) carddraw++;
                }
            }

            if (card.type == CardDB.cardtype.MOB && (TAG_RACE)card.race == TAG_RACE.PET)
            {
                foreach (Minion mnn in p.ownMinions)
                {
                    if (mnn.name == CardDB.cardName.starvingbuzzard) carddraw++;
                }
            }

            if (carddraw==0) return 0;

            if (p.owncards.Count >= 5) return 0;
            pen = -carddraw + p.ownMaxMana - p.mana + p.playactions.Count;

            return pen;
        }

        private int getCardDiscardPenality(CardDB.cardName name, Playfield p)
        {
            if (p.owncards.Count <= 1) return 0;
            int pen = 0;
            if (this.cardDiscardDatabase.ContainsKey(name))
            {
                int newmana=p.mana-cardDiscardDatabase[name];
                bool canplayanothercard = false;
                foreach (Handmanager.Handcard hc in p.owncards)
                {
                    if (this.cardDiscardDatabase.ContainsKey(hc.card.name)) continue;
                    if (hc.card.getManaCost(p,hc.manacost) <= newmana)
                    {
                        canplayanothercard = true;
                    }
                }
                if (canplayanothercard) pen += 10;

            }

            return pen;
        }

        private int getDestroyOwnPenality(CardDB.cardName name, int target, Playfield p, bool lethal)
        {
            if (!this.destroyOwnDatabase.ContainsKey(name)) return 0;
            int pen = 0;
            if ((name == CardDB.cardName.brawl || name == CardDB.cardName.deathwing || name == CardDB.cardName.twistingnether) && p.mobsplayedThisTurn >= 1) return 500;

            if (name == CardDB.cardName.brawl || name == CardDB.cardName.twistingnether)
            {
                int highminion = 0;
                int veryhighminion = 0;
                foreach (Minion m in p.enemyMinions)
                {
                    if (m.Angr >= 5 || m.Hp >= 5) highminion++;
                    if (m.Angr >= 8 || m.Hp >= 8) veryhighminion++;
                }

                if (highminion >= 2 || veryhighminion >= 1)
                {
                    return 0;
                }

                if (p.enemyMinions.Count <= 2 || p.enemyMinions.Count + 2 <= p.ownMinions.Count || p.ownMinions.Count >= 3)
                {
                    return 30;
                }
            }

            if (target >= 0 && target <= 9)
            {
                // dont destroy owns ;_; (except mins with deathrattle effects)

                Minion m = p.ownMinions[target];
                if (m.handcard.card.deathrattle) return 10;
                if (lethal && name == CardDB.cardName.sacrificialpact)
                {
                    int beasts = 0;
                    foreach (Minion mm in p.ownMinions)
                    {
                        if (mm.Ready && mm.handcard.card.name == CardDB.cardName.lightwarden) beasts++;
                    }
                    if (beasts == 0) return 500;
                }
                else
                {

                    return 500;
                }
            }

            return pen;
        }

        private int getDestroyPenality(CardDB.cardName name, int target, Playfield p)
        {
            if (!this.destroyDatabase.ContainsKey(name)) return 0;
            int pen = 0;

            if (target >= 10 && target <= 19)
            {
                // dont destroy owns ;_; (except mins with deathrattle effects)

                Minion m = p.enemyMinions[target-10];

                if (m.Angr >= 4 || m.Hp >= 5)
                {
                    pen = 0; // so we dont destroy cheap ones :D
                }
                else
                {
                    pen = 25;
                }

            }

            return pen;
        }

        private int getSpecialCardComboPenalitys(CardDB.Card card, int target, Playfield p, bool lethal, int choice)
        {
            CardDB.cardName name = card.name;

            if (lethal && card.type == CardDB.cardtype.MOB)
            {

                if (this.buffingMinionsDatabase.ContainsKey(name))
                {
                    if (name == CardDB.cardName.timberwolf || name == CardDB.cardName.houndmaster)
                    {
                        int beasts = 0;
                        foreach (Minion mm in p.ownMinions)
                        {
                            if ((TAG_RACE)mm.handcard.card.race == TAG_RACE.PET) beasts ++;
                        }
                        if (beasts == 0) return 500;
                    }
                    if (name == CardDB.cardName.southseacaptain)
                    {
                        int beasts = 0;
                        foreach (Minion mm in p.ownMinions)
                        {
                            if ((TAG_RACE)mm.handcard.card.race == TAG_RACE.PIRATE) beasts++;
                        }
                        if (beasts == 0) return 500;
                    }
                    if (name == CardDB.cardName.murlocwarleader || name ==CardDB.cardName.grimscaleoracle || name == CardDB.cardName.coldlightseer)
                    {
                        int beasts = 0;
                        foreach (Minion mm in p.ownMinions)
                        {
                            if ((TAG_RACE)mm.handcard.card.race == TAG_RACE.MURLOC) beasts++;
                        }
                        if (beasts == 0) return 500;
                    }
                }
                else
                {
                    if (name == CardDB.cardName.theblackknight)
                    {
                        int beasts = 0;
                        foreach (Minion mm in p.enemyMinions)
                        {
                            if (mm.taunt) beasts++;
                        }
                        if (beasts == 0) return 500;
                    }
                    else
                    {
                        if ((this.HealTargetDatabase.ContainsKey(name) || this.HealHeroDatabase.ContainsKey(name) || this.HealAllDatabase.ContainsKey(name)))
                        {
                            int beasts = 0;
                            foreach (Minion mm in p.ownMinions)
                            {
                                if (mm.Ready && mm.handcard.card.name == CardDB.cardName.lightwarden) beasts++;
                            }
                            if (beasts == 0) return 500;
                        }
                        else
                        {
                            if (!(name == CardDB.cardName.nightblade || card.Charge || this.silenceDatabase.ContainsKey(name) || ((TAG_RACE)card.race == TAG_RACE.PET && p.ownMinions.Find(x => x.name == CardDB.cardName.tundrarhino) != null) || (p.ownMinions.Find(x => x.name == CardDB.cardName.warsongcommander) != null && card.Attack <= 3) || p.owncards.Find(x => x.card.name == CardDB.cardName.charge) != null))
                            {
                                return 500;
                            }
                        }
                    }
                }
            }


            //some effects, which are bad :D
            int pen = 0;
            Minion m = new Minion();
            if (target >= 0 && target <= 9)
            {
                m = p.ownMinions[target];
            }
            if (target >= 10 && target <= 19)
            {
                m = p.enemyMinions[target-10];
            }

            if (card.name == CardDB.cardName.flametonguetotem && p.ownMinions.Count == 0)
            {
                return 100;
            }

            if (name == CardDB.cardName.windfury && !m.Ready) return 500;

            if ((name == CardDB.cardName.wildgrowth || name == CardDB.cardName.nourish)  && p.ownMaxMana == 9 && !(p.ownHeroName == HeroEnum.thief && p.cardsPlayedThisTurn == 0))
            {
                return 500;
            }

            if (name == CardDB.cardName.sylvanaswindrunner)
            {
                if (p.enemyMinions.Count == 0)
                {
                    return 10;
                }
            }

            if (name == CardDB.cardName.betrayal && target >=10 && target <= 19)
            {
                if (m.Angr == 0) return 30;
                if (p.enemyMinions.Count == 1) return 30;
            }


            if (name == CardDB.cardName.houndmaster)
            {
                if(target == -1) return 50;
            }

            if (name == CardDB.cardName.bite)
            {
                if ((p.ownHeroNumAttackThisTurn == 0 || (p.ownHeroWindfury && p.ownHeroNumAttackThisTurn == 1)) && !p.ownHeroFrozen)
                {

                }
                else
                {
                    return 20;
                }
            }

            if (name == CardDB.cardName.deadlypoison)
            {
                    return p.ownWeaponDurability * 2;
            }

            if (name == CardDB.cardName.coldblood)
            {
                if (lethal) return 0;
                return 25;
            }

            if (name == CardDB.cardName.bloodmagethalnos)
            {
                return 10;
            }

            if (name == CardDB.cardName.frostbolt)
            {
                if (target >= 10 && target <= 19)
                {
                    if (m.handcard.card.cost <= 2)
                        return 15;
                }
                return 15;
            }

            if (!lethal && choice == 1 && name == CardDB.cardName.druidoftheclaw)
            {
                 return 20;
            }


            if (name == CardDB.cardName.poweroverwhelming)
            {
                if (target >= 0 && target <= 9 && !m.Ready)
                {
                    return 500;
                }
            }

            if (name == CardDB.cardName.frothingberserker)
            {
                if (p.cardsPlayedThisTurn >= 1) pen = 5;
            }

            if (name == CardDB.cardName.handofprotection)
            {
                if (m.Hp ==1) pen = 15;
            }

            if (lethal)
            {
                if (name == CardDB.cardName.corruption)
                {
                    int beasts = 0;
                    foreach (Minion mm in p.ownMinions)
                    {
                        if (mm.Ready && (mm.handcard.card.name == CardDB.cardName.questingadventurer || mm.handcard.card.name == CardDB.cardName.archmageantonidas || mm.handcard.card.name == CardDB.cardName.manaaddict || mm.handcard.card.name == CardDB.cardName.manawyrm || mm.handcard.card.name == CardDB.cardName.wildpyromancer)) beasts++;
                    }
                    if (beasts == 0) return 500;
                }
            }

            if ( name == CardDB.cardName.divinespirit)
            {
                if (lethal)
                {
                    if (target >= 10 && target <= 19)
                    {
                        if (!m.taunt)
                        {
                            return 500;
                        }
                        else
                        {
                            // combo for killing with innerfire and biggamehunter
                            if (p.owncards.Find(x => x.card.name == CardDB.cardName.biggamehunter) != null && p.owncards.Find(x => x.card.name == CardDB.cardName.innerfire) != null && (m.Hp >= 4 || (p.owncards.Find(x => x.card.name == CardDB.cardName.divinespirit) != null && m.Hp >= 2)))
                            {
                                return 0;
                            }
                            return 500;
                        }
                    }
                }
                else
                {
                    if (target >= 10 && target <= 19)
                    {

                            // combo for killing with innerfire and biggamehunter
                        if (p.owncards.Find(x => x.card.name == CardDB.cardName.biggamehunter) != null && p.owncards.Find(x => x.card.name == CardDB.cardName.innerfire) != null && m.Hp >= 4)
                            {
                                return 0;
                            }
                            return 500;
                    }

                }

                if (target >= 0 && target <= 9)
                {

                    if (m.Hp >= 4)
                    {
                        return 0;
                    }
                    return 15;
                }
 
            }

            if (name == CardDB.cardName.facelessmanipulator )
            {
                if (target == -1 ) 
                {
                    return 50;
                }
                if (m.Angr>=5 || m.handcard.card.cost>=5 || ( m.handcard.card.rarity == 5 || m.handcard.card.cost>=3))
                {
                    return 0;
                }
                return 49;
            }

            if (name == CardDB.cardName.knifejuggler)
            {
                if (p.mobsplayedThisTurn>=1)
                {
                    return 10;
                }
            }

            if ((name == CardDB.cardName.polymorph || name == CardDB.cardName.hex))
            {

                if (target >= 0 && target <= 9)
                {
                    return 500;
                }

                if (target >= 10 && target <= 19)
                {
                    Minion frog = p.enemyMinions[target - 10];
                    if (this.priorityTargets.ContainsKey(frog.name)) return 0;
                    if (frog.Angr >= 4 && frog.Hp >=4) return 0;
                    return 30;
                }
               
            }

            if ((card.name == CardDB.cardName.biggamehunter) && (target == -1 || target <= 9))
            {
                return 40;
            }

            if ((name == CardDB.cardName.defenderofargus || name == CardDB.cardName.sunfuryprotector) && p.ownMinions.Count == 1)
            {
                return 40;
            }
            if ((name == CardDB.cardName.defenderofargus || name == CardDB.cardName.sunfuryprotector) && p.ownMinions.Count == 0)
            {
                return 50;
            }

            if (name == CardDB.cardName.unleashthehounds) 
            {
                if (p.enemyMinions.Count <= 1)
                {
                    return 20;
                }
            }

            if (name == CardDB.cardName.equality) // aoe penality
            {
                if (p.enemyMinions.Count <= 2 || (p.ownMinions.Count - p.enemyMinions.Count  >= 1))
                {
                    return 20;
                }
            }

            if (name == CardDB.cardName.bloodsailraider && p.ownWeaponDurability==0)
            {
                //if you have bloodsailraider and no weapon equiped, but own a weapon:
                foreach (Handmanager.Handcard hc in p.owncards)
                {
                    if (hc.card.type == CardDB.cardtype.WEAPON) return 10;
                }
            }

            if (name == CardDB.cardName.theblackknight)
            {
                if (target == -1)
                {
                    return 50;
                }

                foreach (Minion mnn in p.enemyMinions)
                {
                    if (mnn.taunt && (m.Angr >= 3 || m.Hp >= 3)) return 0;
                }
                return 20;
            }

            if (name == CardDB.cardName.innerfire)
            {
                if (m.name == CardDB.cardName.lightspawn) pen = 500;
            }

            if (name == CardDB.cardName.huntersmark)
            {
                if (target >= 0 && target <= 9) pen = 500; // dont use on own minions
                if (target >= 10 && target <= 19 && (p.enemyMinions[target - 10].Hp <= 4) && p.enemyMinions[target - 10].Angr <= 4) // only use on strong minions
                {
                    pen = 20;
                }
            }
            if (name == CardDB.cardName.aldorpeacekeeper && target == -1)
            {
                pen = 30;
            }
            if ((name == CardDB.cardName.aldorpeacekeeper || name == CardDB.cardName.humility ) && target >= 0 && target <= 19)
            {
                if (target >= 0 && target <= 9) pen = 500; // dont use on own minions
                if (target >= 10 && target <= 19 && p.enemyMinions[target - 10].Angr <= 3) // only use on strong minions
                {
                    pen = 30;
                }
                if (m.name == CardDB.cardName.lightspawn) pen = 500;
            }

            if (name == CardDB.cardName.shatteredsuncleric && target == -1) {pen = 10;}
            if (name == CardDB.cardName.argentprotector && target == -1){pen = 10;}

            if (name == CardDB.cardName.defiasringleader && p.cardsPlayedThisTurn == 0)
            {pen = 10;}
            if (name == CardDB.cardName.bloodknight)
            {
                int shilds = 0;
                foreach (Minion min in p.ownMinions)
                {
                    if (min.divineshild)
                    {
                        shilds++;
                    }
                }
                foreach (Minion min in p.enemyMinions)
                {
                    if (min.divineshild)
                    {
                        shilds++;
                    }
                }
                if (shilds == 0)
                {
                    pen = 10;
                }
            }
            if (name == CardDB.cardName.direwolfalpha)
            {
                int ready = 0;
            foreach (Minion min in p.ownMinions)
            {
                if (min.Ready)
                {ready++;}
            }
                if (ready == 0)
                {pen = 5;}
            }
            if (name == CardDB.cardName.abusivesergeant)
            {
                int ready = 0;
                foreach (Minion min in p.ownMinions)
                {
                    if (min.Ready)
                    {ready++;}
                }
                if (ready == 0)
                {
                    pen = 5;
                }
            }


            if (returnHandDatabase.ContainsKey(name))
            {
                if (name == CardDB.cardName.vanish)
                {
                    //dont vanish if we have minons on board wich are ready
                    bool haveready = false;
                    foreach(Minion mins in p.ownMinions)
                    {
                        if (mins.Ready) haveready = true;
                    }
                    if (haveready) pen += 10;
                }

                if (target >= 0 && target <= 9)
                {
                    Minion mnn = p.ownMinions[target];
                    if (mnn.Ready) pen += 10;
                }
            }

            return pen;
        }

        private int playSecretPenality(CardDB.Card card, Playfield p)
        {
            //penality if we play secret and have playable kirintormage
            int pen = 0;
            if (card.Secret)
            {
                foreach (Handmanager.Handcard hc in p.owncards)
                {
                    if (hc.card.name == CardDB.cardName.kirintormage && p.mana >= hc.getManaCost(p))
                    {
                        pen = 500;
                    }
                }
            }

            return pen;
        }

        ///secret strategys pala
        /// -Attack lowest enemy. If you can’t, use noncombat means to kill it. 
        /// -attack with something able to withstand 2 damage. 
        /// -Then play something that had low health to begin with to dodge Repentance. 
        /// 
        ///secret strategys hunter
        /// - kill enemys with your minions with 2 or less heal.
        ///  - Use the smallest minion available for the first attack 
        ///  - Then smack them in the face with whatever’s left. 
        ///  - If nothing triggered until then, it’s a Snipe, so throw something in front of it that won’t die or is expendable.
        /// 
        ///secret strategys mage
        /// - Play a small minion to trigger Mirror Entity.
        /// Then attack the mage directly with the smallest minion on your side. 
        /// If nothing triggered by that point, it’s either Spellbender or Counterspell, so hold your spells until you can (and have to!) deal with either. 

        private int getPlayCardSecretPenality(CardDB.Card c, Playfield p)
        {
            int pen = 0;
            if (p.enemySecretCount == 0)
            {
                return 0;
            }

            int attackedbefore = 0;

            foreach (Minion mnn in p.ownMinions)
            {
                if (mnn.numAttacksThisTurn >= 1) attackedbefore ++;
            }

            if (c.name == CardDB.cardName.acidicswampooze && (p.enemyHeroName == HeroEnum.warrior || p.enemyHeroName == HeroEnum.thief || p.enemyHeroName == HeroEnum.pala))
            {
                if (p.enemyHeroName == HeroEnum.thief && p.enemyWeaponAttack <= 2)
                {
                    pen += 100;
                }
                else
                {
                    if (p.enemyWeaponAttack <= 1)
                    {
                        pen += 100;
                    }
                }
            }

            if (p.enemyHeroName == HeroEnum.hunter)
            {
                if (c.type == CardDB.cardtype.MOB && (attackedbefore == 0 || c.Health <= 4 || (p.enemyHeroHp >= p.enemyHeroHpStarted && attackedbefore >= 1)))
                {
                    pen += 10;
                }
            }

            if (p.enemyHeroName == HeroEnum.mage )
            {
                if (c.type == CardDB.cardtype.MOB)
                {
                    Minion m = new Minion();
                    m.Hp = c.Health;
                    m.maxHp = c.Health;
                    m.Angr = c.Attack;
                    m.taunt = c.tank;
                    m.name = c.name;
                    //play first the small minion:
                    if ((!isOwnLowestInHand(m, p)&& p.mobsplayedThisTurn == 0) || (p.mobsplayedThisTurn ==0 && attackedbefore>=1) ) pen += 10;
                }

                if (c.type == CardDB.cardtype.SPELL && p.cardsPlayedThisTurn == p.mobsplayedThisTurn)
                {
                    pen += 10;
                }
                
            }

            if (p.enemyHeroName == HeroEnum.pala)
            {
                if (c.type == CardDB.cardtype.MOB)
                {
                    Minion m = new Minion();
                    m.Hp = c.Health;
                    m.maxHp = c.Health;
                    m.Angr = c.Attack;
                    m.taunt = c.tank;
                    m.name = c.name;
                    if ((!isOwnLowestInHand(m, p) && p.mobsplayedThisTurn == 0 )|| attackedbefore == 0) pen += 10;
                }


            }

            

            return pen;
        }

        private int getAttackSecretPenality(Minion m , Playfield p, int target)
        {
            if (p.enemySecretCount == 0)
            {
                return 0;
            }

            int pen = 0;

            int attackedbefore = 0;

            foreach (Minion mnn in p.ownMinions)
            {
                if (mnn.numAttacksThisTurn >= 1) attackedbefore ++;
            }

            if (p.enemyHeroName == HeroEnum.hunter)
            {
                bool islow = isOwnLowest(m, p);
                if (attackedbefore == 0 && islow) pen -= 20;
                if (attackedbefore == 0 && !islow) pen += 10;

                if (target == 200 && p.enemyMinions.Count >=1)
                {
                    //penality if we doestn attacked before
                    if(hasMinionsWithLowHeal(p)) pen += 10; //penality if we doestn attacked minions before
                }
            }

            if (p.enemyHeroName == HeroEnum.mage)
            {
                if (p.mobsplayedThisTurn == 0) pen += 10;

                bool islow = isOwnLowest(m, p);

                if (target == 200 && !islow)
                {
                    pen += 10;
                }
                if (target == 200 && islow && p.mobsplayedThisTurn>=1)
                {
                    pen -= 20;
                }
                
            }

            if (p.enemyHeroName == HeroEnum.pala)
            {

                bool islow = isOwnLowest(m, p);

                if (target >= 10 && target <= 20  && attackedbefore==0)
                {
                    Minion enem = p.enemyMinions[target - 10];
                    if (!isEnemyLowest(enem, p) || m.Hp <= 2) pen += 5;
                }

                if (target == 200 && !islow)
                {
                    pen += 5;
                }

                if (target == 200 && p.enemyMinions.Count >=1 && attackedbefore == 0)
                {
                    pen += 5;
                }

            }


            return pen;
        }






        private int getValueOfMinion(Minion m)
        {
            int ret = 0;
            ret += 2 * m.Angr + m.Hp;
            if (m.taunt) ret += 2;
            if (this.priorityDatabase.ContainsKey(m.name)) ret += 20 + priorityDatabase[m.name];
            return ret;
        }

        private bool isOwnLowest(Minion mnn, Playfield p)
        {
            bool ret = true;
            int val = getValueOfMinion(mnn);
            foreach (Minion m in p.ownMinions)
            {
                if (!m.Ready) continue;
                if (getValueOfMinion(m) < val) ret = false;
            }
            return ret;
        }

        private bool isOwnLowestInHand(Minion mnn, Playfield p)
        {
            bool ret = true;
            Minion m = new Minion();
            int val = getValueOfMinion(mnn);
            foreach (Handmanager.Handcard card in p.owncards)
            {
                if (card.card.type != CardDB.cardtype.MOB) continue;
                CardDB.Card c = card.card;
                m.Hp = c.Health;
                m.maxHp = c.Health;
                m.Angr = c.Attack;
                m.taunt = c.tank;
                m.name = c.name;
                if (getValueOfMinion(m) < val) ret = false;
            }
            return ret;
        }

        private int getValueOfEnemyMinion(Minion m)
        {
            int ret = 0;
            ret += m.Hp;
            if (m.taunt) ret -= 2;
            return ret;
        }

        private bool isEnemyLowest(Minion mnn, Playfield p)
        {
            bool ret = true;
            List<targett> litt= p.getAttackTargets(true);
            int val = getValueOfEnemyMinion(mnn);
            foreach (Minion m in p.enemyMinions)
            {
                if (litt.Find(x => x.target == m.id) == null) continue;
                if (getValueOfEnemyMinion(m) < val) ret = false;
            }
            return ret;
        }

        private bool hasMinionsWithLowHeal(Playfield p)
        {
            bool ret = false;
            foreach (Minion m in p.ownMinions)
            {
                if (m.Hp <= 2 && (m.Ready || this.priorityDatabase.ContainsKey(m.name))) ret = true; 
            }
            return ret;
        }



        private void setupEnrageDatabase()
        {
            enrageDatabase.Add(CardDB.cardName.amaniberserker, 0);
            enrageDatabase.Add(CardDB.cardName.angrychicken, 0);
            enrageDatabase.Add(CardDB.cardName.grommashhellscream, 0);
            enrageDatabase.Add(CardDB.cardName.ragingworgen, 0);
            enrageDatabase.Add(CardDB.cardName.spitefulsmith, 0);
            enrageDatabase.Add(CardDB.cardName.taurenwarrior, 0);
        }

        private void setupHealDatabase()
        {
            HealAllDatabase.Add(CardDB.cardName.holynova, 2);//to all own minions
            HealAllDatabase.Add(CardDB.cardName.circleofhealing, 4);//allminions
            HealAllDatabase.Add(CardDB.cardName.darkscalehealer, 2);//all friends

            HealHeroDatabase.Add(CardDB.cardName.drainlife, 2);//tohero
            HealHeroDatabase.Add(CardDB.cardName.guardianofkings, 6);//tohero
            HealHeroDatabase.Add(CardDB.cardName.holyfire, 5);//tohero
            HealHeroDatabase.Add(CardDB.cardName.priestessofelune, 4);//tohero
            HealHeroDatabase.Add(CardDB.cardName.sacrificialpact, 5);//tohero
            HealHeroDatabase.Add(CardDB.cardName.siphonsoul, 3); //tohero

            HealTargetDatabase.Add(CardDB.cardName.ancestralhealing, 1000);
            HealTargetDatabase.Add(CardDB.cardName.ancientsecrets, 5);
            HealTargetDatabase.Add(CardDB.cardName.holylight, 6);
            HealTargetDatabase.Add(CardDB.cardName.earthenringfarseer, 3);
            HealTargetDatabase.Add(CardDB.cardName.healingtouch, 8);
            HealTargetDatabase.Add(CardDB.cardName.layonhands, 8);
            HealTargetDatabase.Add(CardDB.cardName.lesserheal, 2);
            HealTargetDatabase.Add(CardDB.cardName.voodoodoctor, 2);
            HealTargetDatabase.Add(CardDB.cardName.willofmukla, 8);
            HealTargetDatabase.Add(CardDB.cardName.ancientoflore, 5);
            //HealTargetDatabase.Add(CardDB.cardName.divinespirit, 2);
        }

        private void setupDamageDatabase()
        {

            DamageHeroDatabase.Add(CardDB.cardName.headcrack, 2);

            DamageAllDatabase.Add(CardDB.cardName.abomination,2);
            DamageAllDatabase.Add(CardDB.cardName.dreadinfernal, 1);
            DamageAllDatabase.Add(CardDB.cardName.hellfire, 3);
            DamageAllDatabase.Add(CardDB.cardName.whirlwind, 1);
            DamageAllDatabase.Add(CardDB.cardName.yseraawakens, 5);

            DamageAllEnemysDatabase.Add(CardDB.cardName.arcaneexplosion,1);
            DamageAllEnemysDatabase.Add(CardDB.cardName.consecration, 1);
            DamageAllEnemysDatabase.Add(CardDB.cardName.fanofknives, 1);
            DamageAllEnemysDatabase.Add(CardDB.cardName.flamestrike, 4);
            DamageAllEnemysDatabase.Add(CardDB.cardName.holynova, 2);
            DamageAllEnemysDatabase.Add(CardDB.cardName.lightningstorm, 2);
            DamageAllEnemysDatabase.Add(CardDB.cardName.stomp, 1);
            DamageAllEnemysDatabase.Add(CardDB.cardName.madbomber, 1);
            DamageAllEnemysDatabase.Add(CardDB.cardName.swipe, 4);//1 to others
            
            DamageRandomDatabase.Add(CardDB.cardName.arcanemissiles,1);
            DamageRandomDatabase.Add(CardDB.cardName.avengingwrath, 1);
            DamageRandomDatabase.Add(CardDB.cardName.cleave, 2);
            DamageRandomDatabase.Add(CardDB.cardName.forkedlightning, 2);
            DamageRandomDatabase.Add(CardDB.cardName.multishot, 3);

            DamageTargetSpecialDatabase.Add(CardDB.cardName.crueltaskmaster, 1); // gives 2 attack
            DamageTargetSpecialDatabase.Add(CardDB.cardName.innerrage, 1); // gives 2 attack

            DamageTargetSpecialDatabase.Add(CardDB.cardName.demonfire, 2); // friendly demon get +2/+2
            DamageTargetSpecialDatabase.Add(CardDB.cardName.earthshock, 1); //SILENCE /good for raggy etc or iced
            DamageTargetSpecialDatabase.Add(CardDB.cardName.hammerofwrath, 3); //draw a card
            DamageTargetSpecialDatabase.Add(CardDB.cardName.holywrath, 2);//draw a card
            DamageTargetSpecialDatabase.Add(CardDB.cardName.roguesdoit, 4);//draw a card
            DamageTargetSpecialDatabase.Add(CardDB.cardName.shiv, 1);//draw a card
            DamageTargetSpecialDatabase.Add(CardDB.cardName.savagery, 1);//dmg=herodamage
            DamageTargetSpecialDatabase.Add(CardDB.cardName.shieldslam, 1);//dmg=armor
            DamageTargetSpecialDatabase.Add(CardDB.cardName.slam, 2);//draw card if it survives
            DamageTargetSpecialDatabase.Add(CardDB.cardName.soulfire, 4);//delete a card


            DamageTargetDatabase.Add(CardDB.cardName.keeperofthegrove, 2); // or silence
            DamageTargetDatabase.Add(CardDB.cardName.wrath, 3);//or 1 + card

            DamageTargetDatabase.Add(CardDB.cardName.coneofcold, 1);
            DamageTargetDatabase.Add(CardDB.cardName.arcaneshot, 2);
            DamageTargetDatabase.Add(CardDB.cardName.backstab, 2);
            DamageTargetDatabase.Add(CardDB.cardName.baneofdoom, 2);
            DamageTargetDatabase.Add(CardDB.cardName.barreltoss, 2);
            DamageTargetDatabase.Add(CardDB.cardName.blizzard, 2);
            DamageTargetDatabase.Add(CardDB.cardName.drainlife, 2);
            DamageTargetDatabase.Add(CardDB.cardName.elvenarcher, 1);
            DamageTargetDatabase.Add(CardDB.cardName.eviscerate, 3);
            DamageTargetDatabase.Add(CardDB.cardName.explosiveshot, 5);
            DamageTargetDatabase.Add(CardDB.cardName.fireelemental, 3);
            DamageTargetDatabase.Add(CardDB.cardName.fireball, 6);
            DamageTargetDatabase.Add(CardDB.cardName.fireblast, 1);
            DamageTargetDatabase.Add(CardDB.cardName.frostshock, 1);
            DamageTargetDatabase.Add(CardDB.cardName.frostbolt, 1);
            DamageTargetDatabase.Add(CardDB.cardName.hoggersmash, 4);
            DamageTargetDatabase.Add(CardDB.cardName.holyfire, 5);
            DamageTargetDatabase.Add(CardDB.cardName.holysmite, 2);
            DamageTargetDatabase.Add(CardDB.cardName.icelance, 4);//only if iced
            DamageTargetDatabase.Add(CardDB.cardName.ironforgerifleman, 1);
            DamageTargetDatabase.Add(CardDB.cardName.killcommand, 3);//or 5
            DamageTargetDatabase.Add(CardDB.cardName.lavaburst, 5);
            DamageTargetDatabase.Add(CardDB.cardName.lightningbolt, 2);
            DamageTargetDatabase.Add(CardDB.cardName.mindshatter, 3);
            DamageTargetDatabase.Add(CardDB.cardName.mindspike, 2);
            DamageTargetDatabase.Add(CardDB.cardName.moonfire, 1);
            DamageTargetDatabase.Add(CardDB.cardName.mortalcoil, 1);
            DamageTargetDatabase.Add(CardDB.cardName.mortalstrike, 4);
            DamageTargetDatabase.Add(CardDB.cardName.perditionsblade, 1);
            DamageTargetDatabase.Add(CardDB.cardName.pyroblast, 10);
            DamageTargetDatabase.Add(CardDB.cardName.shadowbolt, 4);
            DamageTargetDatabase.Add(CardDB.cardName.shotgunblast, 1);
            DamageTargetDatabase.Add(CardDB.cardName.si7agent, 2);
            DamageTargetDatabase.Add(CardDB.cardName.starfall, 5);
            DamageTargetDatabase.Add(CardDB.cardName.starfire, 5);//draw a card, but its to strong
            DamageTargetDatabase.Add(CardDB.cardName.stormpikecommando, 5);
            


            


        }

        private void setupsilenceDatabase()
        {
            this.silenceDatabase.Add(CardDB.cardName.dispel, 1);
            this.silenceDatabase.Add(CardDB.cardName.earthshock, 1);
            this.silenceDatabase.Add(CardDB.cardName.massdispel, 1);
            this.silenceDatabase.Add(CardDB.cardName.silence, 1);
            this.silenceDatabase.Add(CardDB.cardName.keeperofthegrove, 1);
            this.silenceDatabase.Add(CardDB.cardName.ironbeakowl, 1);
            this.silenceDatabase.Add(CardDB.cardName.spellbreaker, 1);
        }

        private void setupPriorityList()
        {
            this.priorityDatabase.Add(CardDB.cardName.prophetvelen, 5);
            this.priorityDatabase.Add(CardDB.cardName.archmageantonidas, 5);
            this.priorityDatabase.Add(CardDB.cardName.flametonguetotem, 6);
            this.priorityDatabase.Add(CardDB.cardName.raidleader, 5);
            this.priorityDatabase.Add(CardDB.cardName.grimscaleoracle, 5);
            this.priorityDatabase.Add(CardDB.cardName.direwolfalpha, 6);
            this.priorityDatabase.Add(CardDB.cardName.murlocwarleader, 5);
            this.priorityDatabase.Add(CardDB.cardName.southseacaptain, 5);
            this.priorityDatabase.Add(CardDB.cardName.stormwindchampion, 5);
            this.priorityDatabase.Add(CardDB.cardName.timberwolf, 5);
            this.priorityDatabase.Add(CardDB.cardName.leokk, 5);
            this.priorityDatabase.Add(CardDB.cardName.northshirecleric, 5);
            this.priorityDatabase.Add(CardDB.cardName.sorcerersapprentice, 3);
            this.priorityDatabase.Add(CardDB.cardName.summoningportal, 5);
            this.priorityDatabase.Add(CardDB.cardName.pintsizedsummoner, 3);
            this.priorityDatabase.Add(CardDB.cardName.scavenginghyena, 5);
            this.priorityDatabase.Add(CardDB.cardName.manatidetotem, 5);
        }

        private void setupAttackBuff()
        {
            heroAttackBuffDatabase.Add(CardDB.cardName.bite, 4);
            heroAttackBuffDatabase.Add(CardDB.cardName.claw, 2);
            heroAttackBuffDatabase.Add(CardDB.cardName.heroicstrike, 2);

            this.attackBuffDatabase.Add(CardDB.cardName.abusivesergeant, 2);
            this.attackBuffDatabase.Add(CardDB.cardName.ancientofwar, 5); //choice1
            this.attackBuffDatabase.Add(CardDB.cardName.bananas, 1);
            this.attackBuffDatabase.Add(CardDB.cardName.bestialwrath, 2); // NEVER ON enemy MINION
            this.attackBuffDatabase.Add(CardDB.cardName.blessingofkings, 4);
            this.attackBuffDatabase.Add(CardDB.cardName.blessingofmight, 3);
            this.attackBuffDatabase.Add(CardDB.cardName.coldblood, 2);
            this.attackBuffDatabase.Add(CardDB.cardName.crueltaskmaster, 2);
            this.attackBuffDatabase.Add(CardDB.cardName.darkirondwarf, 2);
            this.attackBuffDatabase.Add(CardDB.cardName.innerrage, 2);
            this.attackBuffDatabase.Add(CardDB.cardName.markofnature, 4);//choice1 
            this.attackBuffDatabase.Add(CardDB.cardName.markofthewild, 2);
            this.attackBuffDatabase.Add(CardDB.cardName.nightmare, 5); //destroy minion on next turn
            this.attackBuffDatabase.Add(CardDB.cardName.rampage, 3);//only damaged minion 
            this.attackBuffDatabase.Add(CardDB.cardName.uproot, 5);

        }

        private void setupHealthBuff()
        {

            this.healthBuffDatabase.Add(CardDB.cardName.ancientofwar, 5);//choice2
            this.healthBuffDatabase.Add(CardDB.cardName.bananas, 1);
            this.healthBuffDatabase.Add(CardDB.cardName.blessingofkings, 4);
            this.healthBuffDatabase.Add(CardDB.cardName.markofnature, 4);//choice2
            this.healthBuffDatabase.Add(CardDB.cardName.markofthewild, 2);
            this.healthBuffDatabase.Add(CardDB.cardName.nightmare, 5);
            this.healthBuffDatabase.Add(CardDB.cardName.powerwordshield, 2);
            this.healthBuffDatabase.Add(CardDB.cardName.rampage, 3);
            this.healthBuffDatabase.Add(CardDB.cardName.rooted, 5);

            this.tauntBuffDatabase.Add(CardDB.cardName.markofnature, 1);
            this.tauntBuffDatabase.Add(CardDB.cardName.markofthewild, 1);
            this.tauntBuffDatabase.Add(CardDB.cardName.rooted, 1);


        }

        private void setupCardDrawBattlecry()
        {
            cardDrawBattleCryDatabase.Add(CardDB.cardName.wrath, 1); //choice=2
            cardDrawBattleCryDatabase.Add(CardDB.cardName.ancientoflore, 2);// choice =1
            cardDrawBattleCryDatabase.Add(CardDB.cardName.nourish, 3); //choice = 2
            cardDrawBattleCryDatabase.Add(CardDB.cardName.ancientteachings, 2);
            cardDrawBattleCryDatabase.Add(CardDB.cardName.excessmana, 1);
            cardDrawBattleCryDatabase.Add(CardDB.cardName.starfire, 1);
            cardDrawBattleCryDatabase.Add(CardDB.cardName.azuredrake, 1);
            cardDrawBattleCryDatabase.Add(CardDB.cardName.coldlightoracle, 2);
            cardDrawBattleCryDatabase.Add(CardDB.cardName.gnomishinventor, 1);
            cardDrawBattleCryDatabase.Add(CardDB.cardName.harrisonjones, 0);
            cardDrawBattleCryDatabase.Add(CardDB.cardName.noviceengineer, 1);
            cardDrawBattleCryDatabase.Add(CardDB.cardName.roguesdoit, 1);
            cardDrawBattleCryDatabase.Add(CardDB.cardName.arcaneintellect, 1);
            cardDrawBattleCryDatabase.Add(CardDB.cardName.hammerofwrath, 1);
            cardDrawBattleCryDatabase.Add(CardDB.cardName.holywrath, 1);
            cardDrawBattleCryDatabase.Add(CardDB.cardName.layonhands, 3);
            cardDrawBattleCryDatabase.Add(CardDB.cardName.massdispel, 1);
            cardDrawBattleCryDatabase.Add(CardDB.cardName.powerwordshield, 1);
            cardDrawBattleCryDatabase.Add(CardDB.cardName.fanofknives, 1);
            cardDrawBattleCryDatabase.Add(CardDB.cardName.shiv, 1);
            cardDrawBattleCryDatabase.Add(CardDB.cardName.sprint, 4);
            cardDrawBattleCryDatabase.Add(CardDB.cardName.farsight, 1);
            cardDrawBattleCryDatabase.Add(CardDB.cardName.lifetap, 1);
            cardDrawBattleCryDatabase.Add(CardDB.cardName.commandingshout, 1);
            cardDrawBattleCryDatabase.Add(CardDB.cardName.shieldblock, 1);
            cardDrawBattleCryDatabase.Add(CardDB.cardName.slam, 1); //if survives
            cardDrawBattleCryDatabase.Add(CardDB.cardName.mortalcoil, 1);//only if kills
            cardDrawBattleCryDatabase.Add(CardDB.cardName.battlerage, 1);//only if wounded own minions
            cardDrawBattleCryDatabase.Add(CardDB.cardName.divinefavor, 1);//only if enemy has more cards than you
        }

        private void setupDiscardCards()
        {
            cardDiscardDatabase.Add(CardDB.cardName.doomguard, 5);
            cardDiscardDatabase.Add(CardDB.cardName.soulfire, 0);
            cardDiscardDatabase.Add(CardDB.cardName.succubus, 2);
        }

        private void setupDestroyOwnCards()
        {
            this.destroyOwnDatabase.Add(CardDB.cardName.brawl, 0);
            this.destroyOwnDatabase.Add(CardDB.cardName.deathwing, 0);
            this.destroyOwnDatabase.Add(CardDB.cardName.twistingnether, 0);
            this.destroyOwnDatabase.Add(CardDB.cardName.naturalize, 0);//not own mins
            this.destroyOwnDatabase.Add(CardDB.cardName.shadowworddeath, 0);//not own mins
            this.destroyOwnDatabase.Add(CardDB.cardName.shadowwordpain, 0);//not own mins
            this.destroyOwnDatabase.Add(CardDB.cardName.siphonsoul, 0);//not own mins
            this.destroyOwnDatabase.Add(CardDB.cardName.biggamehunter, 0);//not own mins
            this.destroyOwnDatabase.Add(CardDB.cardName.hungrycrab, 0);//not own mins
            this.destroyOwnDatabase.Add(CardDB.cardName.sacrificialpact, 0);//not own mins


            this.destroyDatabase.Add(CardDB.cardName.assassinate, 0);//not own mins
            this.destroyDatabase.Add(CardDB.cardName.corruption, 0);//not own mins
            this.destroyDatabase.Add(CardDB.cardName.execute, 0);//not own mins
            this.destroyDatabase.Add(CardDB.cardName.naturalize, 0);//not own mins
            this.destroyDatabase.Add(CardDB.cardName.siphonsoul, 0);//not own mins
            this.destroyDatabase.Add(CardDB.cardName.mindcontrol, 0);//not own mins

        }

        private void setupReturnBackToHandCards()
        {
            returnHandDatabase.Add(CardDB.cardName.ancientbrewmaster, 0);
            returnHandDatabase.Add(CardDB.cardName.dream, 0);
            returnHandDatabase.Add(CardDB.cardName.kidnapper, 0);//if combo
            returnHandDatabase.Add(CardDB.cardName.shadowstep, 0);
            returnHandDatabase.Add(CardDB.cardName.vanish, 0);
            returnHandDatabase.Add(CardDB.cardName.youthfulbrewmaster, 0);
        }

        private void setupHeroDamagingAOE()
        {
            this.heroDamagingAoeDatabase.Add(CardDB.cardName.unknown, 0);
        }

        private void setupSpecialMins()
        {
            this.specialMinions.Add(CardDB.cardName.amaniberserker, 0);
            this.specialMinions.Add(CardDB.cardName.angrychicken, 0);
            this.specialMinions.Add(CardDB.cardName.abomination, 0);
            this.specialMinions.Add(CardDB.cardName.acolyteofpain, 0);
            this.specialMinions.Add(CardDB.cardName.alarmobot, 0);
            this.specialMinions.Add(CardDB.cardName.archmage, 0);
            this.specialMinions.Add(CardDB.cardName.archmageantonidas, 0);
            this.specialMinions.Add(CardDB.cardName.armorsmith, 0);
            this.specialMinions.Add(CardDB.cardName.auchenaisoulpriest, 0);
            this.specialMinions.Add(CardDB.cardName.azuredrake, 0);
            this.specialMinions.Add(CardDB.cardName.barongeddon, 0);
            this.specialMinions.Add(CardDB.cardName.bloodimp, 0);
            this.specialMinions.Add(CardDB.cardName.bloodmagethalnos, 0);
            this.specialMinions.Add(CardDB.cardName.cairnebloodhoof, 0);
            this.specialMinions.Add(CardDB.cardName.cultmaster, 0);
            this.specialMinions.Add(CardDB.cardName.dalaranmage, 0);
            this.specialMinions.Add(CardDB.cardName.demolisher, 0);
            this.specialMinions.Add(CardDB.cardName.direwolfalpha, 0);
            this.specialMinions.Add(CardDB.cardName.doomsayer, 0);
            this.specialMinions.Add(CardDB.cardName.emperorcobra, 0);
            this.specialMinions.Add(CardDB.cardName.etherealarcanist, 0);
            this.specialMinions.Add(CardDB.cardName.flametonguetotem, 0);
            this.specialMinions.Add(CardDB.cardName.flesheatingghoul, 0);
            this.specialMinions.Add(CardDB.cardName.gadgetzanauctioneer, 0);
            this.specialMinions.Add(CardDB.cardName.grimscaleoracle, 0);
            this.specialMinions.Add(CardDB.cardName.grommashhellscream, 0);
            this.specialMinions.Add(CardDB.cardName.gruul, 0);
            this.specialMinions.Add(CardDB.cardName.gurubashiberserker, 0);
            this.specialMinions.Add(CardDB.cardName.harvestgolem, 0);
            this.specialMinions.Add(CardDB.cardName.hogger, 0);
            this.specialMinions.Add(CardDB.cardName.illidanstormrage, 0);
            this.specialMinions.Add(CardDB.cardName.impmaster, 0);
            this.specialMinions.Add(CardDB.cardName.knifejuggler, 0);
            this.specialMinions.Add(CardDB.cardName.koboldgeomancer, 0);
            this.specialMinions.Add(CardDB.cardName.lepergnome, 0);
            this.specialMinions.Add(CardDB.cardName.lightspawn, 0);
            this.specialMinions.Add(CardDB.cardName.lightwarden, 0);
            this.specialMinions.Add(CardDB.cardName.lightwell, 0);
            this.specialMinions.Add(CardDB.cardName.loothoarder, 0);
            this.specialMinions.Add(CardDB.cardName.lorewalkercho, 0);
            this.specialMinions.Add(CardDB.cardName.malygos, 0);
            this.specialMinions.Add(CardDB.cardName.manaaddict, 0);
            this.specialMinions.Add(CardDB.cardName.manatidetotem, 0);
            this.specialMinions.Add(CardDB.cardName.manawraith, 0);
            this.specialMinions.Add(CardDB.cardName.manawyrm, 0);
            this.specialMinions.Add(CardDB.cardName.masterswordsmith, 0);
            this.specialMinions.Add(CardDB.cardName.murloctidecaller, 0);
            this.specialMinions.Add(CardDB.cardName.murlocwarleader, 0);
            this.specialMinions.Add(CardDB.cardName.natpagle, 0);
            this.specialMinions.Add(CardDB.cardName.northshirecleric, 0);
            this.specialMinions.Add(CardDB.cardName.ogremagi, 0);
            this.specialMinions.Add(CardDB.cardName.oldmurkeye, 0);
            this.specialMinions.Add(CardDB.cardName.patientassassin, 0);
            this.specialMinions.Add(CardDB.cardName.pintsizedsummoner, 0);
            this.specialMinions.Add(CardDB.cardName.prophetvelen, 0);
            this.specialMinions.Add(CardDB.cardName.questingadventurer, 0);
            this.specialMinions.Add(CardDB.cardName.ragingworgen, 0);
            this.specialMinions.Add(CardDB.cardName.raidleader, 0);
            this.specialMinions.Add(CardDB.cardName.savannahhighmane, 0);
            this.specialMinions.Add(CardDB.cardName.scavenginghyena, 0);
            this.specialMinions.Add(CardDB.cardName.secretkeeper, 0);
            this.specialMinions.Add(CardDB.cardName.sorcerersapprentice, 0);
            this.specialMinions.Add(CardDB.cardName.southseacaptain, 0);
            this.specialMinions.Add(CardDB.cardName.spitefulsmith, 0);
            this.specialMinions.Add(CardDB.cardName.starvingbuzzard, 0);
            this.specialMinions.Add(CardDB.cardName.stormwindchampion, 0);
            this.specialMinions.Add(CardDB.cardName.summoningportal, 0);
            this.specialMinions.Add(CardDB.cardName.sylvanaswindrunner, 0);
            this.specialMinions.Add(CardDB.cardName.taurenwarrior, 0);
            this.specialMinions.Add(CardDB.cardName.thebeast, 0);
            this.specialMinions.Add(CardDB.cardName.timberwolf, 0);
            this.specialMinions.Add(CardDB.cardName.tirionfordring, 0);
            this.specialMinions.Add(CardDB.cardName.tundrarhino, 0);
            this.specialMinions.Add(CardDB.cardName.unboundelemental, 0);
            //this.specialMinions.Add(CardDB.cardName.venturecomercenary, 0);
            this.specialMinions.Add(CardDB.cardName.violetteacher, 0);
            this.specialMinions.Add(CardDB.cardName.warsongcommander, 0);
            this.specialMinions.Add(CardDB.cardName.waterelemental, 0);

            // naxx cards
            //this.specialMinions.Add(CardDB.cardName.baronrivendare, 0);
            //this.specialMinions.Add(CardDB.cardName.nerubianegg, 0);
            //this.specialMinions.Add(CardDB.cardName.undertaker, 0);
            //this.specialMinions.Add(CardDB.cardName.dancingswords, 0);
            //this.specialMinions.Add(CardDB.cardName.voidcaller, 0);
            //this.specialMinions.Add(CardDB.cardName.anubarambusher, 0);
            //this.specialMinions.Add(CardDB.cardName.darkcultist, 0);
            //this.specialMinions.Add(CardDB.cardName.webspinner, 0);

        }

        private void setupBuffingMinions()
        {
            buffingMinionsDatabase.Add(CardDB.cardName.abusivesergeant, 0);
            buffingMinionsDatabase.Add(CardDB.cardName.captaingreenskin, 0);
            buffingMinionsDatabase.Add(CardDB.cardName.cenarius, 0);
            buffingMinionsDatabase.Add(CardDB.cardName.coldlightseer, 0);
            buffingMinionsDatabase.Add(CardDB.cardName.crueltaskmaster, 0);
            buffingMinionsDatabase.Add(CardDB.cardName.darkirondwarf, 0);
            buffingMinionsDatabase.Add(CardDB.cardName.defenderofargus, 0);
            buffingMinionsDatabase.Add(CardDB.cardName.direwolfalpha, 0);
            buffingMinionsDatabase.Add(CardDB.cardName.flametonguetotem, 0);
            buffingMinionsDatabase.Add(CardDB.cardName.grimscaleoracle, 0);
            buffingMinionsDatabase.Add(CardDB.cardName.houndmaster, 0);
            buffingMinionsDatabase.Add(CardDB.cardName.leokk, 0);
            buffingMinionsDatabase.Add(CardDB.cardName.murlocwarleader, 0);
            buffingMinionsDatabase.Add(CardDB.cardName.raidleader, 0);
            buffingMinionsDatabase.Add(CardDB.cardName.shatteredsuncleric, 0);
            buffingMinionsDatabase.Add(CardDB.cardName.southseacaptain, 0);
            buffingMinionsDatabase.Add(CardDB.cardName.spitefulsmith, 0);
            buffingMinionsDatabase.Add(CardDB.cardName.stormwindchampion, 0);
            buffingMinionsDatabase.Add(CardDB.cardName.templeenforcer, 0);
            buffingMinionsDatabase.Add(CardDB.cardName.timberwolf, 0);

            buffing1TurnDatabase.Add(CardDB.cardName.abusivesergeant, 0);
            buffing1TurnDatabase.Add(CardDB.cardName.darkirondwarf, 0);

        }
        private void setupEnemyTargetPriority()
        {
            priorityTargets.Add(CardDB.cardName.angrychicken, 10);
            priorityTargets.Add(CardDB.cardName.lightwarden, 10);
            priorityTargets.Add(CardDB.cardName.secretkeeper, 10);
            priorityTargets.Add(CardDB.cardName.youngdragonhawk, 10);
            priorityTargets.Add(CardDB.cardName.bloodmagethalnos, 10);
            priorityTargets.Add(CardDB.cardName.direwolfalpha, 10);
            priorityTargets.Add(CardDB.cardName.doomsayer, 10);
            priorityTargets.Add(CardDB.cardName.knifejuggler, 10);
            priorityTargets.Add(CardDB.cardName.koboldgeomancer, 10);
            priorityTargets.Add(CardDB.cardName.manaaddict, 10);
            priorityTargets.Add(CardDB.cardName.masterswordsmith, 10);
            priorityTargets.Add(CardDB.cardName.natpagle, 10);
            priorityTargets.Add(CardDB.cardName.murloctidehunter, 10);
            priorityTargets.Add(CardDB.cardName.pintsizedsummoner, 10);
            priorityTargets.Add(CardDB.cardName.wildpyromancer, 10);
            priorityTargets.Add(CardDB.cardName.alarmobot, 10);
            priorityTargets.Add(CardDB.cardName.acolyteofpain, 10);
            priorityTargets.Add(CardDB.cardName.demolisher, 10);
            priorityTargets.Add(CardDB.cardName.flesheatingghoul, 10);
            priorityTargets.Add(CardDB.cardName.impmaster, 10);
            priorityTargets.Add(CardDB.cardName.questingadventurer, 10);
            priorityTargets.Add(CardDB.cardName.raidleader, 10);
            priorityTargets.Add(CardDB.cardName.thrallmarfarseer, 10);
            priorityTargets.Add(CardDB.cardName.cultmaster, 10);
            priorityTargets.Add(CardDB.cardName.leeroyjenkins, 10);
            priorityTargets.Add(CardDB.cardName.violetteacher, 10);
            priorityTargets.Add(CardDB.cardName.gadgetzanauctioneer, 10);
            priorityTargets.Add(CardDB.cardName.hogger, 10);
            priorityTargets.Add(CardDB.cardName.illidanstormrage, 10);
            priorityTargets.Add(CardDB.cardName.barongeddon, 10);
            priorityTargets.Add(CardDB.cardName.stormwindchampion, 10);
            priorityTargets.Add(CardDB.cardName.gurubashiberserker, 10);
            //priorityTargets.Add(CardDB.cardName.cairnebloodhoof, 19);
            //priorityTargets.Add(CardDB.cardName.harvestgolem, 16);

            //warrior cards
            priorityTargets.Add(CardDB.cardName.frothingberserker, 10);
            priorityTargets.Add(CardDB.cardName.warsongcommander, 10);

            //warlock cards
            priorityTargets.Add(CardDB.cardName.summoningportal, 10);

            //shaman cards
            priorityTargets.Add(CardDB.cardName.dustdevil, 10);
            priorityTargets.Add(CardDB.cardName.wrathofairtotem, 1);
            priorityTargets.Add(CardDB.cardName.flametonguetotem, 10);
            priorityTargets.Add(CardDB.cardName.manatidetotem, 10);
            priorityTargets.Add(CardDB.cardName.unboundelemental, 10);

            //rogue cards

            //priest cards
            priorityTargets.Add(CardDB.cardName.northshirecleric, 10);
            priorityTargets.Add(CardDB.cardName.lightwell, 10);
            priorityTargets.Add(CardDB.cardName.auchenaisoulpriest, 10);
            priorityTargets.Add(CardDB.cardName.prophetvelen, 10);

            //paladin cards

            //mage cards
            priorityTargets.Add(CardDB.cardName.manawyrm, 10);
            priorityTargets.Add(CardDB.cardName.sorcerersapprentice, 10);
            priorityTargets.Add(CardDB.cardName.etherealarcanist, 10);
            priorityTargets.Add(CardDB.cardName.archmageantonidas, 10);

            //hunter cards
            priorityTargets.Add(CardDB.cardName.timberwolf, 10);
            priorityTargets.Add(CardDB.cardName.scavenginghyena, 10);
            priorityTargets.Add(CardDB.cardName.starvingbuzzard, 10);
            priorityTargets.Add(CardDB.cardName.leokk, 10);
            priorityTargets.Add(CardDB.cardName.tundrarhino, 10);
        }


    }

}
