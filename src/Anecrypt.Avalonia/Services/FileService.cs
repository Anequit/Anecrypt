using Avalonia.Platform.Storage;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Anecrypt.Avalonia.Services;

internal class FileService
{
    private readonly IStorageProvider _storageProvider;

    public FileService(IStorageProvider storageProvider)
    {
        _storageProvider = storageProvider;
    }

    public async Task<IReadOnlyList<IStorageFolder>> OpenFolderPickerAsync(FolderPickerOpenOptions options)
    {
        return await _storageProvider.OpenFolderPickerAsync(options);
    }

    public async Task<IReadOnlyList<IStorageFile>> OpenFilePickerAsync(FilePickerOpenOptions options)
    {
        return await _storageProvider.OpenFilePickerAsync(options);
    }
}
