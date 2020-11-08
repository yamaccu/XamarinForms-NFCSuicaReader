using System;
using Xamarin.Forms;
using Xamarin_NFCSuicaReader.Droid.Models;
using Xamarin_NFCSuicaReader.Models;

//プラットフォーム側の実装をDependencyServiceに登録
[assembly: Dependency(typeof(NFCService))]

namespace Xamarin_NFCSuicaReader.Droid.Models
{
    class NFCService : INFCService
    {
        //NFC受信データ
        private static byte[] resData = new byte[0];

        //NFC受信データをネイティブ側からセット
        public void setresData(byte[] data, int length)
        {
            Array.Resize(ref resData, length);
            resData = data;
        }

        //NFC受信データをPCL側から取得
        public byte[] getresData()
        {
            return resData;
        }

        //古いNFC受信データをクリア
        public void resDataClear()
        {
            Array.Resize(ref resData, 0);
        }
    }
}