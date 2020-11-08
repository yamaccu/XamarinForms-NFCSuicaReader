using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Nfc;
using Android.Nfc.Tech;
using Android.OS;
using Android.Widget;
using Prism;
using Prism.Ioc;
using System;
using Xamarin_NFCSuicaReader.Droid.Models;

namespace Xamarin_NFCSuicaReader.Droid
{
    [Activity(Theme = "@style/MainTheme",
              ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        //NFCAdapterメンバ変数登録
        private NfcAdapter nfcAdapter;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(savedInstanceState);

            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);
            LoadApplication(new App(new AndroidInitializer()));

            //NfcAdapterのインスタンス取得
            nfcAdapter = NfcAdapter.GetDefaultAdapter(this);
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        protected override void OnResume()
        {
            base.OnResume();

            //インテントフィルタの設定
            //タグディスパッチシステム参照、全部インテントフィルタしておく
            //https://developer.android.com/guide/topics/connectivity/nfc/nfc?hl=ja
            var tagDetected = new IntentFilter(NfcAdapter.ActionTagDiscovered);
            var ndefDetected = new IntentFilter(NfcAdapter.ActionNdefDiscovered);
            var techDetected = new IntentFilter(NfcAdapter.ActionTechDiscovered);

            //インテントの生成
            var filters = new[] { tagDetected, ndefDetected, techDetected };
            var intent = new Intent(this, this.GetType()).AddFlags(ActivityFlags.SingleTop);
            var pendingIntent = PendingIntent.GetActivity(this, 0, intent, 0);

            //NFC検出を実行
            nfcAdapter.EnableForegroundDispatch(this, pendingIntent, filters, null);
        }

        protected override void OnNewIntent(Intent intent)
        {
            NFCService nfcService = new NFCService();

            //Tag検出されたら処理を実行
            if (intent.Action == NfcAdapter.ActionTagDiscovered)
            {
                //検出データを抽出
                var tag = (Tag)intent.GetParcelableExtra(NfcAdapter.ExtraTag);
//                var id = intent.GetByteArrayExtra(NfcAdapter.ExtraId);
//                var rawTagMessages = intent.GetParcelableArrayExtra(NfcAdapter.ExtraTag);
//                var rawMessages = intent.GetParcelableArrayExtra(NfcAdapter.ExtraNdefMessages);

                //接続設定、FericaはNFCFタグ
                var nfcf = NfcF.Get(tag);

                try
                {
                    nfcf.Connect();

                    //Fericaの仕様：https://www.sony.co.jp/Products/felica/business/tech-support/st_usmnl.html
                    //鉄道系カードのシステムコード 0003

                    //Polling command
                    var packetSize = (byte)0x06;
                    var commandCode = (byte)0x00;
                    var systemCode = new byte[] { 0x00, 0x03 };             
                    var requestCode = (byte)0x01;                   
                    var timeSlot = (byte)0x0f;

                    var pollingRequest = new byte[6];
                    pollingRequest[0] = packetSize;
                    pollingRequest[1] = commandCode;
                    pollingRequest[2] = systemCode[0];
                    pollingRequest[3] = systemCode[1];
                    pollingRequest[4] = requestCode;
                    pollingRequest[5] = timeSlot;


                    //Pollin commandの返信
                    var PoolingResponse = nfcf.Transceive(pollingRequest);

                    //IDm抽出
                    var IDm = new byte[8];
                    Array.Copy(PoolingResponse, 2, IDm, 0, 8);


                    //Service command for 属性情報
                    //サービスコード008bは属性情報　http://jennychan.web.fc2.com/format/suica.html#008B
                    commandCode = 0x02;
                    var serviceCodeNum = (byte)0x01;
                    var serviceCode = new byte[2] {0x8b,0x00 };
                    packetSize = (Convert.ToByte(serviceCodeNum * 2 + 11));
                                        
                    var serviceRequest = new byte[packetSize];
                    serviceRequest[0] = packetSize;
                    serviceRequest[1] = commandCode;
                    serviceRequest[2] = IDm[0];
                    serviceRequest[3] = IDm[1];
                    serviceRequest[4] = IDm[2];
                    serviceRequest[5] = IDm[3];
                    serviceRequest[6] = IDm[4];
                    serviceRequest[7] = IDm[5];
                    serviceRequest[8] = IDm[6];
                    serviceRequest[9] = IDm[7];
                    serviceRequest[10] = serviceCodeNum;
                    serviceRequest[11] = serviceCode[0];
                    serviceRequest[12] = serviceCode[1];

                    //Service commandの返信、読み捨て
                    var rawServiceRequestResponse = nfcf.Transceive(serviceRequest);


                    //サービスコマンド008bに対するRead without encryptionコマンド
                    commandCode = 0x06;
                    var blockNum = (byte)0x01;
                    var blockList = new byte[2] { 0x80, 0x00 };
                    packetSize = Convert.ToByte(serviceCodeNum * 2 + blockNum * 2 + 12);

                    var readWithoutEncryption = new byte[packetSize];

                    readWithoutEncryption[0] = packetSize;
                    readWithoutEncryption[1] = commandCode;
                    readWithoutEncryption[2] = IDm[0];
                    readWithoutEncryption[3] = IDm[1];
                    readWithoutEncryption[4] = IDm[2];
                    readWithoutEncryption[5] = IDm[3];
                    readWithoutEncryption[6] = IDm[4];
                    readWithoutEncryption[7] = IDm[5];
                    readWithoutEncryption[8] = IDm[6];
                    readWithoutEncryption[9] = IDm[7];
                    readWithoutEncryption[10] = serviceCodeNum;
                    readWithoutEncryption[11] = serviceCode[0];
                    readWithoutEncryption[12] = serviceCode[1];
                    readWithoutEncryption[13] = blockNum;
                    readWithoutEncryption[14] = blockList[0];
                    readWithoutEncryption[15] = blockList[1];

                    //Read without encryptionコマンドの返信、返信データは後ろの16byte分
                    //16byteの内訳　http://jennychan.web.fc2.com/format/suica.html#008B
                    var data = nfcf.Transceive(readWithoutEncryption);

                    //受信データをresDataにセット
                    nfcService.setresData(data, data.Length);

                    nfcf.Close();
                }
                catch
                {
                    Toast.MakeText(Application.Context, "失敗、もう一度タッチしてください", ToastLength.Long).Show();
                    return;
                }
            }
        }
    }

    public class AndroidInitializer : IPlatformInitializer
    {
        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // Register any platform specific implementations
        }
    }
}

