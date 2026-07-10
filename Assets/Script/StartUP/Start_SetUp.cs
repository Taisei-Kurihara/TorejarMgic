using UnityEngine;

public class Start_SetUp
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        Debug.Log("Start_SetUp/Init_0:UnityProject実行開始");

        GamePresenter gamePresenter = GamePresenter.Instance;
        Debug.Log("Start_SetUp/Init_1:GamePresenter.Instance");

        // InGameManager初期化＋ゲーム開始.
        gamePresenter.StartInGame();
        Debug.Log("Start_SetUp/Init_2:StartInGame呼び出し");

    }
}
