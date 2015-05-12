using System.Windows.Controls;
using System.Collections.Generic;
namespace BomberMan
{
    class CContainer
    {
        public List<CGameObject>[][] Field;
        public List<CGameObject> Bricks = new List<CGameObject>();
        public List<CGameObject> Bombs = new List<CGameObject>();
        public Dictionary<string, CGameObject> Players = new Dictionary<string, CGameObject>();
        public Dictionary<string, int> DeathCounter = new Dictionary<string, int>();
        public List<CGameObject> Explosions = new List<CGameObject>();
        public List<CGameObject> Death_Bricks = new List<CGameObject>();
        public List<CGameObject> Death_Players = new List<CGameObject>();
        public MainWindow Main;
        public CContainer(int FieldSize)
        {
            Field = new List<CGameObject>[FieldSize][];
            for (int x = 0; x < FieldSize; x++)
            {
                Field[x] = new List<CGameObject>[FieldSize];
                for (int y = 0; y < FieldSize; y++)
                    Field[x][y] = new List<CGameObject>();
            }
            this.Main = App.Current.MainWindow as MainWindow;
        }
        public bool RemoveF(CGameObject gb)
        {
            if (Field[gb.iX][gb.iY].Contains(gb))
            {
                Field[gb.iX][gb.iY].Remove(gb);
                return true;
            }
            return false;
        }
        public void AddF(CGameObject gb)
        {
            Field[gb.iX][gb.iY].Add(gb);
        }
    }
}