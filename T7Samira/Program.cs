using System;
using System.Linq;
using System.Collections.Generic;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.Events;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.MenuUI.Values;
using EnsoulSharp.SDK.Prediction;
using EnsoulSharp.SDK.Utility;
using SharpDX;
using Color = System.Drawing.Color;

namespace T7Samirausing System;
using System.Linq;
using System.Collections.Generic;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.Events;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.MenuUI.Values;
using EnsoulSharp.SDK.Prediction;
using EnsoulSharp.SDK.Utility;
using SPrediction;
using SharpDX;
using Color = System.Drawing.Color;

namespace T7Samira
{
    class Program : Base
    {
        private static void Main(string[] args)
        {
            GameEvent.OnGameLoad += OnGameLoad;
        }

        private static void OnGameLoad()
        {
            if (ObjectManager.Player.CharacterName != CharName) return;

            Q1 = new Spell(SpellSlot.Q, 950f);
            Q1.SetSkillshot(0.25f, 79f, 2500f, true, SkillshotType.Line);
            Q2 = new Spell(SpellSlot.Q, 329f);
            Q2.SetSkillshot(0.25f, 150f, 9999f, false, SkillshotType.Cone);
            W = new Spell(SpellSlot.W, 390f);
            E = new Spell(SpellSlot.E, 600f);
            //E.SetSkillshot(0f, 170f, 2050f, false, SkillshotType.Line);
            R = new Spell(SpellSlot.R, 600f);
            
            ConeAOEInput = new SpellPrediction.PredictionInput { Aoe = true, Collision = false, CollisionObjects = CollisionObjects.Heroes, Delay = 0.25f, From = myhero.Position, Type = SkillshotType.Cone, Range = 350f, Radius = 120f, Speed = 9999f };

            Potion = new Items.Item(ItemId.Health_Potion, 0f);
            Biscuit = new Items.Item(ItemId.Total_Biscuit_of_Rejuvenation, 0f);
            RPotion = new Items.Item(ItemId.Refillable_Potion, 0f);
            CPotion = new Items.Item(ItemId.Corrupting_Potion, 0f);
            
            if (myhero.GetSpellSlot("SummonerDot") != SpellSlot.Unknown)
            {
                ignite = new Spell(myhero.GetSpellSlot("SummonerDot"), 600f);
            }

            EnemyPlayerNames = GameObjects.EnemyHeroes.Select(x => x.CharacterName).ToArray();
            EnemyADC = GetEnemyADC();

            DatMenu();

            Game.OnUpdate += OnUpdate;
            GameObject.OnMissileCreate += OnMissile;
            GameObject.OnDelete += GameObject_OnDelete;
            //AIHeroClient.OnProcessSpellCast += OnProcessSpell;
            Drawing.OnDraw += OnDraw;
            Drawing.OnEndScene += OnEndScene;
            AIHeroClient.OnLevelUp += OnLevelUp;
            Gapcloser.OnGapcloser += OnGapcloser;
            AIBaseClient.OnBuffGain += OnBuffGain;
            Orbwalker.OnAction += OnAction;
            Tick.OnTick += OnTick;

            if (myhero.Level == 1) DelayAction.Add(300, delegate { myhero.Spellbook.LevelSpell(SpellSlot.Q); });

            myhero.SetSkin(comb(misc, "skinID"));

            Console.WriteLine("T7 " + CharName + " Loaded");
            Game.Print("<b><font color='#0040FF'>T7</font><font color='#ab180e'>" + CharName + " </font></b> Alpha Version Loaded!");
        }

        private static void GameObject_OnDelete(GameObject sender, EventArgs args)
        {
            if (IncomingMissile != null && sender.Name == IncomingMissile.Name) IncomingMissile = null;
        }

        private static void OnTick(EventArgs args)
        {
            if (IncomingMissile != null && IncomingMissile.Position.Distance(myhero.Position) < 400)
            {
                W.Cast();
                //Game.Print(true);
                IncomingMissile = null;
            }
        }

        private static void OnEndScene(EventArgs args)
        {
            foreach (var target in GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(2000) && !x.IsDead && x.IsHPBarRendered && Drawing.WorldToScreen(x.Position).IsOnScreen()))
            {
                float damage = target.GetTotalSpelldamage();

                var hpBar = target.HPBarPosition;

                //if (damage > target.Health)
                //{
                //    Drawing.DrawText(hpBar.X + 69, hpBar.Y - 45, System.Drawing.Color.White, "KILLABLE");
                //}

                var damagePercentage = ((target.Health - damage) > 0 ? (target.Health - damage) : 0) / target.MaxHealth;
                var currentHealthPercentage = target.Health / target.MaxHealth;

                var startPoint = new Vector2(hpBar.X - 45 + damagePercentage * 104, hpBar.Y - 18);
                var endPoint = new Vector2(hpBar.X - 45 + currentHealthPercentage * 104, hpBar.Y - 18);

                Drawing.DrawLine(startPoint, endPoint, 12, Color.FromArgb(99, Color.LawnGreen));
            }
        }
    

        private static void OnBuffGain(AIBaseClient sender, AIBaseClientBuffGainEventArgs args)
        {
            if (sender.IsEnemy && (args.Buff.Type == BuffType.Sleep || args.Buff.Type == BuffType.Snare || args.Buff.Type == BuffType.Stun) && misc.check("PS") && !sender.HasBuff(EnemyPassiveProced) && sender.DistanceToPlayer() < 600)
            {
                Orbwalker.Attack(sender);
            }
        }

        //private static void OnProcessSpell(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        //{
        //    if (misc.check("BW") && sender.IsEnemy && W.IsReady() && args.Slot == SpellSlot.R && args.SData.CastType == SpellDataCastType.Missile && args.Target.IsMe)
        //    {
        //        //DelayAction.Add((int)((args.Start.DistanceToPlayer() / args.SData.MissileSpeed) * 1000) + (int)(args.CastTime * 1000) - 300,delegate { Game.Print(true); W.Cast(); });
        //    }                
        //}

        private static void OnMissile(GameObject sender, EventArgs args)
        {
            var missile = sender as MissileClient;
            if (sender.IsEnemy && !sender.Name.Contains("Minion") && !sender.Name.Contains("Basic") && KnownMissiles.Contains(missile.Name))
            {
                //Game.Print(sender.Name);
                IncomingMissile = missile;
            }              
        }

        private static void OnAction(object sender, OrbwalkerActionArgs args)
        {
            if (WaitforAA && args.Type == OrbwalkerType.AfterAttack)
            {
                WaitforAA = !WaitforAA;
                LastAttackType = 0;
            }
        }

        private static void OnGapcloser(AIHeroClient sender, Gapcloser.GapcloserArgs args)
        {
            if (sender.IsEnemy && !sender.IsDead && !myhero.IsDead && args.EndPosition.DistanceToPlayer() < Q1.Range && Q1.IsReady())
            {
                //Q1.CastIfHitchanceEquals(sender, HitChance.Dash, true);
                var pred = Q1.GetPrediction(sender);

                if (pred.Hitchance == HitChance.Dash)
                    Q1.Cast(pred.CastPosition);
            }
        }

        #region Events
        public static void OnLevelUp(AIHeroClient sender, AIHeroClientLevelUpEventArgs args)
        {
            if (!sender.IsMe || !misc.check("autolevel")) return;

            DelayAction.Add(1, delegate
            {
                if (myhero.Level > 1 && myhero.Level < 4)
                {
                    switch (myhero.Level)
                    {
                        case 2:
                            myhero.Spellbook.LevelSpell(SpellSlot.E);
                            break;
                        case 3:
                            myhero.Spellbook.LevelSpell(SpellSlot.W);
                            break;
                    }
                }
                else if (myhero.Level >= 4)
                {
                    if (myhero.Spellbook.CanSpellBeUpgraded(SpellSlot.R))
                    {
                        myhero.Spellbook.LevelSpell(SpellSlot.R);
                    }
                    else if (myhero.Spellbook.CanSpellBeUpgraded(SpellSlot.Q))
                    {
                        myhero.Spellbook.LevelSpell(SpellSlot.Q);
                    }
                    else if (myhero.Spellbook.CanSpellBeUpgraded(SpellSlot.W))
                    {
                        myhero.Spellbook.LevelSpell(SpellSlot.W);
                    }
                    else if (myhero.Spellbook.CanSpellBeUpgraded(SpellSlot.E))
                    {
                        myhero.Spellbook.LevelSpell(SpellSlot.E);
                    }
                }
            });
        }

        private static void OnDraw(EventArgs args)
        {
            if (myhero.IsDead) return;
            

            if (draw.check("dq") && (draw.check("drdy") ? Q1.IsReady() : true))
            {
                Render.Circle.DrawCircle(myhero.Position, Q1.Range, Color.Red, 5, true);
            }

            if (draw.check("dw") && (draw.check("drdy") ? W.IsReady() : true))
            {
                Render.Circle.DrawCircle(myhero.Position, W.Range, Color.DarkRed, 5, true);
            }

            if (draw.check("deraa") && (draw.check("drdy") ? E.IsReady() : true))
            {
                Render.Circle.DrawCircle(myhero.Position, E.Range, Color.Red, 5, true);
            }

            var target = TargetSelector.GetTarget(1000f, DamageType.Physical);

            //if (draw.check("DRAWTARGET") && target != null && target.DistanceToPlayer() < R.Range)
            //{
            //    Render.Circle.DrawCircle(target.Position, target.BoundingRadius - 10, Color.LightBlue, 3);
            //}


            //Drawing.DrawLine(Drawing.WorldToScreen(myhero.Position), Drawing.WorldToScreen(myhero.Direction), 3, Color.Red);
            //Drawing.
            //var test = new Geometry.Line(target.HPBarPosition, target.HPBarPosition+ new Vector2(20, 20), 100);
            //test.UpdatePolygon();
            //test.Draw(Color.Red, 25);
            //var test = new EnsoulSharp.SDK.Utility.Render.Line(myhero.HPBarPosition + new Vector2(-50, -50), myhero.HPBarPosition + new Vector2(0, 20), 50, SharpDX.ColorBGRA.FromRgba(Color.Red.ToRgba()));
            //test.Add(1f);
            //Game.Print();
            //test.


            //Geometry.Rectangle test = new Geometry.Rectangle(ObjectManager.Player.Position, myhero.Position.Extend(Game.CursorPos, 350), 79);
            //test.Draw(Color.Red);
            //foreach (var targe in GameObjects.EnemyHeroes.Where(x => !x.IsDead && x.IsVisibleOnScreen))
            //{
            //    Drawing.DrawText(Drawing.WorldToScreen(targe.Position).X - 20, Drawing.WorldToScreen(targe.Position).Y + 30, targe.killable() ? Color.White : Color.Transparent, "Killable");
            //}
            //Drawing.DrawText(Drawing.WorldToScreen(myhero.Position).X - 20, Drawing.WorldToScreen(myhero.Position).Y + 30, Color.Green, WaitforAA.ToString());
            if (target != null)
            {
                //Game.Print(target.DistanceToPlayer());

                if (draw.check("DRAWWAY") && target.GetWaypoints().Any())
                {
                    var wayp = target.GetWaypoints().LastOrDefault();

                    if (wayp.IsValid() && wayp.ToVector3World().IsOnScreen() && target.Position.IsOnScreen())
                    {
                        Drawing.DrawLine(Drawing.WorldToScreen(target.Position), Drawing.WorldToScreen(wayp.ToVector3World()), 2, Color.White);
                    }
                }

            }
            //for (int i = 0; i < myhero.Buffs.Count(); i++)
            //{
            //    var buff = myhero.Buffs.ToArray()[i];
            //    Drawing.DrawText(Drawing.WorldToScreen(Game.CursorPos).X, Drawing.WorldToScreen(Game.CursorPos).Y + (14 * i) + 60, Color.Red, buff.Name + " " + buff.Count);
            //}
        }

        private static void OnUpdate(EventArgs args)
        {
            if (myhero.IsDead) return;

            switch (Orbwalker.ActiveMode)
            {
                case OrbwalkerMode.Combo:
                    Combo();
                    break;
                case OrbwalkerMode.LaneClear:
                    if (myhero.ManaPercent >= slider(laneclear, "LMINMANA")) Clear();
                    break;
                case OrbwalkerMode.Harass:
                    SpamQ();
                    break;
            }

            SmoothRMovement();

            Misc();

            if (myhero.HasBuff(RBuffName) && E.IsReady() && GameObjects.EnemyHeroes.Where(x => x.DistanceToPlayer() < E.Range).Any())
            {
                var jumptarget = GameObjects.EnemyHeroes.Where(x => x.DistanceToPlayer() < E.Range).OrderBy(x => x.Health).FirstOrDefault();

                if (jumptarget != null)
                {
                    //Game.Print(true);
                    E.Cast(jumptarget);
                }
            }
            //var target = GetTarget();
            //if (target != null) Game.Print(target.qtt(R.Speed, 500) / 1000);
        }
        #endregion

        static void Combo()
        {
            var target = TargetSelector.GetTarget(1200f, DamageType.Physical);

            if (target == null || myhero.HasBuff(RBuffName)) return;

            if (!myhero.HasBuff(PassiveName) && target.InAARangeOf(myhero))
            {
                WaitforAA = true;
                return;
            }

            //if(myhero.HasBuff(RBuffName) && E.IsReady() && GameObjects.EnemyHeroes.Where(x => x.DistanceToPlayer() < E.Range).Any())
            //{
            //    var jumptarget = GameObjects.EnemyHeroes.Where(x => x.DistanceToPlayer() < E.Range).OrderBy(x => x.Health).FirstOrDefault();

            //    if (jumptarget != null)
            //    {
            //        //Game.Print(true);
            //        E.Cast(jumptarget);
            //    }
            //}

            if (combo.check("CQ") && Q1.IsReady() && !(WaitforAA || LastAttackType == 1))
            {
                if (target.DistanceToPlayer() < Q2.Range)
                {
                    var pred = Q2.GetPrediction(target);

                    if (pred.Hitchance >= hitchance && Q2.Cast(pred.CastPosition))
                    {
                        LastAttackType = 1;
                        WaitforAA = true;
                    }                  
                }
                else
                {
                    var pred = Q1.GetPrediction(target);

                    if (pred.Hitchance >= hitchance && Q1.Cast(pred.CastPosition, true))
                    {
                        WaitforAA = true;
                        LastAttackType = 1;
                    } 
                    //else if(pred.Hitchance == HitChance.Collision && E.IsInRange(target) && E.IsReady() && combo.check("CE") && E.Cast(target) == CastStates.SuccessfullyCasted)
                    //{
                    //    LastAttackType = 3;
                    //    WaitforAA = true;
                    //}
                }
            }

            if (combo.check("CW") && W.IsReady() && (myhero.CountEnemies(W.Range - 10) >= slider(combo, "CWMIN" ) || (combo.check("CWS") && Q1.CooldownTime > 1f)) && !Q1.IsReady() && !E.IsReady() && GameObjects.EnemyHeroes.Any(x => x.IsFacing(myhero)))
            {
                //Game.Print(true);
                W.Cast();
                LastAttackType = 2;
                WaitforAA = true; 
            }

            if (combo.check("CE") && E.IsReady())
            {
                var closeenemyturret = GameObjects.EnemyTurrets.OrderByDescending(x => x.Distance(target)).FirstOrDefault().Position;
                if (combo.check("CEQ") && Q1.IsReady() && myhero.CountEnemies(E.Range) > 1)
                {
                    foreach (var enemy in GameObjects.EnemyHeroes.Where(x => x.DistanceToPlayer() < E.Range))
                    {
                        var pred = E.GetPrediction(enemy);

                        if (pred.AoeTargetsHitCount > 1)
                        {
                            E.Cast(enemy);
                            DelayAction.Add(50, delegate { Q1.Cast(); WaitforAA = true; });
                        }
                    }
                }
                //else if (target.DistanceToPlayer() < 500 && combo.check("CEG"))
                //{
                //    //return;
                //}
                else if (target.DistanceToPlayer() <= E.Range && combo.check("CES") && !WaitforAA && LastAttackType != 3 && (target.Distance(closeenemyturret) >= 850 || (myhero.HealthPercent > 40 || target.GetTotalSpelldamage() > target.Health)) /*&& !(Q1.IsReady() && !WaitforAA && LastAttackType != 3) *//*!((Q1.IsReady() && LastAttackType != 1 && target.DistanceToPlayer() < Q1.Range) || !(WaitforAA && target.InAARangeOf(myhero)))*/)
                {
                    //Game.Print(true);
                    E.Cast(target);
                    LastAttackType = 3;
                    WaitforAA = true;
                }
                else if (combo.check("CEG") && !target.InAARangeOf(myhero) && !target.IsFacing(myhero) && myhero.IsFacing(target) && !GameObjects.AllyHeroes.Any(x => target.InAARangeOf(x)))
                {
                    var closerminion = GameObjects.Minions.Where(x => myhero.Position.Extend(x.Position, 650f).Distance(target) < myhero.Distance(target) && x.Distance(target) < 500).OrderBy(x => x.Distance(target)).FirstOrDefault();
                    if (closerminion != null)
                        E.Cast(closerminion);
                }        
            }

            if (combo.check("CR") && R.IsReady() && myhero.CountEnemies(R.Range - 100) > 0)
            {
                R.Cast();
                LastAttackType = 4;
            }

            if (combo.check("CIGN") && ignite != null && ignite.CanCast(target) && ((target.Health <= myhero.GetSummonerSpellDamage(target, SummonerSpell.Ignite) && myhero.Distance(target) < 400 && !GameObjects.AllyHeroes.Any(x => target.InAARangeOf(x))) ||
                    (target.HealthPercent > 5 && GameObjects.AllyHeroes.Count(x => x != myhero && target.InAARangeOf(x) && x.IsFacing(target)) > 0)))
            {
                ignite.Cast(target, true);
            }

        }

        static void Clear()
        {
            var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q1.Range) && x.IsMinion());

            var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(Q1.Range));

            var lMobs = GameObjects.Jungle.Where(x => x.IsValidTarget(Q1.Range) && x.GetJungleType() == JungleType.Legendary);

            var bMobs = GameObjects.Jungle.Where(x => x.IsValidTarget(Q1.Range) && x.GetJungleType() == JungleType.Large);

            IEnumerable<AIMinionClient> targets;

            if (minions.Any()) targets = minions;
            else if (lMobs.Any()) targets = lMobs;
            else if (bMobs.Any()) targets = bMobs;
            else targets = mobs;

            if (targets.Any())
            {
                if (laneclear.check("LQ") && Q1.IsReady() && !(laneclear.check("LQCLOSE") && !targets.Any(x => x.DistanceToPlayer() < 350)))
                {
                    //if (slider(laneclear, "LQMIN") > 1)
                    //{

                    //}
                    //var minion = targets.FirstOrDefault(x => HealthPrediction.GetPrediction(x, x.qtt(Q1.Speed, 250)) > 5 && !(targets.Count() > 1 && x == Orbwalker.GetTarget()));

                    foreach (var minion in targets)
                    {
                        SpellPrediction.PredictionOutput pred = null;
                        
                        if (minion.DistanceToPlayer() < Q2.Range)
                        {
                            if (HealthPrediction.GetPrediction(minion, 250) < 5) continue;

                            pred = Q2.GetPrediction(minion, true);
                            Q2.Cast(pred.CastPosition);
                        }
                        else
                        {
                            if (HealthPrediction.GetPrediction(minion, minion.qtt(Q1.Speed, 250)) < 5) continue;

                            pred = Q2.GetPrediction(minion);
                            Q1.Cast(pred.CastPosition);
                        }
                    }
                }

                if (laneclear.check("LW") && W.IsReady() && targets.Count(x => x.DistanceToPlayer() < W.Range) > slider(laneclear, "LWMIN"))
                {
                    W.Cast();
                }

                if (laneclear.check("LE") && E.IsReady() && targets.Where(x => x.DistanceToPlayer() < E.Range).Count() > slider(laneclear, "LEMIN"))
                {
                    foreach (var minion in targets)
                    {
                        if (myhero.Position.Extend(minion.Position, E.Range + 400f).IsUnderEnemyTurret()) continue;
                        var pred = E.GetPrediction(minion, false, -1, CollisionObjects.Minions);

                        if (pred.CollisionObjects.Count()> slider(laneclear, "LEMIN"))
                        {
                            E.Cast(minion);
                        }
                    }
                }
            }
        }

        static void SpamQ()
        {
            if (misc.check("HQ") && Q1.IsReady())
            {
                var target = TargetSelector.GetTarget(Q1.Range, DamageType.Physical);

                if (target != null && target.DistanceToPlayer() < Q1.Range)
                {
                    Q1.CastIfHitchanceMinimum(target, hitchance, true);
                }
            }
        }

        static void SmoothRMovement()
        {
            if (myhero.HasBuff(RBuffName) || myhero.HasBuff(WBuffName))
            {
                Orbwalker.AttackState = false;
                myhero.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
            }
            else
            {
                Orbwalker.AttackState = true;
            }
        }

        static void Misc()
        {
            if (misc.check("QKS") && Q1.IsReady())
            {
                var target = TargetSelector.GetTarget(Q1.Range, DamageType.Physical);

                if (target != null && Q1.GetDamage(target) > target.Health)
                {
                    Q1.CastIfHitchanceMinimum(target, hitchance);
                }
            }

            if (misc.check("AUTOPOT") && !myhero.HasBuffOfType(BuffType.Heal) && myhero.HealthPercent <= slider(misc, "POTMIN") && !myhero.InShop())
            {
                if (Items.CanUseItem(myhero, Potion.Id)) Potion.Cast();

                else if (Items.CanUseItem(myhero, Biscuit.Id)) Biscuit.Cast();

                else if (Items.CanUseItem(myhero, RPotion.Id)) RPotion.Cast();

                else if (Items.CanUseItem(myhero, CPotion.Id)) CPotion.Cast();
            }

            

            //if (misc["FOCUS"].GetValue<MenuList>().Index == 2) misc["CFOCUS"].GetValue<MenuList>().Visible = true;
            //else misc["CFOCUS"].GetValue<MenuList>().Visible = false;
        }

        static void DatMenu()
        {
            menu = new Menu("t7" + CharName, "T7 " + CharName, true);
            menu.Add(new MenuSeparator("sep", "By Toyota7 v" + version));

            combo = new Menu("combo", "Combo");
            combo.Add(new MenuBool("CQ", "Use Q"));
            combo.Add(new MenuBool("CW", "Use W", false));
            combo.Add(new MenuBool("CWS", "Use W For Stacks", false));
            combo.Add(new MenuSlider("CWMIN", "Min Enemies For W", 2, 1, 5));
            combo.Add(new MenuBool("CE", "Use E"));
            combo.Add(new MenuBool("CEG", "Use E On Minions To Gapclose"));
            combo.Add(new MenuBool("CES", "Use E For Stacks"));
            combo.Add(new MenuBool("CEQ", "Use EQ On Multiple Targets", false));
            combo.Add(new MenuBool("CR", "Use R"));        
            combo.Add(new MenuBool("CIGN", "Use Ignite"));
            menu.Add(combo);

            laneclear = new Menu("LANEC", "Laneclear");
            laneclear.Add(new MenuBool("LQ", "Use Q"));
            laneclear.Add(new MenuBool("LQCLOSE", "Only Use Close Range Q(Blade)"));
            //laneclear.Add(new MenuSlider("LQMIN", "Min Minions For Q Blade", 5, 1, 10));
            laneclear.Add(new MenuBool("LW", "Use W", false));
            laneclear.Add(new MenuSlider("LWMIN", "Min Minions For W", 5, 1, 10));
            laneclear.Add(new MenuBool("LE", "Use E", false));
            laneclear.Add(new MenuSlider("LEMIN", "Min Minions For E", 2, 1, 4));
            laneclear.Add(new MenuSlider("LMINMANA", "Min Mana % To Laneclear", 50, 5, 100));
            menu.Add(laneclear);

            draw = new Menu("draw", "Drawings");
            draw.Add(new MenuBool("dq", "Draw Q Range"));
            draw.Add(new MenuBool("dw", "Draw W Range"));
            draw.Add(new MenuBool("deraa", "Draw E/R Range"));
            draw.Add(new MenuBool("drdy", "Draw Only Ready Spells", false));
            draw.Add(new MenuBool("DRAWWAY", "Draw Target Waypoints", true));
            draw.Add(new MenuBool("DRAWDMG", "Draw Estimated Dmg on Enemies", true));
            menu.Add(draw);

            misc = new Menu("misc", "Misc");
            //misc.Add(new MenuSeparator("sep10", "Focusing Settings"));
            //misc.Add(new MenuList("FOCUS", "Focus On: ", new string[] { "Enemy ADC", "All Champs(TS)", "Custom Champion" }, 0));
            //misc.Add(new MenuList("CFOCUS", "Which Champion To Focus On? ", EnemyPlayerNames, 0));
            misc.Add(new MenuSeparator("sep12", "Other Settings"));
            Prediction.Initialize(misc, "SPRED");
            misc.Add(new MenuList("chit", "Min Prediction Hitchance", new string[] { "Low", "Medium", "High", "Very High" }, 1)).ValueChanged += (s, e) =>
            {
                hitchance = (HitChance)comb(misc, "chit") + 1;
            };
            misc.Add(new MenuBool("BW", "Block Dangerous Spells With W"));
            foreach (var target in GameObjects.EnemyHeroes)
            {
                if (target.Spellbook.GetSpell(SpellSlot.Q).SData.CastType == SpellDataCastType.Missile ||
                    target.Spellbook.GetSpell(SpellSlot.W).SData.CastType == SpellDataCastType.Missile ||
                    target.Spellbook.GetSpell(SpellSlot.E).SData.CastType == SpellDataCastType.Missile ||
                    target.Spellbook.GetSpell(SpellSlot.R).SData.CastType == SpellDataCastType.Missile)
                {
                    misc.Add(new MenuBool("B" + target.Name, "Block " + target.Name));
                }
            }
            misc.Add(new MenuBool("PS", "Always Proc Passive On Stunned Enemies", false));
            misc.Add(new MenuBool("QGAP", "Use Q(Ranged) On Gapclosers", false));
            misc.Add(new MenuBool("HQ", "Use Q(Ranged) To Harass"));
            misc.Add(new MenuBool("QKS", "Killsteal With Q"));
            misc.Add(new MenuSeparator("sep14", "Auto Potion"));
            misc.Add(new MenuBool("AUTOPOT", "Activate Auto Potion"));
            misc.Add(new MenuSlider("POTMIN", "Min Health % To Active Potion", 50, 1, 100));
            misc.Add(new MenuSeparator("sep15", "Auto Level Up"));
            misc.Add(new MenuBool("autolevel", "Activate Auto Level Up Spells"));


            misc.Add(new MenuList("skinID", "Skin Hack", new string[]
            {
                "Default", "Psy Ops"
            }))
            .ValueChanged += (s, e) => myhero.SetSkin(comb(misc, "skinID"));
            menu.Add(misc);

            menu.Attach();
        }
    }
}

{
    class Program : Base
    {
        private static void Main(string[] args)
        {
            GameEvent.OnGameLoad += OnGameLoad;
        }

        private static void OnGameLoad()
        {
            if (ObjectManager.Player.CharacterName != CharName) return;

            Q1 = new Spell(SpellSlot.Q, 950f);
            Q1.SetSkillshot(0.25f, 79f, 2500f, true, SkillshotType.Line);
            Q2 = new Spell(SpellSlot.Q, 329f);
            Q2.SetSkillshot(0.25f, 150f, 9999f, false, SkillshotType.Cone);
            W = new Spell(SpellSlot.W, 390f);
            E = new Spell(SpellSlot.E, 600f);
            R = new Spell(SpellSlot.R, 600f);
            
            ConeAOEInput = new SpellPrediction.PredictionInput { Aoe = true, Collision = false, CollisionObjects = CollisionObjects.Heroes, Delay = 0.25f, From = myhero.Position, Type = SkillshotType.Cone, Range = 350f, Radius = 120f, Speed = 9999f };

            Potion = new Items.Item(ItemId.Health_Potion, 0f);
            Biscuit = new Items.Item(ItemId.Total_Biscuit_of_Rejuvenation, 0f);
            RPotion = new Items.Item(ItemId.Refillable_Potion, 0f);
            CPotion = new Items.Item(ItemId.Corrupting_Potion, 0f);
            
            if (myhero.GetSpellSlot("SummonerDot") != SpellSlot.Unknown)
            {
                ignite = new Spell(myhero.GetSpellSlot("SummonerDot"), 600f);
            }

            EnemyPlayerNames = GameObjects.EnemyHeroes.Select(x => x.CharacterName).ToArray();
            EnemyADC = GetEnemyADC();

            DatMenu();

            Game.OnUpdate += OnUpdate;
            GameObject.OnMissileCreate += OnMissile;
            GameObject.OnDelete += GameObject_OnDelete;
            Drawing.OnDraw += OnDraw;
            Drawing.OnEndScene += OnEndScene;
            AIHeroClient.OnLevelUp += OnLevelUp;
            Gapcloser.OnGapcloser += OnGapcloser;
            AIBaseClient.OnBuffGain += OnBuffGain;
            Orbwalker.OnAction += OnAction;
            Tick.OnTick += OnTick;

            if (myhero.Level == 1) DelayAction.Add(300, delegate { myhero.Spellbook.LevelSpell(SpellSlot.Q); });

            myhero.SetSkin(comb(misc, "skinID"));

            Console.WriteLine("T7 " + CharName + " Loaded");
            Game.Print("<b><font color='#0040FF'>T7</font><font color='#ab180e'>" + CharName + " </font></b> Alpha Version Loaded!");
        }

        private static void GameObject_OnDelete(GameObject sender, EventArgs args)
        {
            if (sender.Name == IncomingMissile.Name) IncomingMissile = null;
        }

        private static void OnTick(EventArgs args)
        {
            if (IncomingMissile != null && IncomingMissile.Position.Distance(myhero.Position) < 400)
            {
                W.Cast();
                //Game.Print(true);
                IncomingMissile = null;
            }
        }

        private static void OnEndScene(EventArgs args) //Credits babazhou
        {
            foreach (var target in GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(2000) && !x.IsDead && x.IsHPBarRendered && Drawing.WorldToScreen(x.Position).IsOnScreen()))
            {
                float damage = target.GetTotalSpelldamage();

                var hpBar = target.HPBarPosition;

                var damagePercentage = ((target.Health - damage) > 0 ? (target.Health - damage) : 0) / target.MaxHealth;
                var currentHealthPercentage = target.Health / target.MaxHealth;

                var startPoint = new Vector2(hpBar.X - 45 + damagePercentage * 104, hpBar.Y - 18);
                var endPoint = new Vector2(hpBar.X - 45 + currentHealthPercentage * 104, hpBar.Y - 18);

                Drawing.DrawLine(startPoint, endPoint, 12, Color.FromArgb(99, Color.LawnGreen));
            }
        }
    

        private static void OnBuffGain(AIBaseClient sender, AIBaseClientBuffGainEventArgs args)
        {
            if (sender.IsEnemy && (args.Buff.Type == BuffType.Sleep || args.Buff.Type == BuffType.Snare || args.Buff.Type == BuffType.Stun) && misc.check("PS") && !sender.HasBuff(EnemyPassiveProced) && sender.DistanceToPlayer() < 600)
            {
                Orbwalker.Attack(sender);
            }
        }
        private static void OnMissile(GameObject sender, EventArgs args)
        {
            var missile = sender as MissileClient;
            if (sender.IsEnemy && !sender.Name.Contains("Minion") && !sender.Name.Contains("Basic") && KnownMissiles.Contains(missile.Name))
            {
                IncomingMissile = missile;
            }              
        }

        private static void OnAction(object sender, OrbwalkerActionArgs args)
        {
            if (WaitforAA && args.Type == OrbwalkerType.AfterAttack)
            {
                WaitforAA = !WaitforAA;
                LastAttackType = 0;
            }
        }

        private static void OnGapcloser(AIHeroClient sender, Gapcloser.GapcloserArgs args)
        {
            if (sender.IsEnemy && !sender.IsDead && !myhero.IsDead && args.EndPosition.DistanceToPlayer() < Q1.Range && Q1.IsReady())
            {
                var pred = Q1.GetPrediction(sender);

                if (pred.Hitchance == HitChance.Dash)
                    Q1.Cast(pred.CastPosition);
            }
        }

        #region Events
        public static void OnLevelUp(AIHeroClient sender, AIHeroClientLevelUpEventArgs args)
        {
            if (!sender.IsMe || !misc.check("autolevel")) return;

            DelayAction.Add(1, delegate
            {
                if (myhero.Level > 1 && myhero.Level < 4)
                {
                    switch (myhero.Level)
                    {
                        case 2:
                            myhero.Spellbook.LevelSpell(SpellSlot.E);
                            break;
                        case 3:
                            myhero.Spellbook.LevelSpell(SpellSlot.W);
                            break;
                    }
                }
                else if (myhero.Level >= 4)
                {
                    if (myhero.Spellbook.CanSpellBeUpgraded(SpellSlot.R))
                    {
                        myhero.Spellbook.LevelSpell(SpellSlot.R);
                    }
                    else if (myhero.Spellbook.CanSpellBeUpgraded(SpellSlot.Q))
                    {
                        myhero.Spellbook.LevelSpell(SpellSlot.Q);
                    }
                    else if (myhero.Spellbook.CanSpellBeUpgraded(SpellSlot.W))
                    {
                        myhero.Spellbook.LevelSpell(SpellSlot.W);
                    }
                    else if (myhero.Spellbook.CanSpellBeUpgraded(SpellSlot.E))
                    {
                        myhero.Spellbook.LevelSpell(SpellSlot.E);
                    }
                }
            });
        }

        private static void OnDraw(EventArgs args)
        {
            if (myhero.IsDead) return;
            

            if (draw.check("dq") && (draw.check("drdy") ? Q1.IsReady() : true))
            {
                Render.Circle.DrawCircle(myhero.Position, Q1.Range, Color.Red, 5, true);
            }

            if (draw.check("dw") && (draw.check("drdy") ? W.IsReady() : true))
            {
                Render.Circle.DrawCircle(myhero.Position, W.Range, Color.DarkRed, 5, true);
            }

            if (draw.check("deraa") && (draw.check("drdy") ? E.IsReady() : true))
            {
                Render.Circle.DrawCircle(myhero.Position, E.Range, Color.Red, 5, true);
            }

            var target = TargetSelector.GetTarget(1000f, DamageType.Physical);

            if (target != null)
            {
                if (draw.check("DRAWWAY") && target.GetWaypoints().Any())
                {
                    var wayp = target.GetWaypoints().LastOrDefault();

                    if (wayp.IsValid() && wayp.ToVector3World().IsOnScreen() && target.Position.IsOnScreen())
                    {
                        Drawing.DrawLine(Drawing.WorldToScreen(target.Position), Drawing.WorldToScreen(wayp.ToVector3World()), 2, Color.White);
                    }
                }

            }
        }

        private static void OnUpdate(EventArgs args)
        {
            if (myhero.IsDead) return;

            switch (Orbwalker.ActiveMode)
            {
                case OrbwalkerMode.Combo:
                    Combo();
                    break;
                case OrbwalkerMode.LaneClear:
                    if (myhero.ManaPercent >= slider(laneclear, "LMINMANA")) Clear();
                    break;
                case OrbwalkerMode.Harass:
                    SpamQ();
                    break;
            }

            SmoothRMovement();

            Misc();
        }
        #endregion

        static void Combo()
        {
            var target = TargetSelector.GetTarget(1200f, DamageType.Physical);

            if (target == null || myhero.HasBuff(RBuffName)) return;

            if (!myhero.HasBuff(PassiveName) && target.InAARangeOf(myhero))
            {
                WaitforAA = true;
                return;
            }

            if (combo.check("CQ") && Q1.IsReady() && !(WaitforAA || LastAttackType == 1))
            {
                if (target.DistanceToPlayer() < Q2.Range)
                {
                    var pred = Q2.GetPrediction(target);

                    if (pred.Hitchance >= hitchance && Q2.Cast(pred.CastPosition))
                    {
                        LastAttackType = 1;
                        WaitforAA = true;
                    }                  
                }
                else
                {
                    var pred = Q1.GetPrediction(target);

                    if (pred.Hitchance >= hitchance && Q1.Cast(pred.CastPosition, true))
                    {
                        WaitforAA = true;
                        LastAttackType = 1;
                    } 
                }
            }

            if (combo.check("CW") && W.IsReady() && (myhero.CountEnemies(W.Range - 10) >= slider(combo, "CWMIN" ) || (combo.check("CWS") && Q1.CooldownTime > 1f)) && !Q1.IsReady() && !E.IsReady() && GameObjects.EnemyHeroes.Any(x => x.IsFacing(myhero)))
            {
                W.Cast();
                LastAttackType = 2;
                WaitforAA = true; 
            }

            if (combo.check("CE") && E.IsReady())
            {
                var closeenemyturret = GameObjects.EnemyTurrets.OrderByDescending(x => x.Distance(target)).FirstOrDefault().Position;
                if (combo.check("CEQ") && Q1.IsReady() && myhero.CountEnemies(E.Range) > 1)
                {
                    foreach (var enemy in GameObjects.EnemyHeroes.Where(x => x.DistanceToPlayer() < E.Range))
                    {
                        var pred = E.GetPrediction(enemy);

                        if (pred.AoeTargetsHitCount > 1)
                        {
                            E.Cast(enemy);
                            DelayAction.Add(50, delegate { Q1.Cast(); WaitforAA = true; });
                        }
                    }
                }
                else if (target.DistanceToPlayer() <= E.Range && combo.check("CES") && !WaitforAA && LastAttackType != 3 && (target.Distance(closeenemyturret) >= 850 || (myhero.HealthPercent > 40 || target.GetTotalSpelldamage() > target.Health)) /*&& !(Q1.IsReady() && !WaitforAA && LastAttackType != 3) *//*!((Q1.IsReady() && LastAttackType != 1 && target.DistanceToPlayer() < Q1.Range) || !(WaitforAA && target.InAARangeOf(myhero)))*/)
                {
                    E.Cast(target);
                    LastAttackType = 3;
                    WaitforAA = true;
                }
                else if (combo.check("CEG") && !target.InAARangeOf(myhero) && !target.IsFacing(myhero) && myhero.IsFacing(target) && !GameObjects.AllyHeroes.Any(x => target.InAARangeOf(x)))
                {
                    var closerminion = GameObjects.Minions.Where(x => myhero.Position.Extend(x.Position, 650f).Distance(target) < myhero.Distance(target) && x.Distance(target) < 500).OrderBy(x => x.Distance(target)).FirstOrDefault();
                    if (closerminion != null)
                        E.Cast(closerminion);
                }        
            }

            if (combo.check("CR") && R.IsReady() && myhero.CountEnemies(R.Range - 100) > 0)
            {
                R.Cast();
                LastAttackType = 4;
            }

            if (combo.check("CIGN") && ignite != null && ignite.CanCast(target) && ((target.Health <= myhero.GetSummonerSpellDamage(target, SummonerSpell.Ignite) && myhero.Distance(target) < 400 && !GameObjects.AllyHeroes.Any(x => target.InAARangeOf(x))) ||
                    (target.HealthPercent > 5 && GameObjects.AllyHeroes.Count(x => x != myhero && target.InAARangeOf(x) && x.IsFacing(target)) > 0)))
            {
                ignite.Cast(target, true);
            }

        }

        static void Clear()
        {
            var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q1.Range) && x.IsMinion());

            var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(Q1.Range));

            var lMobs = GameObjects.Jungle.Where(x => x.IsValidTarget(Q1.Range) && x.GetJungleType() == JungleType.Legendary);

            var bMobs = GameObjects.Jungle.Where(x => x.IsValidTarget(Q1.Range) && x.GetJungleType() == JungleType.Large);

            IEnumerable<AIMinionClient> targets;

            if (minions.Any()) targets = minions;
            else if (lMobs.Any()) targets = lMobs;
            else if (bMobs.Any()) targets = bMobs;
            else targets = mobs;

            if (targets.Any())
            {
                if (laneclear.check("LQ") && Q1.IsReady() && !(laneclear.check("LQCLOSE") && !targets.Any(x => x.DistanceToPlayer() < 350)))
                {
                    foreach (var minion in targets)
                    {
                        SpellPrediction.PredictionOutput pred = null;
                        
                        if (minion.DistanceToPlayer() < Q2.Range)
                        {
                            if (HealthPrediction.GetPrediction(minion, 250) < 5) continue;

                            pred = Q2.GetPrediction(minion, true);
                            Q2.Cast(pred.CastPosition);
                        }
                        else
                        {
                            if (HealthPrediction.GetPrediction(minion, minion.qtt(Q1.Speed, 250)) < 5) continue;

                            pred = Q2.GetPrediction(minion);
                            Q1.Cast(pred.CastPosition);
                        }
                    }
                }

                if (laneclear.check("LW") && W.IsReady() && targets.Count(x => x.DistanceToPlayer() < W.Range) > slider(laneclear, "LWMIN"))
                {
                    W.Cast();
                }

                if (laneclear.check("LE") && E.IsReady() && targets.Where(x => x.DistanceToPlayer() < E.Range).Count() > slider(laneclear, "LEMIN"))
                {
                    foreach (var minion in targets)
                    {
                        if (myhero.Position.Extend(minion.Position, E.Range + 400f).IsUnderEnemyTurret()) continue;
                        var pred = E.GetPrediction(minion, false, -1, CollisionObjects.Minions);

                        if (pred.CollisionObjects.Count()> slider(laneclear, "LEMIN"))
                        {
                            E.Cast(minion);
                        }
                    }
                }
            }
        }

        static void SpamQ()
        {
            if (misc.check("HQ") && Q1.IsReady())
            {
                var target = TargetSelector.GetTarget(Q1.Range, DamageType.Physical);

                if (target != null && target.DistanceToPlayer() < Q1.Range)
                {
                    Q1.CastIfHitchanceMinimum(target, hitchance, true);
                }
            }
        }

        static void SmoothRMovement()
        {
            if (myhero.HasBuff(RBuffName) || myhero.HasBuff(WBuffName))
            {
                Orbwalker.AttackState = false;
                myhero.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
            }
            else
            {
                Orbwalker.AttackState = true;
            }
        }

        static void Misc()
        {
            if (misc.check("QKS") && Q1.IsReady())
            {
                var target = TargetSelector.GetTarget(Q1.Range, DamageType.Physical);

                if (target != null && Q1.GetDamage(target) > target.Health)
                {
                    Q1.CastIfHitchanceMinimum(target, hitchance);
                }
            }

            if (misc.check("AUTOPOT") && !myhero.HasBuffOfType(BuffType.Heal) && myhero.HealthPercent <= slider(misc, "POTMIN") && !myhero.InShop())
            {
                if (Items.CanUseItem(myhero, Potion.Id)) Potion.Cast();

                else if (Items.CanUseItem(myhero, Biscuit.Id)) Biscuit.Cast();

                else if (Items.CanUseItem(myhero, RPotion.Id)) RPotion.Cast();

                else if (Items.CanUseItem(myhero, CPotion.Id)) CPotion.Cast();
            }
        }

        static void DatMenu()
        {
            menu = new Menu("t7" + CharName, "T7 " + CharName, true);
            menu.Add(new MenuSeparator("sep", "By Toyota7 v" + version));

            combo = new Menu("combo", "Combo");
            combo.Add(new MenuBool("CQ", "Use Q"));
            combo.Add(new MenuBool("CW", "Use W", false));
            combo.Add(new MenuBool("CWS", "Use W For Stacks", false));
            combo.Add(new MenuSlider("CWMIN", "Min Enemies For W", 2, 1, 5));
            combo.Add(new MenuBool("CE", "Use E"));
            combo.Add(new MenuBool("CEG", "Use E On Minions To Gapclose"));
            combo.Add(new MenuBool("CES", "Use E For Stacks"));
            combo.Add(new MenuBool("CEQ", "Use EQ On Multiple Targets", false));
            combo.Add(new MenuBool("CR", "Use R"));        
            combo.Add(new MenuBool("CIGN", "Use Ignite"));
            menu.Add(combo);

            laneclear = new Menu("LANEC", "Laneclear");
            laneclear.Add(new MenuBool("LQ", "Use Q"));
            laneclear.Add(new MenuBool("LQCLOSE", "Only Use Close Range Q(Blade)"));
            //laneclear.Add(new MenuSlider("LQMIN", "Min Minions For Q Blade", 5, 1, 10));
            laneclear.Add(new MenuBool("LW", "Use W", false));
            laneclear.Add(new MenuSlider("LWMIN", "Min Minions For W", 5, 1, 10));
            laneclear.Add(new MenuBool("LE", "Use E", false));
            laneclear.Add(new MenuSlider("LEMIN", "Min Minions For E", 2, 1, 4));
            laneclear.Add(new MenuSlider("LMINMANA", "Min Mana % To Laneclear", 50, 5, 100));
            menu.Add(laneclear);

            draw = new Menu("draw", "Drawings");
            draw.Add(new MenuBool("dq", "Draw Q Range"));
            draw.Add(new MenuBool("dw", "Draw W Range"));
            draw.Add(new MenuBool("deraa", "Draw E/R Range"));
            draw.Add(new MenuBool("drdy", "Draw Only Ready Spells", false));
            draw.Add(new MenuBool("DRAWWAY", "Draw Target Waypoints", true));
            draw.Add(new MenuBool("DRAWDMG", "Draw Estimated Dmg on Enemies", true));
            menu.Add(draw);

            misc = new Menu("misc", "Misc");
            //misc.Add(new MenuSeparator("sep10", "Focusing Settings"));
            //misc.Add(new MenuList("FOCUS", "Focus On: ", new string[] { "Enemy ADC", "All Champs(TS)", "Custom Champion" }, 0));
            //misc.Add(new MenuList("CFOCUS", "Which Champion To Focus On? ", EnemyPlayerNames, 0));
            misc.Add(new MenuSeparator("sep12", "Other Settings"));
            misc.Add(new MenuList("chit", "Min Prediction Hitchance", new string[] { "Low", "Medium", "High", "Very High" }, 1)).ValueChanged += (s, e) =>
            {
                hitchance = (HitChance)comb(misc, "chit") + 1;
            };
            misc.Add(new MenuBool("BW", "Block Dangerous Spells With W"));
            foreach (var target in GameObjects.EnemyHeroes)
            {
                if (target.Spellbook.GetSpell(SpellSlot.Q).SData.CastType == SpellDataCastType.Missile ||
                    target.Spellbook.GetSpell(SpellSlot.W).SData.CastType == SpellDataCastType.Missile ||
                    target.Spellbook.GetSpell(SpellSlot.E).SData.CastType == SpellDataCastType.Missile ||
                    target.Spellbook.GetSpell(SpellSlot.R).SData.CastType == SpellDataCastType.Missile)
                {
                    misc.Add(new MenuBool("B" + target.Name, "Block " + target.Name));
                }
            }
            misc.Add(new MenuBool("PS", "Always Proc Passive On Stunned Enemies", false));
            misc.Add(new MenuBool("QGAP", "Use Q(Ranged) On Gapclosers", false));
            misc.Add(new MenuBool("HQ", "Use Q(Ranged) To Harass"));
            misc.Add(new MenuBool("QKS", "Killsteal With Q"));
            misc.Add(new MenuSeparator("sep14", "Auto Potion"));
            misc.Add(new MenuBool("AUTOPOT", "Activate Auto Potion"));
            misc.Add(new MenuSlider("POTMIN", "Min Health % To Active Potion", 50, 1, 100));
            misc.Add(new MenuSeparator("sep15", "Auto Level Up"));
            misc.Add(new MenuBool("autolevel", "Activate Auto Level Up Spells"));


            misc.Add(new MenuList("skinID", "Skin Hack", new string[]
            {
                "Default", "Psy Ops"
            }))
            .ValueChanged += (s, e) => myhero.SetSkin(comb(misc, "skinID"));
            menu.Add(misc);

            menu.Attach();
        }
    }
}
