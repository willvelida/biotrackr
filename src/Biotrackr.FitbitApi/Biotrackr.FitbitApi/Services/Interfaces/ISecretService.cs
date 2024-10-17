namespace Biotrackr.FitbitApi.Services.Interfaces
{
    public interface ISecretService
    {
        Task<string> GetSecretAsync(string secretName);
    }
}
