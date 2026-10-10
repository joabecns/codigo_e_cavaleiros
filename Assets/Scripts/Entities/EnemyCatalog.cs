namespace CodigoECavaleiros.Entities
{
    /// <summary>Ordem das fases e prefab 3D de cada inimigo (equivale ao create_enemy do Python).</summary>
    public static class EnemyCatalog
    {
        static readonly string[] PrefabNames =
        {
            "Vil_Palhaco", "Vil_Rato", "Vil_Coelho", "Vil_Sargento", "Boss_Capitao"
        };

        public static Enemy Create(int stage)
        {
            switch (stage)
            {
                case 0: return new PalhacoBug();
                case 1: return new RatoLoop();
                case 2: return new CoelhoNulo();
                case 3: return new SargentoHeranca();
                case 4: return new CompiladorSombrio();
                default: throw new System.ArgumentOutOfRangeException("stage");
            }
        }

        public static string PrefabName(int stage) { return PrefabNames[stage]; }

        public static string PrefabPath(int stage)
        {
            return "Assets/Prefabs/Characters/" + PrefabNames[stage] + ".prefab";
        }
    }
}
