﻿using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using SwashbucklerDiary.Components;
using SwashbucklerDiary.Extensions;
using SwashbucklerDiary.IServices;
using SwashbucklerDiary.Models;
using SwashbucklerDiary.Utilities;

namespace SwashbucklerDiary.Pages
{
    public partial class ReadPage : ImportantComponentBase, IAsyncDisposable
    {
        private bool ShowDelete;

        private bool ShowMenu;

        private bool ShowShare;

        private bool ShowExport;

        private bool Markdown;

        private bool Privacy;

        private DiaryModel Diary = new();

        private IJSObjectReference module = default!;

        private List<DynamicListItem> MenuItems = new();

        private List<DynamicListItem> ShareItems = new();

        private List<DiaryModel> ExportDiaries = new();

        [Inject]
        private IDiaryService DiaryService { get; set; } = default!;

        [Inject]
        private IIconService IconService { get; set; } = default!;

        [Inject]
        private IAppDataService AppDataService { get; set; } = default!;

        [Parameter]
        public Guid Id { get; set; }

        protected override void OnInitialized()
        {
            LoadView();
            base.OnInitialized();
        }

        protected override async Task OnInitializedAsync()
        {
            await LoadSettings();
            await base.OnInitializedAsync();
        }

        protected override async Task OnParametersSetAsync()
        {
            await UpdateDiary();
            await base.OnParametersSetAsync();
        }

        protected async override Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                module = await JS.InvokeAsync<IJSObjectReference>("import", "./js/screenshot.js");
            }
        }

        private List<TagModel> Tags => Diary.Tags ?? new();

        private bool IsTop => Diary.Top;

        private bool IsPrivate => Diary.Private;

        private bool ShowTitle => !string.IsNullOrEmpty(Diary.Title);

        private bool ShowWeather => !string.IsNullOrEmpty(Diary.Weather);

        private bool ShowMood => !string.IsNullOrEmpty(Diary.Mood);

        private bool ShowLocation => !string.IsNullOrEmpty(Diary.Location);

        private string TopText() => IsTop ? "Diary.CancelTop" : "Diary.Top";

        private string MarkdownText() => Markdown ? "Diary.Text" : "Diary.Markdown";

        private string MarkdownIcon() => Markdown ? "mdi-format-text" : "mdi-language-markdown-outline";
        
        private string PrivateText() => IsPrivate ? "Read.ClosePrivacy" : "Read.OpenPrivacy";
        
        private string PrivateIcon() => IsPrivate ? "mdi-lock-open-variant-outline" : "mdi-lock-outline";

        private async Task UpdateDiary()
        {
            var diary = await DiaryService.FindAsync(Id);
            if (diary == null)
            {
                await NavigateToBack();
                return;
            }

            Diary = diary;
            Diary.Content = StaticCustomScheme.CustomSchemeRender(Diary.Content);
        }

        private async Task LoadSettings()
        {
            Markdown = await SettingsService.Get(SettingType.Markdown);
            Privacy = await SettingsService.Get(SettingType.PrivacyMode);
        }

        private void LoadView()
        {
            MenuItems = new List<DynamicListItem>()
            {
                new(this, "Share.Copy","mdi-content-copy",OnCopy),
                new(this, TopText,"mdi-format-vertical-align-top",OnTopping),
                new(this, "Diary.Export","mdi-export",OpenExportDialog),
                new(this, MarkdownText,MarkdownIcon,MarkdownChanged),
                new(this, PrivateText, PrivateIcon, DiaryPrivacyChanged,()=>Privacy)
            };

            ShareItems = new()
            {
                new(this, "Share.TextShare","mdi-format-text",ShareText),
                new(this, "Share.ImageShare","mdi-image-outline",ShareImage),
            };
        }

        async ValueTask IAsyncDisposable.DisposeAsync()
        {
            if (module is not null)
            {
                await module.DisposeAsync();
            }

            GC.SuppressFinalize(this);
        }

        private void OpenDeleteDialog()
        {
            ShowDelete = true;
            StateHasChanged();
        }

        private async Task HandleDelete()
        {
            ShowDelete = false;
            bool flag = await DiaryService.DeleteAsync(Diary);
            if (flag)
            {
                await AlertService.Success(I18n.T("Share.DeleteSuccess"));
                await NavigateToBack();
            }
            else
            {
                await AlertService.Error(I18n.T("Share.DeleteFail"));
            }
        }

        private Task OnEdit()
        {
            return NavigateService.PushAsync($"/write?DiaryId={Id}", false);
        }

        private async Task OnTopping()
        {
            Diary.Top = !Diary.Top;
            await DiaryService.UpdateAsync(Diary);
            StateHasChanged();
        }

        private async Task OnCopy()
        {
            var content = GetDiaryCopyContent();
            await PlatformService.SetClipboard(content);
            await AlertService.Success(I18n.T("Share.CopySuccess"));
        }

        private async Task ShareText()
        {
            ShowShare = false;
            StateHasChanged();
            var content = GetDiaryCopyContent();
            await PlatformService.ShareText(I18n.T("Share.Share"), content);
            await HandleAchievements(AchievementType.Share);
        }

        private async Task ShareImage()
        {
            await AlertService.StartLoading();
            ShowShare = false;
            StateHasChanged();
            await Task.Delay(1000);

            var base64 = await module.InvokeAsync<string>("getScreenshotBase64", new object[1] { "#screenshot" });
            base64 = base64.Substring(base64.IndexOf(",") + 1);

            string fn = "Screenshot.png";
            string path = await AppDataService.CreateCacheFileAsync(fn, Convert.FromBase64String(base64));

            await AlertService.StopLoading();
            StateHasChanged();

            await PlatformService.ShareFile(I18n.T("Share.Share"), path);
            await HandleAchievements(AchievementType.Share);
        }

        private string GetWeatherIcon(string? key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return "mdi-weather-cloudy";
            }

            return IconService.GetWeatherIcon(key);
        }

        private string GetMoodIcon(string? key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return "mdi-emoticon-outline";
            }

            return IconService.GetMoodIcon(key);
        }

        private async Task MarkdownChanged()
        {
            Markdown = !Markdown;
            await SettingsService.Save(SettingType.Markdown, Markdown);
            StateHasChanged();
        }

        private async Task DiaryPrivacyChanged()
        {
            Diary.Private = !Diary.Private;
            Diary.UpdateTime = DateTime.Now;
            await DiaryService.UpdateAsync(Diary);
            await AlertService.Success(I18n.T("Read.PrivacyAlert"));
        }

        private string CounterValue()
        {
            string? value = Diary.Content;
            int len = 0;
            if (string.IsNullOrWhiteSpace(value))
            {
                return len + " " + I18n.T("Write.CountUnit");
            }

            value = value.Trim();
            if (I18n.T("Write.WordCountType") == WordCountType.Word.ToString())
            {
                len = value.WordCount();
            }

            if (I18n.T("Write.WordCountType") == WordCountType.Character.ToString())
            {
                len = value.CharacterCount();
            }

            return len + " " + I18n.T("Write.CountUnit");
        }

        private void OpenExportDialog()
        {
            var diary = Diary;
            ExportDiaries = new() { diary };
            ShowExport = true;
            StateHasChanged();
        }

        private string GetDiaryCopyContent()
        {
            if (string.IsNullOrEmpty(Diary.Title))
            {
                return Diary.Content!;
            }

            return Diary.Title + "\n" + Diary.Content;
        }
    }
}
