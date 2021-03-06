using System;
using System.Linq;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.Utility;
using SharpDX;
using Color = System.Drawing.Color;

namespace T7Annie
{
    public class HasCorona
    {
        #region Decler
        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;
        public static Spell ignite;
        static Menu menu, combo, laneclear, draw,harass;
        static bool gotAggro, skinchange = false;
        public static AIHeroClient myhero { get { return ObjectManager.Player; } }
        static HitChance hitchance;
        static Items.Item Potion, Biscuit, RPotion, CPotion;
        #endregion

        private static void Main(string[] args)
        {
            GameEvent.OnGameLoad += OnGameLoad;
        }

        private static void OnGameLoad()
        {
            if (ObjectManager.Player.CharacterName != "Annie") return;

            Q = new Spell(SpellSlot.Q, 625f);
            Q.SetTargetted(0.25f, 1392f); //real speed found manually
            W = new Spell(SpellSlot.W, 550f);
            W.SetSkillshot(0.25f, 50f, 3000f, false, SpellType.Cone);
            E = new Spell(SpellSlot.E);
            R = new Spell(SpellSlot.R, 600f);
            R.SetSkillshot(0.25f, 50f, 9999f, false, SpellType.Circle);

            Potion = new Items.Item(ItemId.Health_Potion, 0f);
            Biscuit = new Items.Item(ItemId.Total_Biscuit_of_Everlasting_Will, 0f);
            RPotion = new Items.Item(ItemId.Refillable_Potion, 0f);
            CPotion = new Items.Item(ItemId.Corrupting_Potion, 0f);

            if (myhero.GetSpellSlot("SummonerDot") != SpellSlot.Unknown)
            {
                ignite = new Spell(myhero.GetSpellSlot("SummonerDot"), 600f);
            }

            DatMenu();       

            Game.OnUpdate += OnUpdate;
            Drawing.OnDraw += OnDraw;
            Orbwalker.OnBeforeAttack += OnBeforeAA;
            AIBaseClient.OnProcessSpellCast += OnProcessSpellCast;
            AIHeroClient.OnAggro += OnAggro;
            AIHeroClient.OnLevelUp += OnLevelUp;

            if (myhero.Level == 1) DelayAction.Add(300, delegate { myhero.Spellbook.LevelSpell(SpellSlot.Q); });
            menu["skin"].GetValue<MenuList>().Index = 0;

            Console.WriteLine("T7 Annie Loaded");
            Game.Print("<b><font color='#0040FF'>T7</font><font color='#990000'> Annie </font></b> Loaded!");
        }

        #region Events
        public static void OnLevelUp(AIHeroClient sender, AIHeroClientLevelUpEventArgs args)
        {
            if (!sender.IsMe || !menu.check("autol")) return;

            //var order = new int[] { 1,2,3,1,1,4,1,2,1,2,4,2,2,3,3,4,3,3 }; 

            DelayAction.Add(1, delegate
            {              
                //myhero.Spellbook.LevelSpell((SpellSlot)(order[args.Level - 1]));

                if (myhero.Level == 2)
                {
                    if (!myhero.GetSpell(SpellSlot.W).Learned) myhero.Spellbook.LevelSpell(SpellSlot.W);
                    else myhero.Spellbook.LevelSpell(SpellSlot.E);
                    return;
                }
                if (myhero.Level == 3)
                {
                    if (!myhero.GetSpell(SpellSlot.E).Learned) myhero.Spellbook.LevelSpell(SpellSlot.E);
                    else myhero.Spellbook.LevelSpell(SpellSlot.W);
                    return;
                }

                myhero.Spellbook.LevelSpell(SpellSlot.R);
                myhero.Spellbook.LevelSpell(SpellSlot.Q);
                myhero.Spellbook.LevelSpell(SpellSlot.W);
                myhero.Spellbook.LevelSpell(SpellSlot.E);
            });
            }


        private static void OnAggro(AIBaseClient sender, AIBaseClientAggroEventArgs args)
        {
            if (!myhero.IsDead && sender.IsEnemy && !sender.IsMinion() && args.NetworkId == myhero.NetworkId) gotAggro = true;
        }

        private static void OnProcessSpellCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if (myhero.IsDead || !sender.IsEnemy || sender.IsMinion()) return;

            if (args.Slot == SpellSlot.R)
            {
                switch (args.SData.CastType)
                {
                    case SpellDataCastType.CircleMissile:
                        if (args.End.DistanceToPlayer() < args.SData.CastRadius - 20) E.Cast();
                        break;
                    case SpellDataCastType.Missile:
                        var pred = new PredictionInput();
                        pred.Type = SpellType.Line;
                        pred.Range = args.SData.CastRange;
                        pred.Speed = args.SData.MissileSpeed;
                        pred.Radius = args.SData.LineWidth;

                        if (Prediction.GetPrediction(pred).CollisionObjects.Contains(myhero)) E.Cast();
                        break;
                }
            }
            else if (args.Target == myhero) E.Cast();
        }

        private static void OnBeforeAA(object sender, BeforeAttackEventArgs args)
        {
            var target = TargetSelector.GetTarget(1000f);

            if (target != null)
            {
                if (Orbwalker.ActiveMode == OrbwalkerMode.Combo && (Q.IsReady() || W.IsReady()) && myhero.IsFacing(target) && target.InAARangeOf(myhero)) args.Process = false;

                if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear && Q.IsReady() && comb(laneclear, "lq") == 2 && myhero.ManaPercent >= slider(laneclear, "manamin")) args.Process = false;
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo && combo.check("cnaa"))
            {
                args.Process = false;
            }
        }
        #endregion

        private static void Combo()
        {
            var target = TargetSelector.GetTarget(680f, DamageType.Magical);

            if (target == null) return;

            switch (comb(combo, "chit"))
            {
                case 0: hitchance = HitChance.Low; break;
                case 1: hitchance = HitChance.Medium; break;
                case 2: hitchance = HitChance.High; break;
                case 4: hitchance = HitChance.VeryHigh; break;
                default: hitchance = HitChance.Medium; break;
            };

            if (combo.check("cr") && R.CanCast(target) && target != null && target.IsValidTarget())
            {
                if (myhero.GetBuffCount("anniepassivestack") == 3 && E.IsReady()) E.Cast(); // Use E to get the last stack
                if (combo.check("crk") && !target.killable(true,target.MaxHealth * 0.1f)) goto lol;

                var pred = R.GetPrediction(target, true);

                if (pred.Hitchance >= hitchance) R.Cast(pred.CastPosition);
            }


        lol:

            if (combo.check("cq") && Q.CanCast(target) && target != null && target.IsValidTarget())
            {
                if (myhero.HasBuff("anniepassiveprimed") && !myhero.HasBuff("AnnieRController") && combo.check("cr") && target.killable() && R.CanCast(target)) return;
                Q.Cast(target);
            }

            if (combo.check("cw") && W.CanCast(target) && target != null && target.IsValidTarget())
            {
                var pred = W.GetPrediction(target, true);

                if (pred.Hitchance >= hitchance) W.Cast(pred.CastPosition);
            }

            if (myhero.HasBuff("AnnieRController") && target.Distance(myhero.Position) < 1300f)
            {
                if (myhero.GetBuff("AnnieRController").EndTime - Game.Time < 0.5f) return;

                R.CastOnUnit(target);
            }

            if (combo.check("cign") && ignite != null && ignite.CanCast(target) && target.killable())
            {
                if ((target.Health < myhero.GetAutoAttackDamage(target) && target.InAARangeOf(myhero)) || target.killable(false)) return;
                ignite.Cast(target);
            }
        }

        private static void Clear()
        {
            var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q.Range) && x.IsMinion());

            var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(Q.Range));

            var lMobs = GameObjects.Jungle.Where(x => x.IsValidTarget(Q.Range) && x.GetJungleType() == JungleType.Legendary);

            var bMobs = GameObjects.Jungle.Where(x => x.IsValidTarget(Q.Range) && x.GetJungleType() == JungleType.Large);

            System.Collections.Generic.IEnumerable<AIMinionClient> targets;

            if (minions.Any()) targets = minions;
            else if (bMobs.Any()) targets = bMobs;
            else if (lMobs.Any()) targets = lMobs;
            else targets = mobs;

            if (targets == null) return;

            if (comb(laneclear, "lq") > 0 && Q.IsReady())
            {
                if (comb(laneclear, "lq") == 1)
                {
                    var target = targets.Where(x => x.Health > myhero.GetAutoAttackDamage(x) + 10).OrderByDescending(x => HealthPrediction.GetPrediction(x, x.qtt())).FirstOrDefault();
                    Q.Cast(target);
                }
                else
                {
                    var target = targets.Where(x => x != null && HealthPrediction.GetPrediction(x, x.qtt()) > 30 && HealthPrediction.GetPrediction(x, x.qtt()) < Q.GetDamage(x)).OrderByDescending(x => HealthPrediction.GetPrediction(x, x.qtt())).FirstOrDefault();

                    if (target == null || ((Orbwalker.GetTarget() == target as AttackableUnit || Orbwalker.LastTarget == target as AttackableUnit) &&
                        (HealthPrediction.GetPrediction(target, target.qtt()) <= myhero.GetAutoAttackDamage(target) || target.UnderAllyTurret()))) goto lol2;

                    Q.Cast(target);
                }
            }

            lol2:
            var min = slider(laneclear, "lwmin");
            if (laneclear.check("lw") && W.IsReady() && targets.Where(x => x.Health > 20 && W.IsInRange(x)).Count() >= min)
            {
                W.Collision = true;
                foreach (var minion in targets.Where(x => x != null && HealthPrediction.GetPrediction(x, x.qtt()) > 0))
                {
                    var pred = W.GetPrediction(minion as AIBaseClient);

                    if (pred.CollisionObjects.Where(x => x.IsMinion() && !x.IsAlly).Count() >= min)
                    {
                        W.Cast(pred.CastPosition);
                    }
                }
                W.Collision = false;

            }

            if (laneclear.check("lr") && myhero.HasBuff("AnnieRController"))
            {
                if (myhero.GetBuff("AnnieRController").EndTime - Game.Time < 0.5f) return; //prevents throwing null exceptions before the end of R controller

                var target = targets.OrderByDescending(x => x.Health).FirstOrDefault(x => x as AttackableUnit != Orbwalker.GetTarget());
                if (target != null) R.CastOnUnit(target);
            }
        }

        static void SpamQW()
        {
            var target = TargetSelector.GetTarget(680f, DamageType.Magical);

            if (harass.check("hq") && Q.CanCast(target) && target != null && target.IsValidTarget())
            {
                Q.Cast(target);
            }

            if (harass.check("hw") && W.CanCast(target) && target != null && target.IsValidTarget())
            {
                W.CastIfHitchanceMinimum(target, HitChance.Medium);
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
                    if (myhero.ManaPercent >= slider(laneclear, "manamin")) Clear();
                    break;
                case OrbwalkerMode.Harass:
                    SpamQW();
                    break;
            }

            Misc();
        }

        private static void Misc()
        {
            if (menu.check("usee") && E.IsReady()) // Spaghetti shield
            {
                var closee = GameObjects.EnemyHeroes.Where(x => myhero.InAARangeOf(x) && (x.IsFacing(myhero) || x.GetWaypoints().LastOrDefault().DistanceToPlayer() < 100f));

                if (gotAggro && !closee.Any())
                {
                    gotAggro = false;
                }
                else if (gotAggro && closee.Any())
                {
                    E.Cast();
                }
            }

            if (!myhero.HasBuffOfType(BuffType.Heal) && myhero.HealthPercent <= slider(menu, "autopm")) // auto pot
            {
                if (Items.CanUseItem(myhero, Potion.Id)) Potion.Cast();

                else if (Items.CanUseItem(myhero, Biscuit.Id)) Biscuit.Cast();

                else if (Items.CanUseItem(myhero, RPotion.Id)) RPotion.Cast();

                else if (Items.CanUseItem(myhero, CPotion.Id)) CPotion.Cast();
            }

            if (menu.check("wks") && W.IsReady() && myhero.CountEnemyHeroesInRange(W.Range) > 0)
            {
                foreach (var target in GameObjects.EnemyHeroes.Where(x => x.IsValidTarget() && W.CanCast(x) && W.GetDamage(x) > x.Health))
                {
                    W.CastIfHitchanceMinimum(target, HitChance.Medium);
                }
            }

            if (skinchange)
            {
                myhero.SetSkin(comb(menu, "skin"));
                skinchange = false;
            }

            menu["autopm"].GetValue<MenuSlider>().Visible = menu["autop"].GetValue<MenuBool>().Enabled;
        }

        private static void OnDraw(EventArgs args)
        {
            if (myhero.IsDead) return;

            if (ObjectManager.Player.IsDead)
            {
                return;
            }

            if (draw.check("dq") && (draw.check("drdy") ? Q.IsReady() : true))
            {
                Render.Circle.DrawCircle(myhero.Position, Q.Range, Color.Red, 5, true);
            }

            if (draw.check("dw") && (draw.check("drdy") ? W.IsReady() : true))
            {
                Render.Circle.DrawCircle(myhero.Position, W.Range, Color.Red, 5, true);
            }

            if (draw.check("dr") && (draw.check("drdy") ? R.IsReady() : true))
            {
                Render.Circle.DrawCircle(myhero.Position, R.Range, Color.Red, 5, true);
            }

            if (draw.check("dign") && ignite != null && (draw.check("drdy") ? ignite.IsReady() : true))
            {
                Render.Circle.DrawCircle(myhero.Position, ignite.Range, Color.PaleVioletRed, 2);
            }

            foreach (var targe in GameObjects.EnemyHeroes.Where(x => !x.IsDead && x.IsVisibleOnScreen))
            {
                Drawing.DrawText(Drawing.WorldToScreen(targe.Position).X - 20, Drawing.WorldToScreen(targe.Position).Y + 30, targe.killable() ? Color.White : Color.Transparent, "Killable");
            }
        }

        static void DatMenu()
        {
            menu = new Menu("t7annie", "T7 Annie", true);
            menu.Add(new MenuSeparator("sep", "By Toyota7 v1.1"));
            menu.Add(new MenuBool("usee", "Use E On Aggro"));
            menu.Add(new MenuBool("wks", "Use W To KS"));
            menu.Add(new MenuBool("autol", "Auto-LevelUp Skills"));
            menu.Add(new MenuBool("autop", "Auto-HealPotion"));
            menu.Add(new MenuSlider("autopm", "Min Health % To Pot", 75, 5, 95));
            

            combo = new Menu("combo", "Combo");
            combo.Add(new MenuBool("cq", "Use Q"));
            combo.Add(new MenuBool("cw", "Use W"));
            combo.Add(new MenuBool("ce", "Use E"));
            combo.Add(new MenuBool("cr", "Use R"));
            combo.Add(new MenuBool("crk", "Only R If Killable"));
            combo.Add(new MenuBool("cign", "Use Ignite"));
            combo.Add(new MenuList("chit", "Min Prediction Hitchance", new string[] { "Low", "Medium", "High", "Very High" }, 1));
            combo.Add(new MenuBool("cnaa", "Disable AA In Combo", false));
            menu.Add(combo);

            laneclear = new Menu("lanec", "Laneclear");
            laneclear.Add(new MenuList("lq", "Q Usage", new string[] { "Off", "Spam Q", "Lasthit Q" }, 2));
            laneclear.Add(new MenuBool("lw", "Use W", false));
            laneclear.Add(new MenuSlider("lwmin", "Min Minions For W", 2, 1, 5));
            laneclear.Add(new MenuBool("lr", "Auto Control Tibbers(If Active)", false));
            laneclear.Add(new MenuSlider("manamin", "Min Mana % To Cast Spells", 15, 1, 95));
            menu.Add(laneclear);

            harass = new Menu("harass", "Harass");
            harass.Add(new MenuBool("hq", "Use Q"));
            harass.Add(new MenuBool("hw", "Use W"));
            menu.Add(harass);

            draw = new Menu("draw", "Drawings");
            draw.Add(new MenuBool("dq", "Draw Q Range"));
            draw.Add(new MenuBool("dw", "Draw W Range", false));
            draw.Add(new MenuBool("dr", "Draw R Range", false));
            draw.Add(new MenuBool("dign", "Draw Ignite Range", false));
            draw.Add(new MenuBool("dkill", "Draw Killable targets(Text)"));
            draw.Add(new MenuBool("drdy", "Draw Only Ready Spells", false));
            menu.Add(draw);
            menu.Add(new MenuList("skin", "Skin Hack", new string[]
            {
                "Default", "Goth", "Red Riding", "Wonderland", "Prom Queen", "Frostfire", "Reverse", "FrankenTibbers", "Panda", "Sweetheart", "Hextech", "Super Galaxy", "AnnieVersary"
            }))
            .ValueChanged += (s, e) => { skinchange = true; };

            menu.Attach();
        }

        static int comb(Menu submenu, string sig)
        {
            return submenu[sig].GetValue<MenuList>().Index;
        }
        static int slider(Menu submenu, string sig)
        {
            return submenu[sig].GetValue<MenuSlider>().Value;
        }
    }

    #region Extenstions
    public static class Extensions
    {
        public static bool check(this Menu menu, string sig, string sig2 = null)
        {
            return sig2 == null ? menu[sig].GetValue<MenuBool>().Enabled : menu[sig][sig2].GetValue<MenuBool>().Enabled;
        }

        public static bool killable(this AIHeroClient target, bool withignite = true, float extradmgsrc = 0)
        {
            var tibberaa = ((HasCorona.R.Level == 1 ? 50 : (HasCorona.R.Level == 2 ? 75 : 100)) + (0.15f * HasCorona.myhero.TotalMagicalDamage)) * 2;
            var dmg = (HasCorona.Q.IsReady() ? HasCorona.Q.GetDamage(target) : 0) +
                      (HasCorona.W.IsReady() ? HasCorona.W.GetDamage(target) : 0) +
                      (HasCorona.R.IsReady() ? HasCorona.R.GetDamage(target) : 0) +
                      (withignite ? ((HasCorona.ignite != null && HasCorona.ignite.IsReady()) ? HasCorona.myhero.GetSummonerSpellDamage(target, SummonerSpell.Ignite) : 0) : 0) +
                      HasCorona.myhero.GetAutoAttackDamage(target) * 3+
                      extradmgsrc +
                      (HasCorona.R.IsReady() ? tibberaa : 0);

            return dmg > target.Health;
        }

        public static int qtt(this AIMinionClient target)//returns Q travel time to target in milliseconds
        {
            return target == null ? 0 : (int)Math.Floor(target.DistanceToPlayer() / 1.392f) + 250 + 10; //plus cast delay,plus 10 for safety
        }

        public static bool UnderAllyTurret(this AIMinionClient target)
        {
            if (target == null) return false;
            var turret = GameObjects.AllyTurrets.OrderBy(x => x.Distance(target.Position)).FirstOrDefault();
            if (turret != null) return target.Distance(turret.Position) < turret.AttackRange;
            else return false;
        }

        public static bool InAARangeOf(this AIHeroClient player, AIHeroClient target)
        {
            if (player.Distance(target.Position) < target.AttackRange) return true;
            return false;
        }
    }
    #endregion
}
