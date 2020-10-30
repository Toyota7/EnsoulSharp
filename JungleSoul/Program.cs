using System;
using System.Linq;
using System.Collections.Generic;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.MenuUI.Values;
using SharpDX;
using SharpDX.Direct3D9;
using Color = System.Drawing.Color;

namespace JungleSoul
{
    class Program : Base
    {
        static AIHeroClient myhero { get { return ObjectManager.Player; } }
        static int GameTime { get { return (int)Game.Time; } }
        static float temptime = 0f;
        static Font MiniMapFont, MapFont;
        static int FontSize, FontSize2;
        static ColorBGRA FontColor, FontColor2;
        static Color CircleColor;
        static Menu menu, subm;
        
        static Vector2 mgromppos = new Vector2(2024, 8406),
                mbluepos = new Vector2(3788, 7962),
                mwolfpos = new Vector2(3916, 6452),
                mrazorpos = new Vector2(6958, 5526),
                mredpos = new Vector2(7800, 4072),
                mkrugpos = new Vector2(8370, 2668),
                egromppos = new Vector2(12618, 6606),
                ebluepos = new Vector2(11070, 6854),     //m = blue team, r = red team
                ewolfpos = new Vector2(10976, 8272),
                erazorpos = new Vector2(7842, 9480),
                eredpos = new Vector2(7098, 10750),
                ekrugpos = new Vector2(6396, 12062),
                topcrab = new Vector2(4490, 9574),
                botcrab = new Vector2(10576, 5066);

        static List<string> mobnames = new List<string> { "Gromp", "Blue", "Wolves", "Raptors", "Red", "Krugs" };
        static List<string> realmobnames = new List<string> { "SRU_Gromp13.1.1", "SRU_Blue1.1.1", "SRU_Murkwolf2.1.1", "SRU_Razorbeak3.1.1", "SRU_Red4.1.1", "SRU_Krug5.1.1" };
        static List<Vector2> bluejunglepos = new List<Vector2> { mgromppos, mbluepos, mwolfpos, mrazorpos, mredpos, mkrugpos };
        static List<Vector2> redjunglepos = new List<Vector2> { egromppos, ebluepos, ewolfpos, erazorpos, eredpos, ekrugpos };
        static List<Vector2> alljunglepos = bluejunglepos.Concat(redjunglepos).ToList();
        static List<Vector2> crabpos = new List<Vector2> { topcrab, botcrab };
        static List<bool> bluejungle = new List<bool> { true, true, true, true, true, true };
        static List<bool> redjungle = new List<bool> { true, true, true, true, true, true };
        static List<bool> crabs = new List<bool> { true, true }; // top, bot
        static List<float> bluetimers = new List<float> { 0f, 0f, 0f, 0f, 0f, 0f }; //gromp,blue,wolf,razor,red,krug,crab
        static List<float> redtimers = new List<float> { 0f, 0f, 0f, 0f, 0f, 0f };
        static List<float> crabtimers = new List<float> { 0f, 0f };
        static List<int> minimobcounters = new List<int> { 0, 0, 0, 0, 0, 0 }; // blue wolves,raptors,krugs / red -//-
        static float brespawntime = 296.5f, arespawntime = 117f, crabrespawntime = 146f; //in seconds , reb/blue respawn is 300, other mobs(not crab) is 120 , - 4 seconds from delay in ondelete
        static GameObjectTeam myteam = myhero.Team;

        static void Main(string[] args)
        {
            GameEvent.OnGameLoad += OnGameLoad;
        }

        private static void OnGameLoad()
        {
            if (Game.MapId != GameMapId.SummonersRift) return;

            FontSize = 20;
            UpdateFont();
            LoadMenu();

            Game.OnUpdate += CheckTimers;
            Drawing.OnEndScene += OnDraw;
            GameObject.OnDelete += GameObject_OnDelete;

            Console.WriteLine("JungleSoul loaded");
        }

        private static void GameObject_OnDelete(GameObject sender, EventArgs args)
        {
            if ((!sender.Name.ToLower().Contains("sru") && !sender.Name.Contains("MiniKrug")) || sender.Type != GameObjectType.AIMinionClient) return;

            if (sender.Name.Contains("Gromp") && subm.check("SGROMP"))
            {
                if (sender.Position.Distance(mgromppos) < 1400)//blue side
                {
                    bluejungle[0] = false;
                    bluetimers[0] = Game.Time + arespawntime;

                }
                else
                {
                    redjungle[0] = false;
                    redtimers[0] = Game.Time + arespawntime;
                }
            }
            if (sender.Name.Contains("Blue") && subm.check("SBLUE"))
            {
                if (sender.Position.Distance(mbluepos) < 1400)
                {
                    bluejungle[1] = false;
                    bluetimers[1] = Game.Time + brespawntime;
                }
                else
                {
                    redjungle[1] = false;
                    redtimers[1] = Game.Time + brespawntime;
                }
            }
            if (sender.Name.Contains("Murkwolf") && subm.check("SWOLVES"))
            {
                if (sender.Position.Distance(mwolfpos) < 1400)
                {
                    minimobcounters[0]++;                   
                    if (minimobcounters[0] == 3)
                    {
                        bluejungle[2] = false;
                        bluetimers[2] = Game.Time + arespawntime;
                        minimobcounters[0] = 0;
                    }                    
                }
                else
                {
                    minimobcounters[3]++;
                    if (minimobcounters[3] == 3)
                    {
                        redjungle[2] = false;
                        redtimers[2] = Game.Time + arespawntime;
                        minimobcounters[3] = 0;
                    }
                }
            }
            if (sender.Name.Contains("Razor") && subm.check("SRAPTORS"))
            {
                if (sender.Position.Distance(mrazorpos) < 1400)
                {
                    minimobcounters[1]++;
                    if (minimobcounters[1] == 6)
                    {
                        bluejungle[3] = false;
                        bluetimers[3] = Game.Time + arespawntime;
                        minimobcounters[1] = 0;
                    }

                }
                else
                {
                    minimobcounters[4]++;
                    if (minimobcounters[4] == 6)
                    {
                        redjungle[3] = false;
                        redtimers[3] = Game.Time + arespawntime;
                        minimobcounters[4] = 0;
                    }
                }
            }
            if (sender.Name.Contains("Red") && subm.check("SRED"))
            {
                if (sender.Position.Distance(mredpos) < 1400)
                {
                    bluejungle[4] = false;
                    bluetimers[4] = Game.Time + brespawntime;
                }
                else
                {
                    redjungle[4] = false;
                    redtimers[4] = Game.Time + brespawntime;
                }
            }
            if (sender.Name.Contains("Krug") && subm.check("SKRUGS"))
            {
                if (sender.Position.Distance(mkrugpos) < 1400)
                {
                    minimobcounters[2]++;
                    if (minimobcounters[2] == 10)
                    {
                        bluejungle[5] = false;
                        bluetimers[5] = Game.Time + arespawntime +6f;
                        minimobcounters[2] = 0;
                    }
                }
                else
                {
                    minimobcounters[5]++;
                    if (minimobcounters[5] == 10)
                    {
                        redjungle[5] = false;
                        redtimers[5] = Game.Time + arespawntime +6f;
                        minimobcounters[5] = 0;
                    }
                }
            }
            if (sender.Name.Contains("Crab"))
            {
                if (sender.Name == "Sru_Crab16.1.1")//top crab
                {
                    crabs[0] = false;
                    crabtimers[0] = Game.Time + crabrespawntime;
                }
                else //bot crab
                {
                    crabs[1] = false;
                    crabtimers[1] = Game.Time + crabrespawntime;
                }
            }
        }

        static void CheckTimers(EventArgs args)
        {
            for (int i = 0; i < 6; i++)
            {
                if (bluetimers[i] != 0f)
                {
                    if (Game.Time >= bluetimers[i])
                    {
                        bluejungle[i] = true;
                        bluetimers[i] = 0f;
                    }
                }

                if (redtimers[i] != 0f)
                {
                    if (Game.Time >= redtimers[i])
                    {
                        redjungle[i] = true;
                        redtimers[i] = 0f;
                    }
                }
            }

            for (int i = 0; i < 2; i++)
            { 
                if (crabtimers[i] != 0f && Game.Time >= crabtimers[i])
                {
                    crabs[i] = true;
                    crabtimers[i] = 0f;
                }
            }     
        }

        private static void OnDraw(EventArgs args)
        {
            FontColor = m_getcolor(menu, "FONTCOLOR");
            FontColor2 = m_getcolor(menu, "FONTCOLOR2");

            if (menu.check("MMTIMERS"))
            {
                if (subm.check("BTT"))
                {
                    for (int i = 0; i < 6; i++)
                    {

                        if (bluetimers[i] == 0f || !subm.check("S"+mobnames[i].ToUpper())) continue;

                        var span = TimeSpan.FromSeconds(bluetimers[i] - Game.Time);
                        var timestr = string.Format("{0}:{1:00}", (int)span.TotalMinutes, span.Seconds);

                        if (Game.Time < bluetimers[i]) DrawFontTextScreen(MiniMapFont, timestr, TacticalMap.WorldToMinimap(bluejunglepos[i].ToVector3()) + new Vector2(-10, -9), FontColor);
                    }
                }

                if (subm.check("RTT"))
                {
                    for (int i = 0; i < 6; i++)
                    {
                        if (redtimers[i] == 0f || !subm.check("S" + mobnames[i].ToUpper())) continue;

                        var span = TimeSpan.FromSeconds(redtimers[i] - Game.Time);
                        var timestr = string.Format("{0}:{1:00}", (int)span.TotalMinutes, span.Seconds);

                        if (Game.Time < redtimers[i]) DrawFontTextScreen(MiniMapFont, timestr, TacticalMap.WorldToMinimap(redjunglepos[i].ToVector3()) + new Vector2(-12, -9), FontColor);
                    }
                }

                if (subm.check("CT"))
                {
                    for (int i = 0; i < 2; i++)
                    {
                        if (crabtimers[i] == 0f) continue;

                        var span = TimeSpan.FromSeconds(crabtimers[i] - Game.Time);
                        var timestr = string.Format("{0}:{1:00}", (int)span.TotalMinutes, span.Seconds);

                        if (Game.Time < crabtimers[i]) DrawFontTextScreen(MiniMapFont, timestr, TacticalMap.WorldToMinimap(crabpos[i].ToVector3()) + new Vector2(-12, -9), FontColor);
                    }
                }
            }
            //====================================================================================================================================
            if (menu.check("MTIMERS"))
            {
                if (subm.check("BTT"))
                {
                    for (int i = 0; i < 6; i++)
                    {
                        if (bluetimers[i] == 0f || !subm.check("S" + mobnames[i].ToUpper())) continue;

                        var span = TimeSpan.FromSeconds(bluetimers[i] - Game.Time);
                        var timestr = string.Format("{0}:{1:00}", (int)span.TotalMinutes, span.Seconds);

                        if (Game.Time < bluetimers[i] /*&& bluejunglepos[i].IsOnScreen()*/)
                            DrawFontTextScreen(MapFont, timestr, Drawing.WorldToScreen(bluejunglepos[i].ToVector3()), FontColor2);
                    }
                }

                if (subm.check("RTT"))
                {
                    for (int i = 0; i < 6; i++)
                    {
                        if (redtimers[i] == 0f || !subm.check("S" + mobnames[i].ToUpper())) continue;

                        var span = TimeSpan.FromSeconds(redtimers[i] - Game.Time);
                        var timestr = string.Format("{0}:{1:00}", (int)span.TotalMinutes, span.Seconds);

                        if (Game.Time < redtimers[i] /*&& redjunglepos[i].IsOnScreen()*/)
                            DrawFontTextScreen(MapFont, timestr, Drawing.WorldToScreen(redjunglepos[i].ToVector3()), FontColor2);
                    }
                }

                if (subm.check("CT"))
                {
                    for (int i = 0; i < 2; i++)
                    {
                        if (crabtimers[i] == 0f) continue;

                        var span = TimeSpan.FromSeconds(crabtimers[i] - Game.Time);
                        var timestr = string.Format("{0}:{1:00}", (int)span.TotalMinutes, span.Seconds);

                        if (Game.Time < crabtimers[i] /*&& crabpos[i].IsOnScreen()*/)
                            DrawFontTextScreen(MapFont, timestr, Drawing.WorldToScreen(crabpos[i].ToVector3()), FontColor2);
                    }
                }
            }
        }

        static void LoadMenu()
        {
            menu = new Menu("mainm", "JungleSoul", true);
            subm = new Menu("sebmen", "Mob Options");

            subm.Add(new MenuBool("BTT", "Blue Team Timers"));
            subm.Add(new MenuBool("RTT", "Red Team Timers"));
            subm.Add(new MenuBool("CT", "Crab Timers"));
            subm.Add(new MenuSeparator("58429849", "-Mob Specific Timer Settings-"));
            subm.Add(new MenuBool("SGROMP", "Gromp"));
            subm.Add(new MenuBool("SBLUE", "Blue"));
            subm.Add(new MenuBool("SWOLVES", "Wolves"));
            subm.Add(new MenuBool("SRAPTORS", "Raptors"));
            subm.Add(new MenuBool("SRED", "Red"));
            subm.Add(new MenuBool("SKRUGS", "Krugs"));

            menu.Add(new MenuSeparator("74289347289", "By Toyota7"));
            menu.Add(new MenuBool("MMTIMERS", "Draw Timers On Minimap"));
            menu.Add(new MenuBool("MTIMERS", "Draw Timers On Map"));
            menu.Add(subm);
            menu.Add(new MenuSlider("FONTSIZE", "Minimap Timers Font Size", 20, 10, 30)).ValueChanged += (s, e) => { FontSize = slider(menu, "FONTSIZE"); UpdateFont(); };
            menu.Add(new MenuSlider("FONTSIZE2", "Map Timers Font Size", 20, 10, 40)).ValueChanged += (s, e) => { FontSize2 = slider(menu, "FONTSIZE2"); UpdateFont(); };
            menu.Add(new MenuColor("FONTCOLOR", "Minimap Timers Color", ColorBGRA.FromRgba(Color.White.ToRgba())));
            menu.Add(new MenuColor("FONTCOLOR2", "Map Timers Color", ColorBGRA.FromRgba(Color.White.ToRgba())));         

            menu.Attach();
        }

        static void UpdateFont()
        {
            MiniMapFont = new Font(Drawing.Direct3DDevice, new FontDescription
            { FaceName = "Arial", Height = FontSize, Weight = FontWeight.SemiBold, OutputPrecision = FontPrecision.Stroke, Quality = FontQuality.ClearType });
            MapFont = new Font(Drawing.Direct3DDevice, new FontDescription
            { FaceName = "Arial", Height = FontSize2, Weight = FontWeight.SemiBold, OutputPrecision = FontPrecision.Stroke, Quality = FontQuality.ClearType });
        }
    }
}
