using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary> Player攻撃状態のインターフェース. </summary>
public interface IPlayerAttackState
{
    void Enter(PlayerController controller);
    void Update(PlayerController controller);
    void Exit(PlayerController controller);
}

/// <summary> 攻撃待機状態. </summary>
public class PlayerAttackState_Idle : IPlayerAttackState
{
    public void Enter(PlayerController controller) { }
    public void Update(PlayerController controller) { }
    public void Exit(PlayerController controller) { }
}

/// <summary> 攻撃実行中状態. </summary>
public class PlayerAttackState_Firing : IPlayerAttackState
{
    private int _attackIndex;

    public PlayerAttackState_Firing(int attackIndex)
    {
        _attackIndex = attackIndex;
    }

    public void Enter(PlayerController controller)
    {
        var manager = controller.GetBulletManager(_attackIndex);
        manager?.OnFirePressed();
    }

    public void Update(PlayerController controller)
    {
    }

    public void Exit(PlayerController controller)
    {
        var manager = controller.GetBulletManager(_attackIndex);
        manager?.OnFireReleased();
    }
}

/// <summary> Player入力処理 + 移動 + 攻撃state管理. </summary>
/// <remarks> MonoBehaviourなし. InputSystem Player ActionMapを使用. </remarks>
public class PlayerController
{
    private InputSystem_Actions _inputActions;
    private GameObject _playerObj;
    private float _moveSpeed = 5f;

    // 攻撃state管理.
    private IPlayerAttackState _currentAttackState;
    private readonly IPlayerAttackState _idleState = new PlayerAttackState_Idle();

    // 攻撃スロット (0~6) のBulletManager.
    private readonly BulletManager[] _bulletManagers = new BulletManager[7];

    public PlayerController(GameObject playerObj)
    {
        _playerObj = playerObj;
        _currentAttackState = _idleState;
    }

    /// <summary> 初期化. InputSystemバインド. </summary>
    public void Initialize()
    {
        _inputActions = new InputSystem_Actions();

        // 攻撃キー0~6バインド.
        _inputActions.Player.Attack0.performed += _ => StartAttack(0);
        _inputActions.Player.Attack0.canceled += _ => EndAttack();
        _inputActions.Player.Attack1.performed += _ => StartAttack(1);
        _inputActions.Player.Attack1.canceled += _ => EndAttack();
        _inputActions.Player.Attack2.performed += _ => StartAttack(2);
        _inputActions.Player.Attack2.canceled += _ => EndAttack();
        _inputActions.Player.Attack3.performed += _ => StartAttack(3);
        _inputActions.Player.Attack3.canceled += _ => EndAttack();
        _inputActions.Player.Attack4.performed += _ => StartAttack(4);
        _inputActions.Player.Attack4.canceled += _ => EndAttack();
        _inputActions.Player.Attack5.performed += _ => StartAttack(5);
        _inputActions.Player.Attack5.canceled += _ => EndAttack();
        _inputActions.Player.Attack6.performed += _ => StartAttack(6);
        _inputActions.Player.Attack6.canceled += _ => EndAttack();

        _inputActions.Player.Enable();
    }

    /// <summary> 入力の有効/無効を切り替え. </summary>
    public void SetInputEnabled(bool enabled)
    {
        if (enabled) _inputActions.Player.Enable();
        else _inputActions.Player.Disable();
    }

    /// <summary> 攻撃スロットにBulletManagerを設定. </summary>
    public void SetBulletManager(int index, BulletManager manager)
    {
        if (index >= 0 && index < _bulletManagers.Length)
        {
            _bulletManagers[index] = manager;
        }
    }

    /// <summary> 攻撃スロットのBulletManagerを取得. </summary>
    public BulletManager GetBulletManager(int index)
    {
        if (index >= 0 && index < _bulletManagers.Length)
        {
            return _bulletManagers[index];
        }
        return null;
    }

    /// <summary> 毎フレーム更新. 移動 + 攻撃state + 弾更新. </summary>
    public void Update()
    {
        UpdateMovement();
        _currentAttackState?.Update(this);
        UpdateBullets();
    }

    /// <summary> 移動処理. </summary>
    private void UpdateMovement()
    {
        if (_playerObj == null) return;

        var moveInput = _inputActions.Player.Move.ReadValue<Vector2>();
        if (moveInput.sqrMagnitude < 0.01f) return;

        var move = new Vector3(moveInput.x, 0f, moveInput.y) * _moveSpeed * Time.deltaTime;
        _playerObj.transform.position += move;
    }

    /// <summary> 攻撃開始. </summary>
    private void StartAttack(int index)
    {
        if (_bulletManagers[index] == null) return;

        ChangeAttackState(new PlayerAttackState_Firing(index));
    }

    /// <summary> 攻撃終了. </summary>
    private void EndAttack()
    {
        ChangeAttackState(_idleState);
    }

    /// <summary> 攻撃stateを変更. </summary>
    private void ChangeAttackState(IPlayerAttackState newState)
    {
        _currentAttackState?.Exit(this);
        _currentAttackState = newState;
        _currentAttackState?.Enter(this);
    }

    /// <summary> 全BulletManagerの弾を更新. </summary>
    private void UpdateBullets()
    {
        for (int i = 0; i < _bulletManagers.Length; i++)
        {
            _bulletManagers[i]?.Update();
        }
    }

    /// <summary> 破棄. </summary>
    public void Dispose()
    {
        _inputActions.Player.Disable();
        _inputActions.Dispose();

        for (int i = 0; i < _bulletManagers.Length; i++)
        {
            _bulletManagers[i]?.Dispose();
        }
    }
}
