using System;
using System.Windows.Controls;
using System.Net.Sockets;
namespace BomberMan
{
    [Serializable]
    class CGameObject
    {
        public double X, Y;
        public int iX, iY, Direction, Sprite;
        public string Type;
        public string NamePlayer;
        [NonSerialized]
        public Image Fase;
        [NonSerialized]
        public Socket Socket;
        public CGameObject()
        {
            NamePlayer = "";
        }
        public CGameObject(int X, int Y, int iX, int iY, string Type)
        {
            this.X = X;
            this.Y = Y;
            this.iX = iX;
            this.iY = iY;
            this.Type = Type;
            this.Sprite = this.Direction = 0;
            Fase = null;
        }
    }
}