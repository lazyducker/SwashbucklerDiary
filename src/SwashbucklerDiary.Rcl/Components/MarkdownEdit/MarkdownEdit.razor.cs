﻿using Masa.Blazor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using SwashbucklerDiary.Rcl.Essentials;
using SwashbucklerDiary.Rcl.Services;
using SwashbucklerDiary.Shared;

namespace SwashbucklerDiary.Rcl.Components
{
    public partial class MarkdownEdit : MyComponentBase
    {
        private const long maxAllowedSize = 512 * 1024 * 1024;

        private bool firstLineIndent;

        private bool codeLineNumber;

        private bool taskListLineThrough;

        private Dictionary<string, object>? _options;

        private MMarkdown mMarkdown = default!;

        private InputFile? inputFile;

        [Inject]
        private IMediaResourceManager MediaResourceManager { get; set; } = default!;

        [Inject]
        private IAppFileManager AppFileManager { get; set; } = default!;

        [Inject]
        private MarkdownJSModule Module { get; set; } = default!;

        [Inject]
        private ILogger<MarkdownEdit> Logger { get; set; } = default!;

        [CascadingParameter(Name = "IsDark")]
        public bool Dark { get; set; }

        [Parameter]
        public string? Value { get; set; }

        [Parameter]
        public EventCallback<string> ValueChanged { get; set; }

        [Parameter]
        public string? Class { get; set; }

        [Parameter]
        public string? WrapClass { get; set; }

        [Parameter]
        public bool Autofocus { get; set; }

        [Parameter]
        public EventCallback OnAfter { get; set; }

        public async Task InsertValueAsync(string value)
        {
            if (string.IsNullOrEmpty(Value))
            {
                await Module.Focus(mMarkdown.Ref);
            }

            await mMarkdown.InsertValueAsync(value);
        }

        public ValueTask<string?> GetValueAsync() => mMarkdown.GetValueAsync();

        protected override void OnInitialized()
        {
            base.OnInitialized();

            ReadSettings();
            SetOptions();
        }

        private string? InternalClass => $"{Class} {(firstLineIndent ? "first-line-indent" : "")} {(taskListLineThrough ? "task-list-line-through" : "")}";

        private void ReadSettings()
        {
            firstLineIndent = SettingService.Get<bool>(Setting.FirstLineIndent);
            codeLineNumber = SettingService.Get<bool>(Setting.CodeLineNumber);
            taskListLineThrough = SettingService.Get<bool>(Setting.TaskListLineThrough);
        }

        private void SetOptions()
        {
            string lang = I18n.Culture.Name.Replace("-", "_");
            string theme = Dark ? "dark" : "light";
            var previewTheme = new Dictionary<string, object?>()
            {
                { "current", theme },
                { "path", $"_content/{StaticWebAssets.RclAssemblyName}/npm/vditor/3.10.4/dist/css/content-theme" }
            };
            var previewHljs = new Dictionary<string, object>()
            {
                { "lineNumber", codeLineNumber }
            };
            var previewMarkdown = new Dictionary<string, object?>()
            {
                { "toc", true },
                { "mark", true }
            };
            var preview = new Dictionary<string, object?>()
            {
                { "theme", previewTheme },
                { "hljs", previewHljs },
                { "markdown", previewMarkdown },
            };
            var link = new Dictionary<string, object?>()
            {
                { "isOpen", false }
            };
            var btnImage = new Dictionary<string, object?>()
            {
                { "hotkey", "⇧⌘I" },
                { "name", "image" },
                { "tipPosition", "n" },
                { "tip", I18n.T("Vditor.AddImage") },
                { "className", "" },
                { "icon", "<svg><use xlink:href=\"#vditor-icon-image\"></use></svg>" },
            };

            var btnAudio = new Dictionary<string, object?>()
            {
                { "hotkey", "⇧⌘A" },
                { "name", "audio" },
                { "tipPosition", "n" },
                { "tip", I18n.T("Vditor.AddAudio") },
                { "className", "" },
                { "icon", "<svg><use xlink:href=\"#vditor-icon-audio\"></use></svg>" },
            };
            var btnVideo = new Dictionary<string, object?>()
            {
                { "hotkey", "⇧⌘V" },
                { "name", "video" },
                { "tipPosition", "n" },
                { "tip", I18n.T("Vditor.AddVideo") },
                { "className", "" },
                { "icon", "<svg><use xlink:href=\"#vditor-icon-video\"></use></svg>" },
            };
            var btnUpload = new Dictionary<string, object?>()
            {
                { "name", "upload" },
                { "className", "d-none" },
            };
            string[] accept = ["image/*", "audio/*", "video/*"];
            var upload = new Dictionary<string, object?>()
            {
                { "max", maxAllowedSize },
                { "accept", accept },
            };

            _options = new()
            {
                { "mode", "ir" },
                { "toolbar", new object[]{"headings", "bold", "italic", "strike", "line", "quote","list", "ordered-list", "check", "indent", "outdent", "code", "inline-code", "link", btnImage, btnAudio, btnVideo, "undo", "redo", "fullscreen", btnUpload }},
                { "placeholder", I18n.T("Write.ContentPlace") },
                { "cdn", $"_content/{StaticWebAssets.RclAssemblyName}/npm/vditor/3.10.4" },
                { "lang", lang },
                { "icon","material" },
                { "theme", theme },
                { "preview", preview },
                { "link", link },
                { "typewriterMode", true },
                { "height", "100%" },
                { "upload", upload },
            };
        }

        private async Task AfterMarkdownRender()
        {
            await Module.After(mMarkdown.Ref);
            if (Autofocus)
            {
                await Module.Autofocus(mMarkdown.Ref);
            }

            if (OnAfter.HasDelegate)
            {
                await OnAfter.InvokeAsync();
            }
        }

        private async void HandleToolbarButtonClick(string btnName)
        {
            switch (btnName)
            {
                case "image":
                    await AddImageAsync();
                    break;
                case "audio":
                    await AddAudioAsync();
                    break;
                case "video":
                    await AddVideoAsync();
                    break;
            }
        }

        private async Task AddImageAsync()
        {
            string? src = await MediaResourceManager.AddImageAsync();
            await AddMediaFileAsync(src, MediaResource.Image);
        }

        private async Task AddAudioAsync()
        {
            string? src = await MediaResourceManager.AddAudioAsync();
            await AddMediaFileAsync(src, MediaResource.Audio);
        }

        private async Task AddVideoAsync()
        {
            string? src = await MediaResourceManager.AddVideoAsync();
            await AddMediaFileAsync(src, MediaResource.Video);
        }

        private async Task AddMediaFileAsync(string? src, MediaResource mediaResource)
        {
            if (string.IsNullOrEmpty(src)) return;
            var kind = MediaResourceManager.GetResourceKind(src);
            if (kind == MediaResource.Unknown)
            {
                return;
            }

            string? insertContent = CreateInsertContent(src, mediaResource);
            if (insertContent is null) return;

            await InsertValueAsync(insertContent);
        }

        private static string? CreateInsertContent(string src, MediaResource mediaResource)
        {
            return mediaResource switch
            {
                MediaResource.Image => $"![]({src})",
                MediaResource.Audio => $"<audio src=\"{src}\" controls ></audio>",
                MediaResource.Video => $"<video src=\"{src}\" controls ></video>",
                _ => null
            };
        }

        public async Task<string?> CreateInsertContent(List<string?> filePaths)
        {
            var resources = await MediaResourceManager.CreateMediaResourceFilesAsync(filePaths);
            var insertContents = resources.Select(it => CreateInsertContent(it.ResourceUri!, it.ResourceType));
            return string.Join("\n", insertContents);
        }

        private async Task HandleBeforeAllUpload()
        {
            await Module.Upload(mMarkdown.Ref, inputFile?.Element);
        }

        private async void LoadFiles(InputFileChangeEventArgs e)
        {
            List<string?> filePaths = [];
            foreach (var browserFile in e.GetMultipleFiles())
            {
                var kind = MediaResourceManager.GetResourceKind(browserFile.Name);
                if (kind == MediaResource.Unknown)
                {
                    continue;
                }

                try
                {
                    var filePath = await AppFileManager.CreateTempFileAsync(browserFile.Name, browserFile.OpenReadStream(maxAllowedSize));
                    filePaths.Add(filePath);
                }
                catch (Exception ex)
                {
                    Logger.LogInformation(ex, "OpenReadStream error");
                }
            }
            if (filePaths.Count == 0)
            {
                return;
            }

            var insertContent = await CreateInsertContent(filePaths);
            if (string.IsNullOrEmpty(insertContent))
            {
                return;
            }

            await InsertValueAsync(insertContent);
        }
    }
}
