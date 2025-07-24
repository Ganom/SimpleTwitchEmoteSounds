using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using CommunityToolkit.Mvvm.ComponentModel;
using SimpleTwitchEmoteSounds.Common;
using SimpleTwitchEmoteSounds.ViewModels;

namespace SimpleTwitchEmoteSounds;

public class ViewLocator(AppViews views) : IDataTemplate
{
    private readonly Dictionary<object, Control> _controlCache = [];

    public Control Build(object? param)
    {
        if (param is null)
        {
            return CreateText("Data is null.");
        }

        if (_controlCache.TryGetValue(param, out var control))
        {
            return control;
        }

        if (views.TryCreateView(param, out var view))
        {
            _controlCache.Add(param, view);

            return view;
        }

        return CreateText($"No View For {param.GetType().Name}.");
    }

    public bool Match(object? data) => data is ObservableObject;

    private static TextBlock CreateText(string text) => new TextBlock
    {
        Text = text
    };
}
