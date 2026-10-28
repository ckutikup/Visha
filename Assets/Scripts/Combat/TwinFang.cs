public static class TwinFang
{
    public const int ResidueStacks = 2;
    public static void ApplyResidue(Poison struckEnemy, Poison otherEnemy)
    {
        if (otherEnemy != null && !otherEnemy.GetComponent<Health>().IsDead)
            otherEnemy.AddStacks(ResidueStacks);
        else if (struckEnemy != null && !struckEnemy.GetComponent<Health>().IsDead)
            struckEnemy.AddStacks(ResidueStacks);
    }
}
