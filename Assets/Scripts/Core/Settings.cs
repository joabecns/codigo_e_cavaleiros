namespace CodigoECavaleiros
{
    /// <summary>Constantes de balanceamento (equivale ao settings.py do protótipo).</summary>
    public static class Settings
    {
        public const string GameTitle = "CAÇADORES DE BUGS";
        public const string GameSubtitle = "A batalha do conhecimento";
        public const string MenuScene = "Menu";
        public const string BattleScene = "Batalha";

        public const int QuestionsPerRound = 5;
        public const float QuestionTime = 15f;
        public const float BossFinalPhaseTime = 10f;
        public const int TotalStages = 5; // 4 inimigos comuns + chefão

        public const int PlayerHp = 100, PlayerFc = 30, PlayerAtk = 12, PlayerDef = 4;
        public const int MaxLevel = 5;
        public const int SkillCost = 8;
        public const float SkillMult = 1.5f;
        public const int FcPerHit = 4;
        public const float HealBetweenFights = 0.35f;

        public static readonly string[] AllTopics = { "variaveis", "repeticao", "funcoes", "poo", "ia" };

        public static string TopicName(string topic)
        {
            switch (topic)
            {
                case "variaveis": return "Variáveis e tipos";
                case "repeticao": return "Estruturas de repetição";
                case "funcoes": return "Funções e listas";
                case "poo": return "Classes e POO";
                case "ia": return "Fundamentos de IA";
                default: return topic;
            }
        }
    }
}
