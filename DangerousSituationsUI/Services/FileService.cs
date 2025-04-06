using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DangerousSituationsUI.Services;

public class FilesService
{
    #region Private Fields
    public Window? Target => App.Current?.CurrentWindow;
    #endregion

    public async Task<List<IStorageFile>?> OpenVideoFolderAsync()
    {
        var folder = await OpenFolederAsync();
        if (folder != null)
        {
            var files = folder?.GetItemsAsync().ToBlockingEnumerable();
            List<IStorageFile> storageFiles = new();

            foreach(var file in files) if (file.Name.Split('.')[1] != "mp4") throw new Exception();
            
            foreach (var file in files)
            {
                if (file.Path.IsFile)
                {
                    var storageFile = await Target.StorageProvider.TryGetFileFromPathAsync(file.Path);
                    storageFiles.Add(storageFile);
                }
            }
            return storageFiles;
        }
        else return null;
    }

    private async Task<IStorageFolder?> OpenFolederAsync()
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
            FileTypeFilter = [new FilePickerFileType("Video") { Patterns = ["*.mp4"] }],
            AllowMultiple = false
        });

        return files.Count >= 1 ? files[0] : null;
    }
}
