using System;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.Events;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.MenuUI.Values;
using EnsoulSharp.SDK.Utility;
using SharpDX;
//using Color = System.Drawing.Color;

namespace T7Feeder
{
    public class Ability
    {
        public string Name { get; set; }
        public List<SpellSlot> SpellSlots { get; set; }
    }
    class Program
    {
        private static void Main(string[] args) { GameEvent.OnGameLoad += OnLoad; }
        public static AIHeroClient myhero { get { return ObjectManager.Player; } }
        private static Menu menu;

        private static Spell Ghost = null;
        private static Spell Heal = null;

        private static bool Chatted;
        private static bool TopPointReached = false;
        private static bool BotPointReached = false;
        private static bool MidPointReached = false;
        private static bool SayNoEnemies = false;
        private static bool FinishedBuild = false;

        private static readonly Vector3 OrderSpawn = new Vector3(394, 461, 171);
        private static readonly Vector3 ChaosSpawn = new Vector3(14340, 14391, 179);
        private static readonly Vector3 TopPoint = new Vector3(3142, 13402, 52.8381f);
        private static readonly Vector3 BotPoint = new Vector3(13498, 3284, 51);
        private static readonly Vector3 MidPoint = new Vector3(4131, 4155, 115);

        private static string[] Messages = { "wat", "how?" , "mate..", "-_-", "why?", "laaaaag", "oh my god this lag is unreal",
                                             "rito pls 500 ping", "sorry lag", "help pls", "nooob wtf", "team???", "i can't carry dis",
                                             "wtf how?", "wow rito nerf pls", "omg so op", "what's up with this lag?", "is the server lagging again?",
                                             "i call black magic", "pls fix rito", "this champ is bad", "i was afk", "so lucky", "much wow", "rito plox fix servers",
                                             "sry my dog had to take a sh1 t", "report this noob supp", "dont worry i have a plan", "report this useless team no help"};
        private static string[] Chats = { "/all", " " };

        private static List<Ability> ChampList = new List<Ability>()
        {
            new Ability
                {
                    Name = "Blitzcrank",
                    SpellSlots = new List<SpellSlot>() { SpellSlot.W }
                },
            new Ability
                {
                    Name = "Bard",
                    SpellSlots = new List<SpellSlot>() { SpellSlot.W }
                },
            new Ability
                {
                    Name = "DrMundo",
                    SpellSlots = new List<SpellSlot>() { SpellSlot.R }
                },
            new Ability
                {
                    Name = "Draven",
                    SpellSlots = new List<SpellSlot>() { SpellSlot.W }
                },
            new Ability
                {
                    Name = "Evelynn",
                    SpellSlots = new List<SpellSlot>() { SpellSlot.W }
                },
            new Ability
                {
                    Name = "Garen",
                    SpellSlots = new List<SpellSlot>() { SpellSlot.Q }
                },
            new Ability
                {
                    Name = "Hecarim",
                    SpellSlots = new List<SpellSlot>() { SpellSlot.E }
                },
            new Ability
                {
                    Name = "Karma",
                    SpellSlots = new List<SpellSlot>() { SpellSlot.E }
                },
            new Ability
                {
                    Name = "Kayle",
                    SpellSlots = new List<SpellSlot>() { SpellSlot.W }
                },
            new Ability
                {
                    Name = "Kennen",
                    SpellSlots = new List<SpellSlot>() { SpellSlot.E }
                },
            new Ability
                {
                    Name = "Lulu",
                    SpellSlots = new List<SpellSlot>() { SpellSlot.W }
                },
            new Ability
                {
                    Name = "MasterYi",
                    SpellSlots = new List<SpellSlot>() { SpellSlot.R }
                },
            new Ability
                {
                    Name = "Nunu",
                    SpellSlots = new List<SpellSlot>() { SpellSlot.W }
                },
            new Ability
                {
                    Name = "Olaf",
                    SpellSlots = new List<SpellSlot>() { SpellSlot.R }
                },
            new Ability
                {
                    Name = "Orianna",
                    SpellSlots = new List<SpellSlot>() { SpellSlot.W }
                },
            new Ability
                {
                    Name = "Poppy",
                    SpellSlots = new List<SpellSlot>() { SpellSlot.W }
                },
            new Ability
                {
                    Name = "Quinn",
                    SpellSlots = new List<SpellSlot>() { SpellSlot.R }
                },
            new Ability
                {
                    Name = "Rammus",
                    SpellSlots = new List<SpellSlot>() { SpellSlot.Q }
                },
            new Ability
                {
                    Name = "Rumble",
                    SpellSlots = new List<SpellSlot>() { SpellSlot.W }
                },
            new Ability
                {
                    Name = "Shyvana",
                    SpellSlots = new List<SpellSlot>() { SpellSlot.W }
                },
            new Ability
                {
                    Name = "Singed",
                    SpellSlots = new List<SpellSlot>() { SpellSlot.R }
                },
            new Ability
                {
                    Name = "Sivir",
                    SpellSlots = new List<SpellSlot>() { SpellSlot.R }
                },
            new Ability
                {
                    Name = "Skarner",
                    SpellSlots = new List<SpellSlot>() { SpellSlot.W }
                },
            new Ability
                {
                    Name = "Sona",
                    SpellSlots = new List<SpellSlot>() { SpellSlot.E }
                },
            new Ability
                {
                    Name = "Teemo",
                    SpellSlots = new List<SpellSlot>() { SpellSlot.W }
                },
            new Ability
                {
                    Name = "Twitch",
                    SpellSlots = new List<SpellSlot>() { SpellSlot.Q }
                },
            new Ability
                {
                    Name = "Udyr",
                    SpellSlots = new List<SpellSlot>() { SpellSlot.E }
                },
            new Ability
                {
                    Name = "Volibear",
                    SpellSlots = new List<SpellSlot>() { SpellSlot.Q }
                },
            new Ability
                {
                    Name = "Zilean",
                    SpellSlots = new List<SpellSlot>() { SpellSlot.W, SpellSlot.E }
                }
        };

        public static void OnLoad()
        {
            
            Game.OnUpdate += OnTick;
            GameEvent.OnGameEnd += OnEnd;
            DatMenu();

            if (myhero.GetSpellSlot("SummonerHaste") != SpellSlot.Unknown)
            {
                Ghost = new Spell(myhero.GetSpellSlot("SummonerHaste"), 600f);
            }
            if (myhero.GetSpellSlot("SummonerHeal") != SpellSlot.Unknown)
            {
                Heal = new Spell(myhero.GetSpellSlot("SummonerHeal"), 600f);
            }

            Game.Print("<font color='#0040FF'>T7</font><font color='#09FF00'> Feeder</font> : Loaded!(v1.2)");          
            Thread.Sleep(15500);
        }

        private static void OnEnd()
        {
            Game.Say("gg honor me", true);
        }

        private static void OnTick(EventArgs args)
        {
            Checks();
            if (menu.check("ABILITIES") && !myhero.IsDead) Abilities();
            if (menu.check("SPELLS") && menu.check("ACTIVE") && !myhero.IsDead) SummonerSpells();
            if (comb(menu, "MSGS") != 0) ChatOnDeath();
            if (menu.check("AUTOBUY") && !FinishedBuild) Shopping();
            if (menu.check("ACTIVE") && !myhero.IsDead) Feed();
        }

        static int comb(Menu submenu, string sig)
        {
            return submenu[sig].GetValue<MenuList>().Index;
        }
        static int slider(Menu submenu, string sig)
        {
            return submenu[sig].GetValue<MenuSlider>().Value;
        }

        private static void SummonerSpells()
        {
            if (Heal.IsReady() && !myhero.HasBuff("SRHomeguardSpeed") && !myhero.IsDead) Heal.Cast();

            if (Ghost.IsReady() && !myhero.HasBuff("SRHomeguardSpeed") && !myhero.IsDead) Ghost.Cast();
        }

        private static void Shopping()
        { //best spaghetti ever made
            if (myhero.InShop())
            {               
                if (!Items.HasItem(myhero, ItemId.Boots_of_Speed) && !Items.HasItem(myhero, ItemId.Boots_of_Mobility) && myhero.Gold >= 300)
                    myhero.BuyItem(ItemId.Boots_of_Speed);

                else if (Items.HasItem(myhero, ItemId.Boots_of_Speed) && !Items.HasItem(myhero, ItemId.Boots_of_Mobility) && myhero.Gold >= 700)
                    myhero.BuyItem(ItemId.Boots_of_Mobility);

                else if (Items.HasItem(myhero, ItemId.Boots_of_Mobility) && !Items.HasItem(myhero, ItemId.Aether_Wisp) && myhero.Gold >= 850)
                    myhero.BuyItem(ItemId.Aether_Wisp);

                else if (Items.HasItem(myhero, ItemId.Aether_Wisp) && !Items.HasItem(myhero, ItemId.Zeal) && myhero.Gold >= 1400)
                    myhero.BuyItem(ItemId.Zeal);

                else if (Items.HasItem(myhero, ItemId.Zeal) && !Items.HasItem(myhero, ItemId.Ardent_Censer) && myhero.Gold >= 1450)
                    myhero.BuyItem(ItemId.Ardent_Censer);

                else if (Items.HasItem(myhero, ItemId.Ardent_Censer) && !Items.HasItem(myhero, ItemId.Aether_Wisp) && myhero.Gold >= 850)
                    myhero.BuyItem(ItemId.Aether_Wisp);

                else if (Items.HasItem(myhero, ItemId.Aether_Wisp) && !Items.HasItem(myhero, ItemId.Phantom_Dancer) && myhero.Gold >= 1200)
                    myhero.BuyItem(ItemId.Phantom_Dancer);

                else if (Items.HasItem(myhero, ItemId.Phantom_Dancer) && !Items.HasItem(myhero, ItemId.Zeal) && myhero.Gold >= 1400)
                    myhero.BuyItem(ItemId.Zeal);

                else if (Items.HasItem(myhero, ItemId.Zeal) && !Items.HasItem(myhero, ItemId.Youmuus_Ghostblade) && myhero.Gold >= 2900)
                {
                    myhero.BuyItem(ItemId.Youmuus_Ghostblade);
                    FinishedBuild = true;
                }
            }
        }

        private static void Checks()
        {
            if (myhero.IsDead)
            {
                TopPointReached = false;
                BotPointReached = false;
                MidPointReached = false;
            }
            if (!myhero.IsDead) Chatted = false;

            if (!menu.check("ACTIVE")) SayNoEnemies = false;
        }

        private static void ChatOnDeath()
        {
            if (myhero.IsDead && Chatted == false)
            {
                switch (comb(menu, "MSGS"))
                {
                    case 0:
                        break;
                    case 1:
                        var Random1 = new Random();
                        DelayAction.Add(slider(menu, "CHATDELAY") * 1000, delegate { Game.Say(Messages[Random1.Next(0, Messages.Count())],true); });
                        Chatted = true;
                        break;
                    case 2:
                        var Random2 = new Random();
                        DelayAction.Add(slider(menu, "CHATDELAY") * 1000, delegate { Game.Say(Messages[Random2.Next(0, Messages.Count())],false); });
                        Chatted = true;
                        break;
                    case 3:
                        var Random3a = new Random();
                        var Random3b = new Random();
                        DelayAction.Add(slider(menu, "CHATDELAY") * 1000, delegate { Game.Say(Messages[Random3a.Next(0, Messages.Count())],Random3b.Next(100) < 50); });
                        Chatted = true;
                        break;
                    default:
                        break;
                }
            }
        }

        private static void Abilities()
        {
            var champ = ChampList.FirstOrDefault(h => h.Name == myhero.CharacterName);

            if (champ == null) return;

            foreach (var slot in champ.SpellSlots)
            {
                if (myhero.Spellbook.GetSpell(slot).IsUpgradable) myhero.Spellbook.LevelSpell(slot);

                if (myhero.Spellbook.GetSpell(slot).IsReady) myhero.Spellbook.CastSpell(slot, myhero.Position);
            }
        }

        private static void Feed()
        {
            switch (comb(menu, "MODE"))
            {
                case 0:
                    if (GameObjects.EnemyHeroes.Count(x => x.IsVisible) < 1)
                    {
                        if (SayNoEnemies == false)
                        {
                            Game.Print("<font color='#FF0000'>WARNING:</font> No Enemies Found To Feed!\nGoing To Mid Instead :)");
                            SayNoEnemies = true;
                        }
                        goto case 2;                        
                    }

                    var target = GameObjects.EnemyHeroes.Where(x => !x.IsDead && x.IsVisible && x.IsValidTarget()).OrderBy(y => y.Distance(myhero.Position)).FirstOrDefault();

                    if (target != null)
                    {
                        Orbwalker.Move(target.Position);
                    }
                    break;
                case 1:
                    if (!TopPointReached)
                    {
                        Orbwalker.Move(TopPoint);
                        if (myhero.Distance(TopPoint) <= 100) TopPointReached = true;
                    }
                    else
                    {
                        if (myhero.Team == GameObjectTeam.Order) Orbwalker.Move(ChaosSpawn);
                        else Orbwalker.Move(OrderSpawn);
                    }
                    break;
                case 2:
                    if (myhero.InShop())
                    {
                        if (myhero.Team == GameObjectTeam.Order) Orbwalker.Move(ChaosSpawn);
                        else Orbwalker.Move(OrderSpawn);
                    }
                    else
                    {
                        if (!MidPointReached)
                        {
                            Orbwalker.Move(MidPoint);
                            if (myhero.Distance(MidPoint) <= 100) MidPointReached = true;
                        }
                        else
                        {
                            if (myhero.Team == GameObjectTeam.Order) Orbwalker.Move(ChaosSpawn);
                            else Orbwalker.Move(OrderSpawn);
                        }
                    }
                    break;
                case 3:
                    if (!BotPointReached)
                    {
                        Orbwalker.Move(BotPoint);
                        if (myhero.Distance(BotPoint) <= 100) BotPointReached = true;
                    }
                    else
                    {
                        if (myhero.Team == GameObjectTeam.Order) Orbwalker.Move(ChaosSpawn);
                        else Orbwalker.Move(OrderSpawn);
                    }
                    break;


            }
        }

        private static void DatMenu()
        {
            menu = new Menu("feederkappa", "T7 Feeder", true);

            menu.Add(new MenuSeparator("sep1", "By Toyota7 v1.2b"));
            menu.Add(new MenuSeparator("sep2", "|"));
            menu.Add(new MenuBool("ACTIVE", "Active"));
            menu.Add(new MenuList("MODE", "Feed Mode =>", new string[] { "Closest Enemy", "Top", "Mid", "Bot" }, 0));
            menu.Add(new MenuList("MSGS", "Chat On Death =>", new string[] { "Off", "/all Chat", "Team Chat", "Random Chat" }, 0));
            menu.Add(new MenuSlider("CHATDELAY", "Chat Delay After Death(seconds)", 4, 1, 7));
            menu.Add(new MenuBool("SPELLS", "Use Summoner Spells", false));
            menu.Add(new MenuBool("ABILITIES", "Use Abilities"));
            menu.Add(new MenuBool("AUTOBUY", "Auto Buy Items"));

            menu.Attach();
        }

    }

    public static class Extensions
    {
        public static bool check(this Menu menu, string sig, string sig2 = null)
        {
            return sig2 == null ? menu[sig].GetValue<MenuBool>().Enabled : menu[sig][sig2].GetValue<MenuBool>().Enabled;
        }
    }
}
