using System;
using System.Net.Http;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Client.ViewModels;

public class ViewModelBase : ObservableObject
{
    protected readonly HttpClient Http = new HttpClient {
        BaseAddress = new Uri("http://localhost:5198")
    };
}
