using Prism.Mvvm;
using Prism.Navigation;
using Reactive.Bindings;
using System;
using System.ComponentModel;
using Xamarin.Forms;
using Xamarin_NFCSuicaReader.Models;

namespace Xamarin_NFCSuicaReader.ViewModels
{
    public class DisplayPageViewModel : BindableBase, INotifyPropertyChanged, INavigationAware
    {
        INavigationService navigationService;

        //戻るボタン
        public AsyncReactiveCommand PrevTopCommand { get; set; } = new AsyncReactiveCommand();

        //残高
        public ReactiveProperty<string> zandaka { get; set; } = new ReactiveProperty<string>();


        public DisplayPageViewModel(INavigationService navigationService)
        {
            this.navigationService = navigationService;

            //ボタンを押したときの処理登録
            PrevTopCommand.Subscribe(async _ => await navigationService.GoBackToRootAsync());
        }

        public void OnNavigatedFrom(INavigationParameters parameters)
        {

        }

        public void OnNavigatedTo(INavigationParameters parameters)
        {
            //NFC受信データを取得
            INFCService NFCService = DependencyService.Get<INFCService>();
            var rawData = NFCService.getresData();

            //残高を表示
            zandaka.Value = Convert.ToString(Convert.ToInt32(rawData[24]) + Convert.ToInt32(rawData[25]) * 256)+ " 円";
        }
    }
}
