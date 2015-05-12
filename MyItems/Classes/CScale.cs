namespace BomberMan
{
    class CScale
    {
        public int FullWidth, FullHeight;
        public int Size, Scale = 36;
        public CScale(int Size)
        {
            this.Size = Size;
            this.FullWidth = Scale * Size;
            this.FullHeight = Scale * Size;
        }
        public void SizeChanged(int Scale)
        {
            this.Scale = Scale;
            this.FullWidth = Scale * Size;
            this.FullHeight = Scale * Size;
        }
    }
}