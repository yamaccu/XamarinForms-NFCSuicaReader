namespace Xamarin_NFCSuicaReader.Models
{
    public interface INFCService
    {
        //NFC受信データをネイティブ側でセットする
        void setresData(byte[] data, int length);

        //NFC受信データをPCL側から取得する
        byte[] getresData();

        //古いNFC受信データをクリア
        void resDataClear();
    }
}
