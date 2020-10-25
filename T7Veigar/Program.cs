using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;
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
using Menu = EnsoulSharp.SDK.MenuUI.Menu;

namespace T7Veigar
{
    class Program
    {
        #region Declarations
        static void Main(string[] args) { GameEvent.OnGameLoad += OnLoad; }
        public static AIHeroClient myhero { get { return ObjectManager.Player; } }
        private static Menu menu, combo, harass, laneclear, jungleclear, misc, draw, pred , farm, lasthit;
        static HitChance hitQ, hitW, hitE;

        public static Spell Ignite { get; private set; }
        public static Spell Q { get; private set; }
        public static Spell W { get; private set; }
        public static Spell E { get; private set; }
        public static Spell R { get; private set; }

        public static Items.Item Potion { get; set; }
        public static Items.Item Biscuit { get; set; }
        public static Items.Item RPotion { get; set; }
        public static Items.Item CPotion { get; set; }
        #endregion

        #region Events
        private static void OnLoad()
        {
            if (myhero.CharacterName != "Veigar") return;

            Q = new Spell(SpellSlot.Q, 950f);
            Q.SetSkillshot(0.25f, 70f, 2000f, true, SkillshotType.Line);
            W = new Spell(SpellSlot.W, 900f);
            W.SetSkillshot(1.25f, 110f, 9999f, false, SkillshotType.Circle); //1 sec travel + 0.25 cast delay
            E = new Spell(SpellSlot.E, 700f);
            E.SetSkillshot(0.75f, 375f, 2000, true, SkillshotType.Line); //0.5 form time + 0.25 cast delay
            R = new Spell(SpellSlot.R, 650f); // 0.25 delay

            if (myhero.GetSpellSlot("SummonerDot") != SpellSlot.Unknown)
            {
                Ignite = new Spell(myhero.GetSpellSlot("SummonerDot"), 600f);
            }

            Potion = new Items.Item(ItemId.Health_Potion, 0f);
            Biscuit = new Items.Item(ItemId.Total_Biscuit_of_Rejuvenation, 0f);
            RPotion = new Items.Item(ItemId.Refillable_Potion, 0f);
            CPotion = new Items.Item(ItemId.Corrupting_Potion, 0f);
         
            DatMenu();

            Drawing.OnDraw += OnDraw;
            Drawing.OnEndScene += OnEndScene;
            AIHeroClient.OnLevelUp += OnLvlUp;
            Game.OnUpdate += OnTick;
            Gapcloser.OnGapcloser += OnGapcloser;         

            if (myhero.Level == 1) DelayAction.Add(300, delegate { myhero.Spellbook.LevelSpell(SpellSlot.Q); });

            myhero.SetSkin(slider(menu, "skinID"));

            hitQ = (HitChance)comb(pred, "QPred") + 1;
            hitW = (HitChance)comb(pred, "WPred") + 1;
            hitE = (HitChance)comb(pred, "EPred") + 1;

            Game.Print("<b><font color='#0040FF'>T7</font><font color='#FF0505'>Veigar</font><font color='#CC3939'></b>:<i>Reborn</i> </font> v2.1");
        }


        private static void OnTick(EventArgs args)
        {
            if (myhero.IsDead) return;
            
            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo) Combo();            

            if (Orbwalker.ActiveMode == OrbwalkerMode.Harass || key(harass, "AUTOH") && myhero.ManaPercent > slider(harass, "HMIN")) Harass();

            if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear /*|| laneclear.check("AUTOL")*/) Clear();

            if (Orbwalker.ActiveMode == OrbwalkerMode.LastHit) Lasthit();

            if (key(laneclear, "QSTACK") && slider(laneclear, "LMIN") <= myhero.ManaPercent) QStack();
            Misc();
        }

        private static void OnEndScene(EventArgs args)// credits babazhou
        {
            if (!draw.check("drawk") || draw.check("nodraw") || myhero.IsDead) return;

            foreach (var target in GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(2000) && !x.IsDead && x.IsHPBarRendered && Drawing.WorldToScreen(x.Position).IsOnScreen()))
            {
                float damage = ComboDamage(target);

                var hpBar = target.HPBarPosition;

                var damagePercentage = ((target.Health - damage) > 0 ? (target.Health - damage) : 0) / target.MaxHealth;
                var currentHealthPercentage = target.Health / target.MaxHealth;

                var startPoint = new Vector2(hpBar.X - 45 + damagePercentage * 104, hpBar.Y - 18);
                var endPoint = new Vector2(hpBar.X - 45 + currentHealthPercentage * 104, hpBar.Y - 18);

                Drawing.DrawLine(startPoint, endPoint, 12, Color.FromArgb(99, Color.LawnGreen));
            }
        }

        static void OnGapcloser(AIHeroClient sender, Gapcloser.GapcloserArgs args)
        {
            if (E.CanCast(sender) && sender.IsEnemy && comb(misc, "gapmode") != 0 && sender != null)
            {
                var Epred = E.GetSPrediction(sender);

                if (comb(misc, "gapmode") == 1 && !sender.IsFleeing && sender.IsFacing(myhero) && E.Cast(myhero.Position))
                    return;

                else if (comb(misc, "gapmode") == 2 && Epred.HitChance >= hitE && E.Cast(Epred.CastPosition))
                    return;
            }
        }

        private static void OnLvlUp(AIHeroClient sender, AIHeroClientLevelUpEventArgs args)
    {
            if (!sender.IsMe || !misc.check("autolevel")) return;

            DelayAction.Add(1, delegate
            {
                if (myhero.Level > 1 && myhero.Level < 4)
                {
                    switch (myhero.Level)
                    {
                        case 2:
                            myhero.Spellbook.LevelSpell(SpellSlot.W);
                            break;
                        case 3:
                            myhero.Spellbook.LevelSpell(SpellSlot.E);
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
                    else if (myhero.Spellbook.CanSpellBeUpgraded(comb(misc, "LEVELMODE") == 0 ? SpellSlot.E : SpellSlot.W))
                    {
                        myhero.Spellbook.LevelSpell(comb(misc, "LEVELMODE") == 0 ? SpellSlot.E : SpellSlot.W);
                    }
                    else if (myhero.Spellbook.CanSpellBeUpgraded(comb(misc, "LEVELMODE") == 0 ? SpellSlot.W : SpellSlot.E))
                    {
                        myhero.Spellbook.LevelSpell(comb(misc, "LEVELMODE") == 0 ? SpellSlot.W : SpellSlot.E);
                    }
                }
            });
        }

        private static void OnDraw(EventArgs args)
        {
            if (myhero.IsDead || draw.check("nodraw")) return;

            var colorM = draw["dcolor"].GetValue<MenuColor>();
            var color = Color.FromArgb(colorM.ColorR, colorM.ColorG, colorM.ColorB);

            if (draw.check("drawQ") && Q.Level > 0)
            {
                Render.Circle.DrawCircle
                (
                    myhero.Position,                   
                    Q.Range,
                    draw.check("nodrawc") ? (Q.IsReady() ? color : Color.Transparent) : color,
                    5,
                    true
                );
            }

            if (draw.check("drawW") && W.Level > 0)
            {
                Render.Circle.DrawCircle
                (
                    myhero.Position,
                    W.Range,
                    draw.check("nodrawc") ? (W.IsReady() ? color : Color.Transparent) : color
                );
            }

            if (draw.check("drawE") && E.Level > 0)
            {
                Render.Circle.DrawCircle
                (
                    myhero.Position,
                    E.Range,
                    draw.check("nodrawc") ? (E.IsReady() ? color : Color.Transparent) : color
                );
            }

            if (draw.check("drawR") && R.Level > 0)
            {
                Render.Circle.DrawCircle
                (
                    myhero.Position,
                    R.Range,
                    draw.check("nodrawc") ? (R.IsReady() ? color : Color.Transparent) : color
                );
            }

            if (draw.check("drawStacks"))
            {
                Drawing.DrawText(Drawing.WorldToScreen(myhero.Position).X - 50, Drawing.WorldToScreen(myhero.Position).Y + 10,
                                key(laneclear, "QSTACK") ? Color.LightGreen : Color.Red, key(laneclear, "QSTACK") ? "Auto Stacking: ON" : "Auto Stacking: OFF");
            }

            var target = TargetSelector.GetTarget(Q.Range);

            if (draw.check("DRAWTARGET") && target != null)
            {
                Render.Circle.DrawCircle(target.Position, target.BoundingRadius - 15, Color.LightYellow, 4);
            }

            if (draw.check("DRAWWAY") && target != null &&target.GetWaypoints().Any())
            {
                var wayp = target.GetWaypoints().LastOrDefault();

                if (wayp.IsValid() && wayp.ToVector3World().IsOnScreen() && target.Position.IsOnScreen())
                {
                    Drawing.DrawLine(Drawing.WorldToScreen(target.Position), Drawing.WorldToScreen(wayp.ToVector3World()), 2, Color.White);
                }
            }
        }
        #endregion

        #region Modes
        private static void Combo()
        {
            var target = TargetSelector.GetTarget(/*E.IsReady() ? E.Range + 375 : */Q.Range, DamageType.Magical);

            if (target != null && target.IsValidTarget())
            {
                if (E.CanCast(target) && combo.check("CE") && !(combo.check("EIMMO") && target.HaveImmovableBuff()))
                {
                    var Epred = E.GetSPrediction(target);

                    switch (comb(combo, "CEMODE"))
                    {
                        case 0:
                            var pred1 = SPrediction.Prediction.GetFastUnitPosition(target, 0.75f);
                            if (E.IsInRange(pred1)) E.Cast(pred1);
                            else if (pred1.DistanceToPlayer() > E.Range && pred1.DistanceToPlayer() < E.Range + 370f) E.Cast(myhero.Position.Extend(target.Position, 370f));
                            break;
                        case 1:
                            var way = target.GetWaypoints().LastOrDefault();
                            var wayb = way != null && way.IsValid() ? target.DistanceToPlayer() < way.DistanceToPlayer() : false;
                            var dist = target.HaveImmovableBuff() ?
                                                       310f : (target.IsFacing(myhero) ?
                                                                                (target.IsMoving || wayb ? 320f + 0.60f * target.MoveSpeed : 340f) : (target.IsMoving ? 230f - 0.60f * target.MoveSpeed : 260f));
                            var spot = target.Position.Extend(myhero.Position, dist);

                            if (E.IsInRange(spot)) E.Cast(spot);
                            break;
                        case 2:
                            var pred = E.GetAoeSPrediction();

                            if (pred.HitCount >= slider(combo, "CEAOE"))
                                E.Cast(pred.CastPosition);
                        break;             
                    }                                       
                }

                

                if (Q.CanCast(target) && combo.check("CQ"))
                {
                    
                    var Qpred = Q.GetSPrediction(target);
                    var col = Qpred.CollisionResult.Units.Any(x => x.Team != myhero.Team) ? Qpred.CollisionResult.Units.Count(x => x.Team != myhero.Team) < 2 : true;

                    if (col && Qpred.HitChance >= hitQ && Q.Cast(Qpred.CastPosition))
                    {
                        return;
                    }
                }

                if (W.CanCast(target) && combo.check("CW"))
                {
                    var Wpred = W.GetSPrediction(target);

                    switch (comb(combo, "CWMODE"))
                    {
                        case 0:
                            if (Wpred.HitChance >= hitW || Wpred.HitChance == HitChance.Immobile ||
                               (target.HasBuffOfType(BuffType.Slow) && Wpred.HitChance == HitChance.High))
                            {
                                W.Cast(Wpred.CastPosition);
                            }
                            break;
                        case 1:
                            W.Cast(target.Position);
                            break;
                        case 2:
                            if (target.HaveImmovableBuff())
                            {
                                W.Cast(Wpred.CastPosition);
                            }
                            break;
                    }
                }

                if (combo.check("CR") && R.IsReady() &&
                    R.IsInRange(target.Position) && (ComboDamage(target) > target.GetRealHealth(DamageType.Magical) ||
                    myhero.GetSpellDamage(target, SpellSlot.R) > target.GetRealHealth(DamageType.Magical)) && /*!target.HasBuff("bansheesveil") &&*/ !target.HasBuff("SamiraW") && !target.HaveSpellShield())
                {
                    if ((ComboDamage(target) - myhero.GetSpellDamage(target, SpellSlot.R)) > target.Health) return;
                    R.Cast(target);
                }

                if (Ignite != null && combo.check("IgniteC") && Ignite.IsReady() && ComboDamage(target) < target.Health &&
                    Ignite.IsInRange(target.Position) && myhero.GetSummonerSpellDamage(target, SummonerSpell.Ignite) + ComboDamage(target) > target.Health)
                {
                    Ignite.Cast(target);
                }
            }
        }

        private static void Harass()
        {
            var target = TargetSelector.GetTarget(Q.Range, DamageType.Magical);

            if (target != null && target.IsValidTarget(Q.Range))
            {
                if (Q.CanCast(target) && harass.check("HQ"))
                {
                    var Qpred = Q.GetSPrediction(target);

                    if (comb(harass, "HQMODE") == 0)
                    {
                        Q.Cast(Qpred.CastPosition);
                    }
                    else if (Qpred.CollisionResult.Units.Count() < 2 && Qpred.HitChance >= hitQ)
                    {
                        Q.Cast(Qpred.CastPosition);
                    }
                }

                if (W.CanCast(target) && harass.check("HW"))
                {
                    var Wpred = W.GetSPrediction(target);

                    switch (comb(harass, "HWMODE"))
                    {
                        case 0:
                            if (Wpred.HitChance >= hitW || Wpred.HitChance == HitChance.Immobile ||
                               (target.HasBuffOfType(BuffType.Slow) && Wpred.HitChance == HitChance.High))
                            {
                                W.Cast(Wpred.CastPosition);
                            }
                            break;
                        case 1:
                            W.Cast(target.Position);
                            break;
                        case 2:
                            if (target.HaveImmovableBuff())
                            {
                                W.Cast(Wpred.CastPosition);
                            }
                            break;
                    }
                }
            }
        }

        static void Clear()
        {
            var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q.Range) && x.IsMinion());

            var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(Q.Range));

            var lMobs = GameObjects.Jungle.Where(x => x.IsValidTarget(Q.Range) && x.GetJungleType() == JungleType.Legendary);

            var bMobs = GameObjects.Jungle.Where(x => x.IsValidTarget(Q.Range) && x.GetJungleType() == JungleType.Large);

            //IEnumerable<AIMinionClient> targets;

            if (minions.Any()) Laneclear(minions);
            else if (lMobs.Any()) Jungleclear(lMobs);
            else if (bMobs.Any()) Jungleclear(bMobs);
            else Jungleclear(mobs);
        }

        private static void Laneclear(IEnumerable<AIMinionClient> minions)
        {
            if (minions != null && myhero.ManaPercent > slider(laneclear, "LMIN"))
            {
                if (!key(laneclear, "QSTACK") && Q.IsReady() && laneclear.check("LQ"))
                {
                    var pred = Q.GetLineFarmLocation(minions.ToList());

                    if (pred.MinionsHit > 0)
                    {
                        Q.Cast(pred.Position);
                    }
                }

                if (W.IsReady() && laneclear.check("LW"))
                {
                    var pred = W.GetCircularFarmLocation(minions.ToList());

                    if (pred.MinionsHit > slider(laneclear, "LWMIN") && W.Cast(pred.Position)) return;
                }
            }
        }

        private static void Jungleclear(IEnumerable<AIMinionClient> minions)
        {
            if (minions != null && myhero.ManaPercent > slider(jungleclear, "JMIN"))
            {
                if (jungleclear.check("JQ") && Q.IsReady())
                {
                    foreach (var x in minions.Where(x => Q.GetHealthPrediction(x) > 0))
                    {
                        if (comb(jungleclear, "JQMODE") == 0 && x.Name.Contains("Mini")) continue;

                        Q.Cast(Q.GetPrediction(x).CastPosition);
                    };
                }

                if (jungleclear.check("JW") && W.IsReady())
                {
                    if (minions.Count() == 1)
                    {                       
                        var target = minions.FirstOrDefault();

                        if (jungleclear.check("JQ") && W.GetDamage(target) > HealthPrediction.GetPrediction(target, 750, 0) - 10) return;

                        if (target.Health > myhero.GetAutoAttackDamage(target) * 2)
                        {
                            W.Cast(W.GetPrediction(target).CastPosition);
                        }
                    }
                    else
                    {
                        var pred = W.GetCircularFarmLocation(minions.ToList());

                        if (pred.MinionsHit > 0 && W.Cast(pred.Position)) return;
                    }
                }
            }
        }

        static void Lasthit()
        {
            if (!Q.IsReady() || !lasthit.check("LHQ")) return;

            var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q.Range - 10) && x.IsMinion());//.OrderByDescending(x => x.Health);

            if (minions != null)
            {
                AIMinionClient besttarget = null;
                foreach (var minion in minions.Where(x => !x.IsDead && x.Health < QDamage(x) - 10 && Q.GetHealthPrediction(x) > 10))
                {
                    var Qpred = Q.GetPrediction(minion);

                    var collisions = Qpred.CollisionObjects.ToList();

                    if (collisions.Count() > 0 && !lasthit.check("LHQD")) break;

                    else if (collisions.Count() <= 1 && minion.GetMinionType() == MinionTypes.Siege)
                    {
                        besttarget = minion;
                        break;
                    }
                    else if (collisions.Count() == 1 && collisions[0].Health < QDamage(collisions[0]) - 10 && Q.GetHealthPrediction(collisions[0]) > 0 &&
                            !(minion.GetMinionType() == MinionTypes.Siege && HealthPrediction.GetPrediction(minion, (int)(Q.CooldownTime * 1100)) <= 0))
                    {
                        besttarget = minion;
                        break;
                    }
                    else if (collisions.Count() > 1 && collisions[0].Health < QDamage(collisions[0]) - 10 && collisions[1].Health < QDamage(collisions[1]) - 10
                                                    && Q.GetHealthPrediction(collisions[0]) > 0 && Q.GetHealthPrediction(collisions[1]) > 0)
                    {
                        besttarget = minion;
                        break;
                    }
                    else if (collisions.Count() < 2)
                        besttarget = minion;
                }

                if (besttarget != null && Q.CanCast(besttarget) && Q.GetHealthPrediction(besttarget) > 10)
                {
                    //if (HealthPrediction.HasTurretAggro(besttarget) && )
                    Q.Cast(Q.GetPrediction(besttarget).CastPosition);
                }
            }
        }

        private static void Misc()
        {

            if (misc.check("KSR") && R.IsReady())
            {
                foreach (var hero in GameObjects.EnemyHeroes.Where(x => x.GetRealHealth(DamageType.Magical) < myhero.GetSpellDamage(x, SpellSlot.R) && R.CanCast(x) && !x.HaveSpellShield()).OrderByDescending(x => x.ChampionsKilled))
                {
                    if (hero != null) R.Cast(hero);
                }
            }
            
            var target = TargetSelector.GetTarget(Q.Range, DamageType.Magical);

            if (target != null && target.IsValidTarget())
            {
                if (misc.check("KSQ") && Q.CanCast(target) && QDamage(target) > target.Health)
                {
                    var pred = Q.GetSPrediction(target);

                    //if (pred.HitChance >= hitQ) Q.Cast(pred.CastPosition);
                    Q.Cast(pred.CastPosition);
                }

                if (misc.check("KSW") && W.CanCast(target) && WDamage(target) > target.Health)
                {
                    var pred = W.GetSPrediction(target);

                    if (pred.HitChance >= hitW) W.Cast(pred.CastPosition);
                }

                

                if (Ignite != null && misc.check("autoign") && Ignite.IsReady() && target.IsValidTarget(Ignite.Range) &&
                    myhero.GetSummonerSpellDamage(target, SummonerSpell.Ignite) > target.Health)
                {
                    Ignite.Cast(target);
                }
            }  

            if (misc.check("KSJ") && W.IsReady() && GameObjects.JungleLegendary.Any())
            {
                var lmob = GameObjects.JungleLegendary.Where(x => x.IsValidTarget(W.Range) && HealthPrediction.GetPrediction(x, 1000) > 0 && myhero.GetSpellDamage(x, SpellSlot.W) > HealthPrediction.GetPrediction(x, 1000)).FirstOrDefault();

                if (lmob != null && W.IsInRange(lmob.Position)) W.Cast(lmob.Position);               
            }

            if (misc.check("AUTOPOT") && !myhero.HasBuffOfType(BuffType.Heal) && myhero.HealthPercent <= slider(misc, "POTMIN"))
            {
                if (Items.CanUseItem(myhero, Potion.Id)) Potion.Cast();

                else if (Items.CanUseItem(myhero, Biscuit.Id)) Biscuit.Cast();

                else if (Items.CanUseItem(myhero, RPotion.Id)) RPotion.Cast();

                else if (Items.CanUseItem(myhero, CPotion.Id)) CPotion.Cast();
            }
            //myhero.SetSkin(comb(misc, "skinID"));
        }
        #endregion

        #region Menu
        public static void DatMenu()
        {    
            menu = new Menu("veigarxd", "T7 Veigar:R", true);
            combo = new Menu("combo", "Combo");
            harass = new Menu("harass", "Harass");
            farm = new Menu("farmm", "Farm");
            laneclear = new Menu("lclear", "Laneclear");
            jungleclear = new Menu("jclear", "Jungleclear");
            lasthit = new Menu("lasthit", "Lasthit");
            misc = new Menu("misc", "Misc");
            draw = new Menu("draww", "Drawings");
            pred = new Menu("predi", "Prediction");

            menu.Add(new MenuSeparator("58274823", "By Toyota7 v2.1"));          

            combo.Add(new MenuBool("CQ", "Use Q"));
            combo.Add(new MenuBool("CW", "Use W"));
            combo.Add(new MenuBool("CE", "Use E"));
            combo.Add(new MenuBool("CR", "Use R"));
            if (Ignite != null)
                combo.Add(new MenuBool("IgniteC", "Use Ignite", false));
            combo.Add(new MenuSeparator("8942938", "|"));
            combo.Add(new MenuList("CWMODE", "Select W Mode:", new string[] { "With Prediciton", "Without Prediction", "Only On Stunned Enemies" }, 0));
            combo.Add(new MenuList("CEMODE", "Select E Mode: ", new string[] { "Target On The Center", "Target On The Edge(stun)", "AOE" }, 0));
            combo.Add(new MenuSlider("CEAOE", "Min Champs For AOE Function", 2, 1, 5));
            combo.Add(new MenuBool("EIMMO", "Dont Use E On Immobile Enemies", false));
            menu.Add(combo);

            harass.Add(new MenuBool("HQ", "Use Q", false));
            harass.Add(new MenuList("HQMODE", "Select Q Mode", new string[] { "Spam", "Normal" }, 1));
            harass.Add(new MenuBool("HW", "Use W", false));
            harass.Add(new MenuList("HWMODE", "Select W Mode", new string[] { "With Prediciton", "Without Prediction(Not Recommended)", "Only On Stunned Enemies" } , 2));
            harass.Add(new MenuKeyBind("AUTOH", "Auto Harass ", Keys.H, KeyBindType.Toggle)).Permashow(true);
            harass.Add(new MenuSlider("HMIN", "Min Mana % To Harass", 40, 0, 100));
            menu.Add(harass);

            laneclear.Add(new MenuSeparator("89283563453", "Auto Q Stacking"));
            laneclear.Add(new MenuKeyBind("QSTACK", "Auto Stacking", Keys.J, KeyBindType.Toggle)).Permashow(true, null, SharpDX.Color.LightGreen);
            laneclear.Add(new MenuList("QSTACKMODE", "Select Mode", new string[] { "LastHit Only", "Spam Q" }, 0));
            laneclear.Add(new MenuBool("QSTACKDOUBLE", "Q Through Other Minions"));
            laneclear.Add(new MenuSeparator("4225390234", " "));
            laneclear.Add(new MenuSeparator("4214313453", "Laneclear Settings"));
            laneclear.Add(new MenuBool("LQ", "Use Q"));                  
            laneclear.Add(new MenuBool("LW", "Use W", false));
            laneclear.Add(new MenuSlider("LWMIN", "Min Minions For W", 2, 1, 6));
            //laneclear.Add(new MenuBool("AUTOL", new CheckBox("Auto Laneclear", false));
            laneclear.Add(new MenuSlider("LMIN", "Min Mana % To Laneclear/AutoStack", 50, 0, 100));
            menu.Add(laneclear);

            jungleclear.Add(new MenuBool("JQ", "Use Q"));
            jungleclear.Add(new MenuList("JQMODE", "Q Mode", new string[] { "All Monsters", "Big Monsters" }, 0));
            jungleclear.Add(new MenuBool("JW", "Use W", false));
            jungleclear.Add(new MenuSlider("JMIN", "Min Mana % To Jungleclear", 10, 0, 100));
            menu.Add(jungleclear);

            lasthit.Add(new MenuBool("LHQ", "Use Q"));
            lasthit.Add(new MenuBool("LHQD", "Q Through Other Minions"));
            menu.Add(lasthit);

            draw.Add(new MenuBool("nodraw", "Disable All Drawings", false));
            draw.Add(new MenuBool("drawQ", "Draw Q Range"));
            draw.Add(new MenuBool("drawW", "Draw W Range"));
            draw.Add(new MenuBool("drawE", "Draw E Range"));
            draw.Add(new MenuBool("drawR", "Draw R Range"));
            draw.Add(new MenuColor("dcolor", "Range Color", ColorBGRA.FromRgba(Color.Fuchsia.ToRgba())));
            draw.Add(new MenuSeparator("84978942", "|"));
            draw.Add(new MenuBool("drawk", "Draw Combo Damage On Enemies"));
            draw.Add(new MenuBool("DRAWTARGET", "Draw Target", false));
            draw.Add(new MenuBool("DRAWWAY", "Draw Target Waypoint"));
            draw.Add(new MenuBool("nodrawc", "Draw Only Ready Spells", false));
            draw.Add(new MenuBool("drawStacks", "Draw Auto Stack Status", false));
            menu.Add(draw);

            misc.Add(new MenuSeparator("5562522342", "Stealing"));
            misc.Add(new MenuBool("KSQ", "Killsteal with Q"));
            misc.Add(new MenuBool("KSW", "Killsteal with W", false));
            misc.Add(new MenuBool("KSR", "Killsteal with R", false));
            if (Ignite != null)
                misc.Add(new MenuBool("autoign", "Auto Ignite If Killable"));           
            misc.Add(new MenuBool("KSJ", ">Try< To Steal Dragon/Baron/Rift With W"));
            misc.Add(new MenuSeparator("53434342", "AP"));
            misc.Add(new MenuBool("AUTOPOT", "Auto Potion"));
            misc.Add(new MenuSlider("POTMIN", "Min Health % To Activate Potion", 50, 1, 100));
            misc.Add(new MenuSeparator("5124142", "AntiGapcloser"));
            misc.Add(new MenuList("gapmode", "Use E On Gapcloser Mode:", new string[] { "Off", "Self", "Enemy(Pred)" }, 0));
            misc.Add(new MenuSeparator("5124144213412", "AutoLevelUp Spells"));
            misc.Add(new MenuBool("autolevel", "Activate Auto Level Up Spells"));
            misc.Add(new MenuList("LEVELMODE", "Select Sequence", new string[] { "Q>E>W", "Q>W>E" }, 1));         
            menu.Add(misc);
            
            pred.Add(new MenuList("QPred", "Q Min Hitchance -> ", new string[] { "Low", "Medium", "High", "VeryHigh" }, 1)).ValueChanged += (s, e) =>
            {
                hitQ = (HitChance)comb(pred, "QPred") + 1;
            }; ;
            pred.Add(new MenuList("WPred", "W Min Hitchance -> ", new string[] { "Low", "Medium", "High", "VeryHigh" }, 1)).ValueChanged += (s, e) =>
            {
                hitW = (HitChance)comb(pred, "WPred") + 1;
            }; ;
            pred.Add(new MenuList("EPred", "E Min Hitchance -> ", new string[] { "Low", "Medium", "High", "VeryHigh" }, 1)).ValueChanged += (s, e) =>
            {
                hitE = (HitChance)comb(pred, "EPred") + 1;
            }; ;
            Prediction.Initialize(pred);
            menu.Add(pred);

            menu.Add(new MenuSlider("skinID", "Skin Hack", 9,0,31)).ValueChanged += (s, e) => myhero.SetSkin(slider(menu, "skinID"));

            menu.Attach();
        }
        #endregion

        #region Methods
        private static float ComboDamage(AIHeroClient target)
        {
            if (target != null)
            {
                float TotalDamage = 0;

                if (Q.State != SpellState.NotLearned && Q.IsReady()) { TotalDamage += QDamage(target); }

                if (W.State != SpellState.NotLearned && W.IsReady()) { TotalDamage += WDamage(target); }

                if (R.State != SpellState.NotLearned && R.IsReady()) { TotalDamage += (float)myhero.GetSpellDamage(target, SpellSlot.R); }

                return TotalDamage;
            }
            return 0;
        }

        private static void QStack()
        {
            if (!Q.IsReady() || Orbwalker.ActiveMode.Equals(OrbwalkerMode.Combo) || myhero.IsRecalling()) return;

            var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q.Range - 10) && x.IsMinion()).OrderByDescending(x => x.Health);

            if (minions != null)
            {
                if (comb(laneclear, "QSTACKMODE") == 0)
                {
                    AIMinionClient besttarget = null;
                    foreach (var minion in minions.Where(x => !x.IsDead && x.Health < QDamage(x) - 10 && Q.GetHealthPrediction(x) > 10))
                    {
                        var Qpred = Q.GetPrediction(minion);

                        var collisions = Qpred.CollisionObjects.ToList();

                        if (collisions.Count() > 0 && !laneclear.check("QSTACKDOUBLE")) break; 

                        else if (collisions.Count() <= 1 && minion.GetMinionType() == MinionTypes.Siege)
                        {
                            besttarget = minion;
                            break;
                        }
                        else if (collisions.Count() == 1 && collisions[0].Health < QDamage(collisions[0]) - 10 && Q.GetHealthPrediction(collisions[0]) > 0)
                        {
                            besttarget = minion;
                            break;
                        }
                        else if (collisions.Count() > 1 && collisions[0].Health < QDamage(collisions[0]) - 10 && collisions[1].Health < QDamage(collisions[1]) - 10
                                                        && Q.GetHealthPrediction(collisions[0]) > 0 && Q.GetHealthPrediction(collisions[1]) > 0)
                        {
                            besttarget = minion;
                            break;
                        }
                        else if (collisions.Count() < 2)
                            besttarget = minion;
                    }

                    if (besttarget != null && Q.CanCast(besttarget) && Q.GetHealthPrediction(besttarget) > 10)
                    {
                        Q.Cast(Q.GetPrediction(besttarget).CastPosition);
                    }
                }
                else
                {
                    var pred = Q.GetLineFarmLocation(minions.ToList());

                    if (pred.MinionsHit > 0) Q.Cast(pred.Position);
                }                
            }
        }

        private static float QDamage(AIBaseClient target)
        {
            var index = Q.Level - 1;
            
            if (Q.State != SpellState.Ready) return 0f;

            var QDamage = new[] { 80, 120, 160, 200, 240 }[index] +
                          (0.6f * myhero.FlatMagicDamageMod);

            return (float)myhero.CalculateMagicDamage(target, QDamage);
        }

        private static float WDamage(AIHeroClient target)///////
        {
            if (W.State != SpellState.Ready) return 0f;

            var WDamage = (50 * (W.Level + 1)) + myhero.FlatMagicDamageMod;

            return (float)myhero.CalculateMagicDamage(target, WDamage);
        }

        public static int comb(Menu submenu, string sig)
        {
            return submenu[sig].GetValue<MenuList>().Index;
        }
        public static int slider(Menu submenu, string sig)
        {
            return submenu[sig].GetValue<MenuSlider>().Value;
        }

        public static bool key(Menu submenu, string sig)
        {
            return submenu[sig].GetValue<MenuKeyBind>().Active;
        }
        #endregion
    }

    public static class Extensions
    {
        public static bool check(this Menu menu, string sig, string sig2 = null)
        {
            return sig2 == null ? menu[sig].GetValue<MenuBool>().Enabled : menu[sig][sig2].GetValue<MenuBool>().Enabled;
        }

        public static bool ValidTarget(this AIHeroClient hero, int range)
        {
            return !hero.HasBuff("UndyingRage") && !hero.HasBuff("JudicatorIntervention") && !hero.HasBuff("ChronoShift") && !hero.HasBuff("kindredrnodeathbuff") &&
                   !hero.IsInvulnerable && !hero.IsDead && hero.IsValidTarget(range) &&
                   !hero.HasBuffOfType(BuffType.Invulnerability) && !hero.HasBuffOfType(BuffType.SpellImmunity) && !hero.HasBuffOfType(BuffType.SpellShield);

        }

        public static Vector3 Shorten(this Vector3 source, Vector3 to, float distance)
        {
            return source - distance * (to - source).Normalized();
        }
    }
}
