using UnityEngine;

public class PlayerDashState : PlayerState
{
    public PlayerDashState(Player _player, PlayerStateMachine _stateMachine, string _animBoolName) : base(_player, _stateMachine, _animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        SkillManager.Instance.Clone.CreateCloneOnDashStart();

        stateTimer = player.dashDuration;
    }

    public override void Exit()
    {
        base.Exit();

        SkillManager.Instance.Clone.CreateCloneOnDashOver();

        player.SetVelocity(0, rb.velocity.y);
    }

    public override void Update()
    {
        base.Update();

        if (!player.IsGroundDetected() && player.IsWallDetected())
            stateMachine.ChangeState(player.wallSlideState);

        player.SetVelocity(player.dashSpeed * player.dashDirection, 0);

        if (stateTimer < 0) 
            stateMachine.ChangeState(player.idleState);
    }
}
