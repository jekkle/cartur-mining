using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace CarturMining
{
    /// <summary>
    /// More to the game's own Pickaxes skill: more mining damage, more ore per deposit, and from level
    /// 50 a small chance that a hit shatters the whole deposit. Inspired by Smoothbrain's Mining
    /// (deprecated on Thunderstore in 2026); written from scratch against the game's code, none of theirs.
    ///
    /// Read off assembly_valheim (1.0): the vanilla Pickaxes skill only scales pickaxe damage like any
    /// weapon skill. Deposits come in three kinds - MineRock5 (copper, silver: a rock that breaks into
    /// chunks, each dropping in DamageArea), Destructible + DropOnDestroyed (tin, obsidian, plain rock:
    /// one piece, dropping when destroyed), and MineRock (chunks dropping in RPC_Hit).
    /// Both take the hit on the swinging player's machine (Damage, which sends an RPC) and drop the ore
    /// on the deposit's owner, who is often someone else and cannot see the miner's skills. So each
    /// player publishes their Pickaxes level on their own character's ZDO, and the owner reads it.
    /// </summary>
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.jekkle.valheim.carturmining";
        public const string PluginName = "Cartur's Mining";
        public const string PluginVersion = "1.0.0";
        internal const string Name = PluginName;
        internal const string Version = PluginVersion;
        internal const string ServerSection = "1 - Server";

        internal static BepInEx.Logging.ManualLogSource Log;
        internal static ConfigFile Cfg;

        internal static ConfigEntry<float> DamageBonus;
        internal static ConfigEntry<float> YieldBonus;
        internal static ConfigEntry<int> ShatterLevel;
        internal static ConfigEntry<float> ShatterChance;
        internal static ConfigEntry<bool> EnforceVersion;
        internal static ConfigEntry<KeyboardShortcut> ShatterKey;

        private void Awake()
        {
            Log = Logger;
            Cfg = Config;

            DamageBonus = Config.Bind(ServerSection, "DamageBonus", 1f,
                new ConfigDescription("Extra pickaxe damage to ore and rock at Pickaxes 100, on top of the game's own skill bonus. 1 = double, 0 = vanilla.",
                    new AcceptableValueRange<float>(0f, 4f)));
            YieldBonus = Config.Bind(ServerSection, "YieldBonus", 1f,
                new ConfigDescription("Extra ore from each chunk of a deposit at Pickaxes 100, as a share of the normal drop. 1 = double, 0 = vanilla.",
                    new AcceptableValueRange<float>(0f, 4f)));
            ShatterLevel = Config.Bind(ServerSection, "ShatterLevel", 50,
                new ConfigDescription("Pickaxes level from which a hit can shatter the whole deposit at once.", new AcceptableValueRange<int>(0, 101)));
            ShatterChance = Config.Bind(ServerSection, "ShatterChance", 0.02f,
                new ConfigDescription("Chance per hit that the whole deposit shatters, once you have the level. 0.02 = 1 hit in 50.", new AcceptableValueRange<float>(0f, 1f)));
            EnforceVersion = Config.Bind(ServerSection, "EnforceVersion", true,
                "On a server: refuse clients running a different version of this mod.");
            ShatterKey = Config.Bind("2 - Keys", "ShatterKey", new KeyboardShortcut(KeyCode.T, KeyCode.LeftControl),
                "Turns shattering on or off - leave it off when you want a deposit to last.");

            try
            {
                Harmony.CreateAndPatchAll(typeof(Plugin).Assembly, PluginGuid);
                Log.LogInfo($"{PluginName} {PluginVersion} loaded - damage, yield and shattering on the Pickaxes skill.");
            }
            catch (Exception e)
            {
                Log.LogWarning($"{PluginName} failed to patch, game continues without it: {e}");
            }
        }

        private float _publishAt;

        private void Update()
        {
            Mining.Keys();
            if (Time.time >= _publishAt)
            {
                _publishAt = Time.time + 2f;
                Mining.Publish();
            }
        }

        internal static IEnumerable<ConfigEntryBase> ServerOwnedEntries(HashSet<string> sections) =>
            Cfg.Keys.Where(k => sections.Contains(k.Section)).Select(k => Cfg[k]);

        internal static ConfigEntryBase FindEntry(string section, string key) =>
            Cfg.Keys.Where(k => k.Section == section && k.Key == key).Select(k => Cfg[k]).FirstOrDefault();

        internal static void WithoutSaving(Action change)
        {
            bool was = Cfg.SaveOnConfigSet;
            Cfg.SaveOnConfigSet = false;
            try { change(); }
            finally { Cfg.SaveOnConfigSet = was; }
        }
    }

    internal static class Mining
    {
        // A player's Pickaxes level on their own character's ZDO, so whoever owns a deposit can read
        // the miner's level - skills are not otherwise sent to other clients.
        internal static readonly int LevelKey = "cartur_mining_level".GetStableHashCode();

        internal static bool ShatterOn = true;

        internal static void Keys()
        {
            Player p = Player.m_localPlayer;
            if (p == null || Chat.instance != null && Chat.instance.HasFocus() || !Plugin.ShatterKey.Value.IsDown())
                return;
            ShatterOn = !ShatterOn;
            p.Message(MessageHud.MessageType.TopLeft, "Shattering deposits " + (ShatterOn ? "on" : "off"));
        }

        internal static void Publish()
        {
            Player p = Player.m_localPlayer;
            ZNetView nview = p != null ? p.GetComponent<ZNetView>() : null;
            if (nview == null || !nview.IsValid() || !nview.IsOwner())
                return;
            int level = Mathf.RoundToInt(p.GetSkillLevel(Skills.SkillType.Pickaxes));
            if (nview.GetZDO().GetInt(LevelKey, -1) != level)
                nview.GetZDO().Set(LevelKey, level);
        }

        /// The miner's Pickaxes level as a 0..1 factor: the local player's own skills, anyone else's
        /// published level.
        internal static float Factor(HitData hit)
        {
            if (hit == null || hit.m_skill != Skills.SkillType.Pickaxes || !(hit.GetAttacker() is Player p) || p == null)
                return 0f;
            if (p == Player.m_localPlayer)
                return p.GetSkillFactor(Skills.SkillType.Pickaxes);
            ZNetView nview = p.GetComponent<ZNetView>();
            return nview != null && nview.IsValid() ? Mathf.Clamp01(nview.GetZDO().GetInt(LevelKey, 0) / 100f) : 0f;
        }

        // ---- damage, on the miner's side before the hit is sent ---------------------------------

        internal static void Stronger(HitData hit)
        {
            if (hit == null || hit.GetAttacker() != Player.m_localPlayer || hit.m_skill != Skills.SkillType.Pickaxes)
                return;
            float bonus = Factor(hit) * Plugin.DamageBonus.Value;
            if (bonus > 0f)
                hit.m_damage.m_pickaxe *= 1f + bonus;
        }

        // ---- yield, on the owner's side when a chunk breaks --------------------------------------

        internal static void ExtraOre(DropTable table, HitData hit, Vector3 at)
        {
            float extra = Factor(hit) * Plugin.YieldBonus.Value;
            if (table == null || extra <= 0f)
                return;
            int rolls = Mathf.FloorToInt(extra) + (UnityEngine.Random.value < extra - Mathf.Floor(extra) ? 1 : 0);
            for (int i = 0; i < rolls; i++)
                foreach (GameObject drop in table.GetDropList())
                    ItemDrop.OnCreateNew(UnityEngine.Object.Instantiate(drop, at + UnityEngine.Random.insideUnitSphere * 0.3f, Quaternion.identity));
        }

        // ---- shattering, on the miner's side: a breaking blow to every chunk ----------------------

        internal static bool ShouldShatter(HitData hit) =>
            ShatterOn && hit != null && hit.GetAttacker() == Player.m_localPlayer && hit.m_skill == Skills.SkillType.Pickaxes &&
            Player.m_localPlayer.GetSkillLevel(Skills.SkillType.Pickaxes) >= Plugin.ShatterLevel.Value &&
            UnityEngine.Random.value < Plugin.ShatterChance.Value;

        /// Every chunk gets a hit through the deposit's own damage RPC, so the owner breaks each one the
        /// normal way - tool tier, drops, stats and ExtraOre included. Chunks already broken answer
        /// "Already destroyed" and are skipped by the game itself.
        internal static void Shatter(ZNetView nview, string rpc, int chunks, HitData hit)
        {
            HitData blow = hit.Clone();
            blow.m_damage.m_pickaxe = 1000000f;
            blow.m_skillRaiseAmount = 0f;
            // On the owner the RPC runs at once, and the last chunk destroys the deposit (and its
            // ZNetView) mid-loop - so stop as soon as it is gone.
            for (int i = 0; i < chunks && nview != null && nview.IsValid(); i++)
                nview.InvokeRPC(rpc, blow, i);
            Player.m_localPlayer.Message(MessageHud.MessageType.Center, "The deposit shatters!");
        }
    }

    [HarmonyPatch(typeof(MineRock5), nameof(MineRock5.Damage))]
    internal static class MineRock5Hit
    {
        private static readonly FieldInfo Areas = AccessTools.Field(typeof(MineRock5), "m_hitAreas");

        private static void Prefix(HitData hit) => Mining.Stronger(hit);

        private static void Postfix(MineRock5 __instance, HitData hit, ZNetView ___m_nview)
        {
            if (___m_nview == null || !___m_nview.IsValid() || !Mining.ShouldShatter(hit))
                return;
            if (Areas?.GetValue(__instance) is ICollection areas)
                Mining.Shatter(___m_nview, "RPC_Damage", areas.Count, hit);
        }
    }

    [HarmonyPatch(typeof(MineRock5), "DamageArea")]
    internal static class MineRock5Ore
    {
        private static readonly MethodInfo GetArea = AccessTools.Method(typeof(MineRock5), "GetHitArea");

        private static void Postfix(MineRock5 __instance, int hitAreaIndex, HitData hit, bool __result, DropTable ___m_dropItems)
        {
            if (!__result)
                return;
            Vector3 at = hit.m_point;
            object area = GetArea?.Invoke(__instance, new object[] { hitAreaIndex });
            if (area != null && AccessTools.Field(area.GetType(), "m_collider")?.GetValue(area) is Collider c && c != null)
                at = c.bounds.center;
            Mining.ExtraOre(___m_dropItems, hit, at);
        }
    }

    // Tin, obsidian and plain rock are Destructible, not MineRock (read off the spawned
    // MineRock_Tin: Destructible + DropOnDestroyed). Destroy(HitData) runs on the owner with the hit
    // that broke it, then DropOnDestroyed rolls its table; a second roll by the miner's level follows.
    [HarmonyPatch(typeof(Destructible), nameof(Destructible.Damage))]
    internal static class DestructibleHit
    {
        private static void Prefix(HitData hit) => Mining.Stronger(hit);
    }

    [HarmonyPatch(typeof(Destructible), nameof(Destructible.Destroy))]
    internal static class DestructibleOre
    {
        private static void Postfix(Destructible __instance, HitData hit)
        {
            DropOnDestroyed drops = hit != null ? __instance.GetComponent<DropOnDestroyed>() : null;
            if (drops != null)
                Mining.ExtraOre(drops.m_dropWhenDestroyed, hit, __instance.transform.position + Vector3.up * 0.5f);
        }
    }

    [HarmonyPatch(typeof(MineRock), nameof(MineRock.Damage))]
    internal static class MineRockHit
    {
        private static readonly FieldInfo Areas = AccessTools.Field(typeof(MineRock), "m_hitAreas");

        private static void Prefix(HitData hit) => Mining.Stronger(hit);

        private static void Postfix(MineRock __instance, HitData hit, ZNetView ___m_nview)
        {
            if (___m_nview == null || !___m_nview.IsValid() || !Mining.ShouldShatter(hit))
                return;
            int count = Areas?.GetValue(__instance) is ICollection areas ? areas.Count : 0;
            if (count > 0)
                Mining.Shatter(___m_nview, "Hit", count, hit);
        }
    }

    // MineRock.RPC_Hit drops the ore when a chunk's "Health<n>" on the ZDO reaches 0 and returns
    // nothing, so the chunk's health is read before and after to see whether this hit broke it.
    [HarmonyPatch(typeof(MineRock), "RPC_Hit")]
    internal static class MineRockOre
    {
        private static void Prefix(int hitAreaIndex, ZNetView ___m_nview, out float __state)
        {
            __state = ___m_nview != null && ___m_nview.IsValid() && ___m_nview.IsOwner()
                ? ___m_nview.GetZDO().GetFloat("Health" + hitAreaIndex, 1f) : -1f;
        }

        private static void Postfix(int hitAreaIndex, HitData hit, ZNetView ___m_nview, DropTable ___m_dropItems, float __state)
        {
            if (__state <= 0f || ___m_nview == null || !___m_nview.IsValid())
                return;
            if (___m_nview.GetZDO().GetFloat("Health" + hitAreaIndex, 1f) <= 0f)
                Mining.ExtraOre(___m_dropItems, hit, hit.m_point - hit.m_dir * 0.2f);
        }
    }
}
