﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VPS.WP
{
    class WPCommands
    {
        static public readonly string DefaultWPCommand = "WAYPOINT";
        static public readonly string SplineWPCommand = "SPLINE_WAYPOINT";
        static public readonly List<string> CoordsWPCommands = new List<string>() { "WAYPOINT", "SPLINE_WAYPOINT" };
    }
}
