using Velopack;
using Velopack.Sources;

namespace SoundBurst.App.Services;

public sealed class UpdateService
{
    private readonly string _repoUrl;

    public UpdateService(string repoUrl) => _repoUrl = repoUrl;

    public async Task<(bool Available, string? Version, string? Error)> CheckAsync()
    {
        try
        {
            var mgr = CreateManager();
            if (!mgr.IsInstalled)
            {
                return (false, null, "Velopack未インストール（開発実行中）");
            }

            var update = await mgr.CheckForUpdatesAsync().ConfigureAwait(false);
            return update is null
                ? (false, null, null)
                : (true, update.TargetFullRelease.Version.ToString(), null);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    public async Task<(bool Applied, string? Error)> DownloadAndApplyAsync(Action<int>? progress = null)
    {
        try
        {
            var mgr = CreateManager();
            if (!mgr.IsInstalled)
            {
                return (false, "Velopack未インストール（開発実行中）");
            }

            var update = await mgr.CheckForUpdatesAsync().ConfigureAwait(false);
            if (update is null)
            {
                return (false, "更新はありません");
            }

            await mgr.DownloadUpdatesAsync(update, p => progress?.Invoke(p)).ConfigureAwait(false);
            mgr.ApplyUpdatesAndRestart(update);
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private UpdateManager CreateManager()
    {
        var source = new GithubSource(_repoUrl, accessToken: null, prerelease: false);
        return new UpdateManager(source);
    }
}
