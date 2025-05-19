using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Linq;


namespace DangerousSituationsUI.Services;

public class FilesService
{
    #region Private Fields
    public Window? Target => App.Current?.CurrentWindow;
    #endregion

    public async Task<List<IStorageFile>?> OpenVideoFolderAsync()
    {
        var folder = await OpenFolderAsync();
        if (folder == null)
            return null;

        var storageFiles = new List<IStorageFile>();
        var supportedExtensions = new[] { ".mp4", ".avi" };

        await foreach (var item in folder.GetItemsAsync())
        {
            if (item is not IStorageFile file)
                continue;

            var ext = Path.GetExtension(file.Name);
            if (!supportedExtensions.Contains(ext))
                continue;

            var storageFile = await Target.StorageProvider.TryGetFileFromPathAsync(file.Path);
            if (storageFile != null)
            {
                storageFiles.Add(storageFile);
            }
        }

        return storageFiles;
    }

    private async Task<IStorageFolder?> OpenFolderAsync()
    {
        var folders = await Target.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions()
        {
            Title = "Open Video Folder",
            AllowMultiple = false,
        });

        return folders.Count >= 1 ? folders[0] : null;
    }

    public async Task<IStorageFile?> OpenVideoFileAsync()
    {
        var files = await Target.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
        {
            Title = "Open Video File",
            FileTypeFilter = [new FilePickerFileType("Video") { Patterns = ["*.mp4", "*.avi"] }],
            AllowMultiple = false
        });

        return files.Count >= 1 ? files[0] : null;
    }

    public async Task<string?> PickExportPathAsync(string suggestedName = "event")
    {
        if (Target is null)
            return null;

        var file = await Target.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Сохранить отрезок видео",
            SuggestedFileName = suggestedName,
            FileTypeChoices = new[]
            {
                new FilePickerFileType("Видео")
                {
                    Patterns = ["*.mp4", "*.avi"]
                }
            }
        });

        return file?.Path.LocalPath;
    }

}
