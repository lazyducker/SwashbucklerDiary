﻿namespace SwashbucklerDiary.Models
{
    public class AchievementModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public AchievementType Type { get; set; }
        public string Description { get; set; }
        public int Steps { get; set; }
        public UserAchievementModel? UserAchievement { get; set; }

        public AchievementModel(int id, AchievementType type, int steps)
        {
            Id = id;
            Name = $"Achievement.{Enum.GetName(typeof(AchievementType), type)}.{steps}.Name";
            Type = type;
            Description = $"Achievement.{Enum.GetName(typeof(AchievementType), type)}.{steps}.Description";
            Steps = steps;
        }
    }
}
