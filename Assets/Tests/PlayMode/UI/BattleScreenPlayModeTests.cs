#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Visha.UI;
using Visha.Enemies;

public class BattleScreenPlayModeTests
{
    private BattleScreenView screens;
    private BattleHUD hud;
    private Health enemy;
    private Health player;

    [UnitySetUp] public IEnumerator LoadBattle()
    {
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/BattleUI_Demo.unity", new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;
        screens = Object.FindFirstObjectByType<BattleScreenView>();
        hud = Object.FindFirstObjectByType<BattleHUD>();
        enemy = Object.FindFirstObjectByType<EnemyPatternRunner>().GetComponent<Health>();
        player = GameObject.Find("Player").GetComponent<Health>();
    }

    private void Click(string path)
    {
        var button = screens.transform.Find(path).GetComponent<Button>();
        Assert.IsTrue(button.gameObject.activeInHierarchy);
        Assert.IsTrue(button.interactable);
        button.onClick.Invoke();
    }

    [UnityTest] public IEnumerator VictoryAndDefeatRetryRestorePlayableBattle()
    {
        Assert.AreEqual(BattleScreen.Battle, screens.Screen);
        enemy.TakeDamage(999); yield return null;
        Assert.AreEqual(BattleScreen.Victory, screens.Screen);
        Click("VictoryPanel/Card/VictoryRetryButton"); yield return null;
        Assert.AreEqual(BattleScreen.Battle, screens.Screen);
        Assert.AreEqual(enemy.MaxHealth, enemy.CurrentHealth);
        player.TakeDamage(999); yield return null;
        Assert.AreEqual(BattleScreen.Defeat, screens.Screen);
        Click("DefeatPanel/Card/RetryButton"); yield return null;
        Assert.AreEqual(BattleScreen.Battle, screens.Screen);
        Assert.AreEqual(player.MaxHealth, player.CurrentHealth);
        var poison = enemy.GetComponent<Poison>();
        int before = poison.PoisonStacks;
        hud.ChooseAbility(BattleAbility.VenomStrike); hud.SelectTarget();
        Assert.Greater(poison.PoisonStacks, before);
    }

    [UnityTest] public IEnumerator CampaignOwnerCanTakeOverScreensAndBlockBattleInput()
    {
        screens.GetComponent<BattleScreenPresenter>().enabled = false;
        screens.BindNavigation(screens.ShowBattle, null, null, null, null);
        screens.SetHUD(hud); screens.ShowTitle();
        int hp = enemy.CurrentHealth;
        hud.ChooseAbility(BattleAbility.VenomStrike); hud.SelectTarget();
        Assert.AreEqual(hp, enemy.CurrentHealth);
        Assert.AreEqual(0, enemy.GetComponent<Poison>().PoisonStacks);
        Click("TitlePanel/StartButton"); yield return null;
        Assert.AreEqual(BattleScreen.Battle, screens.Screen);
        hud.ChooseAbility(BattleAbility.VenomStrike); hud.SelectTarget();
        Assert.Greater(enemy.GetComponent<Poison>().PoisonStacks, 0);
    }
}
#endif
