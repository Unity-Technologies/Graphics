
public interface IPreviewTask
{
    // needs to have
    // - the way to kick it off
    // - the way to check the status
    // - (optional) the way to cancel

    void Start();

    bool IsComplete();

    void Finish();
}
