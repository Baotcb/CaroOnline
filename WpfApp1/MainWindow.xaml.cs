using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public OptionDetail.EStatus currentTurn;
        public event PropertyChangedEventHandler PropertyChanged;
        ObservableCollection<OptionDetail> _Maps;
        private ObservableCollection<string> _chatMessages;
        public ObservableCollection<string> ChatMessages
        {
            get => _chatMessages;
            set
            {
                _chatMessages = value;
                OnPropertyChanged();
            }
        }
        public ObservableCollection<OptionDetail> Maps
        {
            get => _Maps;
            set
            {
                _Maps = value;
                OnPropertyChanged();
            }
        }
        ObservableCollection<string> _Rooms;
        public ObservableCollection<string> Rooms
        {
            get => _Rooms;
            set
            {
                _Rooms = value;
                OnPropertyChanged();
            }
        }
     

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }
        String _Status;
        public String Status
        {
            get { return _Status; }
            set
            {
                _Status = value;
               OnPropertyChanged();
            }
        }
        HubConnection connection;
        String _idUser;
        public String IdUser
        {
            get { return _idUser; }
            set
            {
                _idUser = value;
              OnPropertyChanged();
            }
        }

        String _Desk;
        public String Desk
        {
            get { return _Desk; }
            set
            {
                _Desk = value;
                OnPropertyChanged();
            }
        }
        async void ConnectHub()
        {
             connection = new HubConnectionBuilder()
                .WithUrl("https://localhost:7186/chatHub")
                .Build();

            connection.On<int,int>("ClickAtPoint", (x,status) =>
            {
                currentTurn=(OptionDetail.EStatus)status;
          
                ClickAtPoint(Maps[x]);
            });
            connection.On<string>("UserJoin", (x) =>
            {
               IdUser ="Player :" + x;

            });
            connection.On<string>("CompetitorJoin", (x) =>
            {
                string mess= "Player " + x + " join game";
                MessageBox.Show(mess);
            });
            connection.On<string>("LeaveGame", (x) =>
            {
                string mess= "User " + x + " leave game";
                MessageBox.Show(mess);
            });
            connection.On<string>("GameFull", (x) =>
            {
                MessageBox.Show(x);
            });
            connection.On<string>("Notice", (x) =>
            {
                Desk = x;
            });
            connection.On<OptionDetail.EStatus>("ChangeTurn", (x) =>
            {
                currentTurn = x;
                UpdateStatus();

            });
            connection.On<List<String>>("Room", (x) =>
            {
                Rooms = new ObservableCollection<string>(x);
            });
            connection.On<string>("ReceiveMess", (x) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ChatMessages.Add(x);
                });
            });




            await connection.StartAsync();
            //await connection.SendAsync("JoinGame");
            await connection.SendAsync("LoadRoom");

        }

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;
            ChatMessages = new ObservableCollection<string>();
            FirstLoad();

        }
        void FirstLoad()
        {
            ReloadMap();
            
            UpdateStatus();
            ConnectHub();
        }
        void ReloadMap()
        {
            Maps = new ObservableCollection<OptionDetail>();
            for (int i = 0; i < 300; i++)
            {
                Maps.Add(new OptionDetail() { status = OptionDetail.EStatus.None });
            }
            currentTurn = OptionDetail.EStatus.X;
            
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var data = (sender as Button).DataContext as OptionDetail;
            var index = Maps.IndexOf(data);
            Task.Run(async () =>
            {
                await connection.SendAsync("Click", index,currentTurn);
            });


        }
      

        void ClickAtPoint(OptionDetail data)
        {
            if (data.status == OptionDetail.EStatus.None)
            {
                data.status = currentTurn;

               // ChangeTurn();
                CheckWin(data);

            }

        }
        //private void ChangeTurn()
        //{
            
        //    currentTurn = currentTurn == OptionDetail.EStatus.X ? OptionDetail.EStatus.O : OptionDetail.EStatus.X;
        //    UpdateStatus();
        //}
        private void UpdateStatus()
        {
            Status = currentTurn == OptionDetail.EStatus.X ? "Lượt của X" : "Lượt của O";
        }
        private void CheckWinColumn(OptionDetail data)
        {
            var clickIndex = Maps.IndexOf(data);

            //check column
            int countLine = 1;
            //check top
            var topIndex = clickIndex;
            while (true)
            {
                topIndex -= 10;
                if (topIndex > 0)
                {
                    if (Maps[topIndex].status == data.status) countLine++;
                    else break;
                }
                else break;
            }
            //check bottom
            var bottomIndex = clickIndex;
            while (true)
            {
                bottomIndex += 10;
                if (bottomIndex < Maps.Count)
                {
                    if (Maps[bottomIndex].status == data.status) countLine++;
                    else break;
                }
                else break;
            }
            if (countLine >= 5)
            {
                var win = currentTurn == OptionDetail.EStatus.X ? "O" : "X";
                MessageBox.Show(@$"{win} Win");
                ReloadMap();

            }


        }
        private void CheckWinRow(OptionDetail data)
        {
            var clickIndex = Maps.IndexOf(data);

            // Check row
            int countLine = 1;
            // Check left
            var leftIndex = clickIndex;
            while (true)
            {
                leftIndex -= 1;
                if (leftIndex % 10 != 9 && leftIndex >= 0)
                {
                    if (Maps[leftIndex].status == data.status) countLine++;
                    else break;
                }
                else break;
            }
            // Check right
            var rightIndex = clickIndex;
            while (true)
            {
                rightIndex += 1;
                if (rightIndex % 10 != 0 && rightIndex < Maps.Count)
                {
                    if (Maps[rightIndex].status == data.status) countLine++;
                    else break;
                }
                else break;
            }
            if (countLine >= 5)
            {
                var win = currentTurn == OptionDetail.EStatus.X ? "O" : "X";
                MessageBox.Show(@$"{win} Win");
                ReloadMap();
            }
        }
        private void CheckWinMainDiagonal(OptionDetail data)
        {
            var clickIndex = Maps.IndexOf(data);

            // Check main diagonal
            int countLine = 1;
            // Check top-left
            var topLeftIndex = clickIndex;
            while (true)
            {
                topLeftIndex -= 11;
                if (topLeftIndex % 10 != 9 && topLeftIndex >= 0)
                {
                    if (Maps[topLeftIndex].status == data.status) countLine++;
                    else break;
                }
                else break;
            }
            // Check bottom-right
            var bottomRightIndex = clickIndex;
            while (true)
            {
                bottomRightIndex += 11;
                if (bottomRightIndex % 10 != 0 && bottomRightIndex < Maps.Count)
                {
                    if (Maps[bottomRightIndex].status == data.status) countLine++;
                    else break;
                }
                else break;
            }
            if (countLine >= 5)
            {
                var win = currentTurn == OptionDetail.EStatus.X ? "O" : "X";
                MessageBox.Show(@$"{win} Win");
                ReloadMap();
            }
        }
        private void CheckWinAntiDiagonal(OptionDetail data)
        {
            var clickIndex = Maps.IndexOf(data);

            // Check anti-diagonal
            int countLine = 1;
            // Check top-right
            var topRightIndex = clickIndex;
            while (true)
            {
                topRightIndex -= 9;
                if (topRightIndex % 10 != 0 && topRightIndex >= 0)
                {
                    if (Maps[topRightIndex].status == data.status) countLine++;
                    else break;
                }
                else break;
            }
            // Check bottom-left
            var bottomLeftIndex = clickIndex;
            while (true)
            {
                bottomLeftIndex += 9;
                if (bottomLeftIndex % 10 != 9 && bottomLeftIndex < Maps.Count)
                {
                    if (Maps[bottomLeftIndex].status == data.status) countLine++;
                    else break;
                }
                else break;
            }
            if (countLine >= 5)
            {
                var win = currentTurn == OptionDetail.EStatus.X ? "O" : "X";
                MessageBox.Show(@$"{win} Win");
                ReloadMap();
            }
        }



        private void CheckWin(OptionDetail data)
        {
            CheckWinColumn(data);
            CheckWinRow(data);
            CheckWinMainDiagonal(data);
            CheckWinAntiDiagonal(data);

        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var data = (sender as Button).Content;
            Task.Run(async () =>
            {
                await connection.SendAsync("JoinGame", data);
            });
            Panel.SetZIndex(overlayGrid, 0);
            Panel.SetZIndex(baseGrid, 1);
        }

   

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            Task.Run(async () =>
            {
                await connection.SendAsync("Disconnect");
            });
        }

        private async void Button_Click_2(object sender, RoutedEventArgs e)
        {
            if (connection.State == HubConnectionState.Connected)
            {
                try
                {
                    await connection.SendAsync("SendMessage", ChatInputTextBox.Text);
                    ChatInputTextBox.Clear();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error sending message: {ex.Message}");
                }
            }
            else
            {
                MessageBox.Show("Connection is not established.");
            }
        }

    }

    public class  OptionDetail: INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        ObservableCollection<int> _Maps;
        ObservableCollection<int> Maps
        {
            get => _Maps;
            set
            {
                _Maps = value;
                OnPropertyChanged();
            }
        }
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }
        EStatus _Status;
        public EStatus status
        {
            get => _Status;
            set
            {
                _Status = value;
                OnPropertyChanged();
                Content =_Status== EStatus.None ? "" : _Status== EStatus.X ? "X" : "O";
            }
        }
        String _Content;
        public String Content
        {
            get => _Content;
            set
            {
                _Content = value;
                OnPropertyChanged();
            }
        }
        
        public enum EStatus
        {
            None,X,O
        }

    }
}