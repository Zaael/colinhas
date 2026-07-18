using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;
using Colinhas.Models;
using Colinhas.Services;

namespace Colinhas.ViewModels;

public partial class TemplatesViewModel : ObservableObject
{
    private readonly TemplateStore _store = new();

    public ObservableCollection<TextTemplate> Templates { get; } = [];

    public bool IsEmpty => Templates.Count == 0;

    public TemplatesViewModel()
    {
        Templates.CollectionChanged += OnTemplatesChanged;
    }

    public void Load()
    {
        Logger.Log("TemplatesViewModel.Load");
        foreach (var template in _store.Load())
            Templates.Add(template);
    }

    private void OnTemplatesChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => OnPropertyChanged(nameof(IsEmpty));

    public void Add(TextTemplate template)
    {
        Templates.Insert(0, template);
        Persist();
    }

    public void Delete(TextTemplate template)
    {
        Templates.Remove(template);
        Persist();
    }

    /// <summary>Call after editing a template's Title/Content in place.</summary>
    public void Persist() => _store.Save(Templates);
}
