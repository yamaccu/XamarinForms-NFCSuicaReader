using Prism.Navigation;

using Reactive.Bindings;
using Xamarin.Forms;
using System.Threading.Tasks;
using Xamarin_NFCSuicaReader.Models;
using Prism.AppModel;

namespace Xamarin_NFCSuicaReader.ViewModels
{
    public class MainPageViewModel : ViewModelBase, IApplicationLifecycleAware
    {
        public ReactiveProperty<ImageSource> imageTouch { get; set; } = new ReactiveProperty<ImageSource>();

        //NFC通信待ちの無限ループフラグ
        private bool loopFLG = false;

        //無限ループ待ち中にページ遷移が発生したフラグ
        private bool pageTransitionFLG = false;

        public MainPageViewModel(INavigationService navigationService)
            : base(navigationService)
        {
            //画像を登録
            imageTouch.Value = ImageSource.FromResource("Xamarin_NFCSuicaReader.Image.touch.png");
        }

        public override async void OnNavigatedTo(INavigationParameters parameters)
        {
            //前回のNFC受信データのクリア
            INFCService NFCService = DependencyService.Get<INFCService>();
            NFCService.resDataClear();

            loopFLG = true;
            pageTransitionFLG = false;

            //非同期の無限ループでNFC受信待ち
            await Task.Run(() =>
            {
                while (loopFLG)
                {
                    //NFC通信確認
                    var dataCheck = NFCService.getresData();

                    if (dataCheck.Length != 0)
                    {
                        loopFLG = false;
                    }
                }
            });

            if (pageTransitionFLG == false)
            {
                //次のページへ遷移
                await NavigationService.NavigateAsync("DisplayPage");
            }
        }

        public override void OnNavigatedFrom(INavigationParameters parameters)
        {
            pageTransitionFLG = true;
            loopFLG = false;
        }

        public void OnSleep()
        {

        }

        public void OnResume()
        {
            pageTransitionFLG = true;
            loopFLG = false;

        }
    }
}
