using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using Object = UnityEngine.Object;

/// <summary> Z~Mキーのテスト機能コントローラー. </summary>
/// <remarks> Keyboard.current で直接読み取り. SetAction で各キーの機能を差し替え可能. </remarks>
public class DebugKeyController
{
    /// <summary> キーのインデックス定数. </summary>
    public const int Key_Z = 0;
    public const int Key_X = 1;
    public const int Key_C = 2;
    public const int Key_V = 3;
    public const int Key_B = 4;
    public const int Key_N = 5;
    public const int Key_M = 6;

    private static readonly string[] KeyNames = { "Z", "X", "C", "V", "B", "N", "M" };
    private readonly Action[] _actions = new Action[7];

    private GameStateManager _gameStateManager;

    // 操作説明UI.
    private GameObject _helpCanvasObj;
    private bool _helpVisible;

    // NotoSansJP フォント (Resources/Fonts & Materials/ から読み込み).
    private const string FontResourcePath = "Fonts & Materials/NotoSansJP-VariableFont_wght SDF";
    private TMP_FontAsset _fontAsset;

    public void Initialize(GameStateManager gameStateManager)
    {
        _gameStateManager = gameStateManager;

        // Z: 探索/Puzzle トグル切り替え.
        _actions[Key_Z] = OnToggleState;

        // X~N: 未実装スロット.
        for (int i = Key_X; i <= Key_N; i++)
        {
            int index = i;
            _actions[i] = () => Debug.Log($"TestKey {KeyNames[index]}: 未実装");
        }

        // M: 操作説明トグル.
        _actions[Key_M] = OnToggleHelp;

        // フォント事前読み込み.
        _fontAsset = Resources.Load<TMP_FontAsset>(FontResourcePath);
        if (_fontAsset == null)
        {
            Debug.LogWarning("DebugKeyController: NotoSansJP フォント読み込み失敗. デフォルトフォント使用.");
        }
    }

    /// <summary> キーの機能を差し替え. </summary>
    public void SetAction(int keyIndex, Action action)
    {
        if (keyIndex >= 0 && keyIndex < _actions.Length)
        {
            _actions[keyIndex] = action;
        }
    }

    /// <summary> 毎フレーム呼び出し. </summary>
    public void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.zKey.wasPressedThisFrame) _actions[Key_Z]?.Invoke();
        if (kb.xKey.wasPressedThisFrame) _actions[Key_X]?.Invoke();
        if (kb.cKey.wasPressedThisFrame) _actions[Key_C]?.Invoke();
        if (kb.vKey.wasPressedThisFrame) _actions[Key_V]?.Invoke();
        if (kb.bKey.wasPressedThisFrame) _actions[Key_B]?.Invoke();
        if (kb.nKey.wasPressedThisFrame) _actions[Key_N]?.Invoke();
        if (kb.mKey.wasPressedThisFrame) _actions[Key_M]?.Invoke();
    }

    /// <summary> Z キー: 探索/Puzzle トグル. </summary>
    private void OnToggleState()
    {
        if (_gameStateManager.IsTransitioning) return;
        _gameStateManager.ToggleStateAsync().Forget();
    }

    /// <summary> M キー: 操作説明表示トグル. </summary>
    private void OnToggleHelp()
    {
        if (_helpCanvasObj == null)
        {
            CreateHelpUI();
            _helpVisible = true;
        }
        else
        {
            _helpVisible = !_helpVisible;
            _helpCanvasObj.SetActive(_helpVisible);
        }
    }

    /// <summary> 操作説明UIを Canvas + TextMeshPro で動的生成. </summary>
    private void CreateHelpUI()
    {
        // Canvas.
        _helpCanvasObj = new GameObject("HelpCanvas");
        Object.DontDestroyOnLoad(_helpCanvasObj);
        var canvas = _helpCanvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = _helpCanvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        _helpCanvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // 半透明背景パネル.
        var panelObj = new GameObject("Panel");
        panelObj.transform.SetParent(_helpCanvasObj.transform, false);
        var panelImage = panelObj.AddComponent<UnityEngine.UI.Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.7f);
        var panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.1f, 0.1f);
        panelRect.anchorMax = new Vector2(0.9f, 0.9f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // ScrollRect (スクロール対応).
        var scrollRect = panelObj.AddComponent<UnityEngine.UI.ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 30f;

        // Viewport (RectMask2D でクリッピング).
        var viewportObj = new GameObject("Viewport");
        viewportObj.transform.SetParent(panelObj.transform, false);
        viewportObj.AddComponent<UnityEngine.UI.RectMask2D>();
        var viewportRect = viewportObj.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;

        // Content (TMP を直接配置. ContentSizeFitter が TMP の preferred size を直接取得).
        var contentObj = new GameObject("Content");
        contentObj.transform.SetParent(viewportObj.transform, false);
        var contentRect = contentObj.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.sizeDelta = Vector2.zero;

        // TextMeshPro を Content に直接配置 (子オブジェクトではなく同一オブジェクト).
        var tmp = contentObj.AddComponent<TextMeshProUGUI>();
        if (_fontAsset != null) tmp.font = _fontAsset;
        tmp.text = BuildHelpText();
        tmp.fontSize = 24;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.enableWordWrapping = true;
        tmp.margin = new Vector4(20f, 20f, 20f, 20f);

        // ContentSizeFitter (同一オブジェクトの TMP から preferred size を直接取得).
        var fitter = contentObj.AddComponent<UnityEngine.UI.ContentSizeFitter>();
        fitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRect;

        // レイアウト強制更新.
        Canvas.ForceUpdateCanvases();

        Debug.Log("DebugKeyController: 操作説明UI生成完了.");
    }

    /// <summary> 操作説明テキストを構築. </summary>
    private string BuildHelpText()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<size=32><b>操作説明</b></size>");
        sb.AppendLine();

        sb.AppendLine("<b>--- 共通 ---</b>");
        sb.AppendLine("Z : 探索 / パズル 切り替え");
        sb.AppendLine("M : この操作説明を表示 / 非表示");
        sb.AppendLine();

        sb.AppendLine("<b>--- 探索モード ---</b>");
        sb.AppendLine("WASD : 移動");
        sb.AppendLine("マウス左 / Enter : 攻撃");
        sb.AppendLine("Space : ジャンプ");
        sb.AppendLine("E (長押し) : インタラクト");
        sb.AppendLine("Shift : ダッシュ");
        sb.AppendLine("1 / 2 : 前 / 次の武器");
        sb.AppendLine();

        sb.AppendLine("<b>--- パズルモード ---</b>");
        sb.AppendLine("左クリック (一覧) : ミノ選択");
        sb.AppendLine("左クリック (盤面) : ミノ配置 / 配置済み再選択");
        sb.AppendLine("右クリック : 選択解除");
        sb.AppendLine("Tab / Shift+Tab : 次 / 前のミノ選択");
        sb.AppendLine("Space : ミノ配置確定");
        sb.AppendLine("Q / E : ミノ回転 (左 / 右)");
        sb.AppendLine("R / F : ミノ移動 (上 / 下)");
        sb.AppendLine("T / G : レイヤー表示 (上 / 下)");
        sb.AppendLine();

        sb.AppendLine("<b>--- テストキー ---</b>");
        sb.AppendLine("X : 未実装");
        sb.AppendLine("C : 未実装");
        sb.AppendLine("V : 未実装");
        sb.AppendLine("B : 未実装");
        sb.AppendLine("N : 未実装");

        return sb.ToString();
    }

    public void Dispose()
    {
        for (int i = 0; i < _actions.Length; i++)
        {
            _actions[i] = null;
        }

        if (_helpCanvasObj != null)
        {
            UnityEngine.Object.Destroy(_helpCanvasObj);
            _helpCanvasObj = null;
        }
    }
}
