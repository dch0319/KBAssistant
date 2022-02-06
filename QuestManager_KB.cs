using Assets;
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
                List<Quest> DailyQuestList = new List<Quest>();
                List<Quest> WeeklyQuestList = new List<Quest>();
                foreach (var quest in QuestList)
                {
                    if (quest.Type == "DAILY") DailyQuestList.Add(quest);
                    else if (quest.Type == "WEEKLY") WeeklyQuestList.Add(quest);
                }

                var questDaily = DailyQuestList.Find(x => x.RewardXP == 1000);
                if (questDaily != null)
                {
                    Debug.Log($"更换奖励为1000的每日任务-ID-{questDaily.ID},描述-{questDaily.Description},奖励-{questDaily.RewardXP}经验");
                    Hearthstone.Progression.QuestManager.Get().RerollQuest(questDaily.ID);
                }

                var questWeekly = WeeklyQuestList.Find(x => x.RewardXP == 1750);
                if (questWeekly != null)
                {
                    Debug.Log($"更换奖励为1750的每周任务-ID-{questWeekly.ID},描述-{questWeekly.Description},奖励-{questWeekly.RewardXP}经验");
                    Hearthstone.Progression.QuestManager.Get().RerollQuest(questWeekly.ID);
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
        public int RerollCount { get; set; }
    }
}
