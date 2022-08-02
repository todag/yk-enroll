using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using YKEnroll.Lib;
using YKEnroll.Win.ViewModels.MVVM;

namespace YKEnroll.Win.ViewModels;

internal class SelectUserViewModel : Presenter
{
    private List<ADUser>? _searchResults;
    
    public ADUser? SelectedUser { get; set; }

    public ICommand OkCommand => new Command(_ => SelectUser((Window)_!));
    public ICommand CancelCommand => new Command(_ => Cancel((Window)_!));
    public ICommand SearchDirectoryCommand => new Command(_ => SearchDirectory());

    public List<ADUser>? SearchResults
    {
        get => _searchResults;
        private set => Update(ref _searchResults, value);
    }

    public string SearchString { get; set; } = string.Empty;

    private void SearchDirectory()
    {
        try
        {
            SearchResults = ADManager.FindUsers(SearchString);
        }
        catch (Exception ex)
        {
            ShowMessage.Error("Search failed!", ex);
        }
    }

    public void SelectUser(Window window)
    {        
        if (SelectedUser != null)
            window.Close();        
    }

    private void Cancel(Window window)
    {        
        SelectedUser = null;
        window.Close();        
    }
}