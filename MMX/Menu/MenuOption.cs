using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public class MenuOption
    {
        public Point pos;
        public Action update;
        public Action<Point, int> render;
        public string configureMessage;

        public MenuOption(int x, int y, Action update, Action<Point, int> render, string configureMessage = null)
        {
            pos = new Point(x, y);
            this.update = update;
            this.render = render;
            this.configureMessage = configureMessage;
        }
    }
}
