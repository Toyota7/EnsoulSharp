using System;
using System.Linq;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.Events;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.MenuUI.Values;
using EnsoulSharp.SDK.Prediction;
using EnsoulSharp.SDK.Utility;
using SharpDX;
using Color = System.Drawing.Color;

namespace T7Samira
{
    public class Base
    {
        #region Declerations
        public static AIHeroClient myhero { get { return ObjectManager.Player; } }
        public static AIHeroClient EnemyADC { get; set; }
        public static Menu menu, combo, laneclear, draw, harass, misc, prot;

        public static int LastAttackType = 0;
        public static string[] EnemyPlayerNames;
        public static string CharName = "Samira", version = "1.0", PassiveName = "SamiraPassiveCombo", EnemyPassiveProced, RBuffName = "SamiraR", WBuffName = "SamiraW";
        public static readonly string[] ADCNames = new string[] { "Ashe","Caitlyn","Corki","Draven","Ezreal","Graves","Jhin","Jinx","Kalista","Kog'Maw","Lucian",
                                                                  "Miss Fortune","Quinn","Sivir","Tristana","Twitch","Urgot","Varus","Vayne","Kai'Sa","Senna","Aphelios","Kindred","Xayah" };
        public static readonly string[] KnownMissiles = new string[] { "EnchantedCrystalArrow", "VeigarR" , "RocketGrabMissile", "CaitlynAceintheHoleMissile", "TristanaR" , "BrandR", "BrandRMissile",
                                                                       "LucianRMissile", "LucianRMissileOffhand", "EzrealR", "DravenR"};
        public static Items.Item Potion { get; set; }
        public static Items.Item Biscuit { get; set; }
        public static Items.Item RPotion { get; set; }
        public static Items.Item CPotion { get; set; }

        public static Spell Q1 { get; set; }
        public static Spell Q2 { get; set; }
        public static Spell W { get; set; }
        public static Spell E { get; set; }
        public static Spell R { get; set; }
        public static Spell ignite { get; set; }

        public static HitChance hitchance { get; set; }

        public static SpellPrediction.PredictionInput ConeAOEInput;

        public static MissileClient IncomingMissile;

        public static bool WaitforAA = false;

        public static float[] wdmgscale = { 20, 35, 50, 65, 80 }, edmgscale = { 50, 60, 70, 80, 90 };
        
        #endregion

        #region Methods
        public static AIHeroClient GetEnemyADC()
        {
            foreach (var name in GameObjects.EnemyHeroes.Select(x => x.CharacterName))
            {
                if (ADCNames.Contains(name)) return GameObjects.EnemyHeroes.FirstOrDefault(x => x.CharacterName == name);
            }

            return null;
        }

        public static AIHeroClient GetTarget()
        {
            var selection = comb(misc, "FOCUS");

            switch (selection)
            {
                case 0:
                    if (EnemyADC != null && EnemyADC.IsValidTarget((int)Q1.Range))
                    {
                        return EnemyADC;
                    }
                    else return TargetSelector.GetTarget(Q1.Range, DamageType.Magical);
                case 1:
                    return TargetSelector.GetTarget(Q1.Range, DamageType.Magical);
                case 2:
                    var target = GameObjects.EnemyHeroes.FirstOrDefault(x => x.CharacterName == EnemyPlayerNames[comb(misc, "CFOCUS")]);

                    if (target != null && target.IsValidTarget((int)Q1.Range))
                    {
                        return target;
                    }
                    else return TargetSelector.GetTarget(Q1.Range, DamageType.Magical);
                default: return TargetSelector.GetTarget(Q1.Range, DamageType.Magical);
            }
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
    }
    #endregion
}

#region Extenstions
public static class Extensions
{
    public static bool check(this Menu menu, string sig, string sig2 = null)
    {
        return sig2 == null ? menu[sig].GetValue<MenuBool>().Enabled : menu[sig][sig2].GetValue<MenuBool>().Enabled;
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

    public static bool CheckCast(this Spell spell, AIBaseClient target)
    {
        return spell.IsReady() && target.Distance(ObjectManager.Player.Position) < spell.Range;
    }

    public static int CountEnemies(this AIHeroClient hero, float range)
    {
        return GameObjects.Heroes.Where(x => x.Team != hero.Team && x.Distance(hero.Position) < range).Count();
    }

    public static int CountAllies(this AIBaseClient hero, float range)
    {
        return GameObjects.Heroes.Where(x => x.Team == hero.Team && x.Distance(hero.Position) < range).Count();
    }
    public static int qtt(this AIBaseClient target, float speed, int delay)//returns Q travel time to target in milliseconds
    {
        return target == null ? 99999 : (int)Math.Floor(target.DistanceToPlayer() / speed) + delay + 10; //plus cast delay,plus 10 for safety
    }

    public static int GetPassiveStacks(this AIHeroClient player)
    {
        return ObjectManager.Player.GetBuffCount(T7Samira.Base.PassiveName);
    }
    public static bool HasHardCC(this AIBaseClient hero)
    {
        return hero.HasBuffOfType(BuffType.Sleep)||  hero.HasBuffOfType(BuffType.Snare) || hero.HasBuffOfType(BuffType.Stun) ||
               hero.HasBuffOfType(BuffType.Knockup) || hero.HasBuffOfType(BuffType.Knockback);
    }

    public static float GetTotalSpelldamage(this AIHeroClient target)
    {
        var player = T7Samira.Base.myhero;

        var qdamage = ((T7Samira.Base.Q1.Level - 1) * 5) + player.TotalAttackDamage;
        var wdamage = T7Samira.Base.W.Level == 0 ? 0 : T7Samira.Base.wdmgscale[T7Samira.Base.W.Level - 1] + (0.8f * player.TotalAttackDamage);
        var edamage = T7Samira.Base.E.Level == 0 ? 0 : T7Samira.Base.edmgscale[T7Samira.Base.E.Level - 1] + (0.2f * player.TotalAttackDamage); //magic???
        var rdamage = (T7Samira.Base.R.Level * 10) + (0.6f * player.TotalAttackDamage);

        var damage = (T7Samira.Base.Q1.IsReady() ?  Damage.CalculatePhysicalDamage(player, target, (double)qdamage) : 0f) +
                     (T7Samira.Base.W.IsReady() ? Damage.CalculatePhysicalDamage(player, target, (double)wdamage) : 0f) +
                     (T7Samira.Base.E.IsReady() ? Damage.CalculateMagicDamage(player, target, (double)edamage) : 0f) +
                     (T7Samira.Base.R.IsReady() ? (Damage.CalculatePhysicalDamage(player, target, (double)rdamage) * 10) : 0f);

        damage += T7Samira.Program.myhero.GetAutoAttackDamage(target) * 3;

        return (float)damage;
    }
}
    #endregion

