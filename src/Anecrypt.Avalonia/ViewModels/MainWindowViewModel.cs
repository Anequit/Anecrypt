using Anecrypt.Avalonia.Services;
using Anecrypt.Core;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Anecrypt.Avalonia.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly FileService? _fileService;

    [ObservableProperty]
    private string _filePath;

    [ObservableProperty]
    private bool _encrypted;

    [ObservableProperty]
    private string _password;

    [ObservableProperty]
    private bool _deleteAfterward;

    [ObservableProperty]
    private bool _includeSymbols;

    [ObservableProperty]
    private bool _includeNumbers;

    [ObservableProperty]
    private bool _busy;

    public MainWindowViewModel()
    {
        FilePath = string.Empty;
        Password = string.Empty;
        IncludeNumbers = true;
        IncludeSymbols = true;

        Busy = false;
        _fileService ??= App.Current?.Services?.GetRequiredService<FileService>();
    }

    [RelayCommand(CanExecute = nameof(Validate))]
    private async Task Encrypt()
    {
        if (_fileService is null)
            return;

        IReadOnlyList<IStorageFolder> result = await _fileService.OpenFolderPickerAsync(new FolderPickerOpenOptions()
        {
            AllowMultiple = false,
            Title = "Location to store encrypted file."
        });

        if (result.Count == 0)
            return;

        Busy = true;
        await Encryption.Encrypt(FilePath, result[0].Path.LocalPath, Password, DeleteAfterward);
        Busy = false;
    }

    [RelayCommand(CanExecute = nameof(Validate))]
    private async Task DecryptAsync()
    {
        if (_fileService is null)
            return;

        IReadOnlyList<IStorageFolder> result = await _fileService.OpenFolderPickerAsync(new FolderPickerOpenOptions()
        {
            AllowMultiple = false,
            Title = "Location to store decrypted file."
        });

        if (result.Count == 0)
            return;

        Busy = true;

        try
        {
            await Encryption.Decrypt(FilePath, result[0].Path.LocalPath, Password, DeleteAfterward);
        }
        catch (Exception ex)
        {
            string content;

            switch (ex)
            {
                case CryptographicException:
                    content = "Incorrect password";
                    break;

                default:
                    content = ex.Message;
                    break;
            }

            await new ContentDialog()
            {
                Content = content,
                Title = "Error",
                PrimaryButtonText = "Close"
            }.ShowAsync();
        }
        finally
        {
            Busy = false;
        }
    }

    [RelayCommand]
    private async Task SelectFileAsync()
    {
        if (_fileService is null)
            return;

        IReadOnlyList<IStorageFile> result = await _fileService.OpenFilePickerAsync(new FilePickerOpenOptions()
        {
            AllowMultiple = false,
            Title = "Target file"
        });

        if (result.Count == 0)
            return;

        FilePath = result[0].Path.LocalPath;
    }

    [RelayCommand]
    private void Drop(IStorageItem file) => FilePath = file.Path.LocalPath;

    [RelayCommand]
    private void GeneratePassword()
    {
        Password = PasswordGenerator.Generate(Random.Shared.Next(18, 22), IncludeSymbols, IncludeNumbers);
    }

    partial void OnFilePathChanged(string value)
    {
        Encrypted = Encryption.IsEncrypted(value);

        NotifyEncryptAndDecrypt();
    }

    partial void OnPasswordChanged(string value) => NotifyEncryptAndDecrypt();

    private void NotifyEncryptAndDecrypt()
    {
        if (!Encrypted)
            EncryptCommand.NotifyCanExecuteChanged();
        else
            DecryptCommand.NotifyCanExecuteChanged();
    }

    private bool Validate() => File.Exists(FilePath) && !string.IsNullOrEmpty(Password);
}
