using System.IO;
using System.Text.Json;
using Colinhas.Models;

namespace Colinhas.Services;

/// <summary>
/// Loads and saves text templates as JSON in the app's local folder.
/// A dedicated DTO is used so we never serialize commands or computed properties.
/// </summary>
public sealed class TemplateStore
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Colinhas", "templates.json");

    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    private sealed record TemplateDto(Guid Id, string Title, string Content);

    public List<TextTemplate> Load()
    {
        try
        {
            if (!File.Exists(FilePath))
                return DefaultTemplates();

            var json = File.ReadAllText(FilePath);
            var dtos = JsonSerializer.Deserialize<List<TemplateDto>>(json, Options);
            if (dtos is null || dtos.Count == 0)
                return DefaultTemplates();

            return dtos
                .Select(d => new TextTemplate { Id = d.Id, Title = d.Title, Content = d.Content })
                .ToList();
        }
        catch (Exception ex)
        {
            Logger.Log($"TemplateStore.Load error: {ex.Message}");
            return DefaultTemplates();
        }
    }

    public void Save(IEnumerable<TextTemplate> templates)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            var dtos = templates.Select(t => new TemplateDto(t.Id, t.Title, t.Content)).ToList();
            File.WriteAllText(FilePath, JsonSerializer.Serialize(dtos, Options));
        }
        catch (Exception ex)
        {
            Logger.Log($"TemplateStore.Save error: {ex.Message}");
        }
    }

    private static List<TextTemplate> DefaultTemplates() =>
    [
        new() { Title = "Saudação", Content = "Olá {nome}, tudo bem? " },
        new()
        {
            Title = "Agendamento",
            Content = "Olá {nome}! Seu horário está confirmado para o dia {data} às {hora}. Qualquer dúvida, estou à disposição.",
        },
    ];
}
