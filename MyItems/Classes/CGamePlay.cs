using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Collections;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Windows.Media;
using System.Linq;
//using System.Threading.Tasks;
using System.Media;
namespace BomberMan
{
    class CGamePlay
    {
        public CScale Scale;
        public CContainer Data;
        public CGameObject Player;
        public CControl Control;
        public double PlayerSped = 3.6;
        CSprites Sprites = new CSprites();
        System.Windows.Forms.Timer GameTimer = new System.Windows.Forms.Timer();
        public int TimerCounter = 0, Animation = 0, CounterStart = 5;
        CSocketInfo SInfo;

        List<CGameObject> PlayerBomb = new List<CGameObject>();

        public CGamePlay(CScale Scale, CSocketInfo SocketInfo)
        {
            this.Scale = Scale;
            Data = new CContainer(this.Scale.Size);
            this.SInfo = SocketInfo;
            Control = new CControl();
            Player = new CGameObject(Scale.Scale * 1, Scale.Scale * 1, 1, 1, "Player");
            Player.NamePlayer = SInfo.PlayerName;
            Data.Field[Player.iX][Player.iX].Add(Player);
            Data.Players.Add(Player.NamePlayer, Player);
            switch (SInfo.Type)
            {
                case "Server":
                    FieldFill(84);
                    Thread Thread = new Thread(Server_Start);
                    Thread.IsBackground = true;
                    Thread.Start();
                    Data.DeathCounter.Add(Player.NamePlayer, 0);
                    break;
                case "Client":
                    Client_Start();
                    break;
            }
            System.Threading.Thread.Sleep(500);
            DrawingAll();
            GameTimer.Interval = 25;
            GameTimer.Tick += new EventHandler(GameTimerTick);
            GameTimer.Start();
            Thread thread = new Thread(Thread_Objects_Animation);
            thread.IsBackground = true;
            thread.Start();
        }
        #region Server Logic
        void Server_Start()
        {
            Player.Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Player.Socket.Bind(new IPEndPoint(SInfo.IPAddress, SInfo.Port));
            Player.Socket.Listen(10);
            while (true)
            {
                Socket Client = Player.Socket.Accept();
                if (Data.Players.Count < 4)
                    ServerLogic(Client);
                else
                    Client.Send(StringToByte("ServerDenied:Много игроков, попробуйте зайти позже."));
            }
        }
        void Client_Start()
        {
            Player.Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Player.Socket.Connect(SInfo.IPAddress, SInfo.Port);
            ServerLogic(Player.Socket);

            Player.Socket.Send(StringToByte("PlayerConnect:" + SInfo.PlayerName));
        }
        void ServerLogic(Socket Client)
        {
            Thread Thread = new Thread(delegate(object obj)
            {
                string Message = "";
                Thread thread = obj as Thread;
                while (true)
                {
                    byte[] Buffer = new byte[1024];
                    int len = Client.Receive(Buffer);
                    Message += ByteToString(Buffer, len);
                    while (Message.Contains("<") && Message.Contains(">"))
                    {
                        int s = Message.IndexOf("<"), e = Message.IndexOf(">");
                        string Messenger = Message.Substring(s + 1, e - 1);
                        Message = Message.Remove(s, e + 1);
                        string[] arr = Messenger.Split(':');
                        bool SendAll = false;
                        switch (arr[0])
                        {
                            #region PlayerCloseGame
                            case "PlayerCloseGame"://Сервер и Клиент
                                SendAll = true;
                                {
                                    string name = arr[1], first = Data.Players.First().Key;
                                    CGameObject Gb = Data.Players[name];
                                    Data.RemoveF(Gb);
                                    DrawingDelete(Gb);
                                    Data.Players.Remove(name);
                                    Data.DeathCounter.Remove(name);
                                    if (SInfo.Type == "Server")
                                    {
                                        Gb.Socket.Shutdown(SocketShutdown.Both);
                                        Gb.Socket.Close();
                                        Send(Messenger);
                                        thread.Abort();
                                    }
                                    else if (name == first)
                                    {
                                        Client.Shutdown(SocketShutdown.Both);
                                        Client.Close();
                                        Player.Socket = null;
                                        Data.Main.Dispatcher.Invoke(new Action(delegate
                                        {
                                            Data.Main.Visibility = Visibility.Hidden;
                                            MessageBox.Show("Сревер покинул игру!", "Хост вышел из игры", MessageBoxButton.OK, MessageBoxImage.Error);
                                            Data.Main.Close();
                                        }));
                                        thread.Abort();
                                    }
                                }
                                break;
                            #endregion
                            #region PlayerMove
                            case "PlayerMove"://Сервер и Клиент
                                SendAll = true;
                                {
                                    double S = Convert.ToDouble(arr[1]);
                                    string[] mas = arr[2].Split(';');
                                    double v = 1;
                                    string name = mas[6];
                                    CGameObject Gb = Data.Players[name];
                                    if (Scale.Scale != (int)S)
                                        v = Convert.ToDouble(S / Scale.Scale);
                                    Gb.X = Convert.ToDouble(mas[0]) / v;
                                    Gb.Y = Convert.ToDouble(mas[1]) / v;
                                    int iX = Convert.ToInt32(mas[2]), iY = Convert.ToInt32(mas[3]);
                                    Gb.Direction = Convert.ToInt32(mas[4]);
                                    Gb.Sprite = Convert.ToInt32(mas[5]);
                                    if (iX != Gb.iX || iY != Gb.iY)
                                    {
                                        Data.RemoveF(Gb);
                                        Gb.iX = iX;
                                        Gb.iY = iY;
                                        Data.AddF(Gb);
                                    }
                                    Data.Main.Dispatcher.Invoke(new Action(delegate
                                    {
                                        Canvas.SetLeft(Gb.Fase, Gb.X);
                                        Canvas.SetTop(Gb.Fase, Gb.Y);
                                        Gb.Fase.Source = Sprites.Bitmap[Gb.Type + "_" + Gb.Direction][Gb.Sprite];
                                    }));
                                }
                                break;
                            #endregion
                            #region BombPlase
                            case "BombPlase"://Сервер и Клиент
                                SendAll = true;
                                {
                                    int iX = Convert.ToInt32(arr[1]), iY = Convert.ToInt32(arr[2]);
                                    Bomb_Plase(new CGameObject(Scale.Scale * iX, Scale.Scale * iY, iX, iY, "Bomb"));
                                }
                                break;
                            #endregion
                            #region PlayerReborn
                            case "PlayerReborn"://Сервер и Клиент
                                SendAll = true;
                                {
                                    CGameObject GB = Data.Players[arr[1]];
                                    GB.Type = "Enemy";
                                    GB.iX = Convert.ToInt32(arr[2]);
                                    GB.iY = Convert.ToInt32(arr[3]);
                                    GB.X = GB.iX * Scale.Scale;
                                    GB.Y = GB.iY * Scale.Scale;
                                    DrawingGameObject(GB);
                                    Data.AddF(GB);
                                }
                                break;
                            #endregion
                            #region PlayerDied
                            case "PlayerDied"://Сервер и Клиент
                                SendAll = true;
                                {
                                    CGameObject GB = Data.Players[arr[1]];
                                    GB.Type = "EnemyDied";
                                    GB.Sprite = 0;
                                    Data.RemoveF(GB);
                                    Data.Death_Players.Add(GB);
                                    Data.DeathCounter[GB.NamePlayer]++;
                                }
                                break;
                            #endregion
                            #region PlayerConnect
                            case "PlayerConnect"://Сервер
                                {
                                    int x = 1, y = 1;
                                    switch (Data.Players.Count)
                                    {
                                        case 1: x = 19; y = 1; break;
                                        case 2: x = 1; y = 19; break;
                                        case 3: x = 19; y = 19; break;
                                    }
                                    CGameObject Enemy = new CGameObject(Scale.Scale * x, Scale.Scale * y, x, y, "Enemy");
                                    Enemy.Socket = Client;
                                    string str = Enemy.NamePlayer = arr[1];
                                    int j = 0;
                                    while (Data.Players.ContainsKey(str))
                                        str = Enemy.NamePlayer + j++;
                                    Enemy.NamePlayer = str;
                                    DrawingGameObject(Enemy);
                                    Data.Players.Add(Enemy.NamePlayer, Enemy);
                                    Data.DeathCounter.Add(Enemy.NamePlayer, 0);
                                    Data.Field[Enemy.iX][Enemy.iY].Add(Enemy);
                                    str = "ServerAccept:" + Enemy.NamePlayer + ":" + Scale.Scale + ":";
                                    List<CGameObject> tmp = new List<CGameObject>();
                                    for (int X = 0; X < Data.Field.Length; X++)
                                        for (int Y = 0; Y < Data.Field[X].Length; Y++)
                                            for (int Z = 0; Z < Data.Field[X][Y].Count; Z++)
                                            {
                                                CGameObject gb = Data.Field[X][Y][Z];
                                                if (gb.Type != "Player" && gb.Type != "Enemy")
                                                    str += gb.X + ";" + gb.Y + ";" + gb.iX + ";" + gb.iY + ";" + gb.Direction + ";" + gb.Type + ";" + gb.NamePlayer + "@";
                                            }
                                    foreach (CGameObject gb in Data.Players.Values)
                                        str += gb.X + ";" + gb.Y + ";" + gb.iX + ";" + gb.iY + ";" + gb.Direction + ";" + gb.Type + ";" + gb.NamePlayer + "@";
                                    Client.Send(StringToByte(str));
                                    str = "NewPlayer:" + Scale.Scale + ":" + Enemy.X + ";" + Enemy.Y + ";" + Enemy.iX + ";" + Enemy.iY + ";" + Enemy.Direction + ";" + Enemy.Type + ";" + Enemy.NamePlayer;
                                    foreach (CGameObject Gb in Data.Players.Values)
                                        if (Gb.Socket != Player.Socket && Gb.Socket != Enemy.Socket)
                                            Gb.Socket.Send(StringToByte(str));
                                    str = "DeathCounter:";
                                    foreach (KeyValuePair<string, int> element in Data.DeathCounter)
                                        str += element.Key + ";" + element.Value + "@";
                                    Client.Send(StringToByte(str));
                                }
                                break;
                            #endregion
                            #region ServerDenied
                            case "ServerDenied"://Клиент
                                {
                                    Data.Main.Dispatcher.Invoke(new Action(delegate
                                    {
                                        Data.Main.Visibility = Visibility.Hidden;
                                        MessageBox.Show(arr[1]);
                                        Data.Main.Close();
                                    }));
                                }
                                break;
                            #endregion
                            #region ServerAccept
                            case "ServerAccept"://Клиент
                                {
                                    Data.Players.Remove(Player.NamePlayer);
                                    Data.RemoveF(Player);
                                    SInfo.PlayerName = arr[1];
                                    double S = Convert.ToDouble(arr[2]);
                                    double v = 1;
                                    if (Scale.Scale != (int)S)
                                        v = Convert.ToDouble(S / Scale.Scale);
                                    string[] Field = arr[3].Split('@');
                                    for (int f = 0; f < Field.Length; f++)
                                    {
                                        if (f >= Field.Length - 1)
                                            break;
                                        string[] mas = Field[f].Split(';');
                                        CGameObject g = new CGameObject();
                                        g.X = Convert.ToDouble(mas[0]) / v;
                                        g.Y = Convert.ToDouble(mas[1]) / v;
                                        g.iX = Convert.ToInt32(mas[2]);
                                        g.iY = Convert.ToInt32(mas[3]);
                                        g.Direction = Convert.ToInt32(mas[4]);
                                        g.Type = mas[5];
                                        g.NamePlayer = mas[6];
                                        switch (g.Type)
                                        {
                                            case "Player":
                                                g.Type = "Enemy";
                                                Data.Players.Add(g.NamePlayer, g);
                                                break;
                                            case "PlayerDied":
                                                Data.Players.Add(g.NamePlayer, g);
                                                break;
                                            case "Enemy":
                                                if (g.NamePlayer == SInfo.PlayerName)
                                                {
                                                    g.Type = "Player";
                                                    g.Socket = Player.Socket;
                                                    Player = g;
                                                }
                                                Data.Players.Add(g.NamePlayer, g);
                                                break;
                                            case "EnemyDied":
                                                if (g.NamePlayer == SInfo.PlayerName)
                                                {
                                                    g.Type = "Player";
                                                    g.Socket = Player.Socket;
                                                    Player = g;
                                                }
                                                Data.Players.Add(g.NamePlayer, g);
                                                break;
                                            case "Brick":
                                                Data.Bricks.Add(g);
                                                break;
                                            case "Bomb":
                                                Data.Bombs.Add(g);
                                                break;
                                        }
                                        Data.AddF(g);
                                    }
                                }
                                break;
                            #endregion
                            #region NewPlayer
                            case "NewPlayer"://Клиент
                                {
                                    string[] mas = arr[2].Split(';');
                                    double S = Convert.ToDouble(arr[1]);
                                    double v = 1;
                                    if (Scale.Scale != (int)S)
                                        v = Convert.ToDouble(S / Scale.Scale);
                                    CGameObject g = new CGameObject();
                                    g.X = Convert.ToDouble(mas[0]) / v;
                                    g.Y = Convert.ToDouble(mas[1]) / v;
                                    g.iX = Convert.ToInt32(mas[2]);
                                    g.iY = Convert.ToInt32(mas[3]);
                                    g.Direction = Convert.ToInt32(mas[4]);
                                    g.Type = mas[5];
                                    g.NamePlayer = mas[6];
                                    DrawingGameObject(g);
                                    Data.Players.Add(g.NamePlayer, g);
                                    Data.DeathCounter.Add(g.NamePlayer, 0);
                                    Data.AddF(g);
                                }
                                break;
                            #endregion
                            #region DeathCounter
                            case "DeathCounter"://Клиент
                                {
                                    string[] tmp = arr[1].Split('@');
                                    for (int j = 0; j < tmp.Length - 1; j++)
                                    {
                                        string[] mas = tmp[j].Split(';');
                                        if (!Data.DeathCounter.ContainsKey(mas[0]))
                                            Data.DeathCounter.Add(mas[0], Convert.ToInt32(mas[1]));
                                    }
                                }
                                break;
                            #endregion
                            #region GenerateBrick
                            case "GenerateBrick"://Клиент
                                {
                                    int scale = Scale.Scale;
                                    string[] Bricks = arr[1].Split('@');
                                    for (int i = 0; i < Bricks.Length - 1; i++)
                                    {
                                        string[] XY = Bricks[i].Split(';');
                                        int iX = Convert.ToInt32(XY[0]), iY = Convert.ToInt32(XY[1]);
                                        if (Data.Field[iX][iY].Count <= 0)
                                        {
                                            CGameObject Brick = new CGameObject(scale * iX, scale * iY, iX, iY, "Brick");
                                            Data.Bricks.Add(Brick);
                                            Data.AddF(Brick);
                                            DrawingGameObject(Brick);
                                        }
                                    }
                                }
                                break;
                            #endregion
                            #region Winer
                            case "Winer"://Клиент
                                {
                                    Winner(arr[1]);
                                }
                                break;
                            #endregion
                        }
                        if (SendAll && SInfo.Type == "Server")
                            foreach (CGameObject Obj in Data.Players.Values)
                                if (Obj.Socket != Player.Socket && Obj.Socket != Client)
                                    Obj.Socket.Send(StringToByte(Messenger));
                    }
                }
            });
            Thread.IsBackground = true;
            Thread.Start(Thread);
        }
        byte[] StringToByte(string str)
        {
            return Encoding.UTF8.GetBytes('<' + str + '>');
        }
        string ByteToString(byte[] Buffer, int len)
        {
            return Encoding.UTF8.GetString(Buffer, 0, len);
        }
        void Send(string str)
        {
            switch (SInfo.Type)
            {
                case "Server":
                    foreach (CGameObject Gb in Data.Players.Values)
                        if (Gb.Socket != Player.Socket)
                            Gb.Socket.Send(StringToByte(str));
                    break;
                case "Client":
                    if (Player.Socket != null)
                        Player.Socket.Send(StringToByte(str));
                    break;
            }
        }
        public void Close()
        {
            Send("PlayerCloseGame:" + Player.NamePlayer);
        }
        #endregion
        #region Game Logic
        void Winner(string name)
        {
            GameTimer.Stop();
            Data.Main.Dispatcher.Invoke(new Action(delegate
            {
                Data.Main.ShowScore(name);
            }));
        }
        void FieldFill(int Bricks)
        {
            if (Bricks >= 268)
                Bricks = 268;
            int Size = Scale.Size - 1, scale = Scale.Scale;
            for (int iX = 0; iX <= Size; iX++)
                for (int iY = 0; iY <= Size; iY++)
                    if (iX % 2 == 0 && iY % 2 == 0 || iX == 0 || iX == Size || iY == 0 || iY == Size)
                        Data.AddF(new CGameObject(scale * iX, scale * iY, iX, iY, "Wall"));
            Random Rand = new Random();
            for (int i = 0; i < Bricks; i++)
            {
                int iX = Rand.Next(0, Size), iY = Rand.Next(0, Size);
                do
                {
                    if (Data.Field[iX][iY].Count == 0 &&
                        ((iX != 1 || iY != 1) && (iX != 2 || iY != 1) && (iX != 1 || iY != 2)) && ((iX != 19 || iY != 19) && (iX != 19 || iY != 18) && (iX != 18 || iY != 19)) && ((iX != 19 || iY != 1) && (iX != 19 || iY != 2) && (iX != 18 || iY != 1)) && ((iX != 1 || iY != 19) && (iX != 2 || iY != 19) && (iX != 1 || iY != 18)))
                    {
                        CGameObject Brick = new CGameObject(scale * iX, scale * iY, iX, iY, "Brick");
                        Data.Bricks.Add(Brick);
                        Data.AddF(Brick);
                        break;
                    }
                    else
                    {
                        iX = Rand.Next(0, Size);
                        iY = Rand.Next(0, Size);
                    }
                }
                while (true);
            }
        }
        void DrawingAll()
        {
            List<UIElement> TBrik_Players = new List<UIElement>();
            List<UIElement> TBomb = new List<UIElement>();
            foreach (CGameObject GB in Data.Bombs)
                DrawingGameObject(GB, TBomb);
            foreach (CGameObject GB in Data.Bricks)
                DrawingGameObject(GB, TBrik_Players);
            foreach (CGameObject GB in Data.Players.Values)
                DrawingGameObject(GB, TBrik_Players);
            Data.Main.AllObj.Children.Clear();
            foreach (Image img in TBrik_Players)
                Data.Main.AllObj.Children.Add(img);
            foreach (Image img in TBomb)
                Data.Main.AllBomb.Children.Add(img);
        }
        void DrawingGameObject(CGameObject GB, List<UIElement> List_UIElement)
        {
            Image image = new Image();
            switch (GB.Type)
            {
                case "Player":
                    image.Source = Sprites.Bitmap["Player_" + GB.Direction][GB.Sprite];
                    break;
                case "Enemy":
                    image.Source = Sprites.Bitmap["Enemy_" + GB.Direction][GB.Sprite];
                    break;
                case "Brick":
                    image.Source = Sprites.Bitmap["Brick"][GB.Sprite];
                    break;
                case "Bomb":
                    image.Source = Sprites.Bitmap["Bomb"][GB.Sprite];
                    break;
                case "PlayerDied":
                    return;
                case "EnemyDied":
                    return;
                default: return;
            }
            GB.Fase = image;
            image.Stretch = Stretch.Fill;
            image.Width = Scale.Scale;
            image.Height = Scale.Scale;
            Canvas.SetLeft(image, GB.X);
            Canvas.SetTop(image, GB.Y);
            List_UIElement.Add(image);
        }
        void DrawingGameObject(CGameObject GB)
        {
            Data.Main.AllObj.Dispatcher.Invoke(new Action(delegate
            {
                Image image = new Image();
                GB.Fase = image;
                GB.Direction = GB.Sprite = 0;
                image.Stretch = Stretch.Fill;
                image.Width = image.Height = Scale.Scale;
                Canvas.SetLeft(image, GB.X);
                Canvas.SetTop(image, GB.Y);
                switch (GB.Type)
                {
                    case "Player":
                        image.Source = Sprites.Bitmap[GB.Type + "_" + GB.Direction][GB.Sprite];
                        Data.Main.AllObj.Children.Add(image);
                        break;
                    case "Enemy":
                        image.Source = Sprites.Bitmap[GB.Type + "_" + GB.Direction][GB.Sprite];
                        Data.Main.AllObj.Children.Add(image);
                        break;
                    case "PlayerDied":
                        image.Source = Sprites.Bitmap[GB.Type][GB.Sprite];
                        Data.Main.AllObj.Children.Add(image);
                        break;
                    case "EnemyDied":
                        image.Source = Sprites.Bitmap[GB.Type][GB.Sprite];
                        Data.Main.AllObj.Children.Add(image);
                        break;
                    case "Brick":
                        image.Source = Sprites.Bitmap[GB.Type][GB.Sprite];
                        Data.Main.AllObj.Children.Add(image);
                        break;
                    case "Bomb":
                        image.Source = Sprites.Bitmap[GB.Type][GB.Sprite];
                        Data.Main.AllBomb.Children.Add(image);
                        break;
                }
            }));
        }
        void DrawingDelete(CGameObject GB)
        {
            Data.Main.AllObj.Dispatcher.Invoke(new Action(delegate
            {
                switch (GB.Type)
                {
                    case "Player":
                        if (Data.Main.AllObj.Children.Contains(GB.Fase))
                            Data.Main.AllObj.Children.Remove(GB.Fase);
                        break;
                    case "Enemy":
                        if (Data.Main.AllObj.Children.Contains(GB.Fase))
                            Data.Main.AllObj.Children.Remove(GB.Fase);
                        break;
                    case "PlayerDied":
                        if (Data.Main.AllObj.Children.Contains(GB.Fase))
                            Data.Main.AllObj.Children.Remove(GB.Fase);
                        break;
                    case "EnemyDied":
                        if (Data.Main.AllObj.Children.Contains(GB.Fase))
                            Data.Main.AllObj.Children.Remove(GB.Fase);
                        break;
                    case "Brick":
                        if (Data.Main.AllObj.Children.Contains(GB.Fase))
                            Data.Main.AllObj.Children.Remove(GB.Fase);
                        break;
                    case "Bomb":
                        if (Data.Main.AllBomb.Children.Contains(GB.Fase))
                            Data.Main.AllBomb.Children.Remove(GB.Fase);
                        break;
                }
            }));
        }
        void Thread_Objects_Animation()
        {
            List<CGameObject> TMP_Delete = new List<CGameObject>();
            CGameObject GB;
            int Timer = 0;
            while (true)
            {
                Thread.Sleep(25);
                Timer++;
                if (Timer > 1000000000)
                    Timer = 0;
                if (Timer % 4 == 0)//Изменение спрайтов подорваных Кирпичных стен
                    for (int i = 0; i < Data.Death_Bricks.Count; i++)
                    {
                        GB = Data.Death_Bricks[i];
                        GB.Sprite++;
                        if (GB.Sprite > Sprites.Bitmap[GB.Type].Count - 1)
                        {
                            Data.Death_Bricks.Remove(GB);
                            Data.Main.Dispatcher.Invoke(new Action(delegate { Data.Main.AllObj.Children.Remove(GB.Fase); }));
                        }
                        else
                        {
                            Data.Main.Dispatcher.Invoke(new Action(delegate { GB.Fase.Source = Sprites.Bitmap[GB.Type][GB.Sprite]; }));
                        }
                    }
                if (Timer % 2 == 0)//Изменение спрайтов Взыров
                    for (int i = 0; i < Data.Explosions.Count; i++)
                    {
                        GB = Data.Explosions[i];
                        if (GB.Direction == 0)
                            GB.Sprite++;
                        else
                            GB.Sprite--;
                        if (GB.Sprite >= 0)
                        {
                            if (GB.Sprite < Sprites.Bitmap[GB.Type].Count)
                                Data.Main.Dispatcher.Invoke(new Action(delegate { GB.Fase.Source = Sprites.Bitmap[GB.Type][GB.Sprite]; }));
                            else
                                GB.Direction = -1;
                        }
                        else
                        {
                            TMP_Delete.Add(GB);
                            Data.Explosions.Remove(GB);
                            Data.RemoveF(GB);
                        }
                    }
                for (int i = 0; i < TMP_Delete.Count; i++)//Удаление отгремевших Взрывов
                {
                    Data.Main.Dispatcher.Invoke(new Action(delegate { Data.Main.Exp.Children.Remove(TMP_Delete[i].Fase.Parent as Canvas); }));
                    TMP_Delete.RemoveAt(i);
                }
                if (Timer % 10 == 0)//Изменения спратов Бомб
                    for (int i = 0; i < Data.Bombs.Count; i++)
                    {
                        GB = Data.Bombs[i];
                        GB.Direction++;
                        GB.Sprite++;
                        if (GB.Sprite >= Sprites.Bitmap["Bomb"].Count)
                            GB.Sprite = 0;
                        Data.Main.Dispatcher.Invoke(new Action(delegate { GB.Fase.Source = Sprites.Bitmap[GB.Type][GB.Sprite]; }));
                        if (GB.Direction >= 20)
                        {
                            Data.Main.Dispatcher.Invoke(new Action(delegate { Data.Main.AllBomb.Children.Remove(GB.Fase); }));
                            Data.Bombs.Remove(GB);
                            Data.RemoveF(GB);
                            Explosion(2, GB.iX, GB.iY);
                        }
                    }
                if (Timer % 7 == 0)//Изменение спрайтов мертвых персонажей
                    for (int i = 0; i < Data.Death_Players.Count; i++)
                    {
                        GB = Data.Death_Players[i];
                        GB.Sprite++;
                        if (GB.Sprite < Sprites.Bitmap[GB.Type].Count)
                            Data.Main.Dispatcher.Invoke(new Action(delegate { GB.Fase.Source = Sprites.Bitmap[GB.Type][GB.Sprite]; }));
                        else
                        {
                            Data.Main.Dispatcher.Invoke(new Action(delegate { Data.Main.AllObj.Children.Remove(GB.Fase); }));
                            Data.Death_Players.Remove(GB);
                            GB.Direction = GB.Sprite = 0;
                        }
                    }
            }
        }
        void GenerateBrick(int Bricks)
        {
            string str = "GenerateBrick:";
            int Size = Scale.Size - 1, scale = Scale.Scale;
            Random Rand = new Random();
            for (int i = 0; i < Bricks; i++)
            {
                int iX = Rand.Next(0, Size), iY = Rand.Next(0, Size);
                do
                {
                    if (Data.Field[iX][iY].Count == 0 &&
                        ((iX != 1 || iY != 1) && (iX != 2 || iY != 1) && (iX != 1 || iY != 2)) && ((iX != 19 || iY != 19) && (iX != 19 || iY != 18) && (iX != 18 || iY != 19)) && ((iX != 19 || iY != 1) && (iX != 19 || iY != 2) && (iX != 18 || iY != 1)) && ((iX != 1 || iY != 19) && (iX != 2 || iY != 19) && (iX != 1 || iY != 18)))
                    {
                        CGameObject Brick = new CGameObject(scale * iX, scale * iY, iX, iY, "Brick");
                        Data.Bricks.Add(Brick);
                        Data.AddF(Brick);
                        DrawingGameObject(Brick);
                        str += Brick.iX + ";" + Brick.iY + "@";
                        break;
                    }
                    else
                    {
                        iX = Rand.Next(0, Size);
                        iY = Rand.Next(0, Size);
                    }
                }
                while (true);
            }
            Send(str);
        }
        bool SwitchExplose(CGameObject GB, int Power)
        {
            switch (GB.Type)
            {
                case "Bomb":
                    Data.Main.Dispatcher.Invoke(new Action(delegate { Data.Main.AllBomb.Children.Remove(GB.Fase); }));
                    Data.Bombs.Remove(GB);
                    Data.RemoveF(GB);
                    Explosion(Power, GB.iX, GB.iY);
                    return true;
                case "Brick":
                    Data.RemoveF(GB);
                    Data.Bricks.Remove(GB);
                    Data.Death_Bricks.Add(GB);
                    if (Data.Bricks.Count <= 0 && SInfo.Type == "Server")
                        GenerateBrick(84);
                    return true;
                case "Player":
                    GB.Type = "PlayerDied";
                    GB.Sprite = 0;
                    Data.RemoveF(GB);
                    Data.Death_Players.Add(GB);
                    Data.DeathCounter[GB.NamePlayer]++;
                    Data.Main.Dispatcher.Invoke(new Action(delegate { Data.Main.CounterStart.Visibility = Visibility.Visible; }));
                    Control.Live = false;
                    Send("PlayerDied:" + GB.NamePlayer);
                    return false;
                case "Enemy":
                    return false;
            }
            return true;
        }
        void Explosion(int Power, int iX, int iY)
        {
            List<CGameObject> L_ExpTMP = new List<CGameObject>();
            CGameObject GB_ExpC = new CGameObject(iX * Scale.Scale, iY * Scale.Scale, iX, iY, "FireCentre");
            L_ExpTMP.Add(GB_ExpC);
            for (int g = 0; g < Data.Field[iX][iY].Count; g++)
            {
                CGameObject tmpgb = Data.Field[iX][iY][g];
                SwitchExplose(tmpgb, Power);
            }
            for (int i = 1; i <= Power; i++)
                if (iX + i <= 19)
                    if (Data.Field[iX + i][iY].Count == 0)
                    {
                        CGameObject GB_ExpR = new CGameObject((iX + i) * Scale.Scale, iY * Scale.Scale, iX, iY, (i != Power) ? "FireHorizontal" : "FireRight");
                        L_ExpTMP.Add(GB_ExpR);
                    }
                    else
                    {
                        bool stop = false;
                        for (int g = 0; g < Data.Field[iX + i][iY].Count; g++)
                        {
                            CGameObject tmpgb = Data.Field[iX + i][iY][g];
                            stop = SwitchExplose(tmpgb, Power);
                        }
                        if (stop)
                            break;
                        else
                        {
                            CGameObject GB_ExpR = new CGameObject((iX + i) * Scale.Scale, iY * Scale.Scale, iX, iY, (i != Power) ? "FireHorizontal" : "FireRight");
                            L_ExpTMP.Add(GB_ExpR);
                        }
                    }
            for (int i = 1; i <= Power; i++)
                if (iX - i >= 1)
                    if (Data.Field[iX - i][iY].Count == 0)
                    {
                        CGameObject GB_ExpL = new CGameObject((iX - i) * Scale.Scale, iY * Scale.Scale, iX, iY, (i != Power) ? "FireHorizontal" : "FireLeft");
                        L_ExpTMP.Add(GB_ExpL);
                    }
                    else
                    {
                        bool stop = false;
                        for (int g = 0; g < Data.Field[iX - i][iY].Count; g++)
                        {
                            CGameObject tmpgb = Data.Field[iX - i][iY][g];
                            stop = SwitchExplose(tmpgb, Power);
                        }
                        if (stop)
                            break;
                        else
                        {
                            CGameObject GB_ExpL = new CGameObject((iX - i) * Scale.Scale, iY * Scale.Scale, iX, iY, (i != Power) ? "FireHorizontal" : "FireLeft");
                            L_ExpTMP.Add(GB_ExpL);
                        }
                    }
            for (int i = 1; i <= Power; i++)
                if (iY + i <= 19)
                    if (Data.Field[iX][iY + i].Count == 0)
                    {
                        CGameObject GB_ExpD = new CGameObject(iX * Scale.Scale, (iY + i) * Scale.Scale, iX, iY, (i != Power) ? "FireVertical" : "FireDown");
                        L_ExpTMP.Add(GB_ExpD);
                    }
                    else
                    {
                        bool stop = false;
                        for (int g = 0; g < Data.Field[iX][iY + i].Count; g++)
                        {
                            CGameObject tmpgb = Data.Field[iX][iY + i][g];
                            stop = SwitchExplose(tmpgb, Power);
                        }
                        if (stop)
                            break;
                        else
                        {
                            CGameObject GB_ExpD = new CGameObject(iX * Scale.Scale, (iY + i) * Scale.Scale, iX, iY, (i != Power) ? "FireVertical" : "FireDown");
                            L_ExpTMP.Add(GB_ExpD);
                        }
                    }
            for (int i = 1; i <= Power; i++)
                if (iY - i >= 1)
                    if (Data.Field[iX][iY - i].Count == 0)
                    {
                        CGameObject GB_ExpU = new CGameObject(iX * Scale.Scale, (iY - i) * Scale.Scale, iX, iY, (i != Power) ? "FireVertical" : "FireUp");
                        L_ExpTMP.Add(GB_ExpU);
                    }
                    else
                    {
                        bool stop = false;
                        for (int g = 0; g < Data.Field[iX][iY - i].Count; g++)
                        {
                            CGameObject tmpgb = Data.Field[iX][iY - i][g];
                            stop = SwitchExplose(tmpgb, Power);
                        }
                        if (stop)
                            break;
                        else
                        {
                            CGameObject GB_ExpU = new CGameObject(iX * Scale.Scale, (iY - i) * Scale.Scale, iX, iY, (i != Power) ? "FireVertical" : "FireUp");
                            L_ExpTMP.Add(GB_ExpU);
                        }
                    }
            for (int i = 0; i < L_ExpTMP.Count; i++)
            {
                CGameObject GB = L_ExpTMP[i];
                Data.Main.Dispatcher.Invoke(new Action(delegate
                {
                    GB.Fase = new Image();
                    GB.Fase.Stretch = Stretch.Fill;
                    GB.Fase.Width = Scale.Scale;
                    GB.Fase.Height = Scale.Scale;
                    GB.Fase.Source = Sprites.Bitmap[GB.Type][GB.Sprite];
                    Canvas.SetLeft(GB.Fase, GB.X);
                    Canvas.SetTop(GB.Fase, GB.Y);
                }));
                Data.Explosions.Add(GB);
                Data.AddF(GB);
            }
            Data.Main.Dispatcher.Invoke(new Action(delegate
            {
                Canvas canvas = new Canvas();
                for (int i = 0; i < L_ExpTMP.Count; i++)
                    canvas.Children.Add(L_ExpTMP[i].Fase);
                Data.Main.Exp.Children.Add(canvas);
            }));
        }
        void GameTimerTick(object sender, EventArgs e)
        {
            if (SInfo.Type == "Server" && Data.DeathCounter.Values.Max() >= SInfo.Frags)
            {
                KeyValuePair<string, int> namewin = Data.DeathCounter.First();
                foreach (KeyValuePair<string, int> f in Data.DeathCounter)
                    if (f.Value <= namewin.Value)
                        namewin = f;
                Winner(namewin.Key);
                Send("Winer:" + namewin.Key);
            }
            TimerCounter++;
            if (TimerCounter > 1000000000)
                TimerCounter = 0;
            if (Control.Live)
            {
                if (Control.Down)
                {
                    Player.Direction = 0;
                    for (double i = 0; i < PlayerSped; i += 0.2)
                    {
                        Player.Y += 0.2;
                        PlayerMove(0);
                    }
                }
                else if (Control.Left)
                {
                    Player.Direction = 1;
                    for (double i = 0; i < PlayerSped; i += 0.2)
                    {
                        Player.X -= 0.2;
                        PlayerMove(1);
                    }
                }
                else if (Control.Up)
                {
                    Player.Direction = 2;
                    for (double i = 0; i < PlayerSped; i += 0.2)
                    {
                        Player.Y -= 0.2;
                        PlayerMove(2);
                    }
                }
                else if (Control.Right)
                {
                    Player.Direction = 3;
                    for (double i = 0; i < PlayerSped; i += 0.2)
                    {
                        Player.X += 0.2;
                        PlayerMove(3);
                    }
                }
                if (Control.Space)
                {
                    bool b = true;
                    for (int g = 0; g < Data.Field[Player.iX][Player.iY].Count; g++)
                        if (Data.Field[Player.iX][Player.iY][g].Type == "Bomb")
                        {
                            b = false;
                            break;
                        }
                    if (b)
                        Bomb_Plase();
                }
            }
            else if (TimerCounter % 30 == 0)
            {
                string str = "";
                if (CounterStart > 0)
                    str = CounterStart.ToString();
                else
                    str = "Go";
                Data.Main.CounterStart.Content = str;
                CounterStart--;
                if (CounterStart < -1)
                {
                    Data.Main.CounterStart.Content = CounterStart = 5;
                    Data.Main.CounterStart.Visibility = Visibility.Hidden;
                    Random rand = new Random();
                    Player.iX = Player.iY = 1;
                    do
                    {
                        int i = rand.Next(0, 20);
                        Player.iX = (i % 2 == 0) ? 1 : 19;
                        i = rand.Next(0, 20);
                        Player.iY = (i % 2 == 0) ? 1 : 19;
                    } while (Data.Field[Player.iX][Player.iY].Count > 0);
                    Player.Type = "Player";
                    Player.X = Player.iX * Scale.Scale;
                    Player.Y = Player.iY * Scale.Scale;
                    DrawingGameObject(Player);
                    Data.AddF(Player);
                    str = "PlayerReborn:" + Player.NamePlayer + ":" + Player.iX + ":" + Player.iY;
                    Send(str);
                    Control.Live = true;
                }
            }
        }
        void Bomb_Plase()
        {
            CGameObject Bomb = new CGameObject(Scale.Scale * Player.iX, Scale.Scale * Player.iY, Player.iX, Player.iY, "Bomb");
            Bomb_Plase(Bomb);
            Send("BombPlase:" + Bomb.iX + ":" + Bomb.iY);
            PlayerBomb.Add(Bomb);
        }
        void Bomb_Plase(CGameObject Bomb)
        {
            DrawingGameObject(Bomb);
            Data.Bombs.Add(Bomb);
            Data.AddF(Bomb);
        }
        void PlayerMove(int Direction)
        {
            for (int i = 0; i < PlayerBomb.Count; i++)
            {
                CGameObject gb = PlayerBomb[i];
                if (!(gb.X < Player.X + Scale.Scale - 1 && gb.X + Scale.Scale - 1 > Player.X && gb.Y < Player.Y + Scale.Scale - 1 && gb.Y + Scale.Scale - 1 > Player.Y))
                    PlayerBomb.Remove(gb);
            }
            double vs = Scale.Scale / 2;
            bool CanGo = true;
            if (Player.X < Scale.Scale - 1 || Player.Y < Scale.Scale - 1 || (Scale.Size - 1) * Scale.Scale < Player.X + Scale.Scale - 1 || (Scale.Size - 1) * Scale.Scale < Player.Y + Scale.Scale - 1)
                CanGo = false;//Проверка на границы поля
            if (CanGo)//Проверка на стены
                foreach (CGameObject gb in Data.Bricks)
                    if (gb.X < Player.X + Scale.Scale - 1 && gb.X + Scale.Scale - 1 > Player.X && gb.Y < Player.Y + Scale.Scale - 1 && gb.Y + Scale.Scale - 1 > Player.Y)
                    {
                        CanGo = false;
                        break;
                    }
            if (CanGo)//Проверка на бомбы
                foreach (CGameObject gb in Data.Bombs)
                {
                    if (gb.X < Player.X + Scale.Scale - 1 && gb.X + Scale.Scale - 1 > Player.X && gb.Y < Player.Y + Scale.Scale - 1 && gb.Y + Scale.Scale - 1 > Player.Y)
                    {
                        CanGo = false;
                        if (PlayerBomb.Contains(gb))
                        {
                            CanGo = true;
                        }
                        break;
                    }
                }
            if (CanGo)//Проверка на неразрушаемые блоки
                for (int X = 2; X <= 18; X++)
                {
                    for (int Y = 2; Y <= 18; Y++)
                    {
                        if (X % 2 == 0 && Y % 2 == 0)
                            if (X * Scale.Scale - 1 < Player.X + Scale.Scale - 1 && X * Scale.Scale - 1 + Scale.Scale > Player.X && Y * Scale.Scale - 1 < Player.Y + Scale.Scale - 1 && Y * Scale.Scale - 1 + Scale.Scale > Player.Y)
                            {
                                CanGo = false;
                                double S = vs / 2;
                                if (Direction == 0 || Direction == 2)
                                    if (X * Scale.Scale + vs + S < Player.X + vs)
                                    {
                                        Player.X += 0.2; ;
                                        Player.Direction = 3;
                                    }
                                    else if (X * Scale.Scale + vs - S > Player.X + vs)
                                    {
                                        Player.X -= 0.2; ;
                                        Player.Direction = 1;
                                    }
                                if (Direction == 1 || Direction == 3)
                                    if (Y * Scale.Scale + vs + S < Player.Y + vs)
                                    {
                                        Player.Y += 0.2; ;
                                        Player.Direction = 0;
                                    }
                                    else if (Y * Scale.Scale + vs - S > Player.Y + vs)
                                    {
                                        Player.Y -= 0.2; ;
                                        Player.Direction = 2;
                                    }
                                break;
                            }
                    }
                }
            if (!CanGo)
                switch (Direction)
                {
                    case 0: Player.Y -= 0.2; ; break;
                    case 1: Player.X += 0.2; ; break;
                    case 2: Player.Y += 0.2; ; break;
                    case 3: Player.X -= 0.2; ; break;
                }
            double iX = (Player.X + vs) / Scale.Scale, iY = (Player.Y + vs) / Scale.Scale;
            if (Player.iX != (int)iX || Player.iY != (int)iY)
            {
                Data.RemoveF(Player);
                Player.iX = (int)iX;
                Player.iY = (int)iY;
                Data.AddF(Player);
            }
            Animation++;
            if (Animation > 1000000000)
                Animation = 0;
            if (Animation % Scale.Scale == 0)
                Player.Sprite++;
            if (Player.Sprite >= 3)
                Player.Sprite = 0;
            Player.Fase.Source = Sprites.Bitmap["Player_" + Player.Direction][Player.Sprite];
            int x = (int)Canvas.GetLeft(Player.Fase);
            int y = (int)Canvas.GetTop(Player.Fase);
            if ((int)Player.X != x || (int)Player.Y != y || Animation % Scale.Scale == 0)
            {
                Canvas.SetLeft(Player.Fase, Player.X);
                Canvas.SetTop(Player.Fase, Player.Y);
                Send("PlayerMove:" + Scale.Scale + ":" + Player.X + ";" + Player.Y + ";" + Player.iX + ";" + Player.iY + ";" + Player.Direction + ";" + Player.Sprite + ";" + Player.NamePlayer);
            }
        }
        #endregion
    }
}