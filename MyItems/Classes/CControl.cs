namespace BomberMan
{
    class CControl
    {
        public bool Down, Left, Up, Right, Space, Live;
        public CControl()
        {
            this.Down = this.Left = this.Up = this.Right = this.Space = false;
            this.Live = true;
        }
    }
}