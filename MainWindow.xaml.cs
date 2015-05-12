using System;
using System.Collections.Generic;
using System.Linq;
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

using System.Diagnostics;
using System.Threading;

namespace BomberMan
{
    public partial class MainWindow : Window
    {
        CGamePlay GamePlay;
        public MainWindow()
        {
            Menu Menu = new Menu();
            Menu.ShowDialog();
            InitializeComponent();
            CScale Scale = new CScale(21);
            GamePlay = new CGamePlay(Scale, new CSocketInfo(Menu));

        }
        public void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (GamePlay.Control.Live)
                switch (e.Key)
                {
                    case Key.Down:
                        GamePlay.Control.Down = true;
                        break;
                    case Key.Left:
                        GamePlay.Control.Left = true;
                        break;
                    case Key.Up:
                        GamePlay.Control.Up = true;
                        break;
                    case Key.Right:
                        GamePlay.Control.Right = true;
                        break;
                    case Key.Space:
                        GamePlay.Control.Space = true;
                        break;
                }
            switch (e.Key)
            {
                case Key.OemPlus:
                    ScalePlus();
                    break;
                case Key.Add:
                    ScalePlus();
                    break;
                case Key.OemMinus:
                    ScaleMinus();
                    break;
                case Key.Subtract:
                    ScaleMinus();
                    break;
                case Key.Tab:
                    ShowScore();
                    this.Score.Visibility = Visibility.Visible;
                    break;
            }
        }
        void ScalePlus()
        {
            if (GamePlay.Scale.Scale < 46)
            {
                GamePlay.PlayerSped += 0.2;
                GamePlay.Scale.SizeChanged(GamePlay.Scale.Scale + 2);
                ScaleAll();
                CounterStart.FontSize += 20;
            }
        }
        void ScaleMinus()
        {
            if (GamePlay.Scale.Scale > 6)
            {
                GamePlay.PlayerSped -= 0.2;
                GamePlay.Scale.SizeChanged(GamePlay.Scale.Scale - 2);
                ScaleAll();
                CounterStart.FontSize -= 20;
            }
        }
        public void Window_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Down:
                    GamePlay.Control.Down = false;
                    break;
                case Key.Left:
                    GamePlay.Control.Left = false;
                    break;
                case Key.Up:
                    GamePlay.Control.Up = false;
                    break;
                case Key.Right:
                    GamePlay.Control.Right = false;
                    break;
                case Key.Space:
                    GamePlay.Control.Space = false;
                    break;
                case Key.Tab:
                    this.Score.Visibility = Visibility.Hidden;
                    break;
            }
        }
        void ShowScore()
        {
            Label P = new Label(), F = new Label();
            P.FontSize = F.FontSize = GamePlay.Scale.Scale;
            P.Foreground = new BrushConverter().ConvertFrom("#FF3399FF") as Brush;
            F.Foreground = new BrushConverter().ConvertFrom("#FFCC6600") as Brush;
            P.Content = "Имя игрока";
            F.Content = "Смертей";
            Player.Children.Clear();
            Player.Children.Add(P);
            Frag.Children.Clear();
            Frag.Children.Add(F);
            foreach (KeyValuePair<string, int> element in GamePlay.Data.DeathCounter)
            {
                P = new Label();
                F = new Label();
                P.FontSize = F.FontSize = GamePlay.Scale.Scale;
                P.Foreground = new BrushConverter().ConvertFrom("#FFC3BD43") as Brush;
                F.Foreground = new BrushConverter().ConvertFrom("#FFCACD0E") as Brush;
                P.Content = element.Key;
                F.Content = element.Value;
                Player.Children.Add(P);
                Frag.Children.Add(F);
            }
        }
        public void ShowScore(string str)
        {
            this.CounterStart.Visibility = Visibility.Hidden;
            this.Score.Visibility = Visibility.Visible;
            Label P = new Label(), F = new Label();
            P.FontSize = F.FontSize = GamePlay.Scale.Scale;
            P.Foreground = new BrushConverter().ConvertFrom("#FF3399FF") as Brush;
            F.Foreground = new BrushConverter().ConvertFrom("#FFCC6600") as Brush;
            P.Content = "Имя игрока";
            F.Content = "Смертей";
            Player.Children.Clear();
            Player.Children.Add(P);
            Frag.Children.Clear();
            Frag.Children.Add(F);
            foreach (KeyValuePair<string, int> element in GamePlay.Data.DeathCounter)
            {
                P = new Label();
                F = new Label();
                P.FontSize = F.FontSize = GamePlay.Scale.Scale;
                P.Foreground = new BrushConverter().ConvertFrom("#FFC3BD43") as Brush;
                F.Foreground = new BrushConverter().ConvertFrom("#FFCACD0E") as Brush;
                P.Content = element.Key;
                F.Content = element.Value;
                if (element.Key == str)
                {
                    P.Background = F.Background = new BrushConverter().ConvertFrom("#7F3CB4E6") as Brush;
                }
                Player.Children.Add(P);
                Frag.Children.Add(F);
            }
        }
        void ScaleAll()
        {
            GamePlay.Control.Down = GamePlay.Control.Up = GamePlay.Control.Right = GamePlay.Control.Left = false;
            this.Width = GamePlay.Scale.FullWidth + 16;
            this.Height = GamePlay.Scale.FullHeight + 38;
            for (int i = 0; i < GamePlay.Data.Field.Length; i++)
            {
                List<CGameObject>[] arr = GamePlay.Data.Field[i];
                for (int j = 0; j < arr.Length; j++)
                    Scale(arr[j]);
            }
            Scale(GamePlay.Data.Death_Players);
        }
        void Scale(List<CGameObject> list)
        {
            for (int f = 0; f < list.Count; f++)
            {
                CGameObject GB = list[f];
                if (GB.Type != "Wall" && GB.Fase != null)
                {
                    double v = GamePlay.Scale.Scale / GB.Fase.Width;
                    GB.Fase.Width = GB.Fase.Height = GamePlay.Scale.Scale;
                    GB.X = Convert.ToInt32(GB.X * v);
                    GB.Y = Convert.ToInt32(GB.Y * v);
                    Canvas.SetLeft(GB.Fase, GB.X);
                    Canvas.SetTop(GB.Fase, GB.Y);
                }
            }
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            GamePlay.Close();
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        Point old;
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            old = e.GetPosition(null);
        }
        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point cur = e.GetPosition(null);
                this.Left += cur.X - old.X;
                this.Top += cur.Y - old.Y;
            }
        }
        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            old = e.GetPosition(null);
        }
    }
}