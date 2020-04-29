using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Virtual_Scanner
{
    public class ChromeSocketComms : IDisposable
    {
        private TextBox LCDLine1;
        private TextBox LCDLine2;
        private TextBlock StatusText;

        private string strLine1Text = "                ";
        private string strLine2Text = "                ";
        private string strStatusText = "";

        private IPAddress ipAddress = null;
        private int PortNumber;

        private static IPHostEntry ipHostInfo;
        SocketAsyncEventArgs AcceptArg, RecieveArg, SendArg, DisconnectArg;
        private static IPEndPoint ipEndPoint;
        private static Socket server_socket;
        private static Socket Client_Socket;
        public MainPage myMainPage;

        private byte[] buffer = new byte[1024];

        public event EventHandler SocketError;
        public event EventHandler RefreshDisplay;

        private int lastReceived = 0;
        private int runTime = 0;
        private DispatcherTimer Timmer;
        private static bool bufferProcessing = false;
        public StorageFolder storageFolder = null;

        protected virtual void OnSocketError(EventArgs e)
        {
            EventHandler handler = SocketError;
            handler?.Invoke(this, e);
        }

        protected virtual void OnRefreshDisplay(EventArgs e)
        {
            EventHandler handler = RefreshDisplay;
            handler?.Invoke(this, e);
        }


        public void CloseChromeSocket()
        {
            Timmer.Stop();
            Dispose();
        }

        public ChromeSocketComms(IPAddress ipAddress, int portNumber,MainPage _myParent)
        {
            this.ipAddress = ipAddress;
            this.PortNumber = portNumber;
            ipEndPoint = new IPEndPoint(ipAddress, PortNumber);
            myMainPage = _myParent;
            StartTimmer();
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
        private void Reset()
        {
            SetLCDBackLight(false);
            Timmer.Stop();
            strStatusText = "Connection not responding. Restarting..";
            UpdateDisplay();
            OnSocketError(null);

        }

        bool SocketConnected(Socket s)
        {
            bool part1 = s.Poll(1000, SelectMode.SelectRead);
            bool part2 = (s.Available == 0);
            if (part1 && part2)
                return false;
            else
                return true;
        }

        private void Timmer_Tick(object sender, object e)
        {
            lastReceived++;
            Debug.WriteLine("Timer tick " + lastReceived.ToString());
            runTime++;
            if (lastReceived > 60 )
            {
                Reset();
            }

            if (Client_Socket != null & lastReceived > 5)
            {
                if (!SocketConnected(Client_Socket))
                {
                    Reset();
                }
            }

            if (runTime == 120)
            {
                Timmer.Stop();
                SendBarCode("GETTIME");
                Timmer.Start();
            }
            if ((runTime % 10) == 0) OnRefreshDisplay(null);

            if (runTime > 3600) runTime = 0;
        }

        public void Dispose()
        {
            try
            {
                Client_Socket.Close();
                Client_Socket = null;
            
            }
            catch (Exception E)
            {
                Debug.WriteLine(E.Message);
            }

            try
            {
                server_socket.Close();
                server_socket = null;

            }
            catch (Exception E)
            {
                Debug.WriteLine(E.Message);
            }

            try
            {
                Timmer.Stop();
                Timmer.Tick -= Timmer_Tick;
                Timmer = null;

            }
            catch (Exception E)
            {
                Debug.WriteLine(E.Message);
            }




            //throw new NotImplementedException();
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

        public void Connect()
        {
            if (server_socket == null)
            {
                server_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                server_socket.NoDelay = true;
                server_socket.Bind(ipEndPoint);
            }
            server_socket.Listen(5000);
            AcceptArg = new SocketAsyncEventArgs();
            AcceptArg.Completed += AcceptArg_Completed;
            server_socket.AcceptAsync(AcceptArg);

            strStatusText = "Listening on " + ipAddress.ToString() + " Port:" + PortNumber.ToString();
            this.UpdateDisplay();

        }

        private void DisconnectArg_Completed(object sender, SocketAsyncEventArgs e)
        {
            strStatusText = "Client Disconnected";
            UpdateDisplay();

        }
        private void AcceptArg_Completed(object sender, SocketAsyncEventArgs e)
        {
            Client_Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Client_Socket = e.AcceptSocket;
            strStatusText = "Remote client connected " + Client_Socket.RemoteEndPoint;
            UpdateDisplay();
            ListenForData();
        }



        private void ListenForData()
        {
            try
            {
                buffer = new byte[1024];
                RecieveArg = new SocketAsyncEventArgs();
                RecieveArg.UserToken = server_socket;
                RecieveArg.RemoteEndPoint = Client_Socket.RemoteEndPoint;
                RecieveArg.SetBuffer(buffer, 0, buffer.Length);
                RecieveArg.Completed += RecieveArg_Completed;
                Client_Socket.ReceiveAsync(RecieveArg);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                OnSocketError(null);
            }
        }



        private async void RecieveArg_Completed(object sender, SocketAsyncEventArgs e)
        {
            try
            {
                while (bufferProcessing)
                {

                }

                byte[] recBuf = new byte[e.BytesTransferred];
                Array.Copy(buffer, recBuf, e.BytesTransferred);

                Client_Socket.ReceiveAsync(RecieveArg);
                ProcessBuffer(recBuf);
            }
            catch (Exception E)
            {
                Debug.WriteLine(E.Message);
                OnSocketError(null);
            }

        }

        public void SendBarCode(string bar_code)
        {
            try
            {
                byte[] data = Encoding.ASCII.GetBytes(bar_code);
                
                //Array.Resize(ref data, data.Length + 1);
                strStatusText = "Sending bar code " + bar_code;
                
                if (SocketConnected(Client_Socket))
                {
                    Client_Socket.Send(data);

                    strStatusText = "Remote client connected " + Client_Socket.RemoteEndPoint;
                }
                else {
                    strStatusText = server_socket.LocalEndPoint.ToString() + " : Remote client not available";
                    OnSocketError(null);
                }

                UpdateDisplay();

            }
            catch (SocketException e)
            {
                Console.WriteLine(e.Message);
                OnSocketError(null);
            }
        }


        private void ProcessBuffer(byte[] recBuf)
        {
            bufferProcessing = true;
            string ReceivedText = "";
            

            try
            {
                if (recBuf.Length < 5)
                {
                    byte[] newRecBuf = new byte[5] { 0, 0, 0, 0, 0 };
                    Array.Copy(recBuf, newRecBuf, recBuf.Length);
                    recBuf = newRecBuf;
                }
                if (recBuf[0] == 20 & recBuf[1] == 20 & recBuf[2] == 20)
                {
                    WriteStringToFile("time.txt", CleanString(Encoding.ASCII.GetString(recBuf)));
                }
                else
                if (recBuf[0] == 12 & recBuf[1] == 14 & recBuf[2] == 30)
                {
                    SetLCDBackLight(true);
                }
                else
                if (recBuf[0] == 12 & recBuf[1] == 14 & recBuf[2] == 10 & recBuf[3] == 15 & recBuf[4] == 50)
                {
                    SetLCDBackLight(false);

                }
                else
                {


                    ReceivedText = Encoding.ASCII.GetString(recBuf);


                    string content = "";
                    
                    byte[] ClearScreen = new byte[7] { 12, 14, 20, 22, 255, 1, 11 };
                    
                    if (GetByteArraySubIndex(recBuf, ClearScreen) >= 0)
                    {
                        strLine1Text = "                ";
                        strLine2Text = "                ";
                    }
                    
                    
                    if (strLine1Text.Length < 16) strLine1Text += "".PadRight(16);

                    
                    if (recBuf[0] == 4 & recBuf[1] == 17)
                    {
                        if (buffer[3] == 1)
                        {
                            strLine2Text = ReceivedText.Substring(4, ReceivedText.Length - 4);
                        }
                        else
                        {
                            int insertPos = recBuf[2];
                            string NewString = CleanString(Encoding.ASCII.GetString(recBuf, 4, recBuf.Length - 4));
                            strLine1Text = strLine1Text.Substring(0, insertPos) + NewString + strLine1Text.Substring(insertPos + NewString.Length, strLine1Text.Length - (insertPos + NewString.Length));
                        }
                    }
                    else
                    {
                        content = CleanString(Encoding.ASCII.GetString(recBuf, 0, recBuf.Length));
                        if (content!="") {

                            var x = content.Split(Environment.NewLine);
                            strLine1Text = x[0];
                            if (x.Length > 1) strLine2Text = x[1]; else strLine2Text = "                ";
                            
                        }

                    }
                }
            }
            catch (Exception ReadError)
            {
                Console.WriteLine(ReadError.Message);
                if (recBuf.Length > 0) strLine1Text = Encoding.ASCII.GetString(recBuf, 0, recBuf.Length);
            }
            finally
            {
                lastReceived = 0;
                bufferProcessing = false;
                UpdateDisplay();
            }

        }

        private int GetByteArraySubIndex(byte[] source, byte[] search_array)
        {
            int result = -1;
            try
            {

                for (int i = 0; i < source.Length - (search_array.Length - 1); i++)
                {
                    bool found = true;
                    for (int j = 0; j < search_array.Length; j++)
                    {
                        found = found & source[i + j] == search_array[j];
                        if (!found) break;
                    }

                    if (found)
                    {
                        result = i;
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return result;
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
                else if (thisChar=="\u0011")
                {
                    StrOut += Environment.NewLine;
                }
            }

            return StrOut;
        }

        private async void WriteStringToFile(string file_name, string data)
        {
            try
            {
                Windows.Storage.StorageFile ticketsFile = await storageFolder.CreateFileAsync(file_name, Windows.Storage.CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteTextAsync(ticketsFile, data);
            }
            catch (Exception E)
            {
                Debug.WriteLine(E.Message);
            }

        }
    }
 
}
