using System;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using System.IO;
namespace BomberMan
{
    class CSprites
    {
        public Dictionary<string, List<BitmapImage>> Bitmap = new Dictionary<string, List<BitmapImage>>();
        public BitmapImage EMPTY = new BitmapImage();
        public CSprites()
        {
            Bitmap.Add("Player_0", new List<BitmapImage>());
            Bitmap.Add("Player_1", new List<BitmapImage>());
            Bitmap.Add("Player_2", new List<BitmapImage>());
            Bitmap.Add("Player_3", new List<BitmapImage>());
            Bitmap.Add("PlayerDied", new List<BitmapImage>());
            Bitmap.Add("Brick", new List<BitmapImage>());
            Bitmap.Add("Bomb", new List<BitmapImage>());
            Bitmap.Add("FireDown", new List<BitmapImage>());
            Bitmap.Add("FireLeft", new List<BitmapImage>());
            Bitmap.Add("FireUp", new List<BitmapImage>());
            Bitmap.Add("FireRight", new List<BitmapImage>());
            Bitmap.Add("FireVertical", new List<BitmapImage>());
            Bitmap.Add("FireHorizontal", new List<BitmapImage>());
            Bitmap.Add("FireCentre", new List<BitmapImage>());
            int x = 1, y = 1;
            LoadImage(Bitmap["Player_0"], 3, ref x, ref y);
            LoadImage(Bitmap["Player_1"], 3, ref x, ref y);
            LoadImage(Bitmap["Player_2"], 3, ref x, ref y);
            LoadImage(Bitmap["Player_3"], 3, ref x, ref y);
            x = 117; y = 1;
            LoadImage(Bitmap["PlayerDied"], 6, ref x, ref y);
            x = 81; y = 313;
            LoadImage(Bitmap["Brick"], 7, ref x, ref y);
            x = 117; y = 41;
            LoadImage(Bitmap["Bomb"], 3, ref x, ref y);
            x = 233; y = 41;
            LoadImage(Bitmap["FireUp"], 4, ref x, ref y);
            x = 233; y = 79;
            LoadImage(Bitmap["FireVertical"], 4, ref x, ref y);
            x = 233; y = 117;
            LoadImage(Bitmap["FireDown"], 4, ref x, ref y);
            x = 233; y = 157;
            LoadImage(Bitmap["FireLeft"], 4, ref x, ref y);
            x = 233; y = 195;
            LoadImage(Bitmap["FireHorizontal"], 4, ref x, ref y);
            x = 233; y = 233;
            LoadImage(Bitmap["FireRight"], 4, ref x, ref y);
            x = 233; y = 273;
            LoadImage(Bitmap["FireCentre"], 4, ref x, ref y);
            Bitmap.Add("Enemy_0", new List<BitmapImage>());
            Bitmap.Add("Enemy_1", new List<BitmapImage>());
            Bitmap.Add("Enemy_2", new List<BitmapImage>());
            Bitmap.Add("Enemy_3", new List<BitmapImage>());
            Bitmap.Add("EnemyDied", new List<BitmapImage>());
            x = 1; y = 156;
            LoadImage(Bitmap["Enemy_0"], 3, ref x, ref y);
            LoadImage(Bitmap["Enemy_1"], 3, ref x, ref y);
            LoadImage(Bitmap["Enemy_2"], 3, ref x, ref y);
            LoadImage(Bitmap["Enemy_3"], 3, ref x, ref y);
            x = 0; y = 362;
            LoadImage(Bitmap["EnemyDied"], 6, ref x, ref y);
            EMPTY.BeginInit();
            EMPTY.UriSource = new Uri(path, UriKind.RelativeOrAbsolute);
            EMPTY.SourceRect = new Int32Rect(0, 0, 1, 1);
            EMPTY.EndInit();

            /*string str = "";
            FileInfo[] files = new DirectoryInfo(path).GetFiles();
            for (int i = 0; i < files.Length; i++)
            {
                FileInfo f = files[i];
                str += String.Format("{0} {1} ", f.Name, Environment.NewLine);
            }
            str += File.Exists(path);
            MessageBox.Show(str);*/
        }
        //string path = "./Hero.png";
        string path = "MyItems/Images/Hero.png";
        public void LoadImage(List<BitmapImage> list, int size, ref int x, ref int y)
        {
            int w = 36, h = 36;
            for (int i = 0; i < size; i++)
            {
                BitmapImage Bitmap_Image = new BitmapImage();
                Bitmap_Image.BeginInit();
                Bitmap_Image.UriSource = new Uri(path, UriKind.RelativeOrAbsolute);
                Bitmap_Image.SourceRect = new Int32Rect(x, y, w, h);
                Bitmap_Image.EndInit();
                list.Add(Bitmap_Image);
                x += w + 2;
            }
            x = 1;
            y += h + 2;
        }
    }
}