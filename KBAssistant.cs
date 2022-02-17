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
using Hearthstone.DataModels;

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

                Traverse QuestManagerTraverse = Traverse.Create(QuestManager.Get());
                List<PlayerQuestState> dailyQuests = QuestManagerTraverse.Method("GetActiveQuestStatesForPool", new Type[] { typeof(QuestPool.QuestPoolType) }).GetValue<List<PlayerQuestState>>(QuestPool.QuestPoolType.DAILY);
                List<PlayerQuestState> weeklyQuests = QuestManagerTraverse.Method("GetActiveQuestStatesForPool", new Type[] { typeof(QuestPool.QuestPoolType) }).GetValue<List<PlayerQuestState>>(QuestPool.QuestPoolType.WEEKLY);
                List<PlayerQuestState> noneQuests = QuestManagerTraverse.Method("GetActiveQuestStatesForPool", new Type[] { typeof(QuestPool.QuestPoolType) }).GetValue<List<PlayerQuestState>>(QuestPool.QuestPoolType.NONE);

                int i = 0;
                Debug.Log("每日任务");
                foreach (PlayerQuestState quest in dailyQuests)
                {
                    QuestDataModel model = Traverse.Create(QuestManager.Get()).Method("CreateQuestDataModel", new Type[] { typeof(PlayerQuestState) }).GetValue<QuestDataModel>(quest);
                    Debug.LogFormat("任务{0}:ID:{1};描述:{2};经验奖励:{3};可更换次数:{4};", ++i, model.QuestId, model.Description, model.RewardTrackXp, model.RerollCount);
                    Quest quest2 = new Quest()
                    {
                        ID = model.QuestId,
                        Description = model.Description,
                        RewardXP = model.RewardTrackXp,
                        Type = "DAILY",
                        RerollCount = model.RerollCount
                    };
                    QuestList.Add(quest2);
                }

                i = 0;
                Debug.Log("每周任务");
                foreach (PlayerQuestState quest in weeklyQuests)
                {
                    QuestDataModel model = Traverse.Create(QuestManager.Get()).Method("CreateQuestDataModel", new Type[] { typeof(PlayerQuestState) }).GetValue<QuestDataModel>(quest);
                    Debug.LogFormat("任务{0}:ID:{1};描述:{2};经验奖励:{3};可更换次数:{4};", ++i, model.QuestId, model.Description, model.RewardTrackXp, model.RerollCount);
                    Quest quest2 = new Quest()
                    {
                        ID = model.QuestId,
                        Description = model.Description,
                        RewardXP = model.RewardTrackXp,
                        Type = "WEEKLY",
                        RerollCount = model.RerollCount
                    };
                    QuestList.Add(quest2);
                }

                i = 0;
                Debug.Log("其他任务");
                foreach (PlayerQuestState quest in noneQuests)
                {
                    QuestDataModel model = Traverse.Create(QuestManager.Get()).Method("CreateQuestDataModel", new Type[] { typeof(PlayerQuestState) }).GetValue<QuestDataModel>(quest);
                    Debug.LogFormat("任务{0}:ID:{1};描述:{2};经验奖励:{3};可更换次数:{4};", ++i, model.QuestId, model.Description, model.RewardTrackXp, model.RerollCount);
                    Quest quest2 = new Quest()
                    {
                        ID = model.QuestId,
                        Description = model.Description,
                        RewardXP = model.RewardTrackXp,
                        Type = "NONE",
                        RerollCount = model.RerollCount
                    };
                    QuestList.Add(quest2);
                }

                for (int j = QuestList.Count - 1; j >= 0; j--)
                {
                    if (QuestList[j].RerollCount <= 0)
                    {
                        QuestList.Remove(QuestList[j]);
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
