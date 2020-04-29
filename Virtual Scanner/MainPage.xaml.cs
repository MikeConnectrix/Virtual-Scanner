using System;
using System.Net;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.Storage;
using Windows.UI.ViewManagement;



// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Virtual_Scanner
{

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private ChromeSocketComms socket = null;
        IPAddress ipAddress = null;
        private int server_port = 8899;
        string file_log_entry = "";

        public MainPage()
        {
            this.InitializeComponent();
            WriteLogEntryToFile("System Starting");

            IPHostEntry ipHost = Dns.GetHostEntry("");
            ipAddress = ipHost.AddressList.Where(p => p.ToString().StartsWith("172")).FirstOrDefault();
            SetUpSocket();
            txtBarcode.Focus(FocusState.Programmatic);
            InputPane.GetForCurrentView().TryHide();
        }
        
        
        private void txtBarcode_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                string bar_code = txtBarcode.Text;
                txtBarcode.Text = "";
               
                WriteBarcodeToFile(bar_code);
                socket.SendBarCode(bar_code);

                if (bar_code.ToUpper().Equals("REINITIALISE"))
                {
                    WriteLogEntryToFile("User reset system");
                    SetUpSocket();
                }

                txtBarcode.Focus(FocusState.Programmatic);
            }

            
        }

        private void SetUpSocket()
        {
            WriteLogEntryToFile("Listening on IP Address " + ipAddress.ToString() + " Port:" + server_port);

            if (socket != null) socket.Dispose();

            socket = new ChromeSocketComms(ipAddress, server_port,this);
            socket.SetLCDDisplay(tbLCDLine1, tbLCDLine2, tbStatus);
            socket.storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            socket.SocketError += Socket_SocketError;
            socket.RefreshDisplay += Socket_RefreshDisplay;
            socket.Connect();

        }

        private void Socket_RefreshDisplay(object sender, EventArgs e)
        {
            try
            {

                txtBarcode.Focus(FocusState.Programmatic);
            }
            catch (Exception E)
            {
                WriteLogEntryToFile("Error setting focus on txtBarcode Input.");
            }
        }

        private void Socket_SocketError(object sender, EventArgs e)
        {
            WriteLogEntryToFile("Socket Error - Resetting comms");
            SetUpSocket();
        }

        private async void WriteBarcodeToFile(string bar_code)
        {
            try
            {
                string file_name = DateTime.Now.ToString("yyyyMMddHH") + "-SCANDATA.xml";
                Windows.Storage.StorageFolder storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
                Windows.Storage.StorageFile ticketsFile = await storageFolder.CreateFileAsync(file_name, Windows.Storage.CreationCollisionOption.OpenIfExists);
                await FileIO.AppendTextAsync(ticketsFile, @"<scan scan_date=""" + DateTime.Now.ToString("dd/MMM/yyyy HH:MM:ss:ff") + @""" scan_data=""" + bar_code + @""" />" + "\n");
            }
            catch(Exception E)
            {
                WriteLogEntryToFile(E.Message);
            }

        }

        private async void WriteLogEntryToFile(string log_entry)
        {
            file_log_entry+= @"<log log_date=""" + DateTime.Now.ToString("dd/MMM/yyyy HH:MM:ss:ff") + @""" log_data=""" + log_entry + @""" />" + "\n";

            try
            {
                string file_name = DateTime.Now.ToString("yyyyMMdd") + "-LOG.xml";
                Windows.Storage.StorageFolder storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
                Windows.Storage.StorageFile ticketsFile = await storageFolder.CreateFileAsync(file_name, Windows.Storage.CreationCollisionOption.OpenIfExists);
                await FileIO.AppendTextAsync(ticketsFile, file_log_entry);
                file_log_entry = "";
            }
            catch (Exception E)
            {
                file_log_entry += @"<log log_date=""" + DateTime.Now.ToString("dd/MMM/yyyy HH:MM:ss:ff") + @""" log_data=""" + E.Message + @""" />" + "\n";
            }

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            socket.CloseChromeSocket();

            this.Frame.Navigate(typeof(GUIPage));
        }
    }
}
