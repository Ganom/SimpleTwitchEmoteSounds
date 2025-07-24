#region

using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

#endregion

namespace SimpleTwitchEmoteSounds.Extensions;

public class FilteredObservableCollection<T> : ObservableCollection<T>
    where T : INotifyPropertyChanged
{
    private ObservableCollection<T> _source;
    private readonly Func<T, bool> _filter;

    public FilteredObservableCollection(ObservableCollection<T> source, Func<T, bool> filter)
    {
        _source = source;
        _filter = filter;

        foreach (var item in _source.Where(filter))
        {
            Add(item);
            item.PropertyChanged += Item_PropertyChanged;
        }

        _source.CollectionChanged += SourceCollectionChanged;
    }

    private void SourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                if (e.NewItems != null)
                    foreach (T item in e.NewItems)
                    {
                        if (!_filter(item))
                            continue;
                        Add(item);
                        item.PropertyChanged += Item_PropertyChanged;
                    }

                break;
            case NotifyCollectionChangedAction.Remove:
                if (e.OldItems != null)
                    foreach (T item in e.OldItems)
                    {
                        if (!Contains(item))
                            continue;
                        Remove(item);
                        item.PropertyChanged -= Item_PropertyChanged;
                    }

                break;
            case NotifyCollectionChangedAction.Reset:
                Refresh();
                break;
            case NotifyCollectionChangedAction.Replace:
                break;
            case NotifyCollectionChangedAction.Move:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        var item = (T)sender!;
        if (_filter(item) && !Contains(item))
        {
            Add(item);
        }
        else if (!_filter(item) && Contains(item))
        {
            Remove(item);
        }
    }

    public void Refresh()
    {
        foreach (var item in this.ToList())
        {
            item.PropertyChanged -= Item_PropertyChanged;
            Remove(item);
        }

        foreach (var item in _source.Where(_filter))
        {
            Add(item);
            item.PropertyChanged += Item_PropertyChanged;
        }
    }

    public void UpdateSource(ObservableCollection<T> newSource)
    {
        _source.CollectionChanged -= SourceCollectionChanged;

        foreach (var item in this.ToList())
        {
            item.PropertyChanged -= Item_PropertyChanged;
            Remove(item);
        }

        _source = newSource;
        _source.CollectionChanged += SourceCollectionChanged;

        foreach (var item in _source.Where(_filter))
        {
            Add(item);
            item.PropertyChanged += Item_PropertyChanged;
        }
    }
}
