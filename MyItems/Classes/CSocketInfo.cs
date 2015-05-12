using System;
using System.Net;
namespace BomberMan
{
    class CSocketInfo
    {
        public string Type, PlayerName;
        public IPAddress IPAddress;
        public int Port, Frags;
        public CSocketInfo(Menu Menu)
        {
            switch (Menu.Status)
            {
                case "Создать":
                    Type = "Server";
                    IPAddress = IPAddress.Any;
                    Port = Convert.ToInt32(Menu.Port.Text.ToString());
                    PlayerName = Menu.PlayerName.Text.ToString();
                    Frags = Convert.ToInt32(Menu.Frags.Text.ToString());
                    break;
                case "Подключится":
                    Type = "Client";
                    IPAddress = IPAddress.Parse(Menu.IP.Text.ToString());
                    Port = Convert.ToInt32(Menu.Port.Text.ToString());
                    PlayerName = Menu.PlayerName.Text.ToString();
                    Frags = 0;
                    break;
            }
        }
    }
}