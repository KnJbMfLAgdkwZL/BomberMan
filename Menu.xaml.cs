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
using System.Windows.Shapes;

using System.Diagnostics;
using System.Threading;
namespace BomberMan
{
    public partial class Menu : Window
    {
        public string Status = "Выход";
        public Menu()
        {
            InitializeComponent();
            about.Text = "                      Управелние:\n← Влево\n↑  Вверх\n→ Вправо\n↓  Вниз\nПробел - Установить бомбу\nEsc - Выход\n-/+ Масштабирование";
        }
        private void LableExit_MouseEnter(object sender, MouseEventArgs e)
        {
            Label Label = sender as Label;
            Label.FontWeight = FontWeights.Bold;
        }
        private void Label_MouseLeave(object sender, MouseEventArgs e)
        {
            Label Label = sender as Label;
            Label.FontWeight = FontWeights.Normal;
        }
        private void Label_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Label Label = sender as Label;
            switch (Label.Content.ToString())
            {
                case "Создать сервер":
                    Main.Visibility = Visibility.Hidden;
                    NameLAbel.Visibility = PlayerName.Visibility = Port.Visibility = PortLable.Visibility = Back.Visibility = Create.Visibility = Visibility.Visible;
                    break;
                case "Присоединится":
                    Main.Visibility = Visibility.Hidden;
                    NameLAbel.Visibility = PlayerName.Visibility = Port.Visibility = PortLable.Visibility = Back.Visibility = Connect.Visibility = Visibility.Visible;
                    break;
                case "Об игре":
                    Main.Visibility = Visibility.Hidden;
                    Back.Visibility = About.Visibility = Visibility.Visible;
                    break;
                case "Назад":
                    NameLAbel.Visibility = PlayerName.Visibility = Port.Visibility = PortLable.Visibility = Back.Visibility = Create.Visibility = Connect.Visibility = About.Visibility = Visibility.Hidden;
                    Main.Visibility = Visibility.Visible;
                    break;
                case "Создать":
                    Status = "Создать";
                    this.Close();
                    break;
                case "Подключится":
                    Status = "Подключится";
                    this.Close();
                    break;
                case "Выход":
                    Process.GetCurrentProcess().Kill();
                    break;
            }
        }
        public void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    Process.GetCurrentProcess().Kill();
                    break;
            }
        }
        private void Frags_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!Char.IsDigit(e.Text, 0))
                e.Handled = true;
        }
        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (Char.IsDigit(e.Text, 0) || e.Text == ".")
                e.Handled = false;
            else
                e.Handled = true;
        }
    }
}