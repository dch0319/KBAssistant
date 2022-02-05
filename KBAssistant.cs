using Assets;
using BepInEx;
using HarmonyLib;
using Hearthstone.Progression;
using PegasusUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;
using BepInEx.Configuration;
using System.ComponentModel;

namespace KBAssistant
{
    [BepInPlugin("KBAssistant", "KB挂机助手", "1.1.0.0")]
    public class KBAssistant : BaseUnityPlugin
    {
        public static float timeScale = 1;
        public static ConfigEntry<float> userTimeScale;
        public static ConfigEntry<EntityPremiumType> entityPremiumTypeValueList;
        public static EntityPremiumType userEntityPremiumType = EntityPremiumType.钻石卡;
        public enum EntityPremiumType
        {
            金卡,
            钻石卡
        }

        void Start()
        {
            Harmony.CreateAndPatchAll(typeof(KBAssistant), null);
            userTimeScale = Config.Bind("动画速度", "值:", timeScale, new ConfigDescription("范围0.01-4", new AcceptableValueRange<float>((float)0.01, 4)));
            entityPremiumTypeValueList = Config.Bind("己方实体卡牌", "类型:", userEntityPremiumType, new ConfigDescription("己方实体卡牌类型", null, new EntityPremiumType()));
        }

        void Update()
        {
            if (timeScale != userTimeScale.Value)
            {
                TimeScaleMgr.Get().SetGameTimeScale(userTimeScale.Value);
                Debug.Log("当前动画速度：" + userTimeScale.Value);
                timeScale = userTimeScale.Value;
            }
            if (userEntityPremiumType != entityPremiumTypeValueList.Value)
            {
                Debug.Log("己方实体卡牌类型：" + entityPremiumTypeValueList.Value);
                userEntityPremiumType = entityPremiumTypeValueList.Value;
            }
        }


        /// <summary>
        /// 解除窗口大小限制
        /// </summary>
        /// <param name="instructions"></param>
        /// <returns></returns>
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(GraphicsResolution), "IsAspectRatioWithinLimit")]
        public static IEnumerable<CodeInstruction> RatioLimitPath(IEnumerable<CodeInstruction> instructions)
        {
            instructions.ToList<CodeInstruction>()[1].opcode = OpCodes.Brtrue;
            return instructions;
        }

        /// <summary>
        /// 跳过中国地区评级检测
        /// </summary>
        /// <returns></returns>
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(SplashScreen), "GetRatingsScreenRegion")]
        public static IEnumerable<CodeInstruction> SkipRatingsScreenRegion(IEnumerable<CodeInstruction> instructions)
        {
            yield return new CodeInstruction(OpCodes.Ldc_I4_0);
            yield return new CodeInstruction(OpCodes.Ret);
        }

        /// <summary>
        /// 己方实体卡牌类型
        /// </summary>
        /// <param name="__result"></param>
        /// <param name="__instance"></param>
        /// <returns></returns>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Entity), "GetPremiumType")]
        public static bool GetPremiumTypeMod(ref TAG_PREMIUM __result, Entity __instance)
        {
            __result = TAG_PREMIUM.NORMAL;
            if (!GameMgr.Get().IsBattlegrounds())
            {
                if (__instance.GetControllerSide() == Player.Side.FRIENDLY)
                {
                    if (userEntityPremiumType == EntityPremiumType.金卡)
                    {
                        __result = TAG_PREMIUM.GOLDEN;
                    }
                    else if (userEntityPremiumType == EntityPremiumType.钻石卡)
                    {
                        if (__instance.HasTag(GAME_TAG.HAS_DIAMOND_QUALITY))
                        {
                            __result = TAG_PREMIUM.DIAMOND;
                        }
                        else
                        {
                            __result = TAG_PREMIUM.GOLDEN;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 弹出任务窗口时更换任务
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(QuestManager), "ShowQuestNotification")]
        public static void ShowQuestNotification_Postfix()
        {
            //Debug.Log("ShowQuestNotification被调用了！");
            HandleQuests();
        }

        public static void HandleQuests()
        {
            List<Quest> QuestList = new List<Quest>();
            string text = string.Empty;
            try
            {
                if (AchieveManager.Get() == null)
                {
                    return;
                }
                Map<int, PlayerQuestState> privateField = (Map<int, PlayerQuestState>)Traverse.Create(Hearthstone.Progression.QuestManager.Get()).Field("m_questState").GetValue();
                for (int i = 0; i < privateField.Count; i++)
                {
                    PlayerQuestState playerQuestState = privateField.Values.ToList()[i];
                    if (playerQuestState.Status == 1 || playerQuestState.Status == 2)
                    {
                        QuestDbfRecord record = GameDbf.Quest.GetRecord(playerQuestState.QuestId);
                        QuestPool.QuestPoolType questPoolType = GetQuestPoolType(record);
                        string text2 = (string)record.Name + "~" + playerQuestState.Progress + "~0~0~" + record.ID + "~" + (string)record.Description + "~" + record.RewardTrackXp + "~" + questPoolType;
                        Quest quest = new Quest()
                        {
                            ID = record.ID,
                            Description = (string)record.Description,
                            RewardXP = record.RewardTrackXp,
                            Type = questPoolType.ToString()
                        };
                        QuestList.Add(quest);
                        text += text2;
                        Debug.Log(text2);
                    }
                }
                if (QuestList.Count != 0)
                {
                    QuestManager_KB.RerollQuest(QuestList);
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }
        public static QuestPool.QuestPoolType GetQuestPoolType(QuestDbfRecord questAsset)
        {
            if (questAsset != null && questAsset.QuestPoolRecord != null)
            {
                _ = questAsset.QuestPoolRecord.QuestPoolType;
                return questAsset.QuestPoolRecord.QuestPoolType;
            }
            return QuestPool.QuestPoolType.NONE;
        }

    }
}
