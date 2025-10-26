using BepInEx;
using GlobalEnums;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;

namespace ClassLibrary1
{
    public class ModConfig
    {
        public static bool EnableFastAttack = true;
        public static bool EnableChargeBoost = true;
        public static bool EnableMantisScale = true;
        public static bool EnableNailCharge = true;
    }

    public class Gui : MonoBehaviour
    {
        private bool _isVisible = false;
        private Rect _windowRect = new Rect(20, 20, 250, 200);

        void Start()
        {
            DontDestroyOnLoad(gameObject);
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1))
                _isVisible = !_isVisible;
        }

        void OnGUI()
        {
            if (!_isVisible) return;

            _windowRect = GUI.Window(0, _windowRect, (id) =>
            {
                ModConfig.EnableFastAttack = GUI.Toggle(
                    new Rect(15, 30, 200, 20),
                    ModConfig.EnableFastAttack,
                    "0.Attack"
                );

                ModConfig.EnableNailCharge = GUI.Toggle(
                    new Rect(15, 60, 200, 20),
                    ModConfig.EnableNailCharge,
                    "0.NailCharge"
                );

                ModConfig.EnableMantisScale = GUI.Toggle(
                    new Rect(15, 90, 200, 20),
                    ModConfig.EnableMantisScale,
                    "0.Mantis"
                );

                ModConfig.EnableChargeBoost = GUI.Toggle(
                    new Rect(15, 120, 200, 20),
                    ModConfig.EnableChargeBoost,
                    "0.Charge"
                );

                GUI.DragWindow(new Rect(0, 0, 10000, 20));
            }, "功能设置");
        }
    }

    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class AttackMod : BaseUnityPlugin
    {
        private const string pluginGuid = "your.unique.mod.id";
        private const string pluginName = "Attack Mod";
        private const string pluginVersion = "1.0.1";

        private readonly Harmony _harmony = new Harmony(pluginGuid);
        private GameObject _guiHolder;

        private void Awake()
        {
            _harmony.PatchAll();

            _guiHolder = new GameObject("GUI_Holder");
            _guiHolder.AddComponent<Gui>();
            DontDestroyOnLoad(_guiHolder);
        }

        private void OnDestroy()
        {
            _harmony.UnpatchAll(pluginGuid);
        }
    }

    [HarmonyPatch(typeof(HeroController))]
    class HeroControllerPatches
    {
        [HarmonyPatch("DoAttack")]
        [HarmonyPostfix]
        static void DoAttackPostfix(HeroController __instance)
        {
            if (!ModConfig.EnableFastAttack) return;

            var traverse = Traverse.Create(__instance);
            traverse.Field("ATTACK_COOLDOWN_TIME").SetValue(0.0001f);
            traverse.Field("ATTACK_COOLDOWN_TIME_CH").SetValue(0.0001f);
        }

        [HarmonyPatch("CanAttack")]
        [HarmonyPostfix]
        static void CanAttackPostfix(HeroController __instance, ref bool __result)
        {
            if (!ModConfig.EnableChargeBoost) return;

            var traverse = Traverse.Create(__instance);
            float attackCooldown = traverse.Field("attack_cooldown").GetValue<float>();

            __result = attackCooldown <= 0f
                && !__instance.cState.dead
                && !__instance.cState.hazardDeath
                && !__instance.cState.hazardRespawning
                && !__instance.controlReqlinquished
                && __instance.hero_state != ActorStates.no_input
                && __instance.hero_state != ActorStates.hard_landing
                && __instance.hero_state != ActorStates.dash_landing;
        }

        [HarmonyPatch("CanNailCharge")]
        [HarmonyPostfix]
        static void CanNailChargePatch(HeroController __instance, ref bool __result)
        {
            if (!ModConfig.EnableNailCharge) return;

            var traverse = Traverse.Create(__instance);
            __result = !__instance.controlReqlinquished
                && !__instance.cState.recoiling
                && !__instance.cState.recoilingLeft
                && !__instance.cState.recoilingRight;
        }
    }


[HarmonyPatch(typeof(HeroController), "Attack")]
    class GrubberFlyBeamScalePatch
    {
        static void Postfix(HeroController __instance, AttackDirection attackDir)
        {
            if (!ModConfig.EnableMantisScale) return;

            var traverse = Traverse.Create(__instance);

            traverse.Field("MANTIS_CHARM_SCALE").SetValue(20f);


            var grubberFlyBeam = traverse.Field("grubberFlyBeam").GetValue<GameObject>();
            var playerData = traverse.Field("playerData").GetValue<PlayerData>();
            var pd = playerData;

            bool shouldForceCreate = pd.health > 0 && !pd.equippedCharm_27 ||
                                    traverse.Field("joniBeam").GetValue<bool>() && pd.equippedCharm_27;

            if (shouldForceCreate && grubberFlyBeam == null)
            {
                var prefab = traverse.Field("grubberFlyBeamPrefabR").GetValue<GameObject>();
                grubberFlyBeam = Object.Instantiate(prefab, __instance.transform.position, Quaternion.identity);
                traverse.Field("grubberFlyBeam").SetValue(grubberFlyBeam);
            }

            if (grubberFlyBeam != null)
            {
                var transform = grubberFlyBeam.transform;
                float targetScale = pd.equippedCharm_13 ? 8f : 8f;

                transform.SetScaleY(targetScale);

                if (attackDir == AttackDirection.upward || attackDir == AttackDirection.downward)
                {
                    transform.localScale = new Vector3(
                        targetScale * transform.localScale.x,
                        targetScale * transform.localScale.y,
                        transform.localScale.z
                    );
                }
            }
        }

        [HarmonyPatch(typeof(HeroController))]
        class MantisScalePatch
        {
            [HarmonyPostfix]
            [HarmonyPatch("Awake")]
            static void ModifyMantisScale(HeroController __instance)
            {
                if (!ModConfig.EnableMantisScale) return;

                Traverse.Create(__instance)
                    .Field("MANTIS_CHARM_SCALE")
                    .SetValue(10f);
            }
        }

        [HarmonyPatch(typeof(HeroController))]
        class jianji
        {
            [HarmonyPatch("Update")]
            [HarmonyPrefix]
            static void Update(HeroController __instance)
            {
                if (!ModConfig.EnableNailCharge) return;

                var traverse = Traverse.Create(__instance);
                traverse.Field("nailChargeTime").SetValue(0.0001f);
            }
        }
    }
}
