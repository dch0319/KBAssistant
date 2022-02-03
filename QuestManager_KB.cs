using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KBAssistant
{
    public class QuestManager_KB
    {
        public static void RerollQuest(List<Quest> QuestList)
        {
            if (QuestList.Any())
            {
                List<Quest> QuestsToReroll = new List<Quest>();//酒馆/乱斗/战旗任务
                List<Quest> QuestList_Daily = new List<Quest>();
                List<Quest> QuestList_Weekly = new List<Quest>();
                foreach (var quest in QuestList)
                {
                    if (quest.Type == "DAILY") QuestList_Daily.Add(quest);
                    else if (quest.Type == "WEEKLY") QuestList_Weekly.Add(quest);

                    if (quest.Type != "NONE" && (quest.Description.Contains("酒馆战旗") || quest.Description.Contains("乱斗模式") || quest.Description.Contains("对决模式")))
                    {
                        QuestsToReroll.Add(quest);
                    }
                }
                if (QuestsToReroll.Any())//更换酒馆/乱斗/战旗任务
                {
                    var quest = QuestsToReroll.Find(x => x.RewardXP == QuestsToReroll.Min(y => y.RewardXP));
                    if (quest != null)
                    {
                        Debug.Log($"[实用功能]更换酒馆/乱斗/战旗任务-ID-{quest.ID},描述-{quest.Description},奖励-{quest.RewardXP}经验");
                        Hearthstone.Progression.QuestManager.Get().RerollQuest(quest.ID);
                    }
                }
                else
                {
                    var questDaily = QuestList_Daily.Find(x => x.Type == "DAILY" && x.RewardXP == QuestList_Daily.Min(y => y.RewardXP) && x.RewardXP <= 1000);
                    if (questDaily != null)
                    {
                        Debug.Log($"[实用功能]未找到可更换的酒馆/乱斗/战旗任务,更换奖励最低的每日任务-ID-{questDaily.ID},描述-{questDaily.Description},奖励-{questDaily.RewardXP}经验");
                        Hearthstone.Progression.QuestManager.Get().RerollQuest(questDaily.ID);
                    }
                    var questWeekly = QuestList_Weekly.Find(x => x.Type == "WEEKLY" && x.RewardXP == QuestList_Weekly.Min(y => y.RewardXP) && x.RewardXP < 2500);
                    if (questWeekly != null)
                    {
                        Debug.Log($"[实用功能]未找到可更换的酒馆/乱斗/战旗任务,更换奖励最低的每周任务-ID-{questWeekly.ID},描述-{questWeekly.Description},奖励-{questWeekly.RewardXP}经验");
                        Hearthstone.Progression.QuestManager.Get().RerollQuest(questWeekly.ID);
                    }
                }
            }
        }
    }

    public class Quest
    {
        public int ID { get; set; }
        public string Description { get; set; }
        public int RewardXP { get; set; }
        public string Type { get; set; }
    }
}
