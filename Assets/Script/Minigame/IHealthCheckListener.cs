public interface IHealthCheckListener
{
    void OnHealthCheckSuccess();
    void OnHealthCheckFailure();
}
