using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using System.Data.SqlClient;

namespace Virtual_Scanner
{
    public class ChromeDBComms : IDisposable
    {

        private TextBox LCDLine1;
        private TextBox LCDLine2;
        private TextBlock StatusText;

        private string strLine1Text = "                ";
        private string strLine2Text = "                ";
        private string strStatusText = "";
        public StorageFolder storageFolder = null;
        private string connectionString = "";



        public MainPage myMainPage;
        private DispatcherTimer Timmer;
        private int lastSweep=0;
        static TimeSpan _offset = new TimeSpan(0, 0, 0);

        private SolidColorBrush GetSolidColorBrush(string hex)
        {
            hex = hex.Replace("#", string.Empty);
            byte a = (byte)(Convert.ToUInt32(hex.Substring(0, 2), 16));
            byte r = (byte)(Convert.ToUInt32(hex.Substring(2, 2), 16));
            byte g = (byte)(Convert.ToUInt32(hex.Substring(4, 2), 16));
            byte b = (byte)(Convert.ToUInt32(hex.Substring(6, 2), 16));
            SolidColorBrush myBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(a, r, g, b));
            return myBrush;
        }

        private async void SetLCDBackLight(bool LightOn)
        {
            try
            {

                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                () =>
                {
                    var BackColour = GetSolidColorBrush("#FF0D0D0F");
                    var ForeColour = GetSolidColorBrush("#FF707070");
                    if (LightOn)
                    {
                        BackColour = GetSolidColorBrush("#FF9835E0");
                        ForeColour = GetSolidColorBrush("#FFFFFFFF");
                    }

                    this.LCDLine1.Background = BackColour;
                    this.LCDLine2.Background = BackColour;
                    this.LCDLine1.Foreground = ForeColour;
                    this.LCDLine2.Foreground = ForeColour;

                }
                ).AsTask();

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public void SetLCDDisplay(TextBox line1, TextBox line2, TextBlock statusText)
        {
            this.LCDLine1 = line1;
            this.LCDLine2 = line2;
            this.StatusText = statusText;
            this.strLine1Text = "                ";
            this.strLine2Text = "                ";
            this.UpdateDisplay();
            SetLCDBackLight(false);



        }
        public static TimeSpan CurrentOffset //Doesn't have to be public, it is for me because I'm presenting it on the UI for my information
        {
            get { return _offset; }
            private set { _offset = value; }
        }

        public static DateTime Now
        {
            get
            {
                return DateTime.Now - CurrentOffset;
            }
        }

        public ChromeDBComms(string ConnectionString, MainPage _myParent)
        {
            this.connectionString = ConnectionString;
            myMainPage = _myParent;
            StartTimmer();
            SetTime();
        }

        static void UpdateOffset(DateTime currentCorrectTime) //May need to be public if you're getting the correct time outside of this class
        {
            CurrentOffset = DateTime.Now - currentCorrectTime;
            //Note that I'm getting network time which is in UTC, if you're getting local time use DateTime.Now instead of DateTime.UtcNow. 
        }

        private void SetTime()
        {
            using (SqlCommand cmd = new SqlCommand("Select GetDate() as DBServerTime", new SqlConnection(connectionString)))
            {
                cmd.Connection.Open();
                using (SqlDataReader rdr = cmd.ExecuteReader()){
                    while (rdr.Read())
                    {
                        UpdateOffset((DateTime)rdr["DBServerTime"]);
                    }
                    
                }
            }
               
        }
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        private async void StartTimmer()
        {
            await myMainPage.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                Timmer = new DispatcherTimer();
                Timmer.Interval = TimeSpan.FromSeconds(1);
                Timmer.Tick += Timmer_Tick;
                Timmer.Start();
            });
        }

        private void Timmer_Tick(object sender, object e)
        {
            lastSweep++;
            strLine1Text = Now.ToString();
            UpdateDisplay();
           
        }

        private async void UpdateDisplay()
        {
            try
            {

                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                () =>
                {
                    strLine1Text = CleanString(strLine1Text);
                    strLine2Text = CleanString(strLine2Text);
                    this.LCDLine1.Text = strLine1Text;
                    this.LCDLine2.Text = strLine2Text;
                    this.StatusText.Text = strStatusText;
                }
                ).AsTask();

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        string CleanString(string stringIn)
        {
            string StrOut = "";

            for (int i = 0; i < stringIn.Length; i++)
            {
                string thisChar = stringIn.Substring(i, 1);
                if (("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz1234567890-_+=!:;/. ").Contains(thisChar))
                {
                    StrOut += thisChar;
                }
                else if (thisChar == "\u0011")
                {
                    StrOut += Environment.NewLine;
                }
            }

            return StrOut;
        }

    }
}
