﻿using BlazorComponent;
using BlazorComponent.I18n;
using Masa.Blazor;
using Microsoft.AspNetCore.Components;
using NoDecentDiary.IServices;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoDecentDiary.Shared
{
    public partial class MainLayout : IDisposable
    {
        [Inject]
        private MasaBlazor? MasaBlazor { get; set; }
        [Inject]
        private NavigationManager? Navigation { get; set; }
        [Inject]
        public INavigateService? NavigateService { get; set; }
        [Inject]
        private I18n? I18n { get; set; }
        [Inject]
        private ISettingsService? SettingsService { get; set; }

        StringNumber SelectedItem = 0;

        readonly List<NavigationButton> NavigationButtons = new()
        {
            new NavigationButton(0,"Main.Diary","mdi-notebook-outline","mdi-notebook",""),
            new NavigationButton(1,"Main.History","mdi-clock-outline","mdi-clock","History"),
            new NavigationButton(2,"Main.Mine","mdi-account-outline","mdi-account","Mine")
        };

        private class NavigationButton
        {
            public NavigationButton(int id, string title, string icon, string selectIcon, string href)
            {
                Id = id;
                Title = title;
                Icon = icon;
                SelectIcon = selectIcon;
                Href = href;
            }
            public int Id;
            public string Title { get; set; }
            public string Icon { get; set; }
            public string SelectIcon { get; set; }
            public string Href { get; set; }
        }

        protected override async Task OnInitializedAsync()
        {
            NavigateService!.Navigation = Navigation;
            await LoadSettings();
            MasaBlazor!.Breakpoint.OnUpdate += InvokeStateHasChangedAsync;
            await base.OnInitializedAsync();
        }
        private async Task LoadSettings()
        {
            var language = await SettingsService!.Get("Language", "zh-CN");
            I18n!.SetCulture(new CultureInfo(language));
        }
        private string? GetIcon(NavigationButton navigationButton)
        {
            return SelectedItem == navigationButton.Id ? navigationButton.SelectIcon : navigationButton.Icon;
        }

        private void ChangeView(NavigationButton navigationButton)
        {
            Navigation!.NavigateTo(navigationButton.Href);
        }

        private bool ShowBottomNavigation()
        {
            var url = Navigation!.ToBaseRelativePath(Navigation.Uri);
            return NavigationButtons.Any(it => it.Href == url.Split("?")[0]);
        }
        private async Task InvokeStateHasChangedAsync()
        {
            await InvokeAsync(StateHasChanged);
        }

        public void Dispose()
        {
            MasaBlazor!.Breakpoint.OnUpdate -= InvokeStateHasChangedAsync;
            GC.SuppressFinalize(this);
        }
    }
}
