using Windows.ApplicationModel;

namespace Colinhas.Services;

/// <summary>
/// "Iniciar com o Windows" para app empacotado (MSIX).
///
/// Apps empacotados não devem escrever na chave Run do registro — o Windows
/// gerencia o startup pela extensão <c>windows.startupTask</c> declarada no
/// Package.appxmanifest. Vantagens: aparece em Gerenciador de Tarefas &gt;
/// Aplicativos de inicialização e é removido junto com o app na desinstalação.
///
/// Detalhe importante: se o usuário desligar por lá, o estado vira
/// <see cref="StartupTaskState.DisabledByUser"/> e o app **não** consegue mais
/// religar sozinho — é proposital, para nenhum app forçar o próprio startup.
/// </summary>
public static class StartupService
{
    /// <summary>Precisa bater com o TaskId declarado no Package.appxmanifest.</summary>
    private const string TaskId = "ColinhasStartup";

    public static async Task<StartupTaskState> GetStateAsync()
    {
        try
        {
            var task = await StartupTask.GetAsync(TaskId);
            return task.State;
        }
        catch (Exception ex)
        {
            Logger.Log($"StartupService: GetState falhou — {ex.Message}");
            return StartupTaskState.Disabled;
        }
    }

    public static async Task<bool> IsEnabledAsync() =>
        await GetStateAsync() is StartupTaskState.Enabled or StartupTaskState.EnabledByPolicy;

    /// <summary>
    /// Liga/desliga o startup. Devolve o estado resultante para o chamador poder
    /// avisar o usuário quando o Windows recusou (DisabledByUser/DisabledByPolicy).
    /// </summary>
    public static async Task<StartupTaskState> SetEnabledAsync(bool enabled)
    {
        try
        {
            var task = await StartupTask.GetAsync(TaskId);

            if (!enabled)
            {
                task.Disable();
                Logger.Log("StartupService: startup desligado");
                return StartupTaskState.Disabled;
            }

            var state = await task.RequestEnableAsync();
            Logger.Log($"StartupService: startup ligado -> {state}");
            return state;
        }
        catch (Exception ex)
        {
            Logger.Log($"StartupService: SetEnabled({enabled}) falhou — {ex.Message}");
            return StartupTaskState.Disabled;
        }
    }
}
