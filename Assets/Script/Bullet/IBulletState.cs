/// <summary> 弾のイベント時に実行されるState (stateListの要素). </summary>
public interface IBulletState
{
    /// <summary> このStateを実行. </summary>
    void Execute(BulletContext context);
}
