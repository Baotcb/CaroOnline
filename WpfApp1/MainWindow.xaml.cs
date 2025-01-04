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
   

        private ObservableCollection<ChatMessage> _chatMessages;
        public ObservableCollection<ChatMessage> ChatMessages
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
        ObservableCollection<Room> _Rooms;
        public ObservableCollection<Room> Rooms
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
        int _xWin;
        int _yWin;
        public int XWin
        {
            get { return _xWin; }
            set
            {
                _xWin = value;
                OnPropertyChanged();
            }
        }
        public int YWin
        {
            get { return _yWin; }
            set
            {
                _yWin = value;
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
                Application.Current.Dispatcher.Invoke(() =>
                {
                    IdUser = "Player :" + x;
                    ChatMessages = new ObservableCollection<ChatMessage>();
                    Luot.Foreground = Status == "X" ? Brushes.Red : Brushes.Blue;
                    Panel.SetZIndex(overlayGrid, 0);
                    Panel.SetZIndex(baseGrid, 1);
                    XWin = 0;
                    YWin = 0;
                });
            });

            connection.On<string>("CompetitorJoin", (x) =>
            {
                string mess= "Player " + x + " join game";
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ChatMessages.Add(new ChatMessage
                    {
                        Content = mess,
                        Status = "None"
                    });
                });
            });
            connection.On<string>("LeaveGame", (x) =>
            {
                string mess= "User " + x + " leave game";
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ChatMessages.Add(new ChatMessage
                    {
                        Content = mess,
                        Status = "None"
                    });
                    ReloadMap();
                    XWin = 0;
                    YWin = 0;
                });
            });
            connection.On<string>("GameFull", (x) =>
            {
                MessageBox.Show(x);
            });
            connection.On<string>("Notice", (x) =>
            {
               
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Desk = x;
                    txtDesk.Foreground= Desk.Contains("X") ? Brushes.Red : Brushes.Blue;
                });
            });
            connection.On<OptionDetail.EStatus>("ChangeTurn", (x) =>
            {
                currentTurn = x;
                UpdateStatus();

            });
            connection.On<List<Room>>("Room", (x) =>
            {
                Rooms = new ObservableCollection<Room>(x);
            });
            connection.On("AutoDisConnect", () =>
            {
                MessageBox.Show("Auto Disconnected because competitor is disconnect");
                Panel.SetZIndex(overlayGrid, 1);
                Panel.SetZIndex(baseGrid, 0);
            });
            connection.On<string, string>("ReceiveMess", (x, y) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var status = Desk.Contains("X")
                        ? (y == "X" ? "Red Right" : "Blue Left")
                        : (y == "X" ? "Red Left" : "Blue Right");

                    ChatMessages.Add(new ChatMessage
                    {
                        Content = x,
                        Status = status
                    });
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
            Rooms = new ObservableCollection<Room>();
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
           if (data.Content != "")
            {
                return;
            }
            var index = Maps.IndexOf(data);
            Task.Run(async () =>
            {
                await connection.SendAsync("Click", index, currentTurn);
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
            Application.Current.Dispatcher.Invoke(() =>
            {
                Status = currentTurn == OptionDetail.EStatus.X ? "X" : "O";
                Luot.Foreground = Status == "X" ? Brushes.Red : Brushes.Blue;
            });
        }
        private void CheckWinColumn(OptionDetail data)
        {
            var clickIndex = Maps.IndexOf(data);

            // Check column
            int countLine = 1;
            // Check top
            var topIndex = clickIndex;
            while (true)
            {
                topIndex -= 15;
                if (topIndex >= 0)
                {
                    if (Maps[topIndex].status == data.status) countLine++;
                    else break;
                }
                else break;
            }
            // Check bottom
            var bottomIndex = clickIndex;
            while (true)
            {
                bottomIndex += 15;
                if (bottomIndex < Maps.Count)
                {
                    if (Maps[bottomIndex].status == data.status) countLine++;
                    else break;
                }
                else break;
            }
            if (countLine >= 5)
            {
                var win = currentTurn == OptionDetail.EStatus.X ? "X" : "O";
                MessageBox.Show(@$"{win} Win");
                if (win == "X")
                {
                    XWin++;
                }
                else
                {
                    YWin++;
                }
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
                if (leftIndex % 15 != 14 && leftIndex >= 0)
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
                if (rightIndex % 15 != 0 && rightIndex < Maps.Count)
                {
                    if (Maps[rightIndex].status == data.status) countLine++;
                    else break;
                }
                else break;
            }
            if (countLine >= 5)
            {
                var win = currentTurn == OptionDetail.EStatus.X ? "X" : "O";
                MessageBox.Show(@$"{win} Win");
                if (win == "X")
                {
                    XWin++;
                }
                else
                {
                    YWin++;
                }
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
                topLeftIndex -= 16;
                if (topLeftIndex % 15 != 14 && topLeftIndex >= 0)
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
                bottomRightIndex += 16;
                if (bottomRightIndex % 15 != 0 && bottomRightIndex < Maps.Count)
                {
                    if (Maps[bottomRightIndex].status == data.status) countLine++;
                    else break;
                }
                else break;
            }
            if (countLine >= 5)
            {
                var win = currentTurn == OptionDetail.EStatus.X ? "X" : "O";
                MessageBox.Show(@$"{win} Win");
                if (win == "X")
                {
                    XWin++;
                }
                else
                {
                    YWin++;
                }
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
                topRightIndex -= 14;
                if (topRightIndex % 15 != 0 && topRightIndex >= 0)
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
                bottomLeftIndex += 14;
                if (bottomLeftIndex % 15 != 14 && bottomLeftIndex < Maps.Count)
                {
                    if (Maps[bottomLeftIndex].status == data.status) countLine++;
                    else break;
                }
                else break;
            }
            if (countLine >= 5)
            {
                var win = currentTurn == OptionDetail.EStatus.X ? "X" : "O";
                MessageBox.Show(@$"{win} Win");
                if (win == "X")
                {
                    XWin++;
                }
                else
                {
                    YWin++;
                }
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
            var button = sender as Button;
            if (button != null)
            {
                var stackPanel = button.Content as StackPanel;
                if (stackPanel != null)
                {
                    var txtRoomName = stackPanel.Children.OfType<TextBlock>().FirstOrDefault();
                    if (txtRoomName != null)
                    {
                        string roomName = txtRoomName.Text;
                        Task.Run(async () =>
                        {
                            await connection.SendAsync("JoinGame", roomName);
                        });
                    }
                }
            }
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
                    await connection.SendAsync("SendMessage", ChatInputTextBox.Text,_Desk);
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
        private async void CreateBtn_Click(object sender, RoutedEventArgs e)
        {
            string roomName = RoomNameTextBox.Text;
            if (Rooms.Where(p => p.RoomName== roomName).ToList().Count()== 1 )
            {
                MessageBox.Show("Room are exist , plese choose another name");
                return;
            }
            if (!string.IsNullOrEmpty(roomName))
            {
                RoomNameTextBox.Clear();
                await connection.SendAsync("CreateRoom", roomName);
            }
            else
            {
                MessageBox.Show("Please enter a room name.");
            }
        }

        private async void FindBtn_Click(object sender, RoutedEventArgs e)
        {
            await connection.SendAsync("Matchmake");
        }

        private void btnQuit_Click(object sender, RoutedEventArgs e)
        {
            Panel.SetZIndex(overlayGrid, 1);
            Panel.SetZIndex(baseGrid, 0);
            Task.Run(async () =>
            {
                await connection.SendAsync("Disconnect");
            });

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
    public class ChatMessage : INotifyPropertyChanged
    {
        private string _content;
        private string _status;

        public string Content
        {
            get => _content;
            set { _content = value; OnPropertyChanged(); }
        }

        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    public class Room : INotifyPropertyChanged
    {
        protected string _roomName;
        protected int _count;
        public string RoomName
        {
            get => _roomName;
            set { _roomName = value; OnPropertyChanged(); }
        }
        public int Count
        {
            get => _count;
            set { _count = value; OnPropertyChanged(); }

        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }


}