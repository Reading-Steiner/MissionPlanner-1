﻿using DotSpatial.Data;
using DotSpatial.Projections;
using GDAL;
using GeoUtility.GeoSystem;
using GeoUtility.GeoSystem.Base;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using Ionic.Zip;
using log4net;
using VPS.ArduPilot;
using VPS.Controls;
using VPS.Grid;
using VPS.Maps;
using VPS.Plugin;
using VPS.Properties;
using VPS.Utilities;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using SharpKml.Base;
using SharpKml.Dom;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using GeoAPI.CoordinateSystems;
using GeoAPI.CoordinateSystems.Transformations;
using Feature = SharpKml.Dom.Feature;
using Formatting = Newtonsoft.Json.Formatting;
using ILog = log4net.ILog;
using Placemark = SharpKml.Dom.Placemark;
using Point = System.Drawing.Point;

namespace VPS.GCSViews
{
    public partial class FlightPlanner : MyUserControl, IDeactivate, IActivate
    {
        public FlightPlanner()
        {
            InitializeComponent();
            //System.Windows.Forms.Control.CheckForIllegalCrossThreadCalls = false;
            Init();
        }


        private void But_mincommands_Click(object sender, System.EventArgs e)
        {
            if (panelWaypoints.Height <= 30)
            {
                panelWaypoints.Height = 166;
                but_mincommands.Text = @"˅";
            }
            else
            {
                panelWaypoints.Height = but_mincommands.Height;
                but_mincommands.Text = @"˄";
            }
        }

        static public Object thisLock = new Object();
        public bool quickadd;
        internal string wpfilename;
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static Propagation prop;
        //public static GMapOverlay airportsoverlay;
        public static GMapOverlay objectsoverlay;
        public static GMapOverlay poioverlay = new GMapOverlay("POI");
        public static GMapOverlay polygonsoverlay;
        public static GMapOverlay routesoverlay;
        private static GMapOverlay rallypointoverlay;
        private static GMapOverlay layerPolygonsOverlay;
        internal GMapPolygon drawnpolygon;
        private static string zone = "50s";
        private readonly Random rnd = new Random();
        public GMapMarker center = new GMarkerGoogle(new PointLatLng(0.0, 0.0), GMarkerGoogleType.none);
        private Dictionary<string, string[]> cmdParamNames = new Dictionary<string, string[]>();
        private GMapMarkerRect CurentRectMarker;
        private altmode currentaltmode = altmode.Relative;
        private GMapMarker CurrentGMapMarker;
        public GMapMarker currentMarker;
        private GMapMarkerPOI CurrentPOIMarker;
        private GMapMarkerRallyPt CurrentRallyPt;
        public GMapOverlay drawnpolygonsoverlay;
        private bool fetchpathrip;
        public GMapOverlay geofenceoverlay;
        public GMapPolygon geofencepolygon;
        private bool grid;
        private List<int> wpMarkersGroup = new List<int>();
        private List<int> polygonMarkersGroup = new List<int>();
        private List<List<Locationwp>> history = new List<List<Locationwp>>();
        private bool isMouseClickOffMenu = false;
        private bool IsMouseClickOffMenu;
        private bool IsMouseDown = false;
        private bool isMarkerDroping = false;
        private bool IsMarkerDroping
        {
            get { return isMarkerDroping; }
            set
            {
                isMarkerDroping = value;
                if (isMarkerDroping)
                {
                    this.MainMap.Cursor = Cursors.Hand;
                }
                else
                {
                    if (IsDrawPolygongridMode || IsDrawWPMode)
                        this.MainMap.Cursor = Cursors.Cross;
                    else
                        this.MainMap.Cursor = Cursors.Default;
                }
            }
        }
        private bool IsMarkerAddable = false;
        private bool IsMarkerDroppable = false;
        private bool IsWndDroppable;
        private bool isWndDroping = false;
        private bool IsWndDroping
        {
            get { return isWndDroping; }
            set
            {
                isWndDroping = value;
                if (isWndDroping)
                {
                    if (MouseDownCurrentPoint.X != MouseDownStartPoint.X)
                    {
                        double tanQ = (double)(MouseDownStartPoint.Y - MouseDownCurrentPoint.Y) /
                            (double)(MouseDownCurrentPoint.X - MouseDownStartPoint.X);
                        if (tanQ < 0.5 && tanQ > -0.5)
                        {
                            if (MouseDownCurrentPoint.X > MouseDownStartPoint.X)
                                this.MainMap.Cursor = Cursors.PanEast;
                            else if (MouseDownCurrentPoint.X < MouseDownStartPoint.X)
                                this.MainMap.Cursor = Cursors.PanWest;
                            else
                                this.MainMap.Cursor = Cursors.NoMove2D;
                        }
                        else if (tanQ > 2 || tanQ < -2)
                        {
                            if (MouseDownCurrentPoint.Y > MouseDownStartPoint.Y)
                                this.MainMap.Cursor = Cursors.PanSouth;
                            else if (MouseDownCurrentPoint.Y < MouseDownStartPoint.Y)
                                this.MainMap.Cursor = Cursors.PanNorth;
                            else
                                this.MainMap.Cursor = Cursors.NoMove2D;
                        }
                        else
                        {
                            if (MouseDownCurrentPoint.X > MouseDownStartPoint.X &&
                                MouseDownCurrentPoint.Y < MouseDownStartPoint.Y)
                                this.MainMap.Cursor = Cursors.PanNE;
                            else if (MouseDownCurrentPoint.X < MouseDownStartPoint.X &&
                                MouseDownCurrentPoint.Y < MouseDownStartPoint.Y)
                                this.MainMap.Cursor = Cursors.PanNW;
                            else if (MouseDownCurrentPoint.X > MouseDownStartPoint.X &&
                                MouseDownCurrentPoint.Y > MouseDownStartPoint.Y)
                                this.MainMap.Cursor = Cursors.PanSE;
                            else if (MouseDownCurrentPoint.X < MouseDownStartPoint.X &&
                                MouseDownCurrentPoint.Y > MouseDownStartPoint.Y)
                                this.MainMap.Cursor = Cursors.PanSW;
                            else
                                this.MainMap.Cursor = Cursors.NoMove2D;
                        }
                    }
                    else if (MouseDownCurrentPoint.Y > MouseDownStartPoint.Y)
                        this.MainMap.Cursor = Cursors.PanSouth;
                    else if(MouseDownCurrentPoint.Y < MouseDownStartPoint.Y)
                        this.MainMap.Cursor = Cursors.PanNorth;
                    else
                        this.MainMap.Cursor = Cursors.NoMove2D;
                }
                else
                {
                    if (IsDrawPolygongridMode || IsDrawWPMode)
                        this.MainMap.Cursor = Cursors.Cross;
                    else
                        this.MainMap.Cursor = Cursors.Default;
                }
            }
        }
        private bool IsMarkerPickable;
        public GMapOverlay kmlpolygonsoverlay;

        /// <summary>
        /// Try to reduce the number of map position changes generated by the code
        /// </summary>
        private DateTime lastmapposchange = DateTime.MinValue;

        private DateTime mapupdate = DateTime.MinValue;
        private string mobileGpsLog = string.Empty;
        internal PointLatLng MouseDownStart;
        internal PointLatLng MouseDownCurrent;
        internal PointLatLng MouseDownEnd;
        private GPoint MouseDownStartPoint;
        private GPoint MouseDownCurrentPoint;
        private GPoint MouseDownPrevPoint;
        private GPoint MouseDownEndPoint;
        private PointLatLngAlt mouseposdisplay = new PointLatLngAlt(0, 0);
        private WPOverlay overlay;
        private VPS.Controls.Icon.Polygon polyicon = new VPS.Controls.Icon.Polygon();
        private VPS.Controls.Icon.WP WPicon = new VPS.Controls.Icon.WP();
        private VPS.Controls.Icon.Zoom zoomicon = new VPS.Controls.Icon.Zoom();
        private ComponentResourceManager rm = new ComponentResourceManager(typeof(FlightPlanner));
        private int selectedrow;
        private bool sethome;
        private bool splinemode;
        private PointLatLng startmeasure;
        public GMapOverlay top;
        public GMapPolygon wppolygon;
        private GMapMarker CurrentMidLine;


        public void Init()
        {
            instance = this;



            // config map             
            MainMap.CacheLocation = Settings.GetDataDirectory() +
                                                   "gmapcache" + Path.DirectorySeparatorChar;

            MainMap.OnPositionChanged += MainMap_OnCurrentPositionChanged;
            MainMap.OnTileLoadStart += MainMap_OnTileLoadStart;
            MainMap.OnTileLoadComplete += MainMap_OnTileLoadComplete;
            MainMap.OnMarkerClick += MainMap_OnMarkerClick;
            MainMap.OnMapZoomChanged += MainMap_OnMapZoomChanged;
            MainMap.OnMapTypeChanged += MainMap_OnMapTypeChanged;
            MainMap.MouseMove += MainMap_MouseMove;
            MainMap.MouseDown += MainMap_MouseDown;
            MainMap.MouseUp += MainMap_MouseUp;
            MainMap.OnMarkerEnter += MainMap_OnMarkerEnter;
            MainMap.OnMarkerLeave += MainMap_OnMarkerLeave;

            MainMap.MapScaleInfoEnabled = false;
            MainMap.ScalePen = new Pen(Color.Red);

            MainMap.DisableFocusOnMouseEnter = true;

            MainMap.ForceDoubleBuffer = false;

            //WebRequest.DefaultWebProxy.Credentials = System.Net.CredentialCache.DefaultCredentials;

            // get map type
            comboBoxMapType.ValueMember = "Name";
            comboBoxMapType.DataSource = GMapProviders.List.ToArray();
            comboBoxMapType.SelectedItem = MainMap.MapProvider;

            comboBoxMapType.SelectedValueChanged += comboBoxMapType_SelectedValueChanged;

            MainMap.RoutesEnabled = true;

            //MainMap.MaxZoom = 18;

            // get zoom  
            MainMap.MinZoom = 0;
            MainMap.MaxZoom = 24;

            // draw this layer first
            layerPolygonsOverlay = new GMapOverlay("layerpolygons");
            MainMap.Overlays.Add(layerPolygonsOverlay);

            kmlpolygonsoverlay = new GMapOverlay("kmlpolygons");
            MainMap.Overlays.Add(kmlpolygonsoverlay);

            geofenceoverlay = new GMapOverlay("geofence");
            MainMap.Overlays.Add(geofenceoverlay);

            rallypointoverlay = new GMapOverlay("rallypoints");
            MainMap.Overlays.Add(rallypointoverlay);

            routesoverlay = new GMapOverlay("routes");
            MainMap.Overlays.Add(routesoverlay);

            polygonsoverlay = new GMapOverlay("polygons");
            MainMap.Overlays.Add(polygonsoverlay);

            //airportsoverlay = new GMapOverlay("airports");
            //MainMap.Overlays.Add(airportsoverlay);

            objectsoverlay = new GMapOverlay("objects");
            MainMap.Overlays.Add(objectsoverlay);

            drawnpolygonsoverlay = new GMapOverlay("drawnpolygons");
            MainMap.Overlays.Add(drawnpolygonsoverlay);

            MainMap.Overlays.Add(poioverlay);

            prop = new Propagation(MainMap);

            top = new GMapOverlay("top");
            //MainMap.Overlays.Add(top);

            objectsoverlay.Markers.Clear();

            // set current marker
            currentMarker = new GMarkerGoogle(MainMap.Position, GMarkerGoogleType.red);
            //top.Markers.Add(currentMarker);

            // map center
            center = new GMarkerGoogle(MainMap.Position, GMarkerGoogleType.none);
            top.Markers.Add(center);

            MainMap.Zoom = 3;

            CMB_altmode.DisplayMember = "Value";
            CMB_altmode.ValueMember = "Key";
            CMB_altmode.DataSource = EnumTranslator.EnumToList<altmode>();

            //set default
            CMB_altmode.SelectedItem = altmode.Relative;

            cmb_missiontype.DataSource = new List<MAVLink.MAV_MISSION_TYPE>()
                {MAVLink.MAV_MISSION_TYPE.MISSION, MAVLink.MAV_MISSION_TYPE.FENCE, MAVLink.MAV_MISSION_TYPE.RALLY};

            updateCMDParams();

            foreach (DataGridViewColumn commandsColumn in Commands.Columns)
            {
                if (commandsColumn is DataGridViewTextBoxColumn)
                    commandsColumn.CellTemplate.Value = "0";
            }

            Commands.Columns[Delete.Index].CellTemplate.Value = "X";
            Commands.Columns[Up.Index].CellTemplate.Value = Resources.up;
            Commands.Columns[Down.Index].CellTemplate.Value = Resources.down;

            Up.Image = Resources.up;
            Down.Image = Resources.down;


            Frame.DisplayMember = "Value";
            Frame.ValueMember = "Key";
            Frame.DataSource = EnumTranslator.EnumToList<altmode>();

            updateMapType(null, null);

            // hide the map to prevent redraws when its loaded
            panelMap.Visible = false;


            // setup geofence
            List<PointLatLng> polygonPoints = new List<PointLatLng>();
            geofencepolygon = new GMapPolygon(polygonPoints, "geofence");
            geofencepolygon.Stroke = new Pen(Color.Pink, 5);
            geofencepolygon.Fill = Brushes.Transparent;

            //setup drawnpolgon
            List<PointLatLng> polygonPoints2 = new List<PointLatLng>();
            drawnpolygon = new GMapPolygon(polygonPoints2, "drawnpoly");
            drawnpolygon.Stroke = new Pen(Color.Red, 2);
            drawnpolygon.Fill = Brushes.Transparent;

            SetInitState();
            /*
            var timer = new System.Timers.Timer();

            // 2 second
            timer.Interval = 2000;
            timer.Elapsed += updateMapType;

            timer.Start();
            */
        }

        public delegate void delegateHandler();
        public delegate void delegateIntChangeHandler(int data);
        
        public delegateIntChangeHandler historyChange;
        




        private void SetInitState()
        {
            SetNoDrawPolygonState();
            this.OutDrawPolygonHandle += SetNoDrawPolygonState;
            this.ToDrawPolygonHandle += SetDrawPolygonState;

            SetNoLaodLayerState();
            MainV2.instance.LoadLayerHandle += SetLaodLayerState;
            MainV2.instance.NoLoadLayerHandle += SetNoLaodLayerState;
        }

        private void SetDrawPolygonState()
        {
            this.MainMap.Cursor = Cursors.Cross;
            this.polyicon.IsSelected = true;
            this.addPolygonPointToolStripMenuItem.Checked = true;
        }

        private void SetNoDrawPolygonState()
        {
            this.MainMap.Cursor = Cursors.Default;
            this.polyicon.IsSelected = false;
            this.addPolygonPointToolStripMenuItem.Checked = false;
        }

        private void SetDrawWPState()
        {
            this.MainMap.Cursor = Cursors.Cross;
            //this.polyicon.IsSelected = true;
            this.drawWPToolStripMenuItem.Checked = true;
        }

        private void SetNoDrawWPState()
        {
            this.MainMap.Cursor = Cursors.Default;
            //this.polyicon.IsSelected = false;
            this.drawWPToolStripMenuItem.Checked = false;
        }


        private void SetLaodLayerState()
        {
            this.zoomicon.IsSelected = true;
        }

        private void SetNoLaodLayerState()
        {
            this.zoomicon.IsSelected = false;
        }

        public static FlightPlanner instance { get; set; }

        public List<PointLatLngAlt> pointlist { get; set; } = new List<PointLatLngAlt>();


        public void Activate()
        {
            timer1.Start();

            // hide altmode if old copter version
            if (MainV2.comPort.BaseStream.IsOpen && MainV2.comPort.MAV.cs.firmware == Firmwares.ArduCopter2 &&
                MainV2.comPort.MAV.cs.version < new Version(3, 3))
            {
                CMB_altmode.Visible = false;
            }
            else
            {
                CMB_altmode.Visible = true;
            }

            // hide spline wp options if not arducopter
            if (MainV2.comPort.MAV.cs.firmware == Firmwares.ArduCopter2)
                CHK_splinedefault.Visible = true;
            else
                CHK_splinedefault.Visible = false;

            updateHome();

            setWPParams();

            updateCMDParams();

            updateDisplayView();

            try
            {
                int.Parse(TXT_DefaultAlt.Text);
            }
            catch
            {
                DevComponents.DotNetBar.MessageBoxEx.Show("请修正默认Alt");
                TXT_DefaultAlt.Text = (50 * CurrentState.multiplieralt).ToString("0");
            }
        }

        public void Deactivate()
        {
            config(true);
            timer1.Stop();
        }

        public void Undo()
        {
            if (history.Count > 0)
            {
                int no = history.Count - 1;
                var pop = history[no];
                history.RemoveAt(no);
                WPtoScreen(pop, false);
                historyChange?.Invoke(history.Count);
            }
        }

        

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // undo
            if (keyData == (Keys.Control | Keys.Z))
            {
                Undo();
                return true;
            }

            if (keyData == (Keys.Delete))
            {
                DeleteMarkerToolStripMenuItem_Click(null, null);
                return true;
            }

            if (keyData == (Keys.Control|Keys.Delete))
            {
                DeleteCurrentWP();
                return true;
            }

            if (keyData == (Keys.Alt | Keys.Delete))
            {
                DeleteCurrentPolygon();
                return true;
            }

            if (keyData == (Keys.Alt | Keys.A))
            {
                polygonMarkersGroupAddAll();
                return true;
            }

            if (keyData == (Keys.Control | Keys.A))
            {
                wpMarkersGroupAddAll();
                return true;
            }

            if (keyData == (Keys.Alt | Keys.C))
            {
                polygonMarkersGroupClear();
                return true;
            }

            if (keyData == (Keys.Control | Keys.C))
            {
                wpMarkersGroupClear();
                return true;
            }

            if (keyData == (Keys.Control | Keys.F))
            {
                wpMarkersGroupFirst();
                return true;
            }

            if (keyData == (Keys.Alt | Keys.F))
            {
                polygonMarkersGroupFirst();
                return true;
            }

            if (keyData == (Keys.Control | Keys.PageUp))
            {
                wpMarkersGroupPrev();
                return true;
            }

            if (keyData == (Keys.Control | Keys.PageDown))
            {
                wpMarkersGroupNext();
                return true;
            }

            if (keyData == (Keys.Alt | Keys.PageUp))
            {
                polygonMarkersGroupPrev();
                return true;
            }

            if (keyData == (Keys.Alt | Keys.PageDown))
            {
                polygonMarkersGroupNext();
                return true;
            }

            if (keyData == (Keys.PageUp))
            {
                wpMarkersGroupPrev();
                polygonMarkersGroupPrev();
                return true;
            }

            if (keyData == (Keys.PageDown))
            {
                wpMarkersGroupNext();
                polygonMarkersGroupNext();
                return true;
            }

            if (keyData == (Keys.Alt | Keys.Up) ||
                keyData == (Keys.Alt | Keys.Down) ||
                keyData == (Keys.Alt | Keys.Left) ||
                keyData == (Keys.Alt | Keys.Right))
            {
                if (keyData == (Keys.Alt | Keys.Up))
                {
                    polygonMarkersGroupMoveUp();
                }

                if (keyData == (Keys.Alt | Keys.Down))
                {
                    polygonMarkersGroupMoveDown();
                }

                if (keyData == (Keys.Alt | Keys.Left))
                {
                    polygonMarkersGroupMoveLeft();
                }

                if (keyData == (Keys.Alt | Keys.Right))
                {
                    polygonMarkersGroupMoveRight();
                }
                return true;
            }
            if (keyData == (Keys.Control | Keys.Up) ||
                keyData == (Keys.Control | Keys.Down) ||
                keyData == (Keys.Control | Keys.Left) ||
                keyData == (Keys.Control | Keys.Right))
            {
                if (keyData == (Keys.Control | Keys.Up))
                {
                    wpMarkersGroupMoveUp();
                }

                if (keyData == (Keys.Control | Keys.Down))
                {
                    wpMarkersGroupMoveDown();
                }

                if (keyData == (Keys.Control | Keys.Left))
                {
                    wpMarkersGroupMoveLeft();
                }

                if (keyData == (Keys.Control | Keys.Right))
                {
                    wpMarkersGroupMoveRight();
                }
                return true;
            }

            if (keyData == (Keys.Up) ||
                keyData == (Keys.Down) ||
                keyData == (Keys.Left) ||
                keyData == (Keys.Right))
            {
                if (keyData == (Keys.Up))
                {
                    wpMarkersGroupMoveUp();
                    polygonMarkersGroupMoveUp();
                }
                if (keyData == (Keys.Down))
                {
                    wpMarkersGroupMoveDown();
                    polygonMarkersGroupMoveDown();
                }
                if (keyData == (Keys.Left))
                {
                    wpMarkersGroupMoveLeft();
                    polygonMarkersGroupMoveLeft();
                }

                if (keyData == (Keys.Right))
                {
                    wpMarkersGroupMoveRight();
                    polygonMarkersGroupMoveRight();
                }
                return true;
            }

            if (keyData == (Keys.Control | Keys.Shift | Keys.A))
            {
                return true;
            }

            if (keyData == (Keys.Alt | Keys.Shift | Keys.A))
            {
                return true;
            }
            //if (keyData == (Keys.Control | Keys.M))
            //{
            //    // get the command list from the datagrid
            //    var commandlist = GetCommandList();
            //    MainV2.comPort.MAV.wps[0] = new Locationwp().Set(MainV2.comPort.MAV.cs.PlannedHomeLocation.Lat,
            //        MainV2.comPort.MAV.cs.PlannedHomeLocation.Lng, MainV2.comPort.MAV.cs.PlannedHomeLocation.Alt, 0);
            //    int a = 1;
            //    commandlist.ForEach(i =>
            //    {
            //        MAVLink.mavlink_mission_item_int_t item = i;
            //        item.seq = (ushort)a;
            //        MainV2.comPort.MAV.wps[a] = item;
            //        a++;
            //    });

            //    return true;
            //}

            return base.ProcessCmdKey(ref msg, keyData);
        }

        public enum altmode
        {
            Relative = MAVLink.MAV_FRAME.GLOBAL_RELATIVE_ALT,
            Absolute = MAVLink.MAV_FRAME.GLOBAL,
            Terrain = MAVLink.MAV_FRAME.GLOBAL_TERRAIN_ALT
        }

        public static RectLatLng GetBoundingLayer(GMapOverlay o)
        {
            RectLatLng ret = RectLatLng.Empty;

            double left = double.MaxValue;
            double top = double.MinValue;
            double right = double.MinValue;
            double bottom = double.MaxValue;

            if (o.IsVisibile)
            {
                foreach (var m in o.Markers)
                {
                    if (m.IsVisible)
                    {
                        // left
                        if (m.Position.Lng < left)
                        {
                            left = m.Position.Lng;
                        }

                        // top
                        if (m.Position.Lat > top)
                        {
                            top = m.Position.Lat;
                        }

                        // right
                        if (m.Position.Lng > right)
                        {
                            right = m.Position.Lng;
                        }

                        // bottom
                        if (m.Position.Lat < bottom)
                        {
                            bottom = m.Position.Lat;
                        }
                    }
                }
                foreach (GMapRoute route in o.Routes)
                {
                    if (route.IsVisible && route.From.HasValue && route.To.HasValue)
                    {
                        foreach (PointLatLng p in route.Points)
                        {
                            // left
                            if (p.Lng < left)
                            {
                                left = p.Lng;
                            }

                            // top
                            if (p.Lat > top)
                            {
                                top = p.Lat;
                            }

                            // right
                            if (p.Lng > right)
                            {
                                right = p.Lng;
                            }

                            // bottom
                            if (p.Lat < bottom)
                            {
                                bottom = p.Lat;
                            }
                        }
                    }
                }
                foreach (GMapPolygon polygon in o.Polygons)
                {
                    if (polygon.IsVisible)
                    {
                        foreach (PointLatLng p in polygon.Points)
                        {
                            // left
                            if (p.Lng < left)
                            {
                                left = p.Lng;
                            }

                            // top
                            if (p.Lat > top)
                            {
                                top = p.Lat;
                            }

                            // right
                            if (p.Lng > right)
                            {
                                right = p.Lng;
                            }

                            // bottom
                            if (p.Lat < bottom)
                            {
                                bottom = p.Lat;
                            }
                        }
                    }
                }
            }

            if (left != double.MaxValue && right != double.MinValue && top != double.MinValue && bottom != double.MaxValue)
            {
                ret = RectLatLng.FromLTRB(left, top, right, bottom);
            }

            return ret;
        }

        public int AddCommand(MAVLink.MAV_CMD cmd, double p1, double p2, double p3, double p4, double x, double y,
            double z, object tag = null)
        {
            selectedrow = Commands.Rows.Add();

            FillCommand(this.selectedrow, cmd, p1, p2, p3, p4, x, y, z, tag);

            writeKML();

            return selectedrow;
        }

        private void OnMidLine_Click()
        {
            int pnt2 = 0;
            var midline = CurrentMidLine.Tag as midline;
            // var pnt1 = int.Parse(midline.now.Tag);

            if (IsDrawPolygongridMode && midline.now != null)
            {
                //点击到多边形中线
                var idx = drawnpolygon.Points.IndexOf(midline.now);
                drawnpolygon.Points.Insert(idx + 1,
                    new PointLatLng(CurrentMidLine.Position.Lat, CurrentMidLine.Position.Lng));

                isSendChange = true;
                redrawPolygonSurvey(drawnpolygon.Points.Select(p => new PointLatLngAlt(p)).ToList());
                isSendChange = false;
            }
            else
            {
                if (int.TryParse(midline.next.Tag, out pnt2))
                {
                    //点击到航点中线
                    if ((MAVLink.MAV_MISSION_TYPE)cmb_missiontype.SelectedValue ==
                        MAVLink.MAV_MISSION_TYPE.FENCE)
                    {
                        var prevtype = Commands.Rows[(int)Math.Max(pnt2 - 2, 0)].Cells[Command.Index].Value.ToString();
                        // match type of prev row
                        InsertCommand(pnt2 - 1, (MAVLink.MAV_CMD)Enum.Parse(typeof(MAVLink.MAV_CMD), prevtype),
                            0, 0, 0, 0,
                            CurrentMidLine.Position.Lng,
                            CurrentMidLine.Position.Lat, 0);

                        ReCalcFence(pnt2 - 1, true, false);
                    }
                    else if ((MAVLink.MAV_MISSION_TYPE)cmb_missiontype.SelectedValue ==
                             MAVLink.MAV_MISSION_TYPE.MISSION)
                    {

                        InsertCommand(pnt2 - 1, MAVLink.MAV_CMD.WAYPOINT, 0, 0, 0, 0,
                            CurrentMidLine.Position.Lng,
                            CurrentMidLine.Position.Lat, float.Parse(TXT_DefaultAlt.Text));

                    }
                }
            }

            CurrentMidLine = null;

            isSendChange = true;
            writeKML();
            isSendChange = false;
            return;
        }

        

        /// <summary>
        /// Reads the EEPROM from a com port
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void BUT_read_Click(object sender, EventArgs e)
        {
            if (Commands.Rows.Count > 0)
            {
                if (sender is FlightData)
                {
                }
                else
                {
                    if (
                        DevComponents.DotNetBar.MessageBoxEx.Show("该操作将清理你现有的航点, 继续?", "确定"
                            ,MessageBoxButtons.OKCancel) != DialogResult.OK)
                    {
                        return;
                    }
                }
            }

            IProgressReporterDialogue frmProgressReporter = new ProgressReporterDialogue
            {
                StartPosition = FormStartPosition.CenterScreen,
                Text = "Receiving WP's"
            };

            //frmProgressReporter.DoWork += getWPs;
            frmProgressReporter.UpdateProgressAndStatus(-1, "Receiving WP's");

            ThemeManager.ApplyThemeTo(frmProgressReporter);

            frmProgressReporter.RunBackgroundOperationAsync();

            frmProgressReporter.Dispose();
        }


        /// <summary>
        /// used to adjust existing point in the datagrid including "H"
        /// </summary>
        /// <param name="pointno"></param>
        /// <param name="lat"></param>
        /// <param name="lng"></param>
        /// <param name="alt"></param>
        public void CallMeDrag(string pointno, double lat, double lng, int alt)
        {
            if (pointno == "")
            {
                return;
            }

            // dragging a WP
            if (pointno == "H")
            {
                // auto update home alt
                SetHomeHere(new PointLatLngAlt(lat, lng, srtm.getAltitude(lat, lng).alt * CurrentState.multiplieralt));
                return;
            }

            if (pointno == "Tracker Home")
            {
                MainV2.comPort.MAV.cs.TrackerLocation = new PointLatLngAlt(lat, lng, alt, "");
                return;
            }

            try
            {
                selectedrow = int.Parse(pointno) - 1;
                Commands.CurrentCell = Commands[1, selectedrow];
                // depending on the dragged item, selectedrow can be reset 
                selectedrow = int.Parse(pointno) - 1;
            }
            catch
            {
                return;
            }

            setfromMap(lat, lng, alt);
        }

        public T DeepClone<T>(T obj)
        {
            using (var ms = new MemoryStream())
            {
                var formatter = new BinaryFormatter();

                formatter.Serialize(ms, obj);

                ms.Position = 0;

                return (T)formatter.Deserialize(ms);
            }
        }

        /// <summary>
        /// from http://stackoverflow.com/questions/1119451/how-to-tell-if-a-line-intersects-a-polygon-in-c
        /// </summary>
        /// <param name="start1"></param>
        /// <param name="end1"></param>
        /// <param name="start2"></param>
        /// <param name="end2"></param>
        /// <returns></returns>
        public PointLatLng FindLineIntersection(PointLatLng start1, PointLatLng end1, PointLatLng start2,
            PointLatLng end2)
        {
            double denom = ((end1.Lng - start1.Lng) * (end2.Lat - start2.Lat)) -
                           ((end1.Lat - start1.Lat) * (end2.Lng - start2.Lng));
            //  AB & CD are parallel         
            if (denom == 0)
                return PointLatLng.Empty;
            double numer = ((start1.Lat - start2.Lat) * (end2.Lng - start2.Lng)) -
                           ((start1.Lng - start2.Lng) * (end2.Lat - start2.Lat));
            double r = numer / denom;
            double numer2 = ((start1.Lat - start2.Lat) * (end1.Lng - start1.Lng)) -
                            ((start1.Lng - start2.Lng) * (end1.Lat - start1.Lat));
            double s = numer2 / denom;
            if ((r < 0 || r > 1) || (s < 0 || s > 1))
                return PointLatLng.Empty;
            // Find intersection point      
            PointLatLng result = new PointLatLng();
            result.Lng = start1.Lng + (r * (end1.Lng - start1.Lng));
            result.Lat = start1.Lat + (r * (end1.Lat - start1.Lat));
            return result;
        }

        public void GeoFencedownloadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            addPolygonMode = false;
            int count = 1;

            if ((MainV2.comPort.MAV.cs.capabilities & (uint)MAVLink.MAV_PROTOCOL_CAPABILITY.MISSION_FENCE) > 0)
            {
                if (!MainV2.comPort.BaseStream.IsOpen)
                {
                    DevComponents.DotNetBar.MessageBoxEx.Show(Strings.PleaseConnect);
                    return;
                }
                try
                {
                    mav_mission.download(MainV2.comPort, MainV2.comPort.MAV.sysid, MainV2.comPort.MAV.compid, MAVLink.MAV_MISSION_TYPE.FENCE).AwaitSync();
                }
                catch
                {
                    DevComponents.DotNetBar.MessageBoxEx.Show("获取栅栏点失败", Strings.ERROR);
                }
                return;
            }

            if (MainV2.comPort.MAV.param["FENCE_ACTION"] == null || MainV2.comPort.MAV.param["FENCE_TOTAL"] == null)
            {
                DevComponents.DotNetBar.MessageBoxEx.Show("Not Supported");
                return;
            }

            if (int.Parse(MainV2.comPort.MAV.param["FENCE_TOTAL"].ToString()) <= 1)
            {
                DevComponents.DotNetBar.MessageBoxEx.Show("Fence points - Nothing to download");
                return;
            }

            geofenceoverlay.Polygons.Clear();
            geofenceoverlay.Markers.Clear();
            geofencepolygon.Points.Clear();

            for (int a = 0; a < count; a++)
            {
                try
                {
                    var plla = MainV2.comPort.getFencePoint(a).AwaitSync();
                    count = plla.total;
                    geofencepolygon.Points.Add(new PointLatLng(plla.plla.Lat, plla.plla.Lng));
                }
                catch
                {
                    DevComponents.DotNetBar.MessageBoxEx.Show("获取栅栏点失败", Strings.ERROR);
                    return;
                }
            }

            // do return location
            geofenceoverlay.Markers.Add(
                new GMarkerGoogle(new PointLatLng(geofencepolygon.Points[0].Lat, geofencepolygon.Points[0].Lng),
                    GMarkerGoogleType.red)
                {
                    ToolTipMode = MarkerTooltipMode.OnMouseOver,
                    ToolTipText = "GeoFence Return"
                });
            geofencepolygon.Points.RemoveAt(0);

            // add now - so local points are calced
            geofenceoverlay.Polygons.Add(geofencepolygon);

            // update flight data
            FlightData.geofence.Markers.Clear();
            FlightData.geofence.Polygons.Clear();
            FlightData.geofence.Polygons.Add(new GMapPolygon(geofencepolygon.Points, "gf fd")
            {
                Stroke = geofencepolygon.Stroke,
                Fill = Brushes.Transparent
            });
            FlightData.geofence.Markers.Add(new GMarkerGoogle(geofenceoverlay.Markers[0].Position, GMarkerGoogleType.red)
            {
                ToolTipText = geofenceoverlay.Markers[0].ToolTipText,
                ToolTipMode = geofenceoverlay.Markers[0].ToolTipMode
            });

            MainMap.UpdatePolygonLocalPosition(geofencepolygon);
            MainMap.UpdateMarkerLocalPosition(geofenceoverlay.Markers[0]);

            MainMap.Invalidate();
        }

        public void getRallyPointsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if ((MainV2.comPort.MAV.cs.capabilities & (uint)MAVLink.MAV_PROTOCOL_CAPABILITY.MISSION_RALLY) >= 0)
            {
                if (!MainV2.comPort.BaseStream.IsOpen)
                {
                    DevComponents.DotNetBar.MessageBoxEx.Show(Strings.PleaseConnect);
                    return;
                }
                mav_mission.download(MainV2.comPort, MainV2.comPort.MAV.sysid, MainV2.comPort.MAV.compid, MAVLink.MAV_MISSION_TYPE.RALLY).AwaitSync();
                return;
            }

            if (MainV2.comPort.MAV.param["RALLY_TOTAL"] == null)
            {
                DevComponents.DotNetBar.MessageBoxEx.Show("Not Supported");
                return;
            }

            if (int.Parse(MainV2.comPort.MAV.param["RALLY_TOTAL"].ToString()) < 1)
            {
                DevComponents.DotNetBar.MessageBoxEx.Show("Rally points - Nothing to download");
                return;
            }

            rallypointoverlay.Markers.Clear();

            int count = int.Parse(MainV2.comPort.MAV.param["RALLY_TOTAL"].ToString());

            for (int a = 0; a < (count); a++)
            {
                try
                {
                    var plla = MainV2.comPort.getRallyPoint(a).AwaitSync();
                    count = plla.total;
                    rallypointoverlay.Markers.Add(new GMapMarkerRallyPt(new PointLatLng(plla.plla.Lat, plla.plla.Lng))
                    {
                        Alt = (int)plla.plla.Alt,
                        ToolTipMode = MarkerTooltipMode.OnMouseOver,
                        ToolTipText = "Rally Point" + "\nAlt: " + (plla.plla.Alt * CurrentState.multiplieralt)
                    });
                }
                catch
                {
                    DevComponents.DotNetBar.MessageBoxEx.Show("获取集结点失败", Strings.ERROR);
                    return;
                }
            }

            MainMap.UpdateMarkerLocalPosition(rallypointoverlay.Markers[0]);

            MainMap.Invalidate();
        }

        public void InsertCommand(int rowIndex, MAVLink.MAV_CMD cmd, double p1, double p2, double p3, double p4, double x, double y,
            double z, object tag = null)
        {
            if (Commands.Rows.Count <= rowIndex)
            {
                AddCommand(cmd, p1, p2, p3, p4, x, y, z, tag);
                return;
            }

            Commands.Rows.Insert(rowIndex);

            this.selectedrow = rowIndex;

            FillCommand(this.selectedrow, cmd, p1, p2, p3, p4, x, y, z, tag);

            writeKML();
        }

        private void Addpolygonmarkergrid(string tag, double lng, double lat, int alt)
        {
            try
            {
                PointLatLng point = new PointLatLng(lat, lng);
                GMapMarkerPolygon m = new GMapMarkerPolygon(point);
                m.ToolTipMode = MarkerTooltipMode.Never;
                m.ToolTipText = "grid" + tag;
                m.Tag = "grid" + tag;
                if (polygonMarkersGroup.Contains(System.Convert.ToInt32(tag)))
                {
                    m.selected = true;
                }

                //MissionPlanner.GMapMarkerRectWPRad mBorders = new MissionPlanner.GMapMarkerRectWPRad(point, (int)float.Parse(TXT_WPRad.Text), MainMap);
                GMapMarkerRect mBorders = new GMapMarkerRect(point);
                {
                    mBorders.InnerMarker = m;
                }

                drawnpolygonsoverlay.Markers.Add(m);
                drawnpolygonsoverlay.Markers.Add(mBorders);
            }
            catch (Exception ex)
            {
                log.Info(ex.ToString());
            }
        }

        /// <summary>
        /// Used for current mouse position
        /// </summary>
        /// <param name="lat"></param>
        /// <param name="lng"></param>
        /// <param name="alt"></param>
        public void SetMouseDisplay(double lat, double lng, int alt)
        {
            mouseposdisplay.Lat = lat;
            mouseposdisplay.Lng = lng;
            mouseposdisplay.Alt = alt;

            CurrentChange?.Invoke(mouseposdisplay);
        }

        public void updateDisplayView()
        {
            //rallyPointsToolStripMenuItem.Visible = MainV2.DisplayConfiguration.displayRallyPointsMenu;
            //geoFenceToolStripMenuItem.Visible = MainV2.DisplayConfiguration.displayGeoFenceMenu;
            //createSplineCircleToolStripMenuItem.Visible = MainV2.DisplayConfiguration.displaySplineCircleAutoWp;
            //textToolStripMenuItem.Visible = MainV2.DisplayConfiguration.displayTextAutoWp;
            //createCircleSurveyToolStripMenuItem.Visible = MainV2.DisplayConfiguration.displayCircleSurveyAutoWp;
            //pOIToolStripMenuItem.Visible = MainV2.DisplayConfiguration.displayPoiMenu;
            //trackerHomeToolStripMenuItem.Visible = MainV2.DisplayConfiguration.displayTrackerHomeMenu;
            CHK_verifyheight.Visible = MainV2.DisplayConfiguration.displayCheckHeightBox;

            //hide dynamically generated toolstrip items in the auto WP dropdown (these do not have name objects populated)
            foreach (ToolStripItem item in planningWPToolStripMenuItem.DropDownItems)
            {
                if (item.Name.Equals(""))
                {
                    item.Visible = MainV2.DisplayConfiguration.displayPluginAutoWp;
                }
            }
        }

        public void updateHome()
        {
            quickadd = true;
            updateHomeText();
            quickadd = false;
        }

        public void WPtoScreen(List<Locationwp> cmds, bool home = true, bool append = false)
        {
            try
            {
                Invoke((MethodInvoker)delegate
                {
                    try
                    {
                        log.Info("Process " + cmds.Count);
                        processToScreen(cmds, home, append);
                    }
                    catch (Exception exx)
                    {
                        log.Info(exx.ToString());
                    }

                    MainV2.comPort.giveComport = false;

                    //BUT_read.Enabled = true;

                    writeKML();
                });
            }
            catch (Exception exx)
            {
                log.Info(exx.ToString());
            }
        }

        public class midline
        {
            public PointLatLngAlt now { get; set; }
            public PointLatLngAlt next { get; set; }
        }

        

        internal IList<Locationwp> GetFlightPlanLocations()
        {
            return GetCommandList().AsReadOnly();
        }


        private void addpolygonmarker(string tag, double lng, double lat, int alt, Color? color, GMapOverlay overlay)
        {
            try
            {
                PointLatLng point = new PointLatLng(lat, lng);
                GMarkerGoogle m = new GMarkerGoogle(point, GMarkerGoogleType.green);
                m.ToolTipMode = MarkerTooltipMode.Always;
                m.ToolTipText = tag;
                m.Tag = tag;

                GMapMarkerRect mBorders = new GMapMarkerRect(point);
                {
                    mBorders.InnerMarker = m;
                    try
                    {
                        mBorders.wprad =
                            (int)(Settings.Instance.GetFloat("TXT_WPRad") / CurrentState.multiplieralt);
                    }
                    catch (Exception ex)
                    {
                        log.Error(ex);
                    }
                    if (color.HasValue)
                    {
                        mBorders.Color = color.Value;
                    }
                }

                overlay.Markers.Add(m);
                overlay.Markers.Add(mBorders);
            }
            catch (Exception)
            {
            }
        }

        private void addpolygonmarkergrid(string tag, double lng, double lat, int alt)
        {
            try
            {
                PointLatLng point = new PointLatLng(lat, lng);
                GMarkerGoogle m = new GMarkerGoogle(point, GMarkerGoogleType.red);
                m.ToolTipMode = MarkerTooltipMode.Never;
                m.ToolTipText = "grid" + tag;
                m.Tag = "grid" + tag;

                //MissionPlanner.GMapMarkerRectWPRad mBorders = new MissionPlanner.GMapMarkerRectWPRad(point, (int)float.Parse(TXT_WPRad.Text), MainMap);
                GMapMarkerRect mBorders = new GMapMarkerRect(point);
                {
                    mBorders.InnerMarker = m;
                }

                drawnpolygonsoverlay.Markers.Add(m);
                drawnpolygonsoverlay.Markers.Add(mBorders);
            }
            catch (Exception ex)
            {
                log.Info(ex.ToString());
            }
        }

        public void NoAddPolygon()
        {
            IsDrawPolygongridMode = false;
        }

        public void AddPolygon()
        {
            IsDrawPolygongridMode = true;
            IsDrawWPMode = false;
        }

        public void AddWP()
        {
            IsDrawWPMode = true;
            IsDrawPolygongridMode = false;
        }

        public void NoAddWP()
        {
            IsDrawWPMode = false;
        }

        public void AddPolygonPointToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!IsDrawPolygongridMode)
            {
                IsDrawPolygongridMode = true;
                IsDrawWPMode = false;
                return;
            }
            AddPolygonPoint(MouseDownStart.Lat, MouseDownStart.Lng);

            isSendChange = true;
            redrawPolygonSurvey(drawnpolygon.Points.Select(p => new PointLatLngAlt(p)).ToList());
            isSendChange = false;
        }
        







        public void GetWPPoint(int index,out PointLatLngAlt wp)
        {

            double lat = double.Parse(Commands.Rows[index].Cells[Lat.Index].Value.ToString());
            double lng = double.Parse(Commands.Rows[index].Cells[Lon.Index].Value.ToString());
            double alt = double.Parse(Commands.Rows[index].Cells[Alt.Index].Value.ToString());
            //string tag = (string)Commands.Rows[index].Cells[TagData.Index].Value;
            wp = new PointLatLngAlt(lat, lng, alt);
            wp.Tag = Commands.Rows[index].Cells[Command.Index].Value.ToString();
            
            wp.Tag2 = ((altmode)Commands.Rows[index].Cells[Frame.Index].Value).ToString();
        }

        
        private void AddWPToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!IsDrawWPMode)
            {
                IsDrawPolygongridMode = false;
                IsDrawWPMode = true;
                return;
            }
            int.TryParse(TXT_DefaultAlt.Text, out int alt);

            AddWPPoint(MouseDownStart.Lat, MouseDownStart.Lng, alt);
        }


        

        public void areaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            double aream2 = Math.Abs((double)calcpolygonarea(drawnpolygon.Points));

            double areaa = aream2 * 0.000247105;

            double areaha = aream2 * 1e-4;

            double areasqf = aream2 * 10.7639;

            DevComponents.DotNetBar.MessageBoxEx.Show(
                "Area: " + aream2.ToString("0") + " m2\n\t" + areaa.ToString("0.00") + " Acre\n\t" +
                areaha.ToString("0.00") + " Hectare\n\t" + areasqf.ToString("0") + " sqf", "Area");
        }



        private double calcpolygonarea(List<PointLatLng> polygon)
        {
            // should be a closed polygon
            // coords are in lat long
            // need utm to calc area

            if (polygon.Count == 0)
            {
                DevComponents.DotNetBar.MessageBoxEx.Show("请划定区域!");
                return 0;
            }

            // close the polygon
            if (polygon[0] != polygon[polygon.Count - 1])
                polygon.Add(polygon[0]); // make a full loop

            CoordinateTransformationFactory ctfac = new CoordinateTransformationFactory();

            IGeographicCoordinateSystem wgs84 = GeographicCoordinateSystem.WGS84;

            int utmzone = (int)((polygon[0].Lng - -186.0) / 6.0);

            IProjectedCoordinateSystem utm = ProjectedCoordinateSystem.WGS84_UTM(utmzone,
                polygon[0].Lat < 0 ? false : true);

            ICoordinateTransformation trans = ctfac.CreateFromCoordinateSystems(wgs84, utm);

            double prod1 = 0;
            double prod2 = 0;

            for (int a = 0; a < (polygon.Count - 1); a++)
            {
                double[] pll1 = { polygon[a].Lng, polygon[a].Lat };
                double[] pll2 = { polygon[a + 1].Lng, polygon[a + 1].Lat };

                double[] p1 = trans.MathTransform.Transform(pll1);
                double[] p2 = trans.MathTransform.Transform(pll2);

                prod1 += p1[0] * p2[1];
                prod2 += p1[1] * p2[0];
            }

            double answer = (prod1 - prod2) / 2;

            if (polygon[0] == polygon[polygon.Count - 1])
                polygon.RemoveAt(polygon.Count - 1); // unmake a full loop

            return answer;
        }

        private void ChangeColumnHeader(string command)
        {
            try
            {
                if (cmdParamNames.ContainsKey(command))
                    for (int i = 1; i <= 7; i++)
                        Commands.Columns[i].HeaderText = cmdParamNames[command][i - 1];
                else
                    for (int i = 1; i <= 7; i++)
                        Commands.Columns[i].HeaderText = "setme";
            }
            catch (Exception ex)
            {
                DevComponents.DotNetBar.MessageBoxEx.Show(ex.ToString());
            }
        }

        public void Chk_grid_CheckedChanged(object sender, EventArgs e)
        {
            grid = chk_grid.Checked;
        }

        public void CHK_splinedefault_CheckedChanged(object sender, EventArgs e)
        {
            splinemode = CHK_splinedefault.Checked;
        }

        public void ClearMission()
        {
            ClearMissionToolStripMenuItem_Click(this, null);
        }

        public void ClearMissionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            quickadd = true;

            // mono fix
            try
            {
                Commands.CurrentCell = null;
            }
            catch { }

            Commands.Rows.Clear();

            selectedrow = 0;
            quickadd = false;

            isSendChange = true;
            writeKML();
            isSendChange = false;
        }

        public void ClearPloygon()
        {
            ClearPolygonToolStripMenuItem_Click(this, null);
        }

        public void ClearPolygonToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //polygongridmode = false;
            if (drawnpolygon == null)
                return;
            drawnpolygon.Points.Clear();
            drawnpolygonsoverlay.Markers.Clear();
            MainMap.Invalidate();

            redrawPolygonSurvey(drawnpolygon.Points.Select(p => new PointLatLngAlt(p)).ToList());
        }

        public void clearRallyPointsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                MainV2.comPort.setParam((byte)MainV2.comPort.sysidcurrent, (byte)MainV2.comPort.compidcurrent, "RALLY_TOTAL", 0);
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
            rallypointoverlay.Markers.Clear();
            MainV2.comPort.MAV.rallypoints.Clear();
        }

        public void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //FENCE_ENABLE ON COPTER
            //FENCE_ACTION ON PLANE

            try
            {
                MainV2.comPort.setParam((byte)MainV2.comPort.sysidcurrent, (byte)MainV2.comPort.compidcurrent, "FENCE_ENABLE", 0);
            }
            catch
            {
                DevComponents.DotNetBar.MessageBoxEx.Show("设置 FENCE_ENABLE 失败");
                return;
            }

            try
            {
                MainV2.comPort.setParam((byte)MainV2.comPort.sysidcurrent, (byte)MainV2.comPort.compidcurrent, "FENCE_ACTION", 0);
            }
            catch
            {
                DevComponents.DotNetBar.MessageBoxEx.Show("设置 FENCE_ACTION 失败");
                return;
            }

            try
            {
                MainV2.comPort.setParam((byte)MainV2.comPort.sysidcurrent, (byte)MainV2.comPort.compidcurrent, "FENCE_TOTAL", 0);
            }
            catch
            {
                CustomMessageBox.Show("设置 FENCE_TOTAL 失败");
                return;
            }

            // clear all
            drawnpolygonsoverlay.Polygons.Clear();
            drawnpolygonsoverlay.Markers.Clear();
            geofenceoverlay.Polygons.Clear();
            geofencepolygon.Points.Clear();
        }

        public void CMB_altmode_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (CMB_altmode.SelectedValue == null)
            {
                CMB_altmode.SelectedIndex = 0;
            }
            else
            {
                currentaltmode = (altmode)CMB_altmode.SelectedValue;
            }
        }


        private void comboBoxMapType_SelectedValueChanged(object sender, EventArgs e)
        {
            try
            {
                // check if we are setting the initial state
                if (MainMap.MapProvider != GMapProviders.EmptyProvider && (GMapProvider)comboBoxMapType.SelectedItem == MapboxUser.Instance)
                {
                    var url = Settings.Instance["MapBoxURL", ""];
                    InputBox.Show("Enter MapBox Share URL", "Enter MapBox Share URL", ref url);
                    var match = Regex.Matches(url, @"\/styles\/[^\/]+\/([^\/]+)\/([^\/\.]+).*access_token=([^#&=]+)");
                    if (match != null)
                    {
                        MapboxUser.Instance.UserName = match[0].Groups[1].Value;
                        MapboxUser.Instance.StyleId = match[0].Groups[2].Value;
                        MapboxUser.Instance.MapKey = match[0].Groups[3].Value;
                        Settings.Instance["MapBoxURL"] = url;
                    }
                    else
                    {
                        DevComponents.DotNetBar.MessageBoxEx.Show(Strings.InvalidField, Strings.ERROR);
                        return;
                    }
                }

                MainMap.MapProvider = (GMapProvider)comboBoxMapType.SelectedItem;
                FlightData.mymap.MapProvider = (GMapProvider)comboBoxMapType.SelectedItem;
                Settings.Instance["MapType"] = comboBoxMapType.Text;
            }
            catch (Exception ex)
            {
                log.Error(ex);
                DevComponents.DotNetBar.MessageBoxEx.Show("Map change failed. try zooming out first.");
            }
        }

        /// <summary>
        /// used to control buttons in the datagrid
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Commands_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.RowIndex < 0)
                    return;
                if (e.ColumnIndex == Delete.Index && (e.RowIndex + 0) < Commands.RowCount) // delete
                {
                    quickadd = true;
                    // mono fix
                    Commands.CurrentCell = null;
                    Commands.Rows.RemoveAt(e.RowIndex);
                    quickadd = false;
                    writeKML();
                }
                if (e.ColumnIndex == Up.Index && e.RowIndex != 0) // up
                {
                    DataGridViewRow myrow = Commands.CurrentRow;
                    Commands.Rows.Remove(myrow);
                    Commands.Rows.Insert(e.RowIndex - 1, myrow);
                    writeKML();
                }
                if (e.ColumnIndex == Down.Index && e.RowIndex < Commands.RowCount - 1) // down
                {
                    DataGridViewRow myrow = Commands.CurrentRow;
                    Commands.Rows.Remove(myrow);
                    Commands.Rows.Insert(e.RowIndex + 1, myrow);
                    writeKML();
                }
            }
            catch (Exception)
            {
                DevComponents.DotNetBar.MessageBoxEx.Show("Row error");
            }
        }

        public void Commands_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            // we have modified a utm coords
            if (e.ColumnIndex == coordZone.Index ||
                e.ColumnIndex == coordNorthing.Index ||
                e.ColumnIndex == coordEasting.Index)
            {
                convertFromUTM(e.RowIndex);
            }

            if (e.ColumnIndex == MGRS.Index)
            {
                convertFromMGRS(e.RowIndex);
            }

            // we have modified a ll coord
            if (e.ColumnIndex == Lat.Index ||
                e.ColumnIndex == Lon.Index)
            {
                try
                {
                    var lat = double.Parse(Commands.Rows[e.RowIndex].Cells[Lat.Index].Value.ToString());
                    var lng = double.Parse(Commands.Rows[e.RowIndex].Cells[Lon.Index].Value.ToString());
                    convertFromGeographic(lat, lng);
                }
                catch (Exception ex)
                {
                    log.Error(ex);
                    DevComponents.DotNetBar.MessageBoxEx.Show("无效的经度/纬度, 请修复", Strings.ERROR);
                }
            }

            Commands_RowEnter(null,
                new DataGridViewCellEventArgs(Commands.CurrentCell.ColumnIndex, Commands.CurrentCell.RowIndex));

            try
            {
                writeKML();
            }
            catch (FormatException)
            {
                DevComponents.DotNetBar.MessageBoxEx.Show(Strings.InvalidNumberEntered, Strings.ERROR);
            }
        }

        public void Commands_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            log.Info(e.Exception + " " + e.Context + " col " + e.ColumnIndex);
            e.Cancel = false;
            e.ThrowException = false;
            //throw new NotImplementedException();
        }

        public void Commands_DefaultValuesNeeded(object sender, DataGridViewRowEventArgs e)
        {
            e.Row.Cells[Frame.Index].Value = CMB_altmode.SelectedValue;
            e.Row.Cells[Delete.Index].Value = "X";
            e.Row.Cells[Up.Index].Value = Resources.up;
            e.Row.Cells[Down.Index].Value = Resources.down;
        }

        public void Commands_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            if (e.Control.GetType() == typeof(DataGridViewComboBoxEditingControl))
            {
                var temp = ((ComboBox)e.Control);
                ((ComboBox)e.Control).SelectionChangeCommitted -= Commands_SelectionChangeCommitted;
                ((ComboBox)e.Control).SelectionChangeCommitted += Commands_SelectionChangeCommitted;
                ((ComboBox)e.Control).ForeColor = Color.White;
                ((ComboBox)e.Control).BackColor = Color.FromArgb(0x43, 0x44, 0x45);
                Debug.WriteLine("Setting event handle");
            }
        }

        /// <summary>
        /// Used to update column headers
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Commands_RowEnter(object sender, DataGridViewCellEventArgs e)
        {
            if (quickadd)
                return;
            try
            {
                selectedrow = e.RowIndex;
                string option = Commands[Command.Index, selectedrow].EditedFormattedValue.ToString();
                string cmd;
                try
                {
                    if (Commands[Command.Index, selectedrow].Value != null)
                        cmd = Commands[Command.Index, selectedrow].Value.ToString();
                    else
                        cmd = option;
                }
                catch
                {
                    cmd = option;
                }
                //Console.WriteLine("editformat " + option + " value " + cmd);
                ChangeColumnHeader(cmd);

                if (cmd == "WAYPOINT")
                {
                }

                //  writeKML();
            }
            catch (Exception ex)
            {
                DevComponents.DotNetBar.MessageBoxEx.Show(ex.ToString());
            }
        }

        public void Commands_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            for (int i = 0; i < Commands.ColumnCount; i++)
            {
                DataGridViewCell tcell = Commands.Rows[e.RowIndex].Cells[i];
                if (tcell.GetType() == typeof(DataGridViewTextBoxCell))
                {
                    if (tcell.Value == null)
                        tcell.Value = "0";
                }
            }

            DataGridViewComboBoxCell cell = Commands.Rows[e.RowIndex].Cells[Command.Index] as DataGridViewComboBoxCell;
            if (cell.Value == null)
            {
                cell.Value = "WAYPOINT";
                cell.DropDownWidth = 200;
                Commands.Rows[e.RowIndex].Cells[Delete.Index].Value = "X";
                if (!quickadd)
                {
                    Commands_RowEnter(sender, new DataGridViewCellEventArgs(0, e.RowIndex - 0)); // do header labels
                    Commands_RowValidating(sender, new DataGridViewCellCancelEventArgs(0, e.RowIndex));
                    // do default values
                }
            }

            DataGridViewComboBoxCell cellFrame = Commands.Rows[e.RowIndex].Cells[Frame.Index] as DataGridViewComboBoxCell;
            if (cellFrame.Value == null)
            {
                cellFrame.Value = CMB_altmode.SelectedValue;
            }


            if (quickadd)
                return;

            try
            {
                Commands.CurrentCell = Commands.Rows[e.RowIndex].Cells[0];

                if (Commands.Rows.Count > 1 && e.RowIndex != 0)
                {
                    if (Commands.Rows[e.RowIndex - 1].Cells[Command.Index].Value.ToString() == "WAYPOINT")
                    {
                        Commands.Rows[e.RowIndex].Selected = true; // highlight row
                    }
                    else
                    {
                        Commands.CurrentCell = Commands[1, e.RowIndex - 1];
                        //Commands_RowEnter(sender, new DataGridViewCellEventArgs(0, e.RowIndex-1));
                    }
                }
            }
            catch (Exception)
            {
            }
            // Commands.EndEdit();
        }

        public void Commands_RowsRemoved(object sender, DataGridViewRowsRemovedEventArgs e)
        {
            writeKML();
        }

        public void Commands_RowValidating(object sender, DataGridViewCellCancelEventArgs e)
        {
            selectedrow = e.RowIndex;
            Commands_RowEnter(sender, new DataGridViewCellEventArgs(0, e.RowIndex - 0));
            // do header labels - encure we dont 0 out valid colums
            int cols = Commands.Columns.Count;
            for (int a = 1; a < cols; a++)
            {
                DataGridViewTextBoxCell cell;
                cell = Commands.Rows[selectedrow].Cells[a] as DataGridViewTextBoxCell;

                if (Commands.Columns[a].HeaderText.Equals("") && cell != null && cell.Value == null)
                {
                    cell.Value = "0";
                }
                else
                {
                    if (cell != null && (cell.Value == null || cell.Value.ToString() == ""))
                    {
                        cell.Value = "?";
                    }
                }
            }
        }

        private void Commands_SelectionChangeCommitted(object sender, EventArgs e)
        {
            // update row headers
            ((ComboBox)sender).ForeColor = Color.White;
            ChangeColumnHeader(((ComboBox)sender).Text);
            try
            {
                // default takeoff to non 0 alt
                if (((ComboBox)sender).Text == "TAKEOFF")
                {
                    if (Commands.Rows[selectedrow].Cells[Alt.Index].Value != null && Commands.Rows[selectedrow].Cells[Alt.Index].Value.ToString() == "0") Commands.Rows[selectedrow].Cells[Alt.Index].Value = TXT_DefaultAlt.Text;
                }

                // default land to 0
                if (((ComboBox)sender).Text == "LAND")
                {
                    if (Commands.Rows[selectedrow].Cells[Alt.Index].Value != null) Commands.Rows[selectedrow].Cells[Alt.Index].Value = "0";
                }

                // default to take shot
                if (((ComboBox)sender).Text == "DO_DIGICAM_CONTROL")
                {
                    if (Commands.Rows[selectedrow].Cells[Lat.Index].Value != null && Commands.Rows[selectedrow].Cells[Lat.Index].Value.ToString() == "0") Commands.Rows[selectedrow].Cells[Lat.Index].Value = (1).ToString();
                }

                if (((ComboBox)sender).Text == "UNKNOWN")
                {
                    string cmdid = "-1";
                    if (InputBox.Show("Mavlink ID", "请输入指令 ID", ref cmdid) == DialogResult.OK)
                    {
                        if (cmdid != "-1")
                        {
                            Commands.Rows[selectedrow].Cells[Command.Index].Tag = ushort.Parse(cmdid);
                        }
                    }
                }

                for (int i = 0; i < Commands.ColumnCount; i++)
                {
                    DataGridViewCell tcell = Commands.Rows[selectedrow].Cells[i];
                    if (tcell.GetType() == typeof(DataGridViewTextBoxCell))
                    {
                        if (tcell.Value.ToString() == "?")
                            tcell.Value = "0";
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }

        /// <summary>
        /// Saves this forms config to MAIN, where it is written in a global config
        /// </summary>
        /// <param name="write">true/false</param>
        private void config(bool write)
        {
            if (write)
            {
                Settings.Instance["FP_HomeLat"] = homePosition.Lat.ToString();
                Settings.Instance["FP_HomeLng"] = homePosition.Lng.ToString();
                Settings.Instance["FP_HomeAlt"] = homePosition.Alt.ToString();
                Settings.Instance["FP_HomeFrame"] = homePosition.Tag2.ToString();

                Settings.Instance["FP_WPRad"] = TXT_WPRad.Text;

                Settings.Instance["FP_LoiterRad"] = TXT_loiterrad.Text;

                Settings.Instance["FP_DefaultAlt"] = TXT_DefaultAlt.Text;

                Settings.Instance["FP_AltMode"] = CMB_altmode.Text;

                Settings.Instance["FP_AltWarn"] = TXT_altwarn.Text;

                Settings.Instance["FP_CoordSystem"] = coordSystem.ToString();
            }
            else
            {
                foreach (string key in Settings.Instance.Keys)
                {
                    switch (key)
                    {
                        case "FP_HomeLat":
                            if (double.TryParse(Settings.Instance[key], out double lat))
                                homePosition.Lat = lat;
                            break;
                        case "FP_HomeLng":
                            if (double.TryParse(Settings.Instance[key], out double lng))
                                homePosition.Lng = lng;
                            break;
                        case "FP_HomeAlt":
                            if (double.TryParse(Settings.Instance[key], out double alt))
                                homePosition.Alt = alt;
                            break;
                        case "FP_HomeFrame":
                            homePosition.Tag2 = "" + Settings.Instance[key];
                            break;
                        case "FP_WPRad":
                            TXT_WPRad.Text = "" + Settings.Instance[key];
                            break;
                        case "FP_LoiterRad":
                            TXT_loiterrad.Text = "" + Settings.Instance[key];
                            break;
                        case "FP_DefaultAlt":
                            TXT_DefaultAlt.Text = "" + Settings.Instance[key];
                            break;
                        case "FP_AltMode":
                            CMB_altmode.Text = "" + Settings.Instance[key];
                            break;
                        case "FP_AltWarn":
                            TXT_altwarn.Text = "" + Settings.Instance["FP_AltWarn"];
                            break;
                        case "FP_CoordSystem":
                            SetCoordSystemHandle("" + Settings.Instance[key]);
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        public void ContextMeasure_Click(object sender, EventArgs e)
        {
            if (startmeasure.IsEmpty)
            {
                startmeasure = MouseDownStart;
                polygonsoverlay.Markers.Add(new GMarkerGoogle(MouseDownStart, GMarkerGoogleType.red));
                MainMap.Invalidate();
                DevComponents.DotNetBar.MessageBoxEx.Show("Measure Dist",
                    "You can now pan/zoom around.\nClick this option again to get the distance.");
            }
            else
            {
                List<PointLatLng> polygonPoints = new List<PointLatLng>
                {
                    startmeasure,
                    MouseDownStart
                };

                GMapPolygon line = new GMapPolygon(polygonPoints, "measure dist");
                line.Stroke.Color = Color.Green;

                polygonsoverlay.Polygons.Add(line);

                polygonsoverlay.Markers.Add(new GMarkerGoogle(MouseDownStart, GMarkerGoogleType.red));
                MainMap.Invalidate();
                DevComponents.DotNetBar.MessageBoxEx.Show("Distance: " +
                                      FormatDistance(MainMap.MapProvider.Projection.GetDistance(startmeasure, MouseDownStart), true) +
                                      " AZ: " +
                                      (MainMap.MapProvider.Projection.GetBearing(startmeasure, MouseDownStart)
                                          .ToString("0")));
                polygonsoverlay.Polygons.Remove(line);
                polygonsoverlay.Markers.Clear();
                startmeasure = new PointLatLng();
            }
        }

        public void ContextMenuStripMain_Closed(object sender, ToolStripDropDownClosedEventArgs e)
        {
            //if (e.CloseReason.ToString() == "AppClicked" || e.CloseReason.ToString() == "AppFocusChange")
            IsMouseClickOffMenu = true;
        }

        public void ContextMenuStripMain_Opening(object sender, CancelEventArgs e)
        {
            if (CurentRectMarker == null && CurrentRallyPt == null && wpMarkersGroup.Count == 0 && polygonMarkersGroup.Count == 0)
            {
                deleteMarkerToolStripMenuItem.Enabled = false;
            }
            else
            {
                deleteMarkerToolStripMenuItem.Enabled = true;
            }

            if (polygonMarkersGroup.Count > 0 || CurentRectMarker != null && CurentRectMarker.InnerMarker.Tag.ToString().Contains("grid"))
            {
                deletePolygonPointToolStripMenuItem.Enabled = true;
            }
            else
            {
                deletePolygonPointToolStripMenuItem.Enabled = false;
            }

            if (wpMarkersGroup.Count > 0 || (CurentRectMarker != null && Regex.IsMatch(CurentRectMarker.InnerMarker.Tag.ToString(), @"^\d+$")))
            {
                deleteWPToolStripMenuItem.Enabled = true;
            }
            else
            {
                deleteWPToolStripMenuItem.Enabled = false;
            }

            //if (MainV2.comPort != null && MainV2.comPort.MAV != null)
            //{
            //    if ((MainV2.comPort.MAV.cs.capabilities & (int)MAVLink.MAV_PROTOCOL_CAPABILITY.MISSION_FENCE) > 0)
            //    {
            //        geoFenceToolStripMenuItem.Visible = false;
            //        rallyPointsToolStripMenuItem.Visible = false;
            //    }
            //    else
            //    {
            //        geoFenceToolStripMenuItem.Visible = true;
            //        rallyPointsToolStripMenuItem.Visible = true;
            //    }
            //}

            IsMouseClickOffMenu = true; // Just incase
        }

        public void ContextMenuStripPoly_Opening(object sender, CancelEventArgs e)
        {
            // update the displayed items
            //if ((MAVLink.MAV_MISSION_TYPE)cmb_missiontype.SelectedValue == MAVLink.MAV_MISSION_TYPE.RALLY)
            //{
            //    fenceInclusionToolStripMenuItem.Visible = false;
            //    fenceExclusionToolStripMenuItem.Visible = false;
            //}
            //else if ((MAVLink.MAV_MISSION_TYPE)cmb_missiontype.SelectedValue == MAVLink.MAV_MISSION_TYPE.FENCE)
            //{
            //    fenceInclusionToolStripMenuItem.Visible = true;
            //    fenceExclusionToolStripMenuItem.Visible = true;
            //}
            //else
            //{
            //    fenceInclusionToolStripMenuItem.Visible = false;
            //    fenceExclusionToolStripMenuItem.Visible = false;
            //}
            IsMouseClickOffMenu = false; // Just incase
        }

        public void ContextMenuStripPoly_Close(object sender, ToolStripDropDownClosedEventArgs e)
        {
            //if (e.CloseReason.ToString() == "AppClicked" || e.CloseReason.ToString() == "AppFocusChange")
            //    isMouseClickOffMenu = true;
        }

        private void convertFromGeographic(double lat, double lng)
        {
            if (lat == 0 && lng == 0)
            {
                return;
            }

            // always update other systems, incase user switchs while planning
            try
            {
                //UTM
                var temp = new PointLatLngAlt(lat, lng);
                int zone = temp.GetUTMZone();
                var temp2 = temp.ToUTM();
                Commands[coordZone.Index, selectedrow].Value = zone;
                Commands[coordEasting.Index, selectedrow].Value = temp2[0].ToString("0.000");
                Commands[coordNorthing.Index, selectedrow].Value = temp2[1].ToString("0.000");
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
            try
            {
                //MGRS
                Commands[MGRS.Index, selectedrow].Value = ((MGRS)new Geographic(lng, lat)).ToString();
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }

        private void convertFromMGRS(int rowindex)
        {
            try
            {
                var mgrs = Commands[MGRS.Index, rowindex].Value.ToString();

                MGRS temp = new MGRS(mgrs);

                var convert = temp.ConvertTo<Geographic>();

                if (convert.Latitude == 0 || convert.Longitude == 0)
                    return;

                Commands[Lat.Index, rowindex].Value = convert.Latitude.ToString();
                Commands[Lon.Index, rowindex].Value = convert.Longitude.ToString();

            }
            catch (Exception ex)
            {
                log.Error(ex);
                return;
            }
        }

        private void convertFromUTM(int rowindex)
        {
            try
            {
                var zone = int.Parse(Commands[coordZone.Index, rowindex].Value.ToString());

                var east = double.Parse(Commands[coordEasting.Index, rowindex].Value.ToString());

                var north = double.Parse(Commands[coordNorthing.Index, rowindex].Value.ToString());

                if (east == 0 && north == 0)
                {
                    return;
                }

                var utm = new utmpos(east, north, zone);

                Commands[Lat.Index, rowindex].Value = utm.ToLLA().Lat;
                Commands[Lon.Index, rowindex].Value = utm.ToLLA().Lng;

            }
            catch (Exception ex)
            {
                log.Error(ex);
                return;
            }
        }

        private void ConvertUTMCoords(GMapRoute route, int zone = -9999)
        {
            for (int i = 0; i < route.Points.Count; i++)
            {
                var pnt = route.Points[i];
                // load input
                utmpos pos = new utmpos(pnt.Lng, pnt.Lat, zone);
                // convert to geo
                var llh = pos.ToLLA();
                // save it back
                route.Points[i] = llh;
                //route.Points[i].Lng = llh.Lng;
            }
        }


        private Locationwp DataViewtoLocationwp(int a)
        {
            try
            {
                Locationwp temp = new Locationwp();
                if (Commands.Rows[a].Cells[Command.Index].Value.ToString().Contains("UNKNOWN"))
                {
                    temp.id = (ushort)Commands.Rows[a].Cells[Command.Index].Tag;
                }
                else
                {
                    temp.id =
                        (ushort)
                        Enum.Parse(typeof(MAVLink.MAV_CMD), Commands.Rows[a].Cells[Command.Index].Value.ToString(),
                            false);
                }
                temp.p1 = float.Parse(Commands.Rows[a].Cells[Param1.Index].Value.ToString());

                temp.alt =
                    (float)
                    (double.Parse(Commands.Rows[a].Cells[Alt.Index].Value.ToString()) / CurrentState.multiplieralt);
                temp.lat = (double.Parse(Commands.Rows[a].Cells[Lat.Index].Value.ToString()));
                temp.lng = (double.Parse(Commands.Rows[a].Cells[Lon.Index].Value.ToString()));

                temp.p2 = (float)(double.Parse(Commands.Rows[a].Cells[Param2.Index].Value.ToString()));
                temp.p3 = (float)(double.Parse(Commands.Rows[a].Cells[Param3.Index].Value.ToString()));
                temp.p4 = (float)(double.Parse(Commands.Rows[a].Cells[Param4.Index].Value.ToString()));

                temp.Tag = Commands.Rows[a].Cells[TagData.Index].Value;

                temp.frame = (byte)(int)Commands.Rows[a].Cells[Frame.Index].Value;

                return temp;
            }
            catch (Exception ex)
            {
                throw new FormatException("Invalid number on row " + (a + 1).ToString(), ex);
            }
        }

        public void DeletePolygonPointToolStripMenuItem_Click(object sender, EventArgs e)
        {

            if (CurentRectMarker != null)
            {
                if (int.TryParse(CurentRectMarker.InnerMarker.Tag.ToString().Replace("grid", ""), out int no))
                {
                    try
                    {
                        drawnpolygon.Points.RemoveAt(no - 1);
                        isSendChange = true;
                        redrawPolygonSurvey(drawnpolygon.Points.Select(p => new PointLatLngAlt(p)).ToList());
                        isSendChange = false;
                    }
                    catch (Exception ex)
                    {
                        log.Error(ex);
                        //CustomMessageBox.Show("Remove point Failed. Please try again.");
                    }
                }
            }
            if (currentMarker != null)
                CurentRectMarker = null;
            redrawPolygonSurvey(drawnpolygon.Points.Select(p => new PointLatLngAlt(p)).ToList());
        }

        public void DeleteWPToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (CurentRectMarker != null)
            {
                if (int.TryParse(CurentRectMarker.InnerMarker.Tag.ToString(), out int no))
                {
                    DeleteWPPoint(no);
                }
            }

            if (currentMarker != null)
                CurentRectMarker = null;

            isSendChange = true;
            writeKML();
            isSendChange = false;
        }

        public void DeleteCurrentWP()
        {
            if (wpMarkersGroup.Count > 0)
            {
                for (int a = Commands.Rows.Count; a > 0; a--)
                {
                    try
                    {
                        if (wpMarkersGroup.Contains(a)) Commands.Rows.RemoveAt(a - 1); // home is 0
                    }
                    catch (Exception ex)
                    {
                        log.Error(ex);
                        DevComponents.DotNetBar.MessageBoxEx.Show("error selecting wp, please try again.");
                    }
                }
                wpMarkersGroupClear();
            }
        }

        public void DeleteCurrentPolygon()
        {
            if (polygonMarkersGroup.Count > 0)
            {
                for (int a = drawnpolygon.LocalPoints.Count; a > 0; a--)
                {
                    try
                    {
                        if (polygonMarkersGroup.Contains(a))
                        {
                            drawnpolygon.Points.RemoveAt(a - 1);
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Error(ex);
                        DevComponents.DotNetBar.MessageBoxEx.Show("error selecting polygon, please try again.");
                    }
                }
                polygonMarkersGroupClear();
            }
            if (currentMarker != null)
                CurentRectMarker = null;
        }

        public void DeleteMarkerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (CurentRectMarker != null)
            {
                if (int.TryParse(CurentRectMarker.InnerMarker.Tag.ToString(), out int no))
                {
                    try
                    {
                        if ((MAVLink.MAV_MISSION_TYPE)cmb_missiontype.SelectedValue ==
                            MAVLink.MAV_MISSION_TYPE.FENCE)
                            ReCalcFence(no - 1, false, true);

                        Commands.Rows.RemoveAt(no - 1); // home is 0
                    }
                    catch (Exception ex)
                    {
                        log.Error(ex);
                        DevComponents.DotNetBar.MessageBoxEx.Show("error selecting wp, please try again.");
                    }
                }
                else if (int.TryParse(CurentRectMarker.InnerMarker.Tag.ToString().Replace("grid", ""), out no))
                {
                    try
                    {
                        drawnpolygon.Points.RemoveAt(no - 1);

                        isSendChange = true;
                        redrawPolygonSurvey(drawnpolygon.Points.Select(p => new PointLatLngAlt(p)).ToList());
                        isSendChange = false;
                    }
                    catch (Exception ex)
                    {
                        log.Error(ex);
                        //CustomMessageBox.Show("Remove point Failed. Please try again.");
                    }
                }
            }
            else if (CurrentRallyPt != null)
            {
                rallypointoverlay.Markers.Remove(CurrentRallyPt);
                MainMap.Invalidate(true);

                CurrentRallyPt = null;
            }
            if (wpMarkersGroup.Count > 0)
            {
                for (int a = Commands.Rows.Count; a > 0; a--)
                {
                    try
                    {
                        if (wpMarkersGroup.Contains(a)) Commands.Rows.RemoveAt(a - 1); // home is 0
                    }
                    catch (Exception ex)
                    {
                        log.Error(ex);
                        DevComponents.DotNetBar.MessageBoxEx.Show("error selecting wp, please try again.");
                    }
                }
                wpMarkersGroupClear();
            }
            if (polygonMarkersGroup.Count > 0)
            {
                for (int a = drawnpolygon.LocalPoints.Count; a > 0; a--)
                {
                    try
                    {
                        if (polygonMarkersGroup.Contains(a))
                        {
                            drawnpolygon.Points.RemoveAt(a - 1);
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Error(ex);
                        DevComponents.DotNetBar.MessageBoxEx.Show("error selecting polygon, please try again.");
                    }
                }
                polygonMarkersGroupClear();
            }


            if (currentMarker != null)
                CurentRectMarker = null;

            isSendChange = true;
            writeKML();
            isSendChange = false;
        }

        private void Dxf_newLine(dxf sender, netDxf.Entities.Line line)
        {
            var route = new GMapRoute(line.Handle);
            route.Points.Add(new PointLatLng(line.StartPoint.Y, line.StartPoint.X));
            route.Points.Add(new PointLatLng(line.EndPoint.Y, line.EndPoint.X));

            route.Stroke = new Pen(Color.FromArgb(line.Color.R, line.Color.G, line.Color.B));

            if (sender.Tag != null)
                ConvertUTMCoords(route, int.Parse(sender.Tag.ToString()));

            kmlpolygonsoverlay.Routes.Add(route);
        }

        private void Dxf_newLwPolyline(dxf sender, netDxf.Entities.LwPolyline pline)
        {
            var route = new GMapRoute(pline.Handle);
            foreach (var item in pline.Vertexes)
            {
                route.Points.Add(new PointLatLng(item.Position.Y, item.Position.X));
            }

            route.Stroke = new Pen(Color.FromArgb(pline.Color.R, pline.Color.G, pline.Color.B));

            if (sender.Tag != null)
                ConvertUTMCoords(route, int.Parse(sender.Tag.ToString()));

            kmlpolygonsoverlay.Routes.Add(route);
        }

        private void Dxf_newMLine(dxf sender, netDxf.Entities.MLine pline)
        {
            var route = new GMapRoute(pline.Handle);
            foreach (var item in pline.Vertexes)
            {
                route.Points.Add(new PointLatLng(item.Location.Y, item.Location.X));
            }

            route.Stroke = new Pen(Color.FromArgb(pline.Color.R, pline.Color.G, pline.Color.B));

            if (sender.Tag != null)
                ConvertUTMCoords(route, int.Parse(sender.Tag.ToString()));

            kmlpolygonsoverlay.Routes.Add(route);
        }

        private void Dxf_newPolyLine(dxf sender, netDxf.Entities.Polyline pline)
        {
            var route = new GMapRoute(pline.Handle);
            foreach (var item in pline.Vertexes)
            {
                route.Points.Add(new PointLatLng(item.Position.Y, item.Position.X));
            }

            route.Stroke = new Pen(Color.FromArgb(pline.Color.R, pline.Color.G, pline.Color.B));

            if (sender.Tag != null)
                ConvertUTMCoords(route, int.Parse(sender.Tag.ToString()));

            kmlpolygonsoverlay.Routes.Add(route);
        }


        private void FetchPath()
        {
            PointLatLngAlt lastpnt = null;

            string maxzoomstring = "20";
            if (InputBox.Show("max zoom", "Enter the max zoom to prefetch to.", ref maxzoomstring) != DialogResult.OK)
                return;

            int maxzoom = 20;
            if (!int.TryParse(maxzoomstring, out maxzoom))
            {
                DevComponents.DotNetBar.MessageBoxEx.Show(Strings.InvalidNumberEntered, Strings.ERROR);
                return;
            }

            fetchpathrip = true;

            maxzoom = Math.Min(maxzoom, MainMap.MaxZoom);

            // zoom
            for (int i = 1; i <= maxzoom; i++)
            {
                // exit if reqested
                if (!fetchpathrip)
                    break;

                lastpnt = null;
                // location
                foreach (var pnt in pointlist)
                {
                    if (pnt == null)
                        continue;

                    // exit if reqested
                    if (!fetchpathrip)
                        break;

                    // setup initial enviroment
                    if (lastpnt == null)
                    {
                        lastpnt = pnt;
                        continue;
                    }

                    RectLatLng area = new RectLatLng();
                    double top = Math.Max(lastpnt.Lat, pnt.Lat);
                    double left = Math.Min(lastpnt.Lng, pnt.Lng);
                    double bottom = Math.Min(lastpnt.Lat, pnt.Lat);
                    double right = Math.Max(lastpnt.Lng, pnt.Lng);

                    area.LocationTopLeft = new PointLatLng(top, left);
                    area.HeightLat = top - bottom;
                    area.WidthLng = right - left;

                    TilePrefetcher obj = new TilePrefetcher();
                    ThemeManager.ApplyThemeTo(obj);
                    obj.KeyDown += obj_KeyDown;
                    obj.ShowCompleteMessage = false;
                    obj.Start(area, i, MainMap.MapProvider, 0, 0);

                    if (obj.UserAborted)
                    {
                        fetchpathrip = false;
                        break;
                    }

                    lastpnt = pnt;
                }
            }
        }

        private void FillCommand(int rowIndex, MAVLink.MAV_CMD cmd, double p1, double p2, double p3, double p4, double x,
            double y, double z, object tag = null)
        {
            Commands.Rows[rowIndex].Cells[Command.Index].Value = cmd.ToString();
            Commands.Rows[rowIndex].Cells[TagData.Index].Tag = tag;
            Commands.Rows[rowIndex].Cells[TagData.Index].Value = tag;

            ChangeColumnHeader(cmd.ToString());

            // switch wp to spline if spline checked
            if (splinemode && cmd == MAVLink.MAV_CMD.WAYPOINT)
            {
                Commands.Rows[rowIndex].Cells[Command.Index].Value = MAVLink.MAV_CMD.SPLINE_WAYPOINT.ToString();
                ChangeColumnHeader(MAVLink.MAV_CMD.SPLINE_WAYPOINT.ToString());
            }

            if (cmd == MAVLink.MAV_CMD.WAYPOINT)
            {
                // add delay if supplied
                Commands.Rows[rowIndex].Cells[Param1.Index].Value = p1;

                setfromMap(y, x, (int)z, Math.Round(p1, 1));
            }
            else if (cmd == MAVLink.MAV_CMD.LOITER_UNLIM)
            {
                setfromMap(y, x, (int)z);
            }
            else
            {
                Commands.Rows[rowIndex].Cells[Param1.Index].Value = p1;
                Commands.Rows[rowIndex].Cells[Param2.Index].Value = p2;
                Commands.Rows[rowIndex].Cells[Param3.Index].Value = p3;
                Commands.Rows[rowIndex].Cells[Param4.Index].Value = p4;
                Commands.Rows[rowIndex].Cells[Lat.Index].Value = y;
                Commands.Rows[rowIndex].Cells[Lon.Index].Value = x;
                Commands.Rows[rowIndex].Cells[Alt.Index].Value = z;
            }
        }

        public void FlightPlanner_FormClosing(object sender, FormClosingEventArgs e)
        {
            timer1.Stop();
        }

        public void FlightPlanner_Load(object sender, EventArgs e)
        {
            quickadd = true;

            Visible = false;

            config(false);

            quickadd = false;

            POI.POIModified += POI_POIModified;

            if (Settings.Instance["WMSserver"] != null)
            {
                WMSProvider.CustomWMSURL = Settings.Instance["WMSserver"];
                WMSProvider.szWmsLayer = Settings.Instance["WMSLayer"];
            }

            trackBar1.Value = (int)MainMap.Zoom;

            updateCMDParams();

            panelMap.Visible = false;

            // mono
            panelMap.Dock = DockStyle.None;
            panelMap.Dock = DockStyle.Fill;
            panelMap_Resize(null, null);

            //set home
            try
            {
                if (homePosition != null)
                {
                    MainMap.Position = new PointLatLng(homePosition.Lat, homePosition.Lng);
                    MainMap.Zoom = 16;
                }
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }

            panelMap.Refresh();

            panelMap.Visible = true;

            writeKML();

            // switch the action and wp table
            if (Settings.Instance["FP_docking"] == "Bottom")
            {
                switchDockingToolStripMenuItem_Click(null, null);
            }

            Visible = true;

            timer1.Start();
        }

        /// <summary>
        /// Format distance according to prefer distance unit
        /// </summary>
        /// <param name="distInKM">distance in kilometers</param>
        /// <param name="toMeterOrFeet">convert distance to meter or feet if true, covert to km or miles if false</param>
        /// <returns>formatted distance with unit</returns>
        private string FormatDistance(double distInKM, bool toMeterOrFeet)
        {
            string sunits = Settings.Instance["distunits"];
            distances units = distances.Meters;

            if (sunits != null)
                try
                {
                    units = (distances)Enum.Parse(typeof(distances), sunits);
                }
                catch (Exception)
                {
                }

            switch (units)
            {
                case distances.Feet:
                    return toMeterOrFeet
                        ? string.Format((distInKM * 3280.8399).ToString("0.00 ft"))
                        : string.Format((distInKM * 0.621371).ToString("0.0000 miles"));
                case distances.Meters:
                default:
                    return toMeterOrFeet
                        ? string.Format((distInKM * 1000).ToString("0.00 m"))
                        : string.Format(distInKM.ToString("0.0000 km"));
            }
        }

        public void fromSHPToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog fd = new OpenFileDialog())
            {
                fd.Filter = "Shape file|*.shp";
                DialogResult result = fd.ShowDialog();
                string file = fd.FileName;
                ProjectionInfo pStart = new ProjectionInfo();
                ProjectionInfo pESRIEnd = KnownCoordinateSystems.Geographic.World.WGS1984;
                bool reproject = false;
                // Poly Clear
                drawnpolygonsoverlay.Markers.Clear();
                drawnpolygonsoverlay.Polygons.Clear();
                drawnpolygon.Points.Clear();
                if (File.Exists(file))
                {
                    string prjfile = Path.GetDirectoryName(file) + Path.DirectorySeparatorChar +
                                     Path.GetFileNameWithoutExtension(file) + ".prj";
                    if (File.Exists(prjfile))
                    {
                        using (
                            StreamReader re =
                                File.OpenText(Path.GetDirectoryName(file) + Path.DirectorySeparatorChar +
                                              Path.GetFileNameWithoutExtension(file) + ".prj"))
                        {
                            pStart.ParseEsriString(re.ReadLine());
                            reproject = true;
                        }
                    }
                    try
                    {
                        IFeatureSet fs = FeatureSet.Open(file);
                        fs.FillAttributes();
                        int rows = fs.NumRows();
                        DataTable dtOriginal = fs.DataTable;
                        for (int row = 0; row < dtOriginal.Rows.Count; row++)
                        {
                            object[] original = dtOriginal.Rows[row].ItemArray;
                        }
                        string path = Path.GetDirectoryName(file);
                        foreach (var feature in fs.Features)
                        {
                            foreach (var point in feature.Coordinates)
                            {
                                if (reproject)
                                {
                                    double[] xyarray = { point.X, point.Y };
                                    double[] zarray = { point.Z };
                                    Reproject.ReprojectPoints(xyarray, zarray, pStart, pESRIEnd, 0, 1);
                                    point.X = xyarray[0];
                                    point.Y = xyarray[1];
                                    point.Z = zarray[0];
                                }
                                drawnpolygon.Points.Add(new PointLatLng(point.Y, point.X));
                                addpolygonmarkergrid(drawnpolygon.Points.Count.ToString(), point.X, point.Y, 0);
                            }
                            // remove loop close
                            if (drawnpolygon.Points.Count > 1 &&
                                drawnpolygon.Points[0] == drawnpolygon.Points[drawnpolygon.Points.Count - 1])
                            {
                                drawnpolygon.Points.RemoveAt(drawnpolygon.Points.Count - 1);
                            }
                            drawnpolygonsoverlay.Polygons.Add(drawnpolygon);
                            MainMap.UpdatePolygonLocalPosition(drawnpolygon);
                            MainMap.Invalidate();
                            MainMap.ZoomAndCenterMarkers(drawnpolygonsoverlay.Id);
                        }
                    }
                    catch (Exception ex)
                    {
                        DevComponents.DotNetBar.MessageBoxEx.Show(Strings.ERROR + "\n" + ex, Strings.ERROR);
                    }
                }
            }
        }

        public void GeoFenceuploadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            addPolygonMode = false;
            //FENCE_ENABLE ON COPTER
            //FENCE_ACTION ON PLANE
            if (!MainV2.comPort.MAV.param.ContainsKey("FENCE_ENABLE") && !MainV2.comPort.MAV.param.ContainsKey("FENCE_ACTION"))
            {
                DevComponents.DotNetBar.MessageBoxEx.Show("Not Supported");
                return;
            }

            if (drawnpolygon == null)
            {
                DevComponents.DotNetBar.MessageBoxEx.Show("No polygon to upload");
                return;
            }

            if (geofenceoverlay.Markers.Count == 0)
            {
                DevComponents.DotNetBar.MessageBoxEx.Show("No return location set");
                return;
            }

            if (drawnpolygon.Points.Count == 0)
            {
                DevComponents.DotNetBar.MessageBoxEx.Show("No polygon drawn");
                return;
            }

            // check if return is inside polygon
            List<PointLatLng> plll = new List<PointLatLng>(drawnpolygon.Points.ToArray());
            // close it
            plll.Add(plll[0]);
            // check it
            if (
                !pnpoly(plll.ToArray(), geofenceoverlay.Markers[0].Position.Lat, geofenceoverlay.Markers[0].Position.Lng))
            {
                DevComponents.DotNetBar.MessageBoxEx.Show("Your return location is outside the polygon");
                return;
            }

            int minalt = 0;
            int maxalt = 0;

            if (MainV2.comPort.MAV.param.ContainsKey("FENCE_MINALT"))
            {
                string minalts =
                    (int.Parse(MainV2.comPort.MAV.param["FENCE_MINALT"].ToString()) * CurrentState.multiplieralt)
                    .ToString(
                        "0");
                if (DialogResult.Cancel == InputBox.Show("Min Alt", "Box Minimum Altitude?", ref minalts))
                    return;

                if (!int.TryParse(minalts, out minalt))
                {
                    DevComponents.DotNetBar.MessageBoxEx.Show("Bad Min Alt");
                    return;
                }
            }

            if (MainV2.comPort.MAV.param.ContainsKey("FENCE_MAXALT"))
            {
                string maxalts =
                    (int.Parse(MainV2.comPort.MAV.param["FENCE_MAXALT"].ToString()) * CurrentState.multiplieralt)
                    .ToString(
                        "0");
                if (DialogResult.Cancel == InputBox.Show("Max Alt", "Box Maximum Altitude?", ref maxalts))
                    return;

                if (!int.TryParse(maxalts, out maxalt))
                {
                    DevComponents.DotNetBar.MessageBoxEx.Show("Bad Max Alt");
                    return;
                }
            }

            try
            {
                if (MainV2.comPort.MAV.param.ContainsKey("FENCE_MINALT"))
                    MainV2.comPort.setParam((byte)MainV2.comPort.sysidcurrent, (byte)MainV2.comPort.compidcurrent, "FENCE_MINALT", minalt);
                if (MainV2.comPort.MAV.param.ContainsKey("FENCE_MAXALT"))
                    MainV2.comPort.setParam((byte)MainV2.comPort.sysidcurrent, (byte)MainV2.comPort.compidcurrent, "FENCE_MAXALT", maxalt);
            }
            catch (Exception ex)
            {
                log.Error(ex);
                DevComponents.DotNetBar.MessageBoxEx.Show("Failed to set min/max fence alt");
                return;
            }

            float oldaction = (float)MainV2.comPort.MAV.param["FENCE_ACTION"];

            try
            {
                MainV2.comPort.setParam((byte)MainV2.comPort.sysidcurrent, (byte)MainV2.comPort.compidcurrent, "FENCE_ACTION", 0);
            }
            catch
            {
                DevComponents.DotNetBar.MessageBoxEx.Show("Failed to set FENCE_ACTION");
                return;
            }

            // points + return + close
            byte pointcount = (byte)(drawnpolygon.Points.Count + 2);


            try
            {
                MainV2.comPort.setParam((byte)MainV2.comPort.sysidcurrent, (byte)MainV2.comPort.compidcurrent, "FENCE_TOTAL", pointcount);
            }
            catch
            {
                DevComponents.DotNetBar.MessageBoxEx.Show("Failed to set FENCE_TOTAL");
                return;
            }

            try
            {
                byte a = 0;
                // add return loc
                MainV2.comPort.setFencePoint(a, new PointLatLngAlt(geofenceoverlay.Markers[0].Position), pointcount);
                a++;
                // add points
                foreach (var pll in drawnpolygon.Points)
                {
                    MainV2.comPort.setFencePoint(a, new PointLatLngAlt(pll), pointcount);
                    a++;
                }

                // add polygon close
                MainV2.comPort.setFencePoint(a, new PointLatLngAlt(drawnpolygon.Points[0]), pointcount);

                try
                {
                    MainV2.comPort.setParam((byte)MainV2.comPort.sysidcurrent, (byte)MainV2.comPort.compidcurrent, "FENCE_ACTION", oldaction);
                }
                catch
                {
                    DevComponents.DotNetBar.MessageBoxEx.Show("Failed to restore FENCE_ACTION");
                    return;
                }

                // clear everything
                drawnpolygonsoverlay.Polygons.Clear();
                drawnpolygonsoverlay.Markers.Clear();
                geofenceoverlay.Polygons.Clear();
                geofencepolygon.Points.Clear();

                // add polygon
                geofencepolygon.Points.AddRange(drawnpolygon.Points.ToArray());

                drawnpolygon.Points.Clear();

                geofenceoverlay.Polygons.Add(geofencepolygon);

                // update flightdata
                FlightData.geofence.Markers.Clear();
                FlightData.geofence.Polygons.Clear();
                FlightData.geofence.Polygons.Add(new GMapPolygon(geofencepolygon.Points, "gf fd")
                {
                    Stroke = geofencepolygon.Stroke,
                    Fill = Brushes.Transparent
                });
                FlightData.geofence.Markers.Add(new GMarkerGoogle(geofenceoverlay.Markers[0].Position,
                    GMarkerGoogleType.red)
                {
                    ToolTipText = geofenceoverlay.Markers[0].ToolTipText,
                    ToolTipMode = geofenceoverlay.Markers[0].ToolTipMode
                });

                MainMap.UpdatePolygonLocalPosition(geofencepolygon);
                MainMap.UpdateMarkerLocalPosition(geofenceoverlay.Markers[0]);

                MainMap.Invalidate();
            }
            catch (Exception ex)
            {
                DevComponents.DotNetBar.MessageBoxEx.Show("Failed to send new fence points " + ex, Strings.ERROR);
            }
        }


        /// <summary>
        /// Get the Google earth ALT for a given coord
        /// </summary>
        /// <param name="lat"></param>
        /// <param name="lng"></param>
        /// <returns>Altitude</returns>
        private double getGEAlt(double lat, double lng)
        {
            double alt = 0;
            //http://maps.google.com/maps/api/elevation/xml

            try
            {
                using (
                    XmlTextReader xmlreader =
                        new XmlTextReader("http://maps.google.com/maps/api/elevation/xml?locations=" +
                                          lat.ToString(new CultureInfo("en-US")) + "," +
                                          lng.ToString(new CultureInfo("en-US")) + "&sensor=true"))
                {
                    while (xmlreader.Read())
                    {
                        xmlreader.MoveToElement();
                        switch (xmlreader.Name)
                        {
                            case "elevation":
                                alt = double.Parse(xmlreader.ReadString(), new CultureInfo("en-US"));
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }

            return alt * CurrentState.multiplieralt;
        }

        private RectLatLng getPolyMinMax(GMapPolygon poly)
        {
            if (poly.Points.Count == 0)
                return new RectLatLng();

            double minx, miny, maxx, maxy;

            minx = maxx = poly.Points[0].Lng;
            miny = maxy = poly.Points[0].Lat;

            foreach (PointLatLng pnt in poly.Points)
            {
                //Console.WriteLine(pnt.ToString());
                minx = Math.Min(minx, pnt.Lng);
                maxx = Math.Max(maxx, pnt.Lng);

                miny = Math.Min(miny, pnt.Lat);
                maxy = Math.Max(maxy, pnt.Lat);
            }

            return new RectLatLng(maxy, minx, Math.Abs(maxx - minx), Math.Abs(miny - maxy));
        }

        //private void getWPs(IProgressReporterDialogue sender)
        //{
        //    var type = (MAVLink.MAV_MISSION_TYPE)Invoke((Func<MAVLink.MAV_MISSION_TYPE>)delegate
        //    {
        //        return (MAVLink.MAV_MISSION_TYPE)cmb_missiontype.SelectedValue;
        //    });

        //    List<Locationwp> cmds = Task.Run(async () => await mav_mission.download(MainV2.comPort, MainV2.comPort.MAV.sysid,
        //        MainV2.comPort.MAV.compid,
        //        type,
        //        (percent, status) =>
        //        {
        //            if (sender.doWorkArgs.CancelRequested)
        //            {
        //                sender.doWorkArgs.CancelAcknowledged = true;
        //                sender.doWorkArgs.ErrorMessage = "User Canceled";
        //                throw new Exception("User Canceled");
        //            }

        //            sender.UpdateProgressAndStatus(percent, status);
        //        }).ConfigureAwait(false)).Result;

        //    WPtoScreen(cmds);
        //}

        public void insertSplineWPToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string wpno = (selectedrow + 1).ToString("0");
            if (InputBox.Show("Insert WP", "Insert WP after wp#", ref wpno) == DialogResult.OK)
            {
                try
                {
                    Commands.Rows.Insert(int.Parse(wpno), 1);
                }
                catch
                {
                    DevComponents.DotNetBar.MessageBoxEx.Show(Strings.InvalidNumberEntered, Strings.ERROR);
                    return;
                }

                selectedrow = int.Parse(wpno);

                try
                {
                    Commands.Rows[selectedrow].Cells[Command.Index].Value = MAVLink.MAV_CMD.SPLINE_WAYPOINT.ToString();
                }
                catch
                {
                    DevComponents.DotNetBar.MessageBoxEx.Show("SPLINE_WAYPOINT command not supported.");
                    Commands.Rows.RemoveAt(selectedrow);
                    return;
                }

                ChangeColumnHeader(MAVLink.MAV_CMD.SPLINE_WAYPOINT.ToString());

                setfromMap(MouseDownStart.Lat, MouseDownStart.Lng, (int)float.Parse(TXT_DefaultAlt.Text));
            }
        }

        public void insertWpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string wpno = (selectedrow + 1).ToString("0");
            if (InputBox.Show("Insert WP", "Insert WP after wp#", ref wpno) == DialogResult.OK)
            {
                try
                {
                    Commands.Rows.Insert(int.Parse(wpno), 1);
                }
                catch
                {
                    DevComponents.DotNetBar.MessageBoxEx.Show("Invalid insert position", Strings.ERROR);
                    return;
                }

                selectedrow = int.Parse(wpno);

                ChangeColumnHeader(MAVLink.MAV_CMD.WAYPOINT.ToString());

                setfromMap(MouseDownStart.Lat, MouseDownStart.Lng, (int)float.Parse(TXT_DefaultAlt.Text));
            }
        }


        private void TiffOverlayToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MainV2.instance.LoadTiffLayer();
        }


        private void TiffManagerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MainV2.instance.ManagerTiffLayer();
        }

        public void kMLOverlayToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog fd = new OpenFileDialog())
            {
                fd.Filter = "All Supported|*.kml;*.kmz;*.dxf;*.gpkg|Google Earth KML|*.kml;*.kmz|AutoCad DXF|*.dxf|GeoPackage|*.gpkg";
                DialogResult result = fd.ShowDialog();
                string file = fd.FileName;
                if (file != "")
                {
                    kmlpolygonsoverlay.Polygons.Clear();
                    kmlpolygonsoverlay.Routes.Clear();

                    FlightData.kmlpolygons.Routes.Clear();
                    FlightData.kmlpolygons.Polygons.Clear();
                    if (file.ToLower().EndsWith("gpkg"))
                    {
                        using (var ogr = OGR.Open(file))
                        {
                            ogr.NewPoint += pnt =>
                            {
                                var mark = new GMarkerGoogle(new PointLatLngAlt(pnt), GMarkerGoogleType.brown_small);
                                FlightData.kmlpolygons.Markers.Add(mark);
                                kmlpolygonsoverlay.Markers.Add(mark);
                            };
                            ogr.NewLineString += ls =>
                            {
                                var route =
                                    new GMapRoute(ls.Select(a => new PointLatLngAlt(a.y, a.x, a.z).Point()), "")
                                    {
                                        IsHitTestVisible = false,
                                        Stroke = new Pen(Color.Red)
                                    };
                                FlightData.kmlpolygons.Routes.Add(route);
                                kmlpolygonsoverlay.Routes.Add(route);
                            };
                            ogr.NewPolygon += ls =>
                            {
                                var polygon =
                                    new GMapPolygon(ls.Select(a => new PointLatLngAlt(a.y, a.x, a.z).Point()).ToList(), "")
                                    {
                                        Fill = Brushes.Transparent,
                                        IsHitTestVisible = false,
                                        Stroke = new Pen(Color.Red)
                                    };
                                FlightData.kmlpolygons.Polygons.Add(polygon);
                                kmlpolygonsoverlay.Polygons.Add(polygon);
                            };

                            ogr.Process();
                        }
                    }
                    else if (file.ToLower().EndsWith("dxf"))
                    {
                        string zone = "-99";
                        InputBox.Show("Zone", "请输入 UTM zone，或放弃更改", ref zone);

                        dxf dxf = new dxf();
                        if (zone != "-99")
                            dxf.Tag = zone;

                        dxf.newLine += Dxf_newLine;
                        dxf.newPolyLine += Dxf_newPolyLine;
                        dxf.newLwPolyline += Dxf_newLwPolyline;
                        dxf.newMLine += Dxf_newMLine;
                        dxf.Read(file);
                    }
                    else
                    {
                        try
                        {
                            string kml = "";
                            string tempdir = "";
                            if (file.ToLower().EndsWith("kmz"))
                            {
                                ZipFile input = new ZipFile(file);

                                tempdir = Path.GetTempPath() + Path.DirectorySeparatorChar + Path.GetRandomFileName();
                                input.ExtractAll(tempdir, ExtractExistingFileAction.OverwriteSilently);

                                string[] kmls = Directory.GetFiles(tempdir, "*.kml");

                                if (kmls.Length > 0)
                                {
                                    file = kmls[0];

                                    input.Dispose();
                                }
                                else
                                {
                                    input.Dispose();
                                    return;
                                }
                            }

                            var sr = new StreamReader(File.OpenRead(file));
                            kml = sr.ReadToEnd();
                            sr.Close();

                            // cleanup after out
                            if (tempdir != "")
                                Directory.Delete(tempdir, true);

                            kml = kml.Replace("<Snippet/>", "");

                            var parser = new Parser();

                            parser.ElementAdded += parser_ElementAdded;
                            parser.ParseString(kml, false);

                            if (DialogResult.Yes ==
                                DevComponents.DotNetBar.MessageBoxEx.Show(Strings.Do_you_want_to_load_this_into_the_flight_data_screen, Strings.Load_data,
                                    MessageBoxButtons.YesNo))
                            {
                                foreach (var temp in kmlpolygonsoverlay.Polygons)
                                {
                                    FlightData.kmlpolygons.Polygons.Add(temp);
                                }
                                foreach (var temp in kmlpolygonsoverlay.Routes)
                                {
                                    FlightData.kmlpolygons.Routes.Add(temp);
                                }
                            }

                            if (
                                DevComponents.DotNetBar.MessageBoxEx.Show(Strings.Zoom_To, Strings.Zoom_to_the_center_or_the_loaded_file, MessageBoxButtons.YesNo) ==
                                DialogResult.Yes)
                            {
                                MainMap.SetZoomToFitRect(GetBoundingLayer(kmlpolygonsoverlay));
                            }
                        }
                        catch (Exception ex)
                        {
                            DevComponents.DotNetBar.MessageBoxEx.Show(Strings.Bad_KML_File + ex);
                        }
                    }
                }
            }
        }

        public void label4_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (MainV2.comPort.MAV.cs.lat != 0)
            {
                SetHomeHere(new PointLatLngAlt(
                                MainV2.comPort.MAV.cs.lat,
                                MainV2.comPort.MAV.cs.lng,
                                MainV2.comPort.MAV.cs.altasl
                                ));
                writeKML();

                zoomToHomeToolStripMenuItem_Click(null, null);
            }
            else
            {
                DevComponents.DotNetBar.MessageBoxEx.Show(
                    "如果你在现场，连接你的APM并等待GPS锁定。然后单击“Home Location”链接将Home设置为您的位置");
            }
        }

        public void lnk_kml_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                Process.Start("http://127.0.0.1:56781/network.kml");
            }
            catch
            {
                DevComponents.DotNetBar.MessageBoxEx.Show("打开 url http://127.0.0.1:56781/network.kml 失败");
            }
        }

        public void LoadAndAppendToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog fd = new OpenFileDialog())
            {
                fd.Filter = "Ardupilot Mission|*.waypoints;*.txt";
                fd.DefaultExt = ".waypoints";
                DialogResult result = fd.ShowDialog();
                string file = fd.FileName;
                if (file != "")
                {
                    readQGC110wpfile(file, true);
                }
            }
        }

        public void loadFromFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog fd = new OpenFileDialog())
            {
                fd.Filter = "Fence (*.fen)|*.fen";
                fd.ShowDialog();
                if (File.Exists(fd.FileName))
                {
                    StreamReader sr = new StreamReader(fd.OpenFile());

                    drawnpolygonsoverlay.Markers.Clear();
                    drawnpolygonsoverlay.Polygons.Clear();
                    drawnpolygon.Points.Clear();

                    int a = 0;

                    while (!sr.EndOfStream)
                    {
                        string line = sr.ReadLine();
                        if (line.StartsWith("#"))
                        {
                        }
                        else
                        {
                            string[] items = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                            if (a == 0)
                            {
                                geofenceoverlay.Markers.Clear();
                                geofenceoverlay.Markers.Add(
                                    new GMarkerGoogle(new PointLatLng(double.Parse(items[0], CultureInfo.InvariantCulture), double.Parse(items[1], CultureInfo.InvariantCulture)),
                                        GMarkerGoogleType.red)
                                    {
                                        ToolTipMode = MarkerTooltipMode.OnMouseOver,
                                        ToolTipText = "GeoFence Return"
                                    });
                                MainMap.UpdateMarkerLocalPosition(geofenceoverlay.Markers[0]);
                            }
                            else
                            {
                                drawnpolygon.Points.Add(new PointLatLng(
                                    double.Parse(items[0], CultureInfo.InvariantCulture),
                                    double.Parse(items[1], CultureInfo.InvariantCulture)));
                                addpolygonmarkergrid(drawnpolygon.Points.Count.ToString(),
                                    double.Parse(items[1], CultureInfo.InvariantCulture),
                                    double.Parse(items[0], CultureInfo.InvariantCulture), 0);
                            }
                            a++;
                        }
                    }

                    // remove loop close
                    if (drawnpolygon.Points.Count > 1 &&
                        drawnpolygon.Points[0] == drawnpolygon.Points[drawnpolygon.Points.Count - 1])
                    {
                        drawnpolygon.Points.RemoveAt(drawnpolygon.Points.Count - 1);
                    }

                    drawnpolygonsoverlay.Polygons.Add(drawnpolygon);

                    MainMap.UpdatePolygonLocalPosition(drawnpolygon);

                    MainMap.Invalidate();
                }
            }
        }

        public void loadFromFileToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog fd = new OpenFileDialog())
            {
                fd.Filter = "Rally (*.ral)|*.ral";
                fd.ShowDialog();
                if (File.Exists(fd.FileName))
                {
                    StreamReader sr = new StreamReader(fd.OpenFile());

                    int a = 0;

                    while (!sr.EndOfStream)
                    {
                        string line = sr.ReadLine();
                        if (line.StartsWith("#"))
                        {
                        }
                        else
                        {
                            string[] items = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                            MAVLink.mavlink_rally_point_t rally = new MAVLink.mavlink_rally_point_t();

                            rally.lat = (int)(float.Parse(items[1], CultureInfo.InvariantCulture) * 1e7);
                            rally.lng = (int)(float.Parse(items[2], CultureInfo.InvariantCulture) * 1e7);
                            rally.alt = (short)float.Parse(items[3], CultureInfo.InvariantCulture);
                            rally.break_alt = (short)float.Parse(items[4], CultureInfo.InvariantCulture);
                            rally.land_dir = (ushort)float.Parse(items[5], CultureInfo.InvariantCulture);
                            rally.flags = byte.Parse(items[6], CultureInfo.InvariantCulture);

                            if (a == 0)
                            {
                                rallypointoverlay.Markers.Clear();

                                rallypointoverlay.Markers.Add(
                                    new GMapMarkerRallyPt(new PointLatLngAlt(rally.lat / 1e7, rally.lng / 1e7,
                                        rally.alt)));
                            }
                            else
                            {
                                rallypointoverlay.Markers.Add(
                                    new GMapMarkerRallyPt(new PointLatLngAlt(rally.lat / 1e7, rally.lng / 1e7,
                                        rally.alt)));
                            }
                            a++;
                        }
                    }
                    
                    MainMap.Invalidate();
                    ZoomToCenterWP();
                }
            }
        }

        public void ZoomToCenterWP()
        {
            RectLatLng ret = new RectLatLng();

            double left = double.MaxValue;
            double top = double.MinValue;
            double right = double.MinValue;
            double bottom = double.MaxValue;

            for(int i =0; i < Commands.Rows.Count; i++)
            {
                GetWPPoint(i, out PointLatLngAlt wp);
                if (wp.Lng < left)
                {
                    left = wp.Lng;
                }

                // top
                if (wp.Lat > top)
                {
                    top = wp.Lat;
                }

                // right
                if (wp.Lng > right)
                {
                    right = wp.Lng;
                }

                // bottom
                if (wp.Lat < bottom)
                {
                    bottom = wp.Lat;
                }
            }
            if (left != double.MaxValue && right != double.MinValue && top != double.MinValue && bottom != double.MaxValue)
            {
                ret = RectLatLng.FromLTRB(left, top, right, bottom);
                MainMap.SetZoomToFitRect(ret);
            }
            else
                return;
        }



        public void FromPolygonToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog fd = new OpenFileDialog())
            {
                fd.Filter = "Polygon (*.poly)|*.poly";
                fd.ShowDialog();
                if (File.Exists(fd.FileName))
                {
                    StreamReader sr = new StreamReader(fd.OpenFile());

                    drawnpolygonsoverlay.Markers.Clear();
                    drawnpolygonsoverlay.Polygons.Clear();
                    drawnpolygon.Points.Clear();

                    int a = 0;

                    while (!sr.EndOfStream)
                    {
                        string line = sr.ReadLine();
                        if (line.StartsWith("#"))
                        {
                        }
                        else
                        {
                            string[] items = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                            if (items.Length < 2)
                                continue;

                            drawnpolygon.Points.Add(new PointLatLng(
                                double.Parse(items[0], CultureInfo.InvariantCulture),
                                double.Parse(items[1], CultureInfo.InvariantCulture)));
                            addpolygonmarkergrid(drawnpolygon.Points.Count.ToString(),
                                double.Parse(items[1], CultureInfo.InvariantCulture),
                                double.Parse(items[0], CultureInfo.InvariantCulture), 0);

                            a++;
                        }
                    }

                    // remove loop close
                    if (drawnpolygon.Points.Count > 1 &&
                        drawnpolygon.Points[0] == drawnpolygon.Points[drawnpolygon.Points.Count - 1])
                    {
                        drawnpolygon.Points.RemoveAt(drawnpolygon.Points.Count - 1);
                    }

                    drawnpolygonsoverlay.Polygons.Add(drawnpolygon);

                    MainMap.UpdatePolygonLocalPosition(drawnpolygon);

                    MainMap.Invalidate();

                    MainMap.ZoomAndCenterMarkers(drawnpolygonsoverlay.Id);
                }
            }
        }






        public void MainMap_Paint(object sender, PaintEventArgs e)
        {
            // draw utm grid
            if (grid)
            {
                if (MainMap.Zoom < 10)
                    return;

                var rect = MainMap.ViewArea;

                var plla1 = new PointLatLngAlt(rect.LocationTopLeft);
                var plla2 = new PointLatLngAlt(rect.LocationRightBottom);

                var center = new PointLatLngAlt(rect.LocationMiddle);

                var zone = center.GetUTMZone();

                var utm1 = plla1.ToUTM(zone);
                var utm2 = plla2.ToUTM(zone);

                var deltax = utm1[0] - utm2[0];
                var deltay = utm1[1] - utm2[1];

                //if (deltax)

                var gridsize = 1000.0;


                if (Math.Abs(deltax) / 100000 < 40)
                    gridsize = 100000;

                if (Math.Abs(deltax) / 10000 < 40)
                    gridsize = 10000;

                if (Math.Abs(deltax) / 1000 < 40)
                    gridsize = 1000;

                if (Math.Abs(deltax) / 100 < 40)
                    gridsize = 100;

                if (Math.Abs(deltax) / 10 < 40)
                    gridsize = 10;

                if (Math.Abs(deltax) / 1 < 40)
                    gridsize = 1;

                // round it - x
                utm1[0] = utm1[0] - (utm1[0] % gridsize);
                // y
                utm2[1] = utm2[1] - (utm2[1] % gridsize);

                // x's
                for (double x = utm1[0]; x < utm2[0]; x += gridsize)
                {
                    var p1 = MainMap.FromLatLngToLocal(PointLatLngAlt.FromUTM(zone, x, utm1[1]));
                    var p2 = MainMap.FromLatLngToLocal(PointLatLngAlt.FromUTM(zone, x, utm2[1]));

                    int x1 = (int)p1.X;
                    int y1 = (int)p1.Y;
                    int x2 = (int)p2.X;
                    int y2 = (int)p2.Y;

                    e.Graphics.DrawLine(new Pen(MainMap.SelectionPen.Color, 1), x1, y1, x2, y2);
                }

                // y's
                for (double y = utm2[1]; y < utm1[1]; y += gridsize)
                {
                    var p1 = MainMap.FromLatLngToLocal(PointLatLngAlt.FromUTM(zone, utm1[0], y));
                    var p2 = MainMap.FromLatLngToLocal(PointLatLngAlt.FromUTM(zone, utm2[0], y));

                    int x1 = (int)p1.X;
                    int y1 = (int)p1.Y;
                    int x2 = (int)p2.X;
                    int y2 = (int)p2.Y;

                    e.Graphics.DrawLine(new Pen(MainMap.SelectionPen.Color, 1), x1, y1, x2, y2);
                }
            }

            e.Graphics.ResetTransform();

            polyicon.Location = new Point(10, 100);
            polyicon.Paint(e.Graphics);

            e.Graphics.ResetTransform();

            WPicon.Location = new Point(10, polyicon.Location.Y + polyicon.Height + 5);
            WPicon.Paint(e.Graphics);

            e.Graphics.ResetTransform();
            zoomicon.Location = new Point(10, WPicon.Location.Y + WPicon.Height + 5);
            zoomicon.Paint(e.Graphics);

            e.Graphics.ResetTransform();
        }

        private void MainMap_Resize(object sender, EventArgs e)
        {
            MainMap.Zoom = MainMap.Zoom + 0.01;
        }

        public void modifyAltToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string altdif = "0";
            InputBox.Show("修改高度", "请输入高度.\n(20 = up 20, *2 = up by alt * 2)",
                ref altdif);

            float altchange = 0;
            float multiplyer = 1;

            try
            {
                if (altdif.Contains("*"))
                {
                    multiplyer = float.Parse(altdif.Replace('*', ' '));
                }
                else
                {
                    altchange = float.Parse(altdif);
                }
            }
            catch
            {
                DevComponents.DotNetBar.MessageBoxEx.Show(Strings.InvalidNumberEntered, Strings.ERROR);
                return;
            }


            foreach (DataGridViewRow line in Commands.Rows)
            {
                line.Cells[Alt.Index].Value =
                    float.Parse(line.Cells[Alt.Index].Value.ToString()) * multiplyer + altchange;
            }
        }

        private void obj_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                fetchpathrip = false;
            }
        }

        public void panelMap_Resize(object sender, EventArgs e)
        {
            // this is a mono fix for the zoom bar
            //Console.WriteLine("panelmap "+panelMap.Size.ToString());
            MainMap.Size = new Size(panelMap.Size.Width - 50, panelMap.Size.Height);
            trackBar1.Location = new Point(panelMap.Size.Width - 50, trackBar1.Location.Y);
            trackBar1.Size = new Size(trackBar1.Size.Width, panelMap.Size.Height - trackBar1.Location.Y);
            label11.Location = new Point(panelMap.Size.Width - 50, label11.Location.Y);
        }

        private void panelWaypoints_ExpandClick(object sender, EventArgs e)
        {
            Commands.AutoResizeColumns();
        }

        private void parser_ElementAdded(object sender, ElementEventArgs e)
        {
            processKML(e.Element);
        }

        public void Planner_Resize(object sender, EventArgs e)
        {
            MainMap.Zoom = trackBar1.Value;
        }

        /// <summary>
        /// from http://www.ecse.rpi.edu/Homepages/wrf/Research/Short_Notes/pnpoly.html
        /// </summary>
        /// <param name="array"> a closed polygon</param>
        /// <param name="testx"></param>
        /// <param name="testy"></param>
        /// <returns> true = outside</returns>
        private bool pnpoly(PointLatLng[] array, double testx, double testy)
        {
            int nvert = array.Length;
            int i, j = 0;
            bool c = false;
            for (i = 0, j = nvert - 1; i < nvert; j = i++)
            {
                if (((array[i].Lng > testy) != (array[j].Lng > testy)) &&
                    (testx <
                     (array[j].Lat - array[i].Lat) * (testy - array[i].Lng) / (array[j].Lng - array[i].Lng) + array[i].Lat))
                    c = !c;
            }
            return c;
        }

        private void POI_POIModified(object sender, EventArgs e)
        {
            POI.UpdateOverlay(poioverlay);
        }

        public void poiaddToolStripMenuItem_Click(object sender, EventArgs e)
        {
            POI.POIAdd(MouseDownStart);
        }

        public void poideleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (CurrentPOIMarker == null)
                return;

            POI.POIDelete(CurrentPOIMarker);
        }

        public void poieditToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (CurrentGMapMarker == null || !(CurrentGMapMarker is GMapMarkerPOI))
                return;

            POI.POIEdit(CurrentPOIMarker);
        }

        public void prefetchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RectLatLng area = MainMap.SelectedArea;
            if (area.IsEmpty)
            {
                var res = DevComponents.DotNetBar.MessageBoxEx.Show("No ripp area defined, ripp displayed on screen?", "Rip",
                    MessageBoxButtons.YesNo);
                if (res == DialogResult.Yes)
                {
                    area = MainMap.ViewArea;
                }
            }

            if (!area.IsEmpty)
            {
                string maxzoomstring = "20";
                if (InputBox.Show("max zoom", "输入 max zoom.", ref maxzoomstring) != DialogResult.OK)
                    return;

                int maxzoom = 20;
                if (!int.TryParse(maxzoomstring, out maxzoom))
                {
                    DevComponents.DotNetBar.MessageBoxEx.Show(Strings.InvalidNumberEntered, Strings.ERROR);
                    return;
                }

                maxzoom = Math.Min(maxzoom, MainMap.MaxZoom);

                for (int i = 1; i <= maxzoom; i++)
                {
                    TilePrefetcher obj = new TilePrefetcher();
                    ThemeManager.ApplyThemeTo(obj);
                    obj.ShowCompleteMessage = false;
                    obj.Start(area, i, MainMap.MapProvider, 0, 0);

                    if (obj.UserAborted)
                    {
                        obj.Dispose();
                        break;
                    }

                    obj.Dispose();
                }
            }
            else
            {
                DevComponents.DotNetBar.MessageBoxEx.Show("按住 ALT 选择区域", "GMap.NET", MessageBoxButtons.OK,
                    MessageBoxIcon.Exclamation);
            }
        }

        public void prefetchWPPathToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FetchPath();
        }

        private void processKML(Element Element)
        {
            Document doc = Element as Document;
            Placemark pm = Element as Placemark;
            Folder folder = Element as Folder;
            Polygon polygon = Element as Polygon;
            LineString ls = Element as LineString;
            MultipleGeometry geom = Element as MultipleGeometry;

            if (doc != null)
            {
                foreach (var feat in doc.Features)
                {
                    //Console.WriteLine("feat " + feat.GetType());
                    //processKML((Element)feat);
                }
            }
            else if (folder != null)
            {
                foreach (Feature feat in folder.Features)
                {
                    //Console.WriteLine("feat "+feat.GetType());
                    //processKML(feat);
                }
            }
            else if (pm != null)
            {
            }
            else if (polygon != null)
            {
                GMapPolygon kmlpolygon = new GMapPolygon(new List<PointLatLng>(), "kmlpolygon");

                kmlpolygon.Stroke.Color = Color.Purple;
                kmlpolygon.Fill = Brushes.Transparent;

                foreach (var loc in polygon.OuterBoundary.LinearRing.Coordinates)
                {
                    kmlpolygon.Points.Add(new PointLatLng(loc.Latitude, loc.Longitude));
                }

                kmlpolygonsoverlay.Polygons.Add(kmlpolygon);
            }
            else if (ls != null)
            {
                GMapRoute kmlroute = new GMapRoute(new List<PointLatLng>(), "kmlroute");

                kmlroute.Stroke.Color = Color.Purple;

                foreach (var loc in ls.Coordinates)
                {
                    kmlroute.Points.Add(new PointLatLng(loc.Latitude, loc.Longitude));
                }

                kmlpolygonsoverlay.Routes.Add(kmlroute);
            }
            else if (geom != null)
            {
                foreach (var geometry in geom.Geometry)
                {
                    processKML(geometry);
                }
            }
        }

        private void processKMLMission(object sender, ElementEventArgs e)
        {
            Element element = e.Element;
            try
            {
                //  log.Info(Element.ToString() + " " + Element.Parent);
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }

            Document doc = element as Document;
            Placemark pm = element as Placemark;
            Folder folder = element as Folder;
            Polygon polygon = element as Polygon;
            LineString ls = element as LineString;

            if (doc != null)
            {
                foreach (var feat in doc.Features)
                {
                    //Console.WriteLine("feat " + feat.GetType());
                    //processKML((Element)feat);
                }
            }
            else if (folder != null)
            {
                foreach (Feature feat in folder.Features)
                {
                    //Console.WriteLine("feat "+feat.GetType());
                    //processKML(feat);
                }
            }
            else if (pm != null)
            {
                //if (pm.Geometry is SharpKml.Dom.Point)
                //{
                //    var point = ((SharpKml.Dom.Point)pm.Geometry).Coordinate;
                //    POI.POIAdd(new PointLatLngAlt(point.Latitude, point.Longitude), pm.Name);
                //}
            }
            else if (polygon != null)
            {
            }
            else if (ls != null)
            {
                foreach (var loc in ls.Coordinates)
                {
                    selectedrow = Commands.Rows.Add();
                    setfromMap(loc.Latitude, loc.Longitude, (int)loc.Altitude);
                }
            }
        }

        /// <summary>
        /// Processes a loaded EEPROM to the map and datagrid
        /// </summary>
        private void processToScreen(List<Locationwp> cmds, bool home, bool append)
        {
            quickadd = true;


            // mono fix
            Commands.CurrentCell = null;

            while (Commands.Rows.Count > 0 && !append) Commands.Rows.Clear();

            if (cmds.Count == 0)
            {
                quickadd = false;
                return;
            }

            Commands.SuspendLayout();
            Commands.Enabled = false;

            int i = Commands.Rows.Count - 1;
            int cmdidx = -1;
            foreach (Locationwp temp in cmds)
            {
                i++;
                cmdidx++;
                //Console.WriteLine("FP processToScreen " + i);
                if (temp.id == 0 && i != 0) // 0 and not home
                    break;
                if (temp.id == 255 && i != 0) // bad record - never loaded any WP's - but have started the board up.
                    break;
                if (cmdidx == 0 && append)
                {
                    // we dont want to add home again.
                    i--;
                    continue;
                }
                if (i + 1 >= Commands.Rows.Count)
                {
                    selectedrow = Commands.Rows.Add();
                }
                //if (i == 0 && temp.alt == 0) // skip 0 home
                //  continue;
                DataGridViewTextBoxCell cell;
                DataGridViewComboBoxCell cellcmd;
                cellcmd = Commands.Rows[i].Cells[Command.Index] as DataGridViewComboBoxCell;
                cellcmd.Value = "UNKNOWN";
                cellcmd.Tag = temp.id;

                foreach (object value in Enum.GetValues(typeof(MAVLink.MAV_CMD)))
                {
                    if ((ushort)value == temp.id)
                    {
                        if (Program.MONO || cellcmd.Items.Contains(value.ToString()))
                            cellcmd.Value = value.ToString();
                        break;
                    }
                }

                // from ap_common.h
                if (temp.id == (ushort)MAVLink.MAV_CMD.WAYPOINT || temp.id == (ushort)MAVLink.MAV_CMD.SPLINE_WAYPOINT ||
                    temp.id == (ushort)MAVLink.MAV_CMD.TAKEOFF || temp.id == (ushort)MAVLink.MAV_CMD.DO_SET_HOME)
                {
                    // not home
                    if (i != 0)
                    {
                        CMB_altmode.SelectedValue = temp.frame;
                    }
                }

                DataGridViewComboBoxCell cellframe = Commands.Rows[i].Cells[Frame.Index] as DataGridViewComboBoxCell;
                cellframe.Value = (int)temp.frame;
                cell = Commands.Rows[i].Cells[Alt.Index] as DataGridViewTextBoxCell;
                cell.Value = temp.alt * CurrentState.multiplieralt;
                cell = Commands.Rows[i].Cells[Lat.Index] as DataGridViewTextBoxCell;
                cell.Value = temp.lat;
                cell = Commands.Rows[i].Cells[Lon.Index] as DataGridViewTextBoxCell;
                cell.Value = temp.lng;

                cell = Commands.Rows[i].Cells[Param1.Index] as DataGridViewTextBoxCell;
                cell.Value = temp.p1;
                cell = Commands.Rows[i].Cells[Param2.Index] as DataGridViewTextBoxCell;
                cell.Value = temp.p2;
                cell = Commands.Rows[i].Cells[Param3.Index] as DataGridViewTextBoxCell;
                cell.Value = temp.p3;
                cell = Commands.Rows[i].Cells[Param4.Index] as DataGridViewTextBoxCell;
                cell.Value = temp.p4;

                // convert to utm/other
                convertFromGeographic(temp.lat, temp.lng);
            }

            Commands.Enabled = true;
            Commands.ResumeLayout();

            setWPParams();

            var type = (MAVLink.MAV_MISSION_TYPE)Invoke((Func<MAVLink.MAV_MISSION_TYPE>)delegate
            {
                return (MAVLink.MAV_MISSION_TYPE)cmb_missiontype.SelectedValue;
            });

            if (!append && type == MAVLink.MAV_MISSION_TYPE.MISSION && Commands.RowCount > 0)
            {
                if (home)
                {
                    try
                    {
                        DataGridViewTextBoxCell cellhome;
                        cellhome = Commands.Rows[0].Cells[Lat.Index] as DataGridViewTextBoxCell;
                        if (cellhome.Value != null)
                        {
                            if (cellhome.Value.ToString() != homePosition.Lat.ToString() && cellhome.Value.ToString() != "0")
                            {
                                //DevComponents.DotNetBar.MessageBoxEx.Show(this, "变更Home位置", "重设Home位置", MessageBoxButtons.YesNo);
                                //var dr = CustomMessageBox.Show("变更Home位置", "重设Home位置",
                                //    MessageBoxButtons.YesNo);

                                //if (dr == (int)DialogResult.Yes)
                                //{
                                SetHomeHere(new PointLatLngAlt(
                                    double.Parse(Commands.Rows[0].Cells[Lat.Index].Value.ToString()),
                                    double.Parse(Commands.Rows[0].Cells[Lon.Index].Value.ToString()),
                                    double.Parse(Commands.Rows[0].Cells[Alt.Index].Value.ToString()) * CurrentState.multiplieralt
                                    ));
                                //}
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Error(ex.ToString());
                    } // if there is no valid home


                    log.Info("remove home from list");
                    Commands.Rows.Remove(Commands.Rows[0]); // remove home row
                }

            }

            quickadd = false;

            writeKML();

            MainMap_OnMapZoomChanged();
        }

        private Dictionary<string, string[]> readCMDXML()
        {
            Dictionary<string, string[]> cmd = new Dictionary<string, string[]>();

            // do lang stuff here

            string file = Settings.GetRunningDirectory() + "mavcmd.xml";

            if (!File.Exists(file))
            {
                DevComponents.DotNetBar.MessageBoxEx.Show("Missing mavcmd.xml file");
                return cmd;
            }

            log.Info("Reading MAV_CMD for " + MainV2.comPort.MAV.cs.firmware);

            using (XmlReader reader = XmlReader.Create(file))
            {
                reader.Read();
                reader.ReadStartElement("CMD");
                if (MainV2.comPort.MAV.cs.firmware == Firmwares.ArduPlane ||
                    MainV2.comPort.MAV.cs.firmware == Firmwares.Ateryx)
                {
                    reader.ReadToFollowing("APM");
                }
                else if (MainV2.comPort.MAV.cs.firmware == Firmwares.ArduRover)
                {
                    reader.ReadToFollowing("APRover");
                }
                else
                {
                    reader.ReadToFollowing("AC2");
                }

                XmlReader inner = reader.ReadSubtree();

                inner.Read();

                inner.MoveToElement();

                inner.Read();

                while (inner.Read())
                {
                    inner.MoveToElement();
                    if (inner.IsStartElement())
                    {
                        string cmdname = inner.Name;
                        string[] cmdarray = new string[7];
                        int b = 0;

                        XmlReader inner2 = inner.ReadSubtree();

                        inner2.Read();

                        while (inner2.Read())
                        {
                            inner2.MoveToElement();
                            if (inner2.IsStartElement())
                            {
                                cmdarray[b] = inner2.ReadString();
                                b++;
                            }
                        }

                        cmd[cmdname] = cmdarray;
                    }
                }
            }

            return cmd;
        }


        private void SaveFile_Click(object sender, EventArgs e)
        {
            savewaypoints();
            writeKML();
        }

        public void SavePolygonToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (drawnpolygon.Points.Count == 0)
            {
                return;
            }


            using (SaveFileDialog sf = new SaveFileDialog())
            {
                sf.Filter = "Polygon (*.poly)|*.poly";
                var result = sf.ShowDialog();
                if (sf.FileName != "" && result == DialogResult.OK)
                {
                    try
                    {
                        StreamWriter sw = new StreamWriter(sf.OpenFile());

                        sw.WriteLine("#saved by Mission Planner " + Application.ProductVersion);

                        if (drawnpolygon.Points.Count > 0)
                        {
                            foreach (var pll in drawnpolygon.Points)
                            {
                                sw.WriteLine(pll.Lat.ToString(CultureInfo.InvariantCulture) + " " + pll.Lng.ToString(CultureInfo.InvariantCulture));
                            }

                            PointLatLng pll2 = drawnpolygon.Points[0];

                            sw.WriteLine(pll2.Lat.ToString(CultureInfo.InvariantCulture) + " " + pll2.Lng.ToString(CultureInfo.InvariantCulture));
                        }

                        sw.Close();
                    }
                    catch
                    {
                        DevComponents.DotNetBar.MessageBoxEx.Show("Failed to write fence file");
                    }
                }
            }
        }

        public void saveRallyPointsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            byte count = 0;

            MainV2.comPort.setParam((byte)MainV2.comPort.sysidcurrent, (byte)MainV2.comPort.compidcurrent, "RALLY_TOTAL", rallypointoverlay.Markers.Count);

            foreach (GMapMarkerRallyPt pnt in rallypointoverlay.Markers)
            {
                try
                {
                    MainV2.comPort.setRallyPoint(count, new PointLatLngAlt(pnt.Position) { Alt = pnt.Alt }, 0, 0, 0,
                        (byte)(float)MainV2.comPort.MAV.param["RALLY_TOTAL"]);
                    count++;
                }
                catch
                {
                    DevComponents.DotNetBar.MessageBoxEx.Show("Failed to save rally point", Strings.ERROR);
                    return;
                }
            }
        }

        public void saveToFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (geofenceoverlay.Markers.Count == 0)
            {
                DevComponents.DotNetBar.MessageBoxEx.Show("Please set a return location");
                return;
            }


            using (SaveFileDialog sf = new SaveFileDialog())
            {
                sf.Filter = "Fence (*.fen)|*.fen";
                var result = sf.ShowDialog();
                if (sf.FileName != "" && result == DialogResult.OK)
                {
                    try
                    {
                        StreamWriter sw = new StreamWriter(sf.OpenFile());

                        sw.WriteLine("#saved by APM Planner " + Application.ProductVersion);

                        sw.WriteLine(geofenceoverlay.Markers[0].Position.Lat.ToString(CultureInfo.InvariantCulture) + " " +
                                     geofenceoverlay.Markers[0].Position.Lng.ToString(CultureInfo.InvariantCulture));
                        if (drawnpolygon.Points.Count > 0)
                        {
                            foreach (var pll in drawnpolygon.Points)
                            {
                                sw.WriteLine(pll.Lat.ToString(CultureInfo.InvariantCulture) + " " + pll.Lng.ToString(CultureInfo.InvariantCulture));
                            }

                            PointLatLng pll2 = drawnpolygon.Points[0];

                            sw.WriteLine(pll2.Lat.ToString(CultureInfo.InvariantCulture) + " " + pll2.Lng.ToString(CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            foreach (var pll in geofencepolygon.Points)
                            {
                                sw.WriteLine(pll.Lat.ToString(CultureInfo.InvariantCulture) + " " + pll.Lng.ToString(CultureInfo.InvariantCulture));
                            }

                            PointLatLng pll2 = geofencepolygon.Points[0];

                            sw.WriteLine(pll2.Lat.ToString(CultureInfo.InvariantCulture) + " " + pll2.Lng.ToString(CultureInfo.InvariantCulture));
                        }

                        sw.Close();
                    }
                    catch
                    {
                        DevComponents.DotNetBar.MessageBoxEx.Show("Failed to write fence file");
                    }
                }
            }
        }

        public void saveToFileToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (rallypointoverlay.Markers.Count == 0)
            {
                DevComponents.DotNetBar.MessageBoxEx.Show("Please set some rally points");
                return;
            }
            /*
Column 1: Field type (RALLY is the only one at the moment -- may have RALLY_LAND in the future)
 Column 2,3: Lat, lon
 Column 4: Loiter altitude
 Column 5: Break altitude (when landing from rally is implemented, this is the altitude to break out of loiter from)
 Column 6: Landing heading (also for future when landing from rally is implemented)
 Column 7: Flags (just 0 for now, also future use).
             */

            using (SaveFileDialog sf = new SaveFileDialog())
            {
                sf.Filter = "Rally (*.ral)|*.ral";
                var result = sf.ShowDialog();
                if (sf.FileName != "" && result == DialogResult.OK)
                {
                    try
                    {
                        using (StreamWriter sw = new StreamWriter(sf.OpenFile()))
                        {
                            sw.WriteLine("#saved by Mission Planner " + Application.ProductVersion);


                            foreach (GMapMarkerRallyPt mark in rallypointoverlay.Markers)
                            {
                                sw.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}", "RALLY", mark.Position.Lat.ToString(CultureInfo.InvariantCulture),
                                    mark.Position.Lng.ToString(CultureInfo.InvariantCulture), mark.Alt.ToString(CultureInfo.InvariantCulture), 0, 0, 0);
                            }
                        }
                    }
                    catch
                    {
                        DevComponents.DotNetBar.MessageBoxEx.Show("Failed to write rally file");
                    }
                }
            }
        }


       



        /// <summary>
        /// form WGS84 covert to Local coordinates
        /// </summary>
        private void CovertToWorkCoordinate(PointLatLngAlt WGS84Point, out PointLatLngAlt WorkPoint)
        {
            var layer = VPS.Layer.MemoryLayerCache.GetLayerFromMemoryCache(Settings.Instance["defaultTiffLayer"]);
            if (layer == null)
            {
            }
            double Scale = layer.Scale;

            PointLatLngAlt Origin = layer.Origin;

            int OriginZone = Origin.GetUTMZone();
            double[] OriginCoord = Origin.ToUTM(OriginZone);

            int WGS84Zone = WGS84Point.GetUTMZone();
            double[] WGS84Coord = WGS84Point.ToUTM(OriginZone);

            double WGS84UTMEast = WGS84Coord[0];
            double WGS84UTMNorth = WGS84Coord[1];
            if (WGS84Zone < 0)
                WGS84UTMNorth = WGS84UTMNorth - 10000000;
            double OriginUTMEast = OriginCoord[0];
            double OriginUTMNorth = OriginCoord[1];
            if (OriginZone < 0)
                OriginUTMNorth = OriginUTMNorth - 10000000;

            double deltaLng, deltaLat, deltaAlt;

            deltaLng = (WGS84UTMEast - OriginUTMEast) / Scale;
            deltaLat = (WGS84UTMNorth - OriginUTMNorth) / Scale;
            deltaAlt = (WGS84Point.Alt - Origin.Alt) / Scale;

            // m => mm
            WorkPoint = new PointLatLngAlt(deltaLat * 1000, deltaLng * 1000, deltaAlt * 1000);

        }

        /// <summary>
        /// form WGS84 covert to Local coordinates
        /// </summary>
        private void CovertToWorkCoordinate(double lng, double lat, double alt, out double x, out double y, out double z)
        {
            this.CovertToWorkCoordinate(new PointLatLngAlt(lat, lng, alt), out PointLatLngAlt wgs84);
            x = wgs84.Lng;
            y = wgs84.Lat;
            z = wgs84.Alt;
        }


        private void CovertFromWorkCoordinate(PointLatLngAlt WorkCoordPoint, out PointLatLngAlt WGS84CorrdPoint)
        {
            var layer = VPS.Layer.MemoryLayerCache.GetLayerFromMemoryCache(Settings.Instance["defaultTiffLayer"]);
            if (layer == null)
            {
            }
            double Scale = layer.Scale;

            PointLatLngAlt Origin = layer.Origin;

            int OriginZone = Origin.GetUTMZone();
            double[] OriginCoord = Origin.ToUTM();

            double OriginUTMEast = OriginCoord[0];
            double OriginUTMNorth = OriginCoord[1];
            if (OriginZone < 0)
                OriginUTMNorth = OriginUTMNorth - 10000000;

            // mm => m
            WorkCoordPoint = new PointLatLngAlt(WorkCoordPoint.Lat / 1000, WorkCoordPoint.Lng / 1000, WorkCoordPoint.Alt / 1000);

            double deltaEast, deltaNorth, deltaAlt;

            deltaEast = (WorkCoordPoint.Lng * Scale + OriginUTMEast);
            deltaNorth = (WorkCoordPoint.Lat * Scale + OriginUTMNorth);
            deltaAlt = (WorkCoordPoint.Alt * Scale + Origin.Alt);

            WGS84CorrdPoint = PointLatLngAlt.FromUTM(OriginZone, deltaEast, deltaNorth);
            WGS84CorrdPoint.Alt = deltaAlt;
        }

        private void CovertFromWorkCoordinate(double x, double y, double z, out double lng, out double lat, out double alt)
        {
            this.CovertFromWorkCoordinate(new PointLatLngAlt(y, x, z), out PointLatLngAlt local);
            lng = local.Lng;
            lat = local.Lat;
            alt = local.Alt;
        }

        public void SaveWPFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFile_Click(null, null);
        }


        private void SetGradAndDistAndAZ(List<PointLatLngAlt> pointlist, PointLatLngAlt HomeLocation)
        {
            int a = 0;
            PointLatLngAlt last = HomeLocation;
            foreach (var lla in pointlist)
            {
                if (lla == null)
                    continue;
                try
                {
                    if (lla.Tag != null && lla.Tag != "H" && !lla.Tag.Contains("ROI"))
                    {
                        double height = lla.Alt - last.Alt;
                        double distance = lla.GetDistance(last);
                        double grad = height / distance;

                        Commands.Rows[int.Parse(lla.Tag) - 1].Cells[Grad.Index].Value =
                            (grad * 100).ToString("0.0");

                        Commands.Rows[int.Parse(lla.Tag) - 1].Cells[Angle.Index].Value =
                            ((180.0 / Math.PI) * Math.Atan(grad)).ToString("0.0");

                        Commands.Rows[int.Parse(lla.Tag) - 1].Cells[Dist.Index].Value =
                            (Math.Sqrt(Math.Pow(distance, 2) + Math.Pow(height, 2)) * CurrentState.multiplierdist)
                            .ToString("0.0");

                        Commands.Rows[int.Parse(lla.Tag) - 1].Cells[AZ.Index].Value =
                            ((lla.GetBearing(last) + 180) % 360).ToString("0");
                    }
                }
                catch (Exception ex)
                {
                    log.Error(ex);
                }
                a++;
                last = lla;
            }
        }


        




        private void setWPParams()
        {
            try
            {
                log.Info("Loading wp params");

                Dictionary<string, double> param = new Dictionary<string, double>((Dictionary<string, double>)MainV2.comPort.MAV.param);

                if (param.ContainsKey("WP_RADIUS"))
                {
                    TXT_WPRad.Text = (((double)param["WP_RADIUS"] * CurrentState.multiplierdist)).ToString();
                }
                if (param.ContainsKey("WPNAV_RADIUS"))
                {
                    TXT_WPRad.Text = (((double)param["WPNAV_RADIUS"] * CurrentState.multiplierdist / 100.0)).ToString();
                }

                log.Info("param WP_RADIUS " + TXT_WPRad.Text);

                try
                {
                    TXT_loiterrad.Enabled = false;
                    if (param.ContainsKey("LOITER_RADIUS"))
                    {
                        TXT_loiterrad.Text = (((double)param["LOITER_RADIUS"] * CurrentState.multiplierdist)).ToString();
                        TXT_loiterrad.Enabled = true;
                    }
                    else if (param.ContainsKey("WP_LOITER_RAD"))
                    {
                        TXT_loiterrad.Text = (((double)param["WP_LOITER_RAD"] * CurrentState.multiplierdist)).ToString();
                        TXT_loiterrad.Enabled = true;
                    }

                    log.Info("param LOITER_RADIUS " + TXT_loiterrad.Text);
                }
                catch (Exception ex)
                {
                    log.Error(ex);
                }
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }
        public void surveyGrid()
        {
            surveyGridToolStripMenuItem_Click(this, null);
        }

        public void surveyGridToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MainV2.instance.AutoWP();
        }

        public void switchDockingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //if (panelAction.Dock == DockStyle.Bottom)
            //{
            //    panelAction.Dock = DockStyle.Right;
                panelWaypoints.Dock = DockStyle.Bottom;
            //}
            //else
            //{
            //panelAction.Dock = DockStyle.Bottom;
            //panelAction.Height = 120;
                panelWaypoints.Dock = DockStyle.Right;
                panelWaypoints.Width = Width / 2;
            //}

            //Settings.Instance["FP_docking"] = panelAction.Dock.ToString();
        }

    public void takeoffToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // altitude
            string alt = "10";

            if (DialogResult.Cancel == InputBox.Show("Altitude", "Please enter your takeoff altitude", ref alt))
                return;

            int alti = -1;

            if (!int.TryParse(alt, out alti))
            {
                MessageBox.Show("Bad Alt");
                return;
            }

            // take off pitch
            int topi = 0;

            if (MainV2.comPort.MAV.cs.firmware == Firmwares.ArduPlane ||
                MainV2.comPort.MAV.cs.firmware == Firmwares.Ateryx)
            {
                string top = "15";

                if (DialogResult.Cancel == InputBox.Show("Takeoff Pitch", "Please enter your takeoff pitch", ref top))
                    return;

                if (!int.TryParse(top, out topi))
                {
                    MessageBox.Show("Bad Takeoff pitch");
                    return;
                }
            }

            selectedrow = Commands.Rows.Add();

            Commands.Rows[selectedrow].Cells[Command.Index].Value = MAVLink.MAV_CMD.TAKEOFF.ToString();

            Commands.Rows[selectedrow].Cells[Param1.Index].Value = topi;

            Commands.Rows[selectedrow].Cells[Alt.Index].Value = alti;

            ChangeColumnHeader(MAVLink.MAV_CMD.TAKEOFF.ToString());

            writeKML();
        }

        /// <summary>
        /// Draw an mav icon, and update tracker location icon and guided mode wp on FP screen
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void timer1_Tick(object sender, EventArgs e)
        {
            try
            {
                if (IsMouseDown || CurentRectMarker != null)
                    return;

                prop.alt = MainV2.comPort.MAV.cs.alt;
                prop.altasl = MainV2.comPort.MAV.cs.altasl;
                prop.center = MainMap.Position;
                prop.Update(MainV2.comPort.MAV.cs.PlannedHomeLocation, MainV2.comPort.MAV.cs.Location,
                    MainV2.comPort.MAV.cs.battery_kmleft);

                routesoverlay.Markers.Clear();

                if (MainV2.comPort.MAV.cs.TrackerLocation != MainV2.comPort.MAV.cs.PlannedHomeLocation &&
                    MainV2.comPort.MAV.cs.TrackerLocation.Lng != 0)
                {
                    addpolygonmarker("Tracker Home", MainV2.comPort.MAV.cs.TrackerLocation.Lng,
                        MainV2.comPort.MAV.cs.TrackerLocation.Lat, (int)MainV2.comPort.MAV.cs.TrackerLocation.Alt,
                        Color.Blue, routesoverlay);
                }

                if (MainV2.comPort.MAV.cs.lat == 0 || MainV2.comPort.MAV.cs.lng == 0)
                    return;

                var marker = Common.getMAVMarker(MainV2.comPort.MAV);

                routesoverlay.Markers.Add(marker);

                if (MainV2.comPort.MAV.cs.mode.ToLower() == "guided" && MainV2.comPort.MAV.GuidedMode.x != 0)
                {
                    addpolygonmarker("Guided Mode", MainV2.comPort.MAV.GuidedMode.y / 1e7, MainV2.comPort.MAV.GuidedMode.x / 1e7,
                        (int)MainV2.comPort.MAV.GuidedMode.z, Color.Blue, routesoverlay);
                }
            }
            catch (Exception ex)
            {
                log.Warn(ex);
            }
        }

        public void trackBar1_Scroll(object sender, EventArgs e)
        {
            try
            {
                lock (thisLock)
                {
                    MainMap.Zoom = trackBar1.Value;
                }
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }

        private void trackBar1_ValueChanged(object sender, EventArgs e)
        {
            try
            {
                lock (thisLock)
                {
                    MainMap.Zoom = trackBar1.Value;
                }
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }

        public void trackerHomeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MainV2.comPort.MAV.cs.TrackerLocation = new PointLatLngAlt(MouseDownEnd)
            {
                Alt = MainV2.comPort.MAV.cs.HomeAlt
            };
        }

        public void TXT_DefaultAlt_KeyPress(object sender, KeyPressEventArgs e)
        {
            float isNumber = 0;
            if (e.KeyChar.ToString() == "\b")
                return;
            e.Handled = !float.TryParse(e.KeyChar.ToString(), out isNumber);
        }

        public void TXT_DefaultAlt_Leave(object sender, EventArgs e)
        {
            float isNumber = 0;
            if (!float.TryParse(TXT_DefaultAlt.Text, out isNumber))
            {
                TXT_DefaultAlt.Text = "200";
            }
        }


        public void TXT_loiterrad_KeyPress(object sender, KeyPressEventArgs e)
        {
            float isNumber = 0;
            if (e.KeyChar.ToString() == "\b")
                return;

            if (e.KeyChar == '-')
                return;

            e.Handled = !float.TryParse(e.KeyChar.ToString(), out isNumber);
        }

        public void TXT_loiterrad_Leave(object sender, EventArgs e)
        {
            float isNumber = 0;
            if (!float.TryParse(TXT_loiterrad.Text, out isNumber))
            {
                TXT_loiterrad.Text = "45";
            }
        }

        public void TXT_WPRad_KeyPress(object sender, KeyPressEventArgs e)
        {
            float isNumber = 0;
            if (e.KeyChar.ToString() == "\b")
                return;
            e.Handled = !float.TryParse(e.KeyChar.ToString(), out isNumber);
        }

        public void TXT_WPRad_Leave(object sender, EventArgs e)
        {
            float isNumber = 0;
            if (!float.TryParse(TXT_WPRad.Text, out isNumber))
            {
                TXT_WPRad.Text = "30";
            }
            if (isNumber > (127 * CurrentState.multiplierdist))
            {
                //CustomMessageBox.Show("The value can only be between 0 and 127 m");
                //TXT_WPRad.Text = (127 * CurrentState.multiplierdist).ToString();
            }
            writeKML();
        }

        private void updateCMDParams()
        {
            cmdParamNames = readCMDXML();

            if ((MAVLink.MAV_MISSION_TYPE)cmb_missiontype.SelectedValue == MAVLink.MAV_MISSION_TYPE.FENCE)
            {
                var fence = new[]
                {
                    "",
                    "",
                    "",
                    "",
                    "Lat",
                    "Long",
                    ""
                };
                cmdParamNames.Clear();
                cmdParamNames.Add(MAVLink.MAV_CMD.FENCE_RETURN_POINT.ToString(), fence.ToArray());
                fence[0] = "Points";
                cmdParamNames.Add(MAVLink.MAV_CMD.FENCE_POLYGON_VERTEX_INCLUSION.ToString(), fence.ToArray());
                cmdParamNames.Add(MAVLink.MAV_CMD.FENCE_POLYGON_VERTEX_EXCLUSION.ToString(), fence.ToArray());
                fence[0] = "Radius";
                cmdParamNames.Add(MAVLink.MAV_CMD.FENCE_CIRCLE_EXCLUSION.ToString(), fence.ToArray());
                cmdParamNames.Add(MAVLink.MAV_CMD.FENCE_CIRCLE_INCLUSION.ToString(), fence.ToArray());

            }
            if ((MAVLink.MAV_MISSION_TYPE)cmb_missiontype.SelectedValue == MAVLink.MAV_MISSION_TYPE.RALLY)
            {
                var rally = new[]
                {
                    "",
                    "",
                    "",
                    "",
                    "Lat",
                    "Long",
                    "Alt"
                };
                cmdParamNames.Clear();
                cmdParamNames.Add(MAVLink.MAV_CMD.RALLY_POINT.ToString(), rally);
            }

            List<string> cmds = new List<string>();

            foreach (string item in cmdParamNames.Keys)
            {
                cmds.Add(item);
            }

            cmds.Add("UNKNOWN");

            Command.DataSource = cmds;

            log.InfoFormat("Command item count {0} orig list {1}", Command.Items.Count, cmds.Count);
        }

        private void updateHomeText()
        {
            // set home location
            if (MainV2.comPort.MAV.cs.HomeLocation.Lat != 0 && MainV2.comPort.MAV.cs.HomeLocation.Lng != 0)
            {
                SetHomeHere(new PointLatLngAlt(
                                MainV2.comPort.MAV.cs.HomeLocation.Lat,
                                MainV2.comPort.MAV.cs.HomeLocation.Lng,
                                MainV2.comPort.MAV.cs.HomeLocation.Alt));
                writeKML();
            }
            else if (MainV2.comPort.MAV.cs.PlannedHomeLocation.Lat != 0 && MainV2.comPort.MAV.cs.PlannedHomeLocation.Lng != 0)
            {
                SetHomeHere(new PointLatLngAlt(
                                MainV2.comPort.MAV.cs.PlannedHomeLocation.Lat,
                                MainV2.comPort.MAV.cs.PlannedHomeLocation.Lng,
                                MainV2.comPort.MAV.cs.PlannedHomeLocation.Alt));
                writeKML();
            }
        }

        private void updateMapPosition(PointLatLng currentloc)
        {
            BeginInvoke((MethodInvoker)delegate
            {
                try
                {
                    if (lastmapposchange.Second != DateTime.Now.Second)
                    {
                        MainMap.Position = currentloc;
                        lastmapposchange = DateTime.Now;
                    }
                }
                catch (Exception ex)
                {
                    log.Error(ex);
                }
            });
        }

        private void updateMapType(object sender, System.Timers.ElapsedEventArgs e)
        {
            log.Info("updateMapType invoke req? " + comboBoxMapType.InvokeRequired);

            if (sender is System.Timers.Timer)
                ((System.Timers.Timer)sender).Stop();

            string mapType = Settings.Instance["MapType"];
            if (!string.IsNullOrEmpty(mapType))
            {
                try
                {
                    var index = GMapProviders.List.FindIndex(x => (x.Name == mapType));

                    if (index != -1) comboBoxMapType.SelectedIndex = index;
                }
                catch (Exception ex)
                {
                    log.Error(ex);
                }
            }
            else
            {
                if (L10N.ConfigLang.IsChildOf(CultureInfo.GetCultureInfo("zh-Hans")))
                {
                    DevComponents.DotNetBar.MessageBoxEx.Show(
                        "亲爱的中国用户，为保证地图使用正常，已为您将默认地图自动切换到具有中国特色的【谷歌中国卫星地图】！\r\n与默认【谷歌卫星地图】的区别：使用.cn服务器，加入火星坐标修正\r\n",
                        "默认地图已被切换");

                    mapType = "谷歌中国卫星地图";
                    try
                    {
                        var index = GMapProviders.List.FindIndex(x => (x.Name == mapType));

                        if (index != -1) comboBoxMapType.SelectedIndex = index;
                    }
                    catch (Exception ex)
                    {
                        log.Error(ex);
                    }
                }
                else
                {
                    mapType = "GoogleSatelliteMap";
                    // set default
                    try
                    {
                        var index = GMapProviders.List.FindIndex(x => (x.Name == mapType));

                        if (index != -1) comboBoxMapType.SelectedIndex = index;
                    }
                    catch (Exception ex)
                    {
                        log.Error(ex);
                    }
                }
            }
        }

        private void updateRowNumbers()
        {
            if (!IsHandleCreated || IsDisposed || Disposing)
                return;

            // number rows 
            BeginInvoke((MethodInvoker)delegate
            {
                // thread for updateing row numbers
                for (int a = 0; a < Commands.Rows.Count - 0; a++)
                {
                    if (IsDisposed || Disposing)
                        return;
                    try
                    {
                        if (Commands.Rows[a].HeaderCell.Value == null)
                        {
                            //Commands.Rows[a].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleLeft;
                            Commands.Rows[a].HeaderCell.Value = (a + 1).ToString();
                        }
                        // skip rows with the correct number
                        string rowno = Commands.Rows[a].HeaderCell.Value.ToString();
                        if (!rowno.Equals((a + 1).ToString()))
                        {
                            // this code is where the delay is when deleting.
                            Commands.Rows[a].HeaderCell.Value = (a + 1).ToString();
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
            });
        }

        /// <summary>
        /// Builds the get Capability request.
        /// </summary>
        /// <param name="serverUrl">The server URL.</param>
        /// <returns></returns>
        private string BuildGetCapabilitityRequest(string serverUrl)
        {
            // What happens if the URL already has  '?'. 
            // For example: http://foo.com?Token=yyyy
            // In this example, the get capability request should be 
            // http://foo.com?Token=yyyy&version=1.1.0&Request=GetCapabilities&service=WMS but not
            // http://foo.com?Token=yyyy?version=1.1.0&Request=GetCapabilities&service=WMS

            // If the URL doesn't contain '?', append it.
            if (!serverUrl.Contains("?"))
            {
                serverUrl += "?";
            }
            else
            {
                // Check if the URL already has query strings.
                // If the URL doesn't have query strings, '?' comes at the end.
                if (!serverUrl.EndsWith("?"))
                {
                    // Already have query string, so add '&' before adding other query strings.
                    serverUrl += "&";
                }
            }
            return serverUrl + "version=1.1.0&Request=GetCapabilities&service=WMS";
        }

        private void groupmarkeradd(GMapMarker marker)
        {
            System.Diagnostics.Debug.WriteLine("add marker " + marker.Tag.ToString());
            wpMarkersGroup.Add(int.Parse(marker.Tag.ToString()));
            if (marker is GMapMarkerWP)
            {
                ((GMapMarkerWP)marker).selected = true;
            }
            if (marker is GMapMarkerRect)
            {
                ((GMapMarkerWP)((GMapMarkerRect)marker).InnerMarker).selected = true;
            }
        }

        private void wpMarkersGroupAdd(GMapMarker marker)
        {

            if (marker is GMapMarkerWP)
            {
                System.Diagnostics.Debug.WriteLine("add marker " + marker.Tag.ToString());
                if (int.TryParse(((GMapMarkerWP)marker).Tag.ToString(), out int no))
                {
                    wpMarkersGroup.Add(int.Parse(marker.Tag.ToString()));
                    ((GMapMarkerWP)marker).selected = true;
                }
            }
            if (marker is GMapMarkerRect)
            {
                System.Diagnostics.Debug.WriteLine("add marker " + ((GMapMarkerWP)((GMapMarkerRect)marker).InnerMarker).Tag.ToString());
                if (int.TryParse(((GMapMarkerWP)((GMapMarkerRect)marker).InnerMarker).Tag.ToString(), out int no))
                {
                    wpMarkersGroup.Add(no);
                    ((GMapMarkerWP)((GMapMarkerRect)marker).InnerMarker).selected = true;
                }
            }
        }

        private void polygonMarkersGroupAdd(GMapMarker marker)
        {
            if (marker is GMapMarkerPolygon)
            {
                System.Diagnostics.Debug.WriteLine("add marker " + marker.Tag.ToString());
                if (int.TryParse(marker.Tag.ToString().Replace("grid", ""), out int no))
                {
                    polygonMarkersGroup.Add(no);
                    ((GMapMarkerPolygon)marker).selected = true;
                }
            }
            if (marker is GMapMarkerRect)
            {
                if (int.TryParse(((GMapMarkerRect)marker).InnerMarker.Tag.ToString().Replace("grid", ""), out int no))
                {
                    polygonMarkersGroup.Add(no);
                    ((GMapMarkerPolygon)((GMapMarkerRect)marker).InnerMarker).selected = true;
                }
            }

        }

        public void wpMarkersGroupAddAll()
        {
            foreach (var marker in MainMap.Overlays.First(a => a.Id == "WPOverlay").Markers)
            {
                try
                {
                    if (marker.Tag != null)
                    {
                        wpMarkersGroupAdd(marker);
                    }
                }
                catch (Exception ex)
                {
                    log.Error(ex);
                }
            }
            writeKML();
        }

        public void polygonMarkersGroupAddAll()
        {
            foreach (var marker in MainMap.Overlays.First(a => a.Id == "drawnpolygons").Markers)
            {
                try
                {
                    if (marker.Tag != null)
                    {
                        polygonMarkersGroupAdd(marker);
                    }
                }
                catch (Exception ex)
                {
                    log.Error(ex);
                }
            }
            redrawPolygonSurvey(drawnpolygon.Points.Select(p => new PointLatLngAlt(p)).ToList());
        }

        public void wpMarkersGroupClear()
        {
            if (wpMarkersGroup.Count > 0)
            {
                wpMarkersGroup.Clear();
                writeKML();
            }
        }

        public void polygonMarkersGroupClear()
        {
            if (polygonMarkersGroup.Count > 0)
            {
                polygonMarkersGroup.Clear();
                redrawPolygonSurvey(drawnpolygon.Points.Select(p => new PointLatLngAlt(p)).ToList());
            }
        }

        public void wpMarkersGroupFirst()
        {
            foreach (var marker in MainMap.Overlays.First(a => a.Id == "WPOverlay").Markers)
            {
                try
                {
                    if (marker.Tag != null && int.TryParse(marker.Tag.ToString(), out int no))
                    {
                        wpMarkersGroup.Clear();
                        wpMarkersGroup.Add(no);
                        writeKML();
                        return;
                    }
                }
                catch (Exception ex)
                {
                    log.Error(ex);
                }
            }
        }

        public void polygonMarkersGroupFirst()
        {
            foreach (var marker in MainMap.Overlays.First(a => a.Id == "drawnpolygons").Markers)
            {
                try
                {
                    if (marker.Tag != null && int.TryParse(marker.Tag.ToString().Replace("grid", ""), out int no))
                    {
                        polygonMarkersGroup.Clear();
                        polygonMarkersGroup.Add(no);
                        redrawPolygonSurvey(drawnpolygon.Points.Select(p => new PointLatLngAlt(p)).ToList());
                        return;
                    }
                }
                catch (Exception ex)
                {
                    log.Error(ex);
                }
            }
        }

        public void wpMarkersGroupNext()
        {
            if (wpMarkersGroup.Count > 0)
            {
                int next = (wpMarkersGroup.Max() + 1);
                bool isEqual = false;
                foreach (var marker in MainMap.Overlays.First(a => a.Id == "WPOverlay").Markers)
                {
                    try
                    {
                        if (marker.Tag != null && int.TryParse(marker.Tag.ToString(), out int no))
                        {
                            if (no == next)
                            {
                                isEqual = true;
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Error(ex);
                    }
                }
                wpMarkersGroup.Clear();
                if (isEqual)
                    wpMarkersGroup.Add(next);
                else
                    wpMarkersGroup.Add(1);

                writeKML();
            }
        }

        public void wpMarkersGroupPrev()
        {
            if (wpMarkersGroup.Count > 0)
            {
                int prev = (wpMarkersGroup.Min() - 1);
                wpMarkersGroup.Clear();
                if (prev > 0)
                    wpMarkersGroup.Add(prev);
                else
                {
                    int maxIndex = 0;
                    foreach (var marker in MainMap.Overlays.First(a => a.Id == "WPOverlay").Markers)
                    {
                        try
                        {
                            if (marker.Tag != null && int.TryParse(marker.Tag.ToString(), out int no))
                            {
                                if (no > maxIndex)
                                {
                                    maxIndex = no;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            log.Error(ex);
                        }
                    }
                    wpMarkersGroup.Add(maxIndex);
                }
                writeKML();
            }
        }

        public void polygonMarkersGroupNext()
        {
            if (polygonMarkersGroup.Count > 0)
            {
                int next = (polygonMarkersGroup.Max() + 1);
                bool isEqual = false;
                foreach (var marker in MainMap.Overlays.First(a => a.Id == "drawnpolygons").Markers)
                {
                    try
                    {
                        if (marker.Tag != null && int.TryParse(marker.Tag.ToString().Replace("grid", ""), out int no))
                        {
                            if (no == next)
                            {
                                isEqual = true;
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Error(ex);
                    }
                }
                polygonMarkersGroup.Clear();
                if (isEqual)
                    polygonMarkersGroup.Add(next);
                else
                    polygonMarkersGroup.Add(1);

                redrawPolygonSurvey(drawnpolygon.Points.Select(p => new PointLatLngAlt(p)).ToList());
            }
        }

        public void polygonMarkersGroupPrev()
        {
            if (polygonMarkersGroup.Count > 0)
            {
                int prev = (polygonMarkersGroup.Min() - 1);
                polygonMarkersGroup.Clear();
                if (prev > 0)
                    polygonMarkersGroup.Add(prev);
                else
                {
                    int maxIndex = 0;
                    foreach (var marker in MainMap.Overlays.First(a => a.Id == "drawnpolygons").Markers)
                    {
                        try
                        {
                            if (marker.Tag != null && int.TryParse(marker.Tag.ToString().Replace("grid", ""), out int no))
                            {
                                if (no > maxIndex)
                                {
                                    maxIndex = no;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            log.Error(ex);
                        }
                    }
                    polygonMarkersGroup.Add(maxIndex);
                }
                redrawPolygonSurvey(drawnpolygon.Points.Select(p => new PointLatLngAlt(p)).ToList());
            }
        }

        private void polygonMarkersGroupMoveUp()
        {
            polygonMarkersGroupMove(0, 1);
        }

        private void polygonMarkersGroupMoveDown()
        {
            polygonMarkersGroupMove(0, -1);
        }

        private void polygonMarkersGroupMoveLeft()
        {
            polygonMarkersGroupMove(-1, 0);
        }

        private void polygonMarkersGroupMoveRight()
        {
            polygonMarkersGroupMove(1, 0);
        }

        private void polygonMarkersGroupMove(int xMove, int yMove)
        {
            if (polygonMarkersGroup.Count > 0)
            {
                foreach (var marker in MainMap.Overlays.First(a => a.Id == "drawnpolygons").Markers)
                {
                    try
                    {
                        if (marker.Tag != null && int.TryParse(marker.Tag.ToString().Replace("grid", ""), out int no))
                        {
                            if (polygonMarkersGroup.Contains(no))
                            {
                                GPoint point = MainMap.FromLatLngToLocal(drawnpolygon.Points[no - 1]);
                                drawnpolygon.Points[no - 1] =
                                    MainMap.FromLocalToLatLng((int)point.X + xMove, (int)point.Y - yMove);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Error(ex);
                    }
                }
                isSendChange = true;
                redrawPolygonSurvey(drawnpolygon.Points.Select(p => new PointLatLngAlt(p)).ToList());
                isSendChange = false;
            }
        }

        private void wpMarkersGroupMoveUp()
        {
            wpMarkersGroupMove(0, 1);
        }

        private void wpMarkersGroupMoveDown()
        {
            wpMarkersGroupMove(0, -1);
        }

        private void wpMarkersGroupMoveLeft()
        {
            wpMarkersGroupMove(-1, 0);
        }

        private void wpMarkersGroupMoveRight()
        {
            wpMarkersGroupMove(1, 0);
        }

        private void wpMarkersGroupMove(int xMove, int yMove)
        {
            if (wpMarkersGroup.Count > 0)
            {
                foreach (var marker in MainMap.Overlays.First(a => a.Id == "WPOverlay").Markers)
                {
                    try
                    {
                        if (marker.Tag != null && int.TryParse(marker.Tag.ToString(), out int no))
                        {
                            if (wpMarkersGroup.Contains(no))
                            {
                                GPoint point = MainMap.FromLatLngToLocal(marker.Position);
                                marker.Position =
                                    MainMap.FromLocalToLatLng((int)point.X + xMove, (int)point.Y - yMove);
                                CallMeDrag(no.ToString(), marker.Position.Lat, marker.Position.Lng, System.Convert.ToInt32(Commands.Rows[no - 1].Cells[Alt.Index].Value.ToString()));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Error(ex);
                    }
                }

                isSendChange = true;
                writeKML();
                isSendChange = false;

            }
        }

        private void MainMap_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                //记录鼠标开始位置
                MouseDownStart = MainMap.FromLocalToLatLng(e.X, e.Y);
                MouseDownStartPoint = new GPoint(e.X, e.Y);
                if (IsMouseClickOffMenu)
                    return;

                if (e.Button == MouseButtons.Middle && Control.ModifierKeys != Keys.Alt && Control.ModifierKeys != Keys.Control)
                {
                    IsMouseDown = true;
                    IsWndDroppable = true;
                    return;
                }

                if (e.Button == MouseButtons.Left && (Control.ModifierKeys == Keys.Control) || Control.ModifierKeys == Keys.Alt)
                {
                    // group move
                    IsMouseDown = true;
                    IsMarkerPickable = true;
                    return;
                }

                if (e.Button == MouseButtons.Left && Control.ModifierKeys != Keys.Alt && Control.ModifierKeys != Keys.Control)
                {
                    IsMouseDown = true;

                    if (currentMarker.IsVisible)
                    {
                        currentMarker.Position = MainMap.FromLocalToLatLng(e.X, e.Y);
                    }
                    if (!IsDrawPolygongridMode && !IsDrawWPMode && CurentRectMarker == null)
                    {
                        IsWndDroppable = true;
                    }
                    else
                    {
                        IsMarkerAddable = true;
                        IsMarkerDroppable = true;
                    }
                    return;
                }
            }
            finally
            {
                MouseDownPrevPoint = new GPoint(e.X, e.Y);
            }
        }

        private void MainMap_MouseEnter(object sender, EventArgs e)
        {
            // MainMap.Focus();
        }

        private void MainMap_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                //记录鼠标位置
                MouseDownCurrent = MainMap.FromLocalToLatLng(e.X, e.Y);
                MouseDownCurrentPoint = new GPoint(e.X, e.Y);

                //鼠标没按下
                if (!IsMouseDown)
                {
                    // update mouse pos display
                    SetMouseDisplay(MouseDownCurrent.Lat, MouseDownCurrent.Lng, 0);
                }

                //鼠标没移动
                if (MouseDownStart == MouseDownCurrent)
                    return;
                //鼠标移动

                //设置移动点
                currentMarker.Position = MouseDownCurrent;

                //可拖动状态下鼠标按下
                if (IsMarkerDroppable)
                {
                    IsMarkerDroping = true;

                    if (CurentRectMarker != null) // left click pan
                    {
                        //更新Polygon点位置
                        try
                        {
                            // 检查polygon点
                            if (CurentRectMarker.InnerMarker.Tag.ToString().Contains("grid"))
                            {
                                drawnpolygon.Points[
                                    int.Parse(
                                        CurentRectMarker.InnerMarker.Tag.ToString().Replace("grid", "")) - 1
                                        ] = new PointLatLng(MouseDownCurrent.Lat, MouseDownCurrent.Lng);

                                redrawPolygonSurvey(drawnpolygon.Points.Select(a => new PointLatLngAlt(a)).ToList());
                            }
                        }
                        catch (Exception ex)
                        {
                            log.Error(ex);
                        }

                        //更新WP位置
                        try
                        {
                            // 检查WP点
                            if ((CurentRectMarker != null && Regex.IsMatch(CurentRectMarker.Tag.ToString(), @"^\d+$")))
                            {
                                int? pIndex = (int?)CurentRectMarker.Tag;
                                if (pIndex.HasValue)
                                {
                                    if (pIndex < wppolygon.Points.Count)
                                    {
                                        wppolygon.Points[pIndex.Value] = MouseDownCurrent;
                                        lock (thisLock)
                                        {
                                            MainMap.UpdatePolygonLocalPosition(wppolygon);
                                            MainMap.Invalidate();
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            log.Error(ex);
                        }

                        //随鼠标位置更新当前标记
                        if (CurentRectMarker.InnerMarker.Tag.ToString().Contains("grid"))
                        {
                            if (currentMarker.IsVisible)
                            {
                                currentMarker.Position = MouseDownCurrent;
                            }
                            CurentRectMarker.Position = MouseDownCurrent;
                            if (CurentRectMarker.InnerMarker != null)
                            {
                                CurentRectMarker.InnerMarker.Position = MouseDownCurrent;
                            }
                        }
                        else if (Regex.IsMatch(CurentRectMarker.Tag.ToString(), @"^\d+$"))
                        {
                            if (currentMarker.IsVisible)
                            {
                                currentMarker.Position = MouseDownCurrent;
                            }
                            CurentRectMarker.Position = MouseDownCurrent;
                            if (CurentRectMarker.InnerMarker != null)
                            {
                                CurentRectMarker.InnerMarker.Position = MouseDownCurrent;
                            }
                        }
                    }
                    else
                    {
                        if (polygonMarkersGroup.Count >= 0 && IsDrawPolygongridMode)
                        {
                            polygonMarkersGroupMove(
                                (int)(MouseDownCurrentPoint.X - MouseDownPrevPoint.X),
                                (int)(MouseDownPrevPoint.Y - MouseDownCurrentPoint.Y)
                                );
                        }
                        if (wpMarkersGroup.Count >= 0 && IsDrawWPMode)
                        {
                            wpMarkersGroupMove(
                                (int)(MouseDownCurrentPoint.X - MouseDownPrevPoint.X),
                                (int)(MouseDownPrevPoint.Y - MouseDownCurrentPoint.Y)
                                );
                        }
                    }
                    //else if (CurrentPOIMarker != null)
                    //{
                    //    CurrentPOIMarker.Position = mousePoint;
                    //}
                    //else if (CurrentGMapMarker != null)
                    //{
                    //    CurrentGMapMarker.Position = mousePoint;
                    //}
                }
                else if (IsWndDroppable)
                {
                    IsWndDroping = true;
                    double latdif = MouseDownStart.Lat - MouseDownCurrent.Lat;
                    double lngdif = MouseDownStart.Lng - MouseDownCurrent.Lng;

                    try
                    {
                        lock (thisLock)
                        {
                            if (!IsMouseClickOffMenu)
                                MainMap.Position = new PointLatLng(center.Position.Lat + latdif,
                                    center.Position.Lng + lngdif);
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Error(ex);
                    }
                }
                else if (IsMarkerPickable)
                {
                    // draw selection box
                    double latdif = MouseDownStart.Lat - MouseDownCurrent.Lat;
                    double lngdif = MouseDownStart.Lng - MouseDownCurrent.Lng;

                    MainMap.SelectedArea = new RectLatLng(Math.Max(MouseDownStart.Lat, MouseDownCurrent.Lat),
                        Math.Min(MouseDownStart.Lng, MouseDownCurrent.Lng), Math.Abs(lngdif), Math.Abs(latdif));
                }
                else if (e.Button == MouseButtons.None)
                {
                    IsMouseDown = false;
                }
            }
            finally
            {
                MouseDownPrevPoint = new GPoint(e.X, e.Y);
            }

        }

        private void MainMap_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right) // ignore right clicks
            {
                if (polyicon.Rectangle.Contains(e.Location)){
                    contextMenuStripMain.Visible = false;
                    ClearPolygonToolStripMenuItem_Click(this, null);
                }
                if (WPicon.Rectangle.Contains(e.Location))
                {
                    contextMenuStripMain.Visible = false;
                    ClearMissionToolStripMenuItem_Click(this, null);
                }
                if (zoomicon.Rectangle.Contains(e.Location))
                {
                    contextMenuStripMain.Visible = false;
                    contextMenuStripTiff.Show(MainMap, e.Location);
                }
                return;
            }
            try
            {
                if (IsMouseClickOffMenu)
                {
                    IsMouseClickOffMenu = false;
                    return;
                }

                // check if the mouse up happend over our button
                if (polyicon.Rectangle.Contains(e.Location))
                {
                    if (e.Button == MouseButtons.Left)
                    {
                        polyicon.IsSelected = !polyicon.IsSelected;

                    }
                    IsDrawPolygongridMode = polyicon.IsSelected ? true : false;

                    //contextMenuStripPoly.Show(MainMap, e.Location);
                    return;
                }
                if (WPicon.Rectangle.Contains(e.Location))
                {
                    if (e.Button == MouseButtons.Left)
                    {
                        surveyGridToolStripMenuItem_Click(this, null);
                    }
                    return;
                }
                if (zoomicon.Rectangle.Contains(e.Location))
                {
                    //contextMenuStripZoom.Show(MainMap, e.Location);
                    if (layerPolygonsOverlay.Polygons.Count > 0)
                        zoomicon.IsSelected = true;
                    else
                        zoomicon.IsSelected = false;
                    if (e.Button == MouseButtons.Left)
                    {
                        if (zoomicon.IsSelected)
                        {
                            zoomToTiffToolStripMenuItem_Click(this, null);
                        }
                        else
                        {
                            contextMenuStripMain.Visible = false;
                            TiffOverlayToolStripMenuItem_Click(this, null);
                        }
                        if (layerPolygonsOverlay.Polygons.Count > 0)
                            zoomicon.IsSelected = true;
                        else
                            zoomicon.IsSelected = false;
                        return;

                    }
                    
                    return;
                }


                else if (e.Button == MouseButtons.None)
                {
                    return;
                }

                MouseDownEnd = MainMap.FromLocalToLatLng(e.X, e.Y);
                MouseDownEndPoint = new GPoint(e.X, e.Y);



                if (IsMouseDown) // mouse down on some other object and dragged to here.
                {
                    if (IsWndDroppable)
                    {
                        return;
                    }
                    else if (IsMarkerPickable && Control.ModifierKeys == Keys.Control)
                    {
                        // group select wps
                        GMapPolygon poly = new GMapPolygon(new List<PointLatLng>(), "temp");

                        poly.Points.Add(MouseDownStart);
                        poly.Points.Add(new PointLatLng(MouseDownStart.Lat, MouseDownEnd.Lng));
                        poly.Points.Add(MouseDownEnd);
                        poly.Points.Add(new PointLatLng(MouseDownEnd.Lat, MouseDownStart.Lng));

                        foreach (var marker in MainMap.Overlays.First(a => a.Id == "WPOverlay").Markers)
                        {
                            if (poly.IsInside(marker.Position))
                            {
                                try
                                {
                                    if (marker.Tag != null)
                                    {
                                        wpMarkersGroupAdd(marker);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    log.Error(ex);
                                }
                            }
                        }

                        return;
                    }
                    else if (IsMarkerPickable && Control.ModifierKeys == Keys.Alt)
                    {
                        // group select wps
                        GMapPolygon poly = new GMapPolygon(new List<PointLatLng>(), "temp");

                        poly.Points.Add(MouseDownStart);
                        poly.Points.Add(new PointLatLng(MouseDownStart.Lat, MouseDownEnd.Lng));
                        poly.Points.Add(MouseDownEnd);
                        poly.Points.Add(new PointLatLng(MouseDownEnd.Lat, MouseDownStart.Lng));

                        foreach (var marker in MainMap.Overlays.First(a => a.Id == "drawnpolygons").Markers)
                        {
                            if (poly.IsInside(marker.Position))
                            {
                                try
                                {
                                    if (marker.Tag != null)
                                    {
                                        polygonMarkersGroupAdd(marker);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    log.Error(ex);
                                }
                            }
                        }

                        return;
                    }
                    else if (IsMarkerDroping)
                    {
                        if (CurentRectMarker != null && CurentRectMarker.InnerMarker != null)
                        {
                            if (CurentRectMarker.InnerMarker.Tag.ToString().Contains("grid"))
                            {
                                try
                                {
                                    drawnpolygon.Points[
                                            int.Parse(CurentRectMarker.InnerMarker.Tag.ToString().Replace("grid", "")) - 1
                                            ] = new PointLatLng(MouseDownEnd.Lat, MouseDownEnd.Lng);

                                    isSendChange = true;
                                    redrawPolygonSurvey(drawnpolygon.Points.Select(a => new PointLatLngAlt(a)).ToList());
                                    isSendChange = false;
                                }
                                catch (Exception ex)
                                {
                                    log.Error(ex);
                                }
                            }
                            else if ((CurentRectMarker != null && Regex.IsMatch(CurentRectMarker.Tag.ToString(), @"^\d+$")))
                            {
                                CallMeDrag(CurentRectMarker.InnerMarker.Tag.ToString(), currentMarker.Position.Lat,
                                currentMarker.Position.Lng, -2);
                            }
                            CurentRectMarker = null;
                        }
                        else
                        {
                            if (polygonMarkersGroup.Count >= 0 && IsDrawPolygongridMode)
                            {
                                polygonMarkersGroupMove(
                                    (int)(MouseDownCurrentPoint.X - MouseDownPrevPoint.X),
                                    (int)(MouseDownPrevPoint.Y - MouseDownCurrentPoint.Y)
                                    );
                            }
                            if (wpMarkersGroup.Count >= 0 && IsDrawWPMode)
                            {
                                wpMarkersGroupMove(
                                    (int)(MouseDownCurrentPoint.X - MouseDownPrevPoint.X),
                                    (int)(MouseDownPrevPoint.Y - MouseDownCurrentPoint.Y)
                                    );
                            }
                        }
                        return;
                    }
                    
                    if (wpMarkersGroup.Count > 0 || polygonMarkersGroup.Count > 0)
                    {
                        if (wpMarkersGroup.Count > 0)
                        {
                            wpMarkersGroupClear();
                            // redraw to remove selection
                        }
                        if (polygonMarkersGroup.Count > 0)
                        {
                            polygonMarkersGroupClear();
                            // redraw to remove selection
                        }
                        return;
                    }

                    if (IsMarkerAddable)
                    {
                        if (CurentRectMarker != null)
                        {
                            // cant add WP in existing rect
                            return;
                        }
                        if (CurrentMidLine is GMapMarkerPlus)
                        {
                            OnMidLine_Click();
                            return;
                        }
                        AddMarkToMap(currentMarker.Position.Lat, currentMarker.Position.Lng, 0);

                    }
                    // drag finished, update poi db
                    //if (CurrentPOIMarker != null)
                    //{
                    //    POI.POIMove(CurrentPOIMarker);
                    //    CurrentPOIMarker = null;
                    //}
                }
            }
            finally
            {
                MainMap.SelectedArea = RectLatLng.Empty;
                IsMouseDown = false;
                IsMarkerAddable = false;
                IsWndDroppable = false;
                IsMarkerDroppable = false;
                IsMarkerPickable = false;
                IsMarkerDroping = false;
                CurentRectMarker = null;
            }
        }

        private void ReCalcFence(int rowno, bool insert, bool delete)
        {
            if (insert)
            {
                var currentlist = GetCommandList();

                var type = currentlist[rowno].id;

                if (type == (ushort) MAVLink.MAV_CMD.FENCE_POLYGON_VERTEX_INCLUSION ||
                    type == (ushort) MAVLink.MAV_CMD.FENCE_POLYGON_VERTEX_EXCLUSION)
                {
                    var oldcount = int.Parse(Commands.Rows[rowno - 1].Cells[Param1.Index].Value.ToString());
                    var newcount = oldcount + 1;

                    var list = currentlist.Where((a, i) =>
                        a.id == type && (a.p1 == 0 || a.p1 == oldcount));
                    //&& i >= rowno - oldcount && i <= rowno + oldcount

                    while (list.Count() > 0)
                    {
                        var length = (int) list.First().p1;
                        if (length == 0)
                            length = 1;
                        int cnt = 0;
                        var sublist = list.Where((a, i) =>
                        {
                            if (a.p1 == 0 && cnt <= length)
                                return true;
                            cnt++;
                            return cnt <= length;
                        }).ToList();

                        if (sublist.Count() == newcount)
                        {
                            foreach (var locationwp in sublist)
                            {
                                var idx = currentlist.IndexOf(locationwp);
                                Commands[Param1.Index, idx].Value = newcount;
                            }

                            return;
                        }

                        if (list.Count() < length)
                            break;
                        list = list.Skip(length);
                    }
                }
            }

            if (delete)
            {
                var currentlist = GetCommandList();

                var type = currentlist[rowno].id;

                if (type == (ushort)MAVLink.MAV_CMD.FENCE_POLYGON_VERTEX_INCLUSION ||
                    type == (ushort)MAVLink.MAV_CMD.FENCE_POLYGON_VERTEX_EXCLUSION)
                {
                    var rowdelete = currentlist[rowno];
                    var oldcount = int.Parse(Commands.Rows[rowno].Cells[Param1.Index].Value.ToString());
                    var newcount = oldcount - 1;

                    var list = currentlist.Where((a, i) =>
                        a.id == type && a.p1 == oldcount);

                    while (list.Count() > 0)
                    {
                        var length = (int)list.First().p1;
                        if (length == 0)
                            length = 1;
                        int cnt = 0;
                        var sublist = list.Take(length).ToList();

                        if (sublist.Contains(rowdelete))
                        {
                            foreach (var locationwp in sublist)
                            {
                                var idx = currentlist.IndexOf(locationwp);
                                Commands[Param1.Index, idx].Value = newcount;
                            }

                            return;
                        }

                        if (list.Count() < length)
                            break;
                        list = list.Skip(length);
                    }
                }
            }
        }

        private void MainMap_OnCurrentPositionChanged(PointLatLng point)
        {
            if (point.Lat > 90)
            {
                point.Lat = 90;
            }
            if (point.Lat < -90)
            {
                point.Lat = -90;
            }
            if (point.Lng > 180)
            {
                point.Lng = 180;
            }
            if (point.Lng < -180)
            {
                point.Lng = -180;
            }
            center.Position = point;


        }

        private void MainMap_OnMapTypeChanged(GMapProvider type)
        {
            comboBoxMapType.SelectedItem = MainMap.MapProvider;

            if (type == WMSProvider.Instance)
            {
                string url = "";
                if (Settings.Instance["WMSserver"] != null)
                    url = Settings.Instance["WMSserver"];
                if (DialogResult.Cancel == InputBox.Show("WMS Server", "请输入 WMS server URL", ref url))
                    return;

                // Build get capability request.
                string szCapabilityRequest = BuildGetCapabilitityRequest(url);

                XmlDocument xCapabilityResponse = MakeRequest(szCapabilityRequest);
                ProcessWmsCapabilitesRequest(xCapabilityResponse);

                Settings.Instance["WMSserver"] = url;
                WMSProvider.CustomWMSURL = url;
            }
        }

        private void MainMap_OnMapZoomChanged()
        {
            if (MainMap.Zoom > 0)
            {
                try
                {
                    trackBar1.Value = (int)(MainMap.Zoom);
                }
                catch (Exception ex)
                {
                    log.Error(ex);
                }
                //textBoxZoomCurrent.Text = MainMap.Zoom.ToString();
                center.Position = MainMap.Position;
            }
        }

        private void MainMap_OnMarkerClick(GMapMarker item, object ei)
        {
            var e = ei as MouseEventArgs;
            //int answer;
            try // when dragging item can sometimes be null
            {
                if (item.Tag == null)
                {
                    // home.. etc
                    return;
                }
                if (int.TryParse(item.Tag.ToString(), out int answer))
                {
                    Commands.CurrentCell = Commands[0, answer - 1];
                }
                if (IsMarkerPickable)
                {
                    if (Control.ModifierKeys == Keys.Control)
                    {
                        try
                        {
                            wpMarkersGroupAdd(item);

                            log.Info("add marker to group");
                        }
                        catch (Exception ex)
                        {
                            log.Error(ex);
                        }
                    }
                    else if (Control.ModifierKeys == Keys.Alt)
                    {
                        try
                        {
                            polygonMarkersGroupAdd(item);

                            log.Info("add marker to group");
                        }
                        catch (Exception ex)
                        {
                            log.Error(ex);
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }

        private void MainMap_OnMarkerEnter(GMapMarker item)
        {
            if (!IsMouseDown)
            {
                if (item is GMapMarkerRect)
                {
                    GMapMarkerRect rc = item as GMapMarkerRect;
                    rc.Pen.Color = Color.Red;
                    MainMap.Invalidate(false);

                    int answer;
                    if (item.Tag != null && rc.InnerMarker != null &&
                        int.TryParse(rc.InnerMarker.Tag.ToString(), out answer))
                    {
                        try
                        {
                            Commands.CurrentCell = Commands[0, answer - 1];
                            //item.ToolTipText = "Alt: " + Commands[Alt.Index, answer - 1].Value;
                            //item.ToolTipMode = MarkerTooltipMode.OnMouseOver;
                        }
                        catch (Exception ex)
                        {
                            log.Error(ex);
                        }
                    }

                    CurentRectMarker = rc;
                }
                if (item is GMapMarkerRallyPt)
                {
                    CurrentRallyPt = item as GMapMarkerRallyPt;
                }
                if (item is GMapMarkerAirport)
                {
                    // do nothing - readonly
                    return;
                }
                if (item is GMapMarkerPlus && ((GMapMarkerPlus)item).Tag is midline)
                {
                    CurrentMidLine = item;
                    return;
                }
                if (item is GMapMarkerPOI)
                {
                    CurrentPOIMarker = item as GMapMarkerPOI;
                }
                if (item is GMapMarkerWP)
                {
                    //CurrentGMapMarker = item;
                }
                if (item is GMapMarker)
                {
                    //CurrentGMapMarker = item;
                }
            }
        }

        private void MainMap_OnMarkerLeave(GMapMarker item)
        {
            if (!IsMouseDown)
            {
                if (item is GMapMarkerRect)
                {
                    CurentRectMarker = null;
                    GMapMarkerRect rc = item as GMapMarkerRect;
                    rc.ResetColor();
                    MainMap.Invalidate(false);
                }
                if (item is GMapMarkerRallyPt)
                {
                    CurrentRallyPt = null;
                }
                if (item is GMapMarkerPOI)
                {
                    CurrentPOIMarker = null;
                }
                if (item is GMapMarkerPlus && ((GMapMarkerPlus)item).Tag is midline)
                {
                    CurrentMidLine = null;
                }
                if (item is GMapMarker)
                {
                    // when you click the context menu this triggers and causes problems
                    CurrentGMapMarker = null;
                }
            }
        }

        private void MainMap_OnTileLoadComplete(long ElapsedMilliseconds)
        {
            //MainMap.ElapsedMilliseconds = ElapsedMilliseconds;

            MethodInvoker m = delegate
            {
                lbl_status.Text = "Status: loaded tiles";

                //panelMenu.Text = "Menu, last load in " + MainMap.ElapsedMilliseconds + "ms";

                //textBoxMemory.Text = string.Format(CultureInfo.InvariantCulture, "{0:0.00}MB of {1:0.00}MB", MainMap.Manager.MemoryCacheSize, MainMap.Manager.MemoryCacheCapacity);
            };
            try
            {
                if (!IsDisposed && IsHandleCreated) BeginInvoke(m);
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }

        private void MainMap_OnTileLoadStart()
        {
            MethodInvoker m = delegate { lbl_status.Text = "Status: loading tiles..."; };
            try
            {
                if (IsHandleCreated) BeginInvoke(m);
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }

        private XmlDocument MakeRequest(string requestUrl)
        {
            try
            {
                HttpWebRequest request = WebRequest.Create(requestUrl) as HttpWebRequest;
                if (!String.IsNullOrEmpty(Settings.Instance.UserAgent))
                    ((HttpWebRequest)request).UserAgent = Settings.Instance.UserAgent;
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;


                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(response.GetResponseStream());
                return (xmlDoc);
            }
            catch (Exception e)
            {
                DevComponents.DotNetBar.MessageBoxEx.Show("Failed to make WMS Server request: " + e.Message);
                return null;
            }
        }

        private void ProcessWmsCapabilitesRequest(XmlDocument xCapabilitesResponse)
        {
            //Create namespace manager
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(xCapabilitesResponse.NameTable);

            //check if the response is a valid xml document - if not, the server might still be able to serve us but all the checks below would fail. example: http://tiles.kartat.kapsi.fi/peruskartta
            //best sign is that there is no node WMT_MS_Capabilities
            if (xCapabilitesResponse.SelectNodes("//WMT_MS_Capabilities", nsmgr).Count == 0)
                return;


            //first, we have to make sure that the server is able to send us png imagery
            bool bPngCapable = false;
            XmlNodeList getMapElements = xCapabilitesResponse.SelectNodes("//GetMap", nsmgr);
            if (getMapElements.Count != 1)
                DevComponents.DotNetBar.MessageBoxEx.Show("Invalid WMS Server response: Invalid number of GetMap elements.");
            else
            {
                XmlNode getMapNode = getMapElements.Item(0);
                //search through all format nodes for image/png
                foreach (XmlNode formatNode in getMapNode.SelectNodes("//Format", nsmgr))
                {
                    if (formatNode.InnerText.Contains("image/png"))
                    {
                        bPngCapable = true;
                        break;
                    }
                }
            }


            if (!bPngCapable)
            {
                DevComponents.DotNetBar.MessageBoxEx.Show("Invalid WMS Server response: Server unable to return PNG images.");
                return;
            }


            //now search through all layer -> srs nodes for EPSG:4326 compatibility
            bool bEpsgCapable = false;
            XmlNodeList srsELements = xCapabilitesResponse.SelectNodes("//SRS", nsmgr);
            foreach (XmlNode srsNode in srsELements)
            {
                if (srsNode.InnerText.Contains("EPSG:4326"))
                {
                    bEpsgCapable = true;
                    break;
                }
            }


            if (!bEpsgCapable)
            {
                DevComponents.DotNetBar.MessageBoxEx.Show(
                    "Invalid WMS Server response: Server unable to return EPSG:4326 / WGS84 compatible images.");
                return;
            }


            // the server is capable of serving our requests - now check if there is a layer to be selected
            // Display layer title in the input box instead of layer name.
            // format: layer -> layer -> name
            //         layer -> layer -> title
            string szLayerSelection = "";
            int iSelect = 0;
            List<string> szListLayerName = new List<string>();
            // Loop through all layers.
            XmlNodeList layerElements = xCapabilitesResponse.SelectNodes("//Layer/Layer", nsmgr);
            foreach (XmlNode layerElement in layerElements)
            {
                // Get Name element.
                var nameNode = layerElement.SelectSingleNode("Name", nsmgr);

                // Skip if no name element is found.
                if (nameNode != null)
                {
                    var name = nameNode.InnerText;
                    // Set the default title as the layer name. 
                    var title = name;
                    // Get Title element.
                    var titleNode = layerElement.SelectSingleNode("Title", nsmgr);
                    if (titleNode != null)
                    {
                        var titleText = titleNode.InnerText;
                        if (!string.IsNullOrWhiteSpace(titleText))
                        {
                            title = titleText;
                        }
                    }
                    szListLayerName.Add(name);

                    szLayerSelection += string.Format("{0}: {1}\n ", iSelect, title);
                    //mixing control and formatting is not optimal...
                    iSelect++;
                }
            }

            //only select layer if there is one
            if (szListLayerName.Count != 0)
            {
                //now let the user select a layer
                string szUserSelection = "";
                if (DialogResult.Cancel ==
                    InputBox.Show("WMS Server",
                        "The following layers were detected:\n " + szLayerSelection +
                        "Please choose one by typing the associated number.", ref szUserSelection))
                    return;
                int iUserSelection = 0;
                try
                {
                    iUserSelection = Convert.ToInt32(szUserSelection);
                }
                catch (Exception ex)
                {
                    log.Error(ex);
                    iUserSelection = 0; //ignore all errors and default to first layer
                }

                WMSProvider.szWmsLayer = szListLayerName[iUserSelection];
                Settings.Instance["WMSLayer"] = WMSProvider.szWmsLayer;
            }
        }

        public void zoomToToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string place = "Perth Airport, Australia";
            if (DialogResult.OK == InputBox.Show("Location", "请输入你的 location", ref place))
            {
                GeoCoderStatusCode status = MainMap.SetPositionByKeywords(place);
                if (status != GeoCoderStatusCode.G_GEO_SUCCESS)
                {
                    DevComponents.DotNetBar.MessageBoxEx.Show("Google Maps Geocoder can't find: '" + place + "', reason: " + status,
                        "GMap.NET", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
                else
                {
                    MainMap.Zoom = 15;
                }
            }
        }

        private void zoomToVehicleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MainV2.comPort.MAV.cs.Location.Lat == 0 && MainV2.comPort.MAV.cs.Location.Lng == 0)
            {
                DevComponents.DotNetBar.MessageBoxEx.Show(Strings.Invalid_Location, Strings.ERROR);
                return;
            }

            MainMap.Position = MainV2.comPort.MAV.cs.Location;
            if(MainMap.Zoom < 17)
                MainMap.Zoom = 17;
        }

        private void zoomToMissionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MainMap.ZoomAndCenterMarkers("WPOverlay");
        }

        private void zoomToHomeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MainMap.Position = MainV2.comPort.MAV.cs.HomeLocation;
            if (MainMap.Zoom < 17)
                MainMap.Zoom = 17;
        }

        public void ShowLayerOverlay(GDAL.GDAL.GeoBitmap geoBitmap)
        {
            layerPolygonsOverlay.Polygons.Clear();

            PointLatLngAlt pos1 = new PointLatLngAlt(geoBitmap.Rect.Top, geoBitmap.Rect.Left);
            PointLatLngAlt pos2 = new PointLatLngAlt(geoBitmap.Rect.Bottom, geoBitmap.Rect.Right);
            List<Bitmap> tiles = new List<Bitmap>();
            List<RectLatLng> positions = new List<RectLatLng>();
            for (int i = 0; i < geoBitmap.BitmapTile.Count; i++) {
                tiles.Add(geoBitmap.BitmapTile[i]._tile);
                positions.Add(geoBitmap.BitmapTile[i]._position);
            }
            var mark = new GMapMarkerLayer(
                pos1, pos2,
                geoBitmap.DisplayBitmap, tiles, positions);


            layerPolygonsOverlay.Polygons.Add(mark);

            FlightPlanner.instance.zoomToTiffLayer();
        }

        public void zoomToTiffLayer()
        {
            zoomToTiffToolStripMenuItem_Click(this, null);
        }

        private void zoomToTiffToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //double Lat = (rect.Left + rect.Right) / 2;
            //double Lng = (rect.Top + rect.Bottom) / 2;
            var layerInfo = VPS.Layer.MemoryLayerCache.GetLayerFromMemoryCache(Settings.Instance["defaultTiffLayer"]);
            if (layerInfo == null)
                return;

            double lng = MainV2.instance.defaultHome.Lng;
            double lat = MainV2.instance.defaultHome.Lat;
            double alt = MainV2.instance.defaultHome.Alt;

            SetHomeHere(new PointLatLngAlt(lat, lng, alt));

            MainMap.SetZoomToFitRect(MainV2.instance.displayRect);
        }


        #region AltFrame
        #region AltFrame 接口函数
        private delegate void SetAltFrameInThread(string frame);
        public void SetAltFrameHandle(string frame)
        {
            if (this.InvokeRequired)
            {
                SetAltFrameInThread inThread = new SetAltFrameInThread(SetAltFrameHandle);
                this.Invoke(inThread, new object[] { frame });
            }
            else
            {
                SetAltFrame(frame);
            }
        }
        #endregion
        #region AltFrame 入口函数
        private void SetAltFrame(string frame)
        {
            switch (frame.ToString())
            {
                case "Relative":
                    CMB_altmode.SelectedText = GCSViews.FlightPlanner.altmode.Relative.ToString();
                    break;
                case "Absolute":
                    CMB_altmode.SelectedText = GCSViews.FlightPlanner.altmode.Absolute.ToString();
                    break;
                case "Terrain":
                    CMB_altmode.SelectedText = GCSViews.FlightPlanner.altmode.Terrain.ToString();
                    break;
                default:
                    CMB_altmode.SelectedText = GCSViews.FlightPlanner.altmode.Relative.ToString();
                    break;
            }
        }
        #endregion
        #endregion

        #region WPRad
        #region WPRad 接口函数
        private delegate void SetWPRadInThread(int wpRad);
        public void SetWPRadHandle(int wpRad)
        {
            if (this.InvokeRequired)
            {
                SetWPRadInThread inThread = new SetWPRadInThread(SetWPRadHandle);
                this.Invoke(inThread, new object[] { wpRad });
            }
            else
            {
                SetWPRad(wpRad);
            }
        }
        #endregion
        #region WPRad 入口函数
        private void SetWPRad(int wpRad)
        {
            TXT_WPRad.Text = wpRad.ToString();
        }
        #endregion
        #endregion

        #region Alt
        #region BaseAlt
        #region BaseAlt 接口函数
        private delegate void SetBaseAltInThread(int baseAlt);
        public void SetBaseAltHandle(int baseAlt)
        {
            if (this.InvokeRequired)
            {
                SetBaseAltInThread inThread = new SetBaseAltInThread(SetBaseAltHandle);
                this.Invoke(inThread, new object[] { baseAlt });
            }
            else
            {
                SetBaseAlt(baseAlt);
            }
        }
        #endregion
        #region BaseAlt 入口函数
        private void SetBaseAlt(int baseAlt)
        {
        }
        #endregion
        #endregion

        #region DefaultAlt
        #region DefaultAlt 接口函数
        private delegate void SetDefaultAltInThread(int defaultAlt);
        public void SetDefaultAltHandle(int defaultAlt)
        {
            if (this.InvokeRequired)
            {
                SetDefaultAltInThread inThread = new SetDefaultAltInThread(SetDefaultAltHandle);
                this.Invoke(inThread, new object[] { defaultAlt });
            }
            else
            {
                SetDefaultAlt(defaultAlt);
            }
        }
        #endregion
        #region DefaultAlt 入口函数
        private void SetDefaultAlt(int defaultAlt)
        {
            TXT_DefaultAlt.Text = defaultAlt.ToString();
        }
        #endregion
        #endregion

        #region WarnAlt
        #region WarnAlt 接口函数
        private delegate void SetWarnAltInThread(int warnAlt);
        public void SetWarnAltHandle(int warnAlt)
        {
            if (this.InvokeRequired)
            {
                SetWarnAltInThread inThread = new SetWarnAltInThread(SetWarnAltHandle);
                this.Invoke(inThread, new object[] { warnAlt });
            }
            else
            {
                SetWarnAlt(warnAlt);
            }
        }
        #endregion
        #region WarnAlt 入口函数
        private void SetWarnAlt(int warnAlt)
        {
            TXT_altwarn.Text = warnAlt.ToString();
        }
        #endregion
        #endregion

        #endregion

        #region AddMarkToMap

        #region PolygonMark
        private static object polygonLock = new object();
        private bool addPolygonMode;
        
        public bool IsDrawPolygongridMode
        {
            get { return addPolygonMode; }
            set
            {
                lock (polygonLock)
                {
                    addPolygonMode = value;
                    if (addPolygonMode)
                        ToDrawPolygonHandle?.Invoke();
                    else
                        OutDrawPolygonHandle?.Invoke();
                }
            }
        }

        #region 响应函数
        public delegateHandler ToDrawPolygonHandle;
        public delegateHandler OutDrawPolygonHandle;
        #endregion

        #endregion

        #region WPMark
        private static object wpLock = new object();
        private bool addWPMode;
        public bool IsDrawWPMode
        {
            get { return addWPMode; }
            set
            {
                lock (wpLock)
                {
                    addWPMode = value;
                    if (addWPMode)
                        ToDrawWPHandle?.Invoke();
                    else
                        OutDrawWPHandle?.Invoke();
                }
            }
        }
        #region 响应函数
        public delegateHandler ToDrawWPHandle;
        public delegateHandler OutDrawWPHandle;
        #endregion

        #endregion

        #region 入口函数
        /// <summary>
        /// Used to create a new WP
        /// </summary>
        /// <param name="lat"></param>
        /// <param name="lng"></param>
        /// <param name="alt"></param>
        public void AddMarkToMap(double lat, double lng, int alt)
        {
            if (IsDrawPolygongridMode)
            {
                AddPolygonPoint(lat, lng);

                isSendChange = true;
                redrawPolygonSurvey(drawnpolygon.Points.Select(p => new PointLatLngAlt(p)).ToList());
                isSendChange = false;
                return;
            }
            else if (IsDrawWPMode)
            {

                if (sethome)
                {
                    sethome = false;
                    CallMeDrag("H", lat, lng, alt);
                    return;
                }
                //creating a WP

                AddWPPoint(lat, lng, alt);

                isSendChange = true;
                writeKML();
                isSendChange = false;
            }
        }
        #endregion

        #region 刷新Polygon显示
        public void redrawPolygonSurvey(List<PointLatLngAlt> list)
        {
            drawnpolygon.Points.Clear();
            drawnpolygonsoverlay.Clear();

            int tag = 0;
            list.ForEach(x =>
            {
                tag++;
                drawnpolygon.Points.Add(x);
                Addpolygonmarkergrid(tag.ToString(), x.Lng, x.Lat, 0);
            });

            drawnpolygonsoverlay.Polygons.Add(drawnpolygon);
            MainMap.UpdatePolygonLocalPosition(drawnpolygon);

            if (drawnpolygon.Points.Count > 0)
            {
                foreach (var pointLatLngAlt in drawnpolygon.Points.CloseLoop().PrevNowNext())
                {
                    var now = pointLatLngAlt.Item2;
                    var next = pointLatLngAlt.Item3;

                    if (now == null || next == null)
                        continue;

                    var mid = new PointLatLngAlt((now.Lat + next.Lat) / 2, (now.Lng + next.Lng) / 2, 0);

                    var pnt = new GMapMarkerPlus(mid);
                    pnt.Tag = new midline() { now = now, next = next };
                    drawnpolygonsoverlay.Markers.Add(pnt);
                }
            }

            MainMap.Invalidate();
            if (isSendChange)
                PolygonListChange?.Invoke(GetPolygonList());
        }
        #endregion

        #region 刷新WPList显示
        /// <summary>
        /// used to write a KML, update the Map view polygon, and update the row headers
        /// </summary>
        public void writeKML()
        {
            // quickadd is for when loading wps from eeprom or file, to prevent slow, loading times
            if (quickadd)
                return;

            if (Disposing)
                return;

            updateRowNumbers();

            PointLatLngAlt home = new PointLatLngAlt();
            if (homePosition != null)
            {
                home = homePosition;
                home.Tag = "H";
            }
            else
            {
                home = new PointLatLngAlt();
                home.Tag = "H";
                home.Tag2 = "Terrain";
            }

            try
            {
                var commandlist = GetCommandList();

                if ((MAVLink.MAV_MISSION_TYPE)cmb_missiontype.SelectedValue == MAVLink.MAV_MISSION_TYPE.MISSION)
                {
                    overlay = new WPOverlay();
                    overlay.overlay.Id = "WPOverlay";

                    try
                    {
                        if (TXT_WPRad.Text == "") TXT_WPRad.Text = "5";
                        if (TXT_loiterrad.Text == "") TXT_loiterrad.Text = "30";

                        overlay.CreateOverlay(home,
                            commandlist,
                            double.Parse(TXT_WPRad.Text) / CurrentState.multiplieralt,
                            double.Parse(TXT_loiterrad.Text) / CurrentState.multiplieralt);
                        foreach (var marker in overlay.overlay.Markers)
                        {
                            try
                            {
                                if (marker is GMapMarkerWP)
                                {
                                    if (int.TryParse(((GMapMarkerWP)marker).Tag.ToString(), out int no))
                                    {
                                        if (wpMarkersGroup.Contains(no))
                                        {
                                            ((GMapMarkerWP)marker).selected = true;
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {

                            }
                        }
                    }
                    catch (FormatException ex)
                    {
                        DevComponents.DotNetBar.MessageBoxEx.Show(Strings.InvalidNumberEntered + "\n" + "WP Radius or Loiter Radius",
                            Strings.ERROR);
                    }

                    MainMap.HoldInvalidation = true;

                    var existing = MainMap.Overlays.Where(a => a.Id == overlay.overlay.Id).ToList();
                    foreach (var b in existing)
                    {
                        MainMap.Overlays.Remove(b);
                    }

                    MainMap.Overlays.Add(overlay.overlay);

                    overlay.overlay.ForceUpdate();

                    SetGradAndDistAndAZ(overlay.pointlist, home);

                    if (overlay.pointlist.Count <= 1)
                    {
                        //RectLatLng? rect = MainMap.GetRectOfAllMarkers(overlay.overlay.Id);
                        //if (rect.HasValue)
                        //{
                        //    MainMap.Position = rect.Value.LocationMiddle;
                        //}

                        //MainMap_OnMapZoomChanged();
                    }

                    pointlist = overlay.pointlist;

                    {
                        foreach (var pointLatLngAlt in pointlist.PrevNowNext())
                        {
                            var prev = pointLatLngAlt.Item1;
                            var now = pointLatLngAlt.Item2;
                            var next = pointLatLngAlt.Item3;

                            if (now == null || next == null)
                                continue;

                            var mid = new PointLatLngAlt((now.Lat + next.Lat) / 2, (now.Lng + next.Lng) / 2,
                                (now.Alt + next.Alt) / 2);

                            var pnt = new GMapMarkerPlus(mid);
                            pnt.Tag = new midline() { now = now, next = next };
                            overlay.overlay.Markers.Add(pnt);
                        }
                    }

                    // draw fence
                    {
                        var fenceoverlay = new WPOverlay();
                        fenceoverlay.overlay.Id = "fence";
                        try
                        {
                            fenceoverlay.CreateOverlay(PointLatLngAlt.Zero,
                            MainV2.comPort.MAV.fencepoints.Values.Select(a => (Locationwp)a).ToList(), 0, 0);
                        }
                        catch
                        {

                        }
                        fenceoverlay.overlay.Markers.Select(a => a.IsHitTestVisible = false).ToArray();
                        var fence = MainMap.Overlays.Where(a => a.Id == "fence");
                        if (fence.Count() > 0)
                            MainMap.Overlays.Remove(fence.First());
                        MainMap.Overlays.Add(fenceoverlay.overlay);

                        fenceoverlay.overlay.ForceUpdate();
                    }

                    MainMap.RefreshInThread();
                }

                if ((MAVLink.MAV_MISSION_TYPE)cmb_missiontype.SelectedValue == MAVLink.MAV_MISSION_TYPE.FENCE)
                {
                    var overlay = new WPOverlay();
                    overlay.overlay.Id = "fence";

                    try
                    {
                        overlay.CreateOverlay(PointLatLngAlt.Zero,
                            commandlist, 0, 0);
                    }
                    catch (FormatException ex)
                    {
                        DevComponents.DotNetBar.MessageBoxEx.Show(Strings.InvalidNumberEntered, Strings.ERROR);
                    }

                    MainMap.HoldInvalidation = true;

                    var existing = MainMap.Overlays.Where(a => a.Id == overlay.overlay.Id).ToList();
                    foreach (var b in existing)
                    {
                        MainMap.Overlays.Remove(b);
                    }

                    MainMap.Overlays.Add(overlay.overlay);

                    overlay.overlay.ForceUpdate();

                    if (true)
                    {
                        foreach (var poly in overlay.overlay.Polygons)
                        {
                            var startwp = int.Parse(poly.Name);
                            var a = 1;
                            foreach (var pointLatLngAlt in poly.Points.CloseLoop().PrevNowNext())
                            {
                                var now = pointLatLngAlt.Item2;
                                var next = pointLatLngAlt.Item3;

                                if (now == null || next == null)
                                    continue;

                                var mid = new PointLatLngAlt((now.Lat + next.Lat) / 2, (now.Lng + next.Lng) / 2, 0);

                                var pnt = new GMapMarkerPlus(mid);
                                pnt.Tag = new midline() { now = now, next = next };
                                ((midline)pnt.Tag).now.Tag = (startwp + a).ToString();
                                ((midline)pnt.Tag).next.Tag = (startwp + a + 1).ToString();
                                overlay.overlay.Markers.Add(pnt);

                                a++;
                            }
                        }
                    }

                    MainMap.Refresh();
                }

                if ((MAVLink.MAV_MISSION_TYPE)cmb_missiontype.SelectedValue == MAVLink.MAV_MISSION_TYPE.RALLY)
                {
                    var overlay = new WPOverlay();
                    overlay.overlay.Id = "rally";

                    try
                    {
                        overlay.CreateOverlay(PointLatLngAlt.Zero,
                            commandlist, 0, 0);
                    }
                    catch (FormatException ex)
                    {
                        DevComponents.DotNetBar.MessageBoxEx.Show(Strings.InvalidNumberEntered, Strings.ERROR);
                    }

                    MainMap.HoldInvalidation = true;

                    var existing = MainMap.Overlays.Where(a => a.Id == overlay.overlay.Id).ToList();
                    foreach (var b in existing)
                    {
                        MainMap.Overlays.Remove(b);
                    }

                    MainMap.Overlays.Add(overlay.overlay);

                    overlay.overlay.ForceUpdate();

                    MainMap.Refresh();
                }
            }
            catch (FormatException ex)
            {
                DevComponents.DotNetBar.MessageBoxEx.Show(Strings.InvalidNumberEntered + "\n" + ex.Message, Strings.ERROR);
            }
            finally
            {
                if (isSendChange)
                    WPListChange?.Invoke(GetWPList());
            }
        }
        #endregion
        #endregion



        #region PolygonList
        public delegate void PlygonListChangeHandle(List<PointLatLngAlt> polygonList);
        public PlygonListChangeHandle PolygonListChange;
        #region SetPolygonList

        #region SetWPList 对外接口
        private delegate void SetPolygonListInThread(List<PointLatLngAlt> wpList);
        public void SetPolygonListHandle(List<PointLatLngAlt> polygonList)
        {
            if (this.InvokeRequired)
            {
                SetWPListInThread inThread = new SetWPListInThread(SetPolygonListHandle);
                this.Invoke(inThread, new object[] { polygonList });
            }
            else
            {
                SetPolygonList(polygonList);
            }
        }

        #endregion

        #region SetPolygonList 入口函数
        private void SetPolygonList(List<PointLatLngAlt> polygonList)
        {
            foreach (var mark in polygonList)
            {
                AddPolygonPoint(mark.Lat, mark.Lng);
            }

            isSendChange = true;
            redrawPolygonSurvey(drawnpolygon.Points.Select(p => new PointLatLngAlt(p)).ToList());
            isSendChange = false;
        }

        #endregion

        #region PolygonPoint
        #region AddPolygonPoint
        public void AddPolygonPoint(double lat, double lng)
        {
            List<PointLatLng> polygonPoints = new List<PointLatLng>();
            if (drawnpolygonsoverlay.Polygons.Count == 0)
            {
                drawnpolygon.Points.Clear();
                drawnpolygonsoverlay.Polygons.Add(drawnpolygon);
            }

            drawnpolygon.Fill = Brushes.Transparent;

            // remove full loop is exists
            if (drawnpolygon.Points.Count > 1 &&
                drawnpolygon.Points[0] == drawnpolygon.Points[drawnpolygon.Points.Count - 1])
                drawnpolygon.Points.RemoveAt(drawnpolygon.Points.Count - 1); // unmake a full loop

            drawnpolygon.Points.Add(new PointLatLng(lat, lng));

            redrawPolygonSurvey(drawnpolygon.Points.Select(p => new PointLatLngAlt(p)).ToList());
        }
        #endregion
        #endregion

        #endregion

        #region GetPolygonList
        public List<PointLatLngAlt> GetPolygonList()
        {
            List<PointLatLngAlt> polygonList = new List<PointLatLngAlt>();
            foreach (var mark in drawnpolygon.Points)
            {
                polygonList.Add(mark);
            }
            return polygonList;
        }
        #endregion

        #endregion

        #region SaveWP

        #region SaveWP 对外接口
        public void SaveWPFile()
        {
            SaveWPFile_Click(this, null);
        }
        #endregion

        #region SaveWP 右键菜单入口
        private void SaveWPFile_Click(object sender, EventArgs e)
        {
            savewaypoints();
            writeKML();
        }
        #endregion

        #region SaveWP 入口函数
        /// <summary>
        /// Saves a waypoint writer file
        /// </summary>
        private void savewaypoints()
        {
            using (SaveFileDialog fd = new SaveFileDialog())
            {
                //fd.Filter = "KML|*.kml|WorkCoordinates Mission|*.waypoints|Mission|*.waypoints;*.txt";
                fd.Filter = "KML|*.kml";
                fd.DefaultExt = ".kml";
                fd.InitialDirectory = Settings.Instance["WPFileDirectory"] ?? "";
                fd.FileName = wpfilename;
                DialogResult result = fd.ShowDialog();
                string file = fd.FileName;
                if (file != "" && result == DialogResult.OK)
                {
                    switch (fd.FilterIndex)
                    {
                        case 1:
                            saveWaypointsKML(file);
                            break;
                        case 2:
                            savelocalwaypoints(file);
                            break;
                        case 3:
                            savewaypoints(file);
                            break;
                    }
                }
            }
        }
        #endregion

        #region SaveWP From
        #region SaveWP KML
        private void saveWaypointsKML(string file)
        {
            // Create the file and writer.
            //StreamWriter sw = new StreamWriter(file, false, System.Text.Encoding.UTF8);
            //XmlWriterSettings settings = new XmlWriterSettings();
            //settings.Indent = true;
            //settings.IndentChars = "  ";
            //settings.NewLineOnAttributes = true;
            //XmlWriter xmlWriter = XmlWriter.Create(sw, settings);
            FileStream fs = new FileStream(file, FileMode.Create);
            XmlTextWriter w = new XmlTextWriter(fs, System.Text.Encoding.UTF8);
            w.IndentChar = System.Convert.ToChar(" ");
            w.Indentation = 4;
            w.Formatting = System.Xml.Formatting.Indented;

            // Start the document.
            w.WriteStartDocument();
            w.WriteStartElement("kml", "http://www.opengis.net/kml/2.2");
            w.WriteStartElement("Document", "");
            w.WriteStartElement("name");
            w.WriteString(file);
            w.WriteEndElement();//name
            w.WriteStartElement("open");
            w.WriteString("1");
            w.WriteEndElement();//open

            //style
            w.WriteStartElement("Style");
            w.WriteAttributeString("id", "waylineGreenPoly");
            w.WriteStartElement("LineStyle");
            w.WriteStartElement("color");
            w.WriteString("FF0AEE8B");
            w.WriteEndElement();//color
            w.WriteStartElement("width");
            w.WriteString("6");
            w.WriteEndElement();//width
            w.WriteEndElement();//LineStyle
            w.WriteEndElement();//Style

            w.WriteStartElement("Style");
            w.WriteAttributeString("id", "waypointStyle");
            w.WriteStartElement("IconStyle");
            w.WriteStartElement("Icon");
            w.WriteStartElement("href");
            w.WriteString("https://cdnen.dji-flighthub.com/static/app/images/point.png");
            w.WriteEndElement();//href
            w.WriteEndElement();//Icon
            w.WriteEndElement();//IconStyle
            w.WriteEndElement();//Style

            //MainData Points
            w.WriteStartElement("Folder");
            w.WriteStartElement("name");
            w.WriteString("Waypoints");
            w.WriteEndElement();//name
            w.WriteStartElement("description");
            w.WriteString("Waypoints in the Mission.");
            w.WriteEndElement();//description
            {
                #region HomePoint
                w.WriteStartElement("Placemark");

                // Write Point element
                w.WriteStartElement("name");
                w.WriteString(string.Format("Home"));
                w.WriteEndElement();//name
                w.WriteStartElement("visibility");
                w.WriteString("false");
                w.WriteEndElement();//visibility
                w.WriteStartElement("description");
                w.WriteString("HOME");
                w.WriteEndElement();//description
                w.WriteStartElement("styleUrl");
                w.WriteString("#waypointStyle");
                w.WriteEndElement();//styleUrl
                w.WriteStartElement("ExtendedData", "www.dji.com");

                var layerInfo = VPS.Layer.MemoryLayerCache.GetLayerFromMemoryCache(Settings.Instance["defaultTiffLayer"]);
                if (layerInfo != null)
                {
                    w.WriteStartElement("local");
                    w.WriteStartElement("coordinates");
                    CovertToWorkCoordinate(
                        homePosition.Lng,
                        homePosition.Lat,
                        homePosition.Alt,
                        out double x, out double y, out double z);
                    w.WriteString(string.Format("{0},{1},{2}", x, y, z));
                    w.WriteEndElement();//coordinates
                    w.WriteStartElement("scale");
                    w.WriteString(layerInfo.Scale.ToString());
                    w.WriteEndElement();//scale

                    w.WriteEndElement();//local
                }
                w.WriteStartElement("param1");
                w.WriteString("0");
                w.WriteEndElement();//param1
                w.WriteStartElement("param2");
                w.WriteString("0");
                w.WriteEndElement();//param2
                w.WriteStartElement("param3");
                w.WriteString("0");
                w.WriteEndElement();//param3
                w.WriteStartElement("param4");
                w.WriteString("0");
                w.WriteEndElement();//param4
                w.WriteEndElement();//ExtendedData

                w.WriteStartElement("Point");
                w.WriteStartElement("altitudeMode");
                int mode = System.Convert.ToInt32(CMB_altmode.SelectedValue);
                {
                    w.WriteAttributeString("id", "10");
                    w.WriteString("relativeToGround");
                }

                w.WriteEndElement();//altitudeMode
                w.WriteStartElement("coordinates");
                w.WriteString(string.Format("{0},{1},{2}",
                        homePosition.Lng,
                        homePosition.Lat,
                        homePosition.Alt));
                w.WriteEndElement();//coordinates
                w.WriteEndElement();//Point
                w.WriteEndElement();//Placemark
                #endregion
            }
            List<PointLatLngAlt> wpList = null;
            switch (CMB_altmode.SelectedIndex)
            {
                case 0:
                    wpList = GetWPList("Terrain");
                    break;
                case 1:
                    wpList = GetWPList("Absolute");
                    break;
                case 2:
                    wpList = GetWPList("Terrain");
                    break;
                default:
                    wpList = GetWPList("Terrain");
                    break;
            }
            #region WPPoints
            int index = 0;
            for (int i = 0; i < Commands.Rows.Count; i++)
            {
                bool isVisbility = false;
                if (wpList[i].Tag == "WAYPOINT")
                    isVisbility = true;
                w.WriteStartElement("Placemark");

                // Write Point element
                w.WriteStartElement("name");
                if (isVisbility)
                    w.WriteString(wpList[index].Tag + " " + (index).ToString());
                else
                    w.WriteString(Commands.Rows[i].Cells[Command.Index].Value.ToString());
                w.WriteEndElement();//name

                w.WriteStartElement("visibility");
                w.WriteString(isVisbility.ToString());
                //if (isVisbility)
                //    w.WriteString("true");
                //else
                //    w.WriteString("false");
                w.WriteEndElement();//visibility

                w.WriteStartElement("description");
                w.WriteString(Commands.Rows[i].Cells[Command.Index].Value.ToString());
                w.WriteEndElement();//description

                if (isVisbility)
                {
                    w.WriteStartElement("styleUrl");
                    w.WriteString("#waypointStyle");
                    w.WriteEndElement();//styleUrl
                }

                w.WriteStartElement("ExtendedData", "www.dji.com");
                w.WriteStartElement("param1");
                w.WriteString(Commands.Rows[i].Cells[Param1.Index].Value.ToString());
                w.WriteEndElement();//param1
                w.WriteStartElement("param2");
                w.WriteString(Commands.Rows[i].Cells[Param2.Index].Value.ToString());
                w.WriteEndElement();//param2
                w.WriteStartElement("param3");
                w.WriteString(Commands.Rows[i].Cells[Param3.Index].Value.ToString());
                w.WriteEndElement();//param3
                w.WriteStartElement("param4");
                w.WriteString(Commands.Rows[i].Cells[Param4.Index].Value.ToString());
                w.WriteEndElement();//param4
                w.WriteEndElement();//ExtendedData

                if (isVisbility)
                {
                    w.WriteStartElement("Point");
                    w.WriteStartElement("altitudeMode");
                    switch (wpList[i].Tag2)
                    {
                        case "Absolute":
                            w.WriteString("absolute");
                            break;
                        case "Terrain":
                            w.WriteString("relativeToGround");
                            break;
                        default:
                            w.WriteString("relativeToGround");
                            break;
                    }

                    w.WriteEndElement();//altitudeMode
                    w.WriteStartElement("coordinates");
                    w.WriteString(string.Format("{0},{1},{2}",
                        wpList[index].Lng,
                        wpList[index].Lat,
                        wpList[index].Alt));
                    w.WriteEndElement();//coordinates
                    w.WriteEndElement();//Point
                }
                w.WriteEndElement();//Placemark

                if (isVisbility)
                    index++;
            }
            w.WriteEndElement();//Folder
            #endregion
            {
                #region WPLines
                //MainData Lines
                w.WriteStartElement("Placemark");
                // Write Lines element
                w.WriteStartElement("name");
                w.WriteString(string.Format("Wayline"));
                w.WriteEndElement();//name
                w.WriteStartElement("visibility");
                w.WriteString("true");
                w.WriteEndElement();//visibility
                w.WriteStartElement("description");
                w.WriteString("Wayline");
                w.WriteEndElement();//description
                w.WriteStartElement("styleUrl");
                w.WriteString("#waylineGreenPoly");
                w.WriteEndElement();//styleUrl


                w.WriteStartElement("LineString");
                w.WriteStartElement("tessellate");
                w.WriteString("1");
                w.WriteEndElement();//tessellate
                w.WriteStartElement("altitudeMode");
                switch (CMB_altmode.SelectedIndex)
                {
                    case 0:
                        w.WriteString("relativeToGround");
                        break;
                    case 1:
                        w.WriteString("absolute");
                        break;
                    case 2:
                        w.WriteString("relativeToGround");
                        break;
                    default:
                        w.WriteString("relativeToGround");
                        break;
                }
                w.WriteEndElement();//altitudeMode
                w.WriteStartElement("coordinates");

                string pointList = "";

                foreach (var marker in wpList)
                {
                    if (marker != null)
                    {
                        pointList += string.Format("{0},{1},{2} ", marker.Lng, marker.Lat, marker.Alt);
                    }
                }

                w.WriteString(pointList);
                w.WriteEndElement();//coordinates
                w.WriteEndElement();//LineString

                w.WriteEndElement();//Placemark
                #endregion
            }
            w.WriteEndElement();//document
            w.WriteEndElement();//kml

            // Ends the document.
            w.WriteEndDocument();

            // close writer
            w.Close();
        }
        #endregion

        #region SaveWP LocalWayPoint
        /// <summary>
        /// Saves localwp as a waypoint writer file
        /// </summary>
        private void savelocalwaypoints(string file)
        {

            Settings.Instance["WPFileDirectory"] = Path.GetDirectoryName(file);
            try
            {
                StreamWriter sw = new StreamWriter(file);
                sw.WriteLine("QGC WPL 111");
                try
                {
                    CovertToWorkCoordinate(
                        homePosition.Lng,
                        homePosition.Lat,
                        homePosition.Alt, 
                        out double x, out double y, out double z);
                    sw.WriteLine("0\t1\t0\t16\t0\t0\t0\t0\t" +
                                 y.ToString("0.000000", new CultureInfo("en-US")) +
                                 "\t" +
                                 x.ToString("0.000000", new CultureInfo("en-US")) +
                                 "\t" +
                                 z.ToString("0.000000", new CultureInfo("en-US")) +
                                 "\t1");
                }
                catch (Exception ex)
                {
                    log.Error(ex);
                    sw.WriteLine("0\t1\t0\t0\t0\t0\t0\t0\t0\t0\t0\t1");
                }
                for (int a = 0; a < Commands.Rows.Count - 0; a++)
                {
                    ushort mode = 0;
                    if (double.Parse(Commands.Rows[a].Cells[Lon.Index].Value.ToString()) == 0 &&
                        double.Parse(Commands.Rows[a].Cells[Lat.Index].Value.ToString()) == 0)
                    {
                        continue;
                    }
                    if (Commands.Rows[a].Cells[0].Value.ToString() == "UNKNOWN")
                    {
                        mode = (ushort)Commands.Rows[a].Cells[Command.Index].Tag;
                    }
                    else
                    {
                        mode =
                            (ushort)
                            (MAVLink.MAV_CMD)
                            Enum.Parse(typeof(MAVLink.MAV_CMD), Commands.Rows[a].Cells[Command.Index].Value.ToString());
                    }

                    sw.Write((a + 1)); // seq
                    sw.Write("\t" + 0); // current
                    sw.Write("\t" + ((int)Commands.Rows[a].Cells[Frame.Index].Value).ToString()); //frame 
                    sw.Write("\t" + mode);

                    sw.Write("\t" +
                             double.Parse(Commands.Rows[a].Cells[Param1.Index].Value.ToString())
                                 .ToString("0.00000000", new CultureInfo("en-US")));
                    sw.Write("\t" +
                             double.Parse(Commands.Rows[a].Cells[Param2.Index].Value.ToString())
                                 .ToString("0.00000000", new CultureInfo("en-US")));
                    sw.Write("\t" +
                             double.Parse(Commands.Rows[a].Cells[Param3.Index].Value.ToString())
                                 .ToString("0.00000000", new CultureInfo("en-US")));
                    sw.Write("\t" +
                             double.Parse(Commands.Rows[a].Cells[Param4.Index].Value.ToString())
                                 .ToString("0.00000000", new CultureInfo("en-US")));

                    CovertToWorkCoordinate(
                        double.Parse(Commands.Rows[a].Cells[Lon.Index].Value.ToString()),
                        double.Parse(Commands.Rows[a].Cells[Lat.Index].Value.ToString()),
                        double.Parse(Commands.Rows[a].Cells[Alt.Index].Value.ToString()) / CurrentState.multiplieralt,
                        out double x, out double y, out double z);

                    sw.Write("\t" + y.ToString("0.00000000", new CultureInfo("en-US")));
                    sw.Write("\t" + x.ToString("0.00000000", new CultureInfo("en-US")));
                    sw.Write("\t" + z.ToString("0.000000", new CultureInfo("en-US")));
                    sw.Write("\t" + 1);
                    sw.WriteLine("");
                }
                sw.Close();

                //lbl_wpfile.Text = "Saved " + Path.GetFileName(file);
            }
            catch (Exception)
            {
                DevComponents.DotNetBar.MessageBoxEx.Show(Strings.ERROR);
            }

        }
        #endregion

        #region SaveWP LocalWayPoint
        void savewaypoints(string file)
        {
            Settings.Instance["WPFileDirectory"] = Path.GetDirectoryName(file);
            try
            {
                StreamWriter sw = new StreamWriter(file);
                sw.WriteLine("QGC WPL 110");
                try
                {
                    sw.WriteLine("0\t1\t0\t16\t0\t0\t0\t0\t" +
                                 homePosition.Lat.ToString("0.000000", new CultureInfo("en-US")) +
                                 "\t" +
                                 homePosition.Lng.ToString("0.000000", new CultureInfo("en-US")) +
                                 "\t" +
                                 homePosition.Alt.ToString("0.000000", new CultureInfo("en-US")) +
                                 "\t1");
                }
                catch (Exception ex)
                {
                    log.Error(ex);
                    sw.WriteLine("0\t1\t0\t0\t0\t0\t0\t0\t0\t0\t0\t1");
                }
                for (int a = 0; a < Commands.Rows.Count - 0; a++)
                {
                    ushort mode = 0;

                    if (Commands.Rows[a].Cells[0].Value.ToString() == "UNKNOWN")
                    {
                        mode = (ushort)Commands.Rows[a].Cells[Command.Index].Tag;
                    }
                    else
                    {
                        mode =
                            (ushort)
                            (MAVLink.MAV_CMD)
                            Enum.Parse(typeof(MAVLink.MAV_CMD), Commands.Rows[a].Cells[Command.Index].Value.ToString());
                    }

                    sw.Write((a + 1)); // seq
                    sw.Write("\t" + 0); // current
                    sw.Write("\t" + ((int)Commands.Rows[a].Cells[Frame.Index].Value).ToString()); //frame 
                    sw.Write("\t" + mode);
                    sw.Write("\t" +
                             double.Parse(Commands.Rows[a].Cells[Param1.Index].Value.ToString())
                                 .ToString("0.00000000", new CultureInfo("en-US")));
                    sw.Write("\t" +
                             double.Parse(Commands.Rows[a].Cells[Param2.Index].Value.ToString())
                                 .ToString("0.00000000", new CultureInfo("en-US")));
                    sw.Write("\t" +
                             double.Parse(Commands.Rows[a].Cells[Param3.Index].Value.ToString())
                                 .ToString("0.00000000", new CultureInfo("en-US")));
                    sw.Write("\t" +
                             double.Parse(Commands.Rows[a].Cells[Param4.Index].Value.ToString())
                                 .ToString("0.00000000", new CultureInfo("en-US")));
                    sw.Write("\t" +
                             double.Parse(Commands.Rows[a].Cells[Lat.Index].Value.ToString())
                                 .ToString("0.00000000", new CultureInfo("en-US")));
                    sw.Write("\t" +
                             double.Parse(Commands.Rows[a].Cells[Lon.Index].Value.ToString())
                                 .ToString("0.00000000", new CultureInfo("en-US")));
                    sw.Write("\t" +
                             (double.Parse(Commands.Rows[a].Cells[Alt.Index].Value.ToString()) /
                              CurrentState.multiplieralt).ToString("0.000000", new CultureInfo("en-US")));
                    sw.Write("\t" + 1);
                    sw.WriteLine("");
                }
                sw.Close();

                //lbl_wpfile.Text = "Saved " + Path.GetFileName(file);
            }
            catch (Exception)
            {
                DevComponents.DotNetBar.MessageBoxEx.Show(Strings.ERROR);
            }
        }
        #endregion
        #endregion

        #endregion

        #region LoadWP
        #region LoadWP 对外接口
        public void LoadWPFile()
        {
            LoadWPFile_Click(this, null);
        }
        #endregion

        #region LoadWP 右键菜单入口
        private void LoadWPFile_Click(object sender, EventArgs e)
        {
            loadwaypoints();
            writeKML();
        }

        public void LoadKMLFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog fd = new OpenFileDialog())
            {
                fd.Filter = "Google Earth KML(*kml;*.kmz) |*.kml;*.kmz";
                DialogResult result = fd.ShowDialog();
                string file = fd.FileName;
                try
                {
                    LoadKMLFile(file);
                    ZoomToCenterWP();
                }
                catch
                {
                    DevComponents.DotNetBar.MessageBoxEx.Show("文件打开时出错", Strings.ERROR);
                    return;
                }
            }
        }

        public void LoadSHPFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog fd = new OpenFileDialog())
            {
                fd.Filter = "Shape file(*.shp)|*.shp";
                DialogResult result = fd.ShowDialog();
                string file = fd.FileName;

                try
                {
                    LoadSHPFile(file);
                    ZoomToCenterWP();
                }
                catch
                {
                    DevComponents.DotNetBar.MessageBoxEx.Show("文件打开时出错", Strings.ERROR);
                    return;
                }
            }
        }

        public void LoadWPFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            loadwaypoints();
            writeKML();
        }
        #endregion

        #region LoadWp 入口函数
        /// <summary>
        /// Saves a waypoint writer file
        /// </summary>
        private void loadwaypoints()
        {
            using (OpenFileDialog fd = new OpenFileDialog())
            {
                fd.Filter = "Default WPFile(*kml)|*.kml|Google Earth KML(*kml;*.kmz) |*.kml;*.kmz|ShapeFile(*.shp)|*.shp|MissionPlanner(*.waypoints)|*.waypoints;*.txt";
                if (Directory.Exists(Settings.Instance["WPFileDirectory"] ?? ""))
                    fd.InitialDirectory = Settings.Instance["WPFileDirectory"];
                DialogResult result = fd.ShowDialog();
                string file = fd.FileName;

                if (File.Exists(file))
                {
                    Settings.Instance["WPFileDirectory"] = Path.GetDirectoryName(file);
                    switch (fd.FilterIndex)
                    {
                        case 1:
                            try
                            {
                                loadWaypointsKML(file, false);
                                ZoomToCenterWP();
                            }
                            catch
                            {
                                DevComponents.DotNetBar.MessageBoxEx.Show("Error opening File", Strings.ERROR);
                                return;
                            }
                            break;
                        case 2:
                            try
                            {
                                LoadKMLFile(file);
                                ZoomToCenterWP();
                            }
                            catch
                            {
                                DevComponents.DotNetBar.MessageBoxEx.Show("Error opening File", Strings.ERROR);
                                return;
                            }
                            break;
                        case 3:
                            try
                            {
                                LoadSHPFile(file);
                                ZoomToCenterWP();
                            }
                            catch
                            {
                                DevComponents.DotNetBar.MessageBoxEx.Show("Error opening File", Strings.ERROR);
                                return;
                            }
                            break;
                        case 4:
                            try
                            {
                                string line = "";
                                using (var fstream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                                using (var fs = new StreamReader(fstream))
                                {
                                    line = fs.ReadLine();
                                }

                                if (line.StartsWith("{"))
                                {
                                    var format = MissionFile.ReadFile(file);

                                    var cmds = MissionFile.ConvertToLocationwps(format);

                                    WPtoScreen(cmds);

                                    isSendChange = true;
                                    writeKML();
                                    isSendChange = false;
                                    MainMap.ZoomAndCenterMarkers("WPOverlay");
                                }
                                else
                                {
                                    wpfilename = file;
                                    if (line.StartsWith("QGC WPL 111"))
                                        readQGC111wpfile(file);
                                    else if (line.StartsWith("QGC WPL 110"))
                                        readQGC110wpfile(file);
                                }
                                ZoomToCenterWP();
                            }
                            catch
                            {
                                DevComponents.DotNetBar.MessageBoxEx.Show("Error opening File", Strings.ERROR);
                                return;
                            }
                            break;
                    }
                    //lbl_wpfile.Text = "Loaded " + Path.GetFileName(file);
                }
            }
        }

        #endregion

        #region LoadWP From

        #region LoadWP DefaultWPFile KML
        private void loadWaypointsKML(string file, bool append)
        {
            // Create the file and writer.
            if (!System.IO.File.Exists(file))
                return;
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(file);
                XmlNamespaceManager nsMgr = new XmlNamespaceManager(xmlDoc.NameTable);
                foreach (XmlNode kml in xmlDoc.ChildNodes)
                {
                    if (kml.Name.Equals("kml"))
                    {
                        nsMgr.AddNamespace("ns", kml.NamespaceURI);

                        foreach (XmlNode doc in kml.ChildNodes)
                        {
                            if (doc.Name.Equals("Document"))
                            {
                                nsMgr.AddNamespace("ns1", doc.NamespaceURI);

                                XmlNode folder = doc.SelectSingleNode(@"./ns1:Folder", nsMgr);

                                List<Locationwp> cmds = new List<Locationwp>();
                                foreach (XmlNode point in folder.ChildNodes)
                                {
                                    if (point.Name == "Placemark")
                                    {
                                        string cmd = "WAYPOINT";
                                        int altitudeMode = 3;
                                        double x = 0, y = 0, z = 0;
                                        double p1 = 0, p2 = 0, p3 = 0, p4 = 0;
                                        //selectedrow = Commands.Rows.Add();
                                        foreach (XmlNode info in point.ChildNodes)
                                        {
                                            switch (info.Name)
                                            {
                                                case "description":
                                                    //cmd = System.Convert.ToInt32(info.SelectSingleNode(@".//@id").Value);
                                                    cmd = info.InnerText;
                                                    break;
                                                case "Point":
                                                    nsMgr.AddNamespace("ns2", info.NamespaceURI);
                                                    var smode = info.SelectSingleNode(@"./ns2:altitudeMode", nsMgr);
                                                    if (smode != null)
                                                    {
                                                        if (smode.InnerText == "relativeToGround")
                                                            altitudeMode = 3;
                                                        else if (smode.InnerText == "absolute")
                                                            altitudeMode = 0;
                                                        else if (smode.InnerText == "clampedToGround")
                                                            altitudeMode = 10;
                                                        else
                                                            altitudeMode = 3;
                                                    }
                                                    else
                                                    {
                                                        altitudeMode = 3;
                                                    }
                                                    var sWGS84 = info.SelectSingleNode(@"./ns2:coordinates", nsMgr);
                                                    if (sWGS84 != null)
                                                    {
                                                        string WGS84 = sWGS84.InnerText;
                                                        var splitWGS84 = WGS84.Split(',');
                                                        x = System.Convert.ToDouble(splitWGS84[0]);
                                                        y = System.Convert.ToDouble(splitWGS84[1]);
                                                        z = System.Convert.ToDouble(splitWGS84[2]);
                                                    }
                                                    break;

                                                case "ExtendedData":
                                                    nsMgr.AddNamespace("ns3", info.NamespaceURI);
                                                    var sp1 = info.SelectSingleNode(@".//ns3:param1", nsMgr);
                                                    if (sp1 != null)
                                                        p1 = System.Convert.ToDouble(sp1.InnerText);
                                                    else
                                                        p1 = 0;
                                                    var sp2 = info.SelectSingleNode(@".//ns3:param2", nsMgr);
                                                    if (sp2 != null)
                                                        p2 = System.Convert.ToDouble(sp2.InnerText);
                                                    else
                                                        p2 = 0;
                                                    var sp3 = info.SelectSingleNode(@".//ns3:param3", nsMgr);
                                                    if (sp3 != null)
                                                        p3 = System.Convert.ToDouble(sp3.InnerText);
                                                    else
                                                        p3 = 0;
                                                    var sp4 = info.SelectSingleNode(@".//ns3:param4", nsMgr);
                                                    if (sp4 != null)
                                                        p4 = System.Convert.ToDouble(sp4.InnerText);
                                                    else
                                                        p4 = 0;
                                                    var sMode = info.SelectSingleNode(@"./ns3:mode", nsMgr);
                                                    if (sMode != null)
                                                    {
                                                        if (int.TryParse(sMode.InnerText, out int mode))
                                                            altitudeMode = mode;
                                                    }
                                                    var sCoord = info.SelectSingleNode(@"./ns3:coord", nsMgr);
                                                    if (sCoord != null)
                                                    {
                                                        string coord = sCoord.InnerText;
                                                        var splitCoord = coord.Split(',');
                                                        x = System.Convert.ToDouble(splitCoord[0]);
                                                        y = System.Convert.ToDouble(splitCoord[1]);
                                                        z = System.Convert.ToDouble(splitCoord[2]);
                                                    }
                                                    break;


                                            }
                                        }


                                        Locationwp temp = new Locationwp();
                                        temp.frame = (byte)int.Parse(altitudeMode.ToString(), CultureInfo.InvariantCulture);
                                        if (cmd != "WAYPOINT")
                                            continue;
                                        if (Enum.TryParse(cmd, false, out MAVLink.MAV_CMD data))
                                        {
                                            temp.id = (ushort)data;
                                        }
                                        else
                                        {
                                            temp.id = (ushort)MAVLink.MAV_CMD.WAYPOINT;
                                        }

                                        if (temp.id == 99)
                                            temp.id = 0;

                                        temp.alt = (float)(double.Parse(z.ToString(), CultureInfo.InvariantCulture));
                                        temp.lat = (double.Parse(y.ToString(), CultureInfo.InvariantCulture));
                                        temp.lng = (double.Parse(x.ToString(), CultureInfo.InvariantCulture));

                                        temp.p1 = float.Parse(p1.ToString(), CultureInfo.InvariantCulture);
                                        temp.p2 = float.Parse(p2.ToString(), CultureInfo.InvariantCulture);
                                        temp.p3 = float.Parse(p3.ToString(), CultureInfo.InvariantCulture);
                                        temp.p4 = float.Parse(p4.ToString(), CultureInfo.InvariantCulture);

                                        cmds.Add(temp);
                                    }
                                }
                                WPtoScreen(cmds, false, append);
                                isSendChange = true;
                                writeKML();
                                isSendChange = false;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }

        }
        #endregion

        #region LoadWp MissionPlanner
        public void readQGC110wpfile(string file, bool append = false)
        {


            try
            {
                var cmds = WaypointFile.ReadWaypointFile(file);

                WPtoScreen(cmds, true, append);

                isSendChange = true;
                writeKML();
                isSendChange = false;
                MainMap.ZoomAndCenterMarkers("WPOverlay");
            }
            catch (Exception ex)
            {
                DevComponents.DotNetBar.MessageBoxEx.Show("Can't open file! " + ex);
            }
        }

        public void readQGC111wpfile(string file, bool append = false)
        {


            try
            {
                var cmds = WaypointFile.ReadWaypointFile(file);

                for (int i = 0; i < cmds.Count; i++)
                {
                    Locationwp temp = new Locationwp();
                    temp.frame = cmds[i].frame;

                    temp.id = cmds[i].id;
                    temp.p1 = cmds[i].p1;
                    temp.p2 = cmds[i].p2;
                    temp.p3 = cmds[i].p3;
                    temp.p4 = cmds[i].p4;

                    if (temp.id == 99)
                        temp.id = 0;

                    CovertFromWorkCoordinate(cmds[i].lng, cmds[i].lat, cmds[i].alt, out double lng, out double lat, out double alt);

                    temp.alt = (float)alt;
                    temp.lat = lat;
                    temp.lng = lng;

                    cmds.RemoveAt(i);
                    cmds.Insert(i, temp);
                }
                WPtoScreen(cmds, true, append);

                isSendChange = true;
                writeKML();
                isSendChange = false;

                MainMap.ZoomAndCenterMarkers("WPOverlay");
            }
            catch (Exception ex)
            {
                DevComponents.DotNetBar.MessageBoxEx.Show("Can't open file! " + ex);
            }
        }
        #endregion

        #region LoadWP SHP
        private void LoadSHPFile(string file)
        {
            ProjectionInfo pStart = new ProjectionInfo();
            ProjectionInfo pESRIEnd = KnownCoordinateSystems.Geographic.World.WGS1984;
            bool reproject = false;

            if (File.Exists(file))
            {
                string prjfile = Path.GetDirectoryName(file) + Path.DirectorySeparatorChar +
                                 Path.GetFileNameWithoutExtension(file) + ".prj";
                if (File.Exists(prjfile))
                {
                    using (
                        StreamReader re =
                            File.OpenText(Path.GetDirectoryName(file) + Path.DirectorySeparatorChar +
                                          Path.GetFileNameWithoutExtension(file) + ".prj"))
                    {
                        pStart.ParseEsriString(re.ReadLine());

                        reproject = true;
                    }
                }

                IFeatureSet fs = FeatureSet.Open(file);

                fs.FillAttributes();

                int rows = fs.NumRows();

                DataTable dtOriginal = fs.DataTable;
                for (int row = 0; row < dtOriginal.Rows.Count; row++)
                {
                    object[] original = dtOriginal.Rows[row].ItemArray;
                }

                foreach (DataColumn col in dtOriginal.Columns)
                {
                    Console.WriteLine(col.ColumnName + " " + col.DataType);
                }

                quickadd = true;

                bool dosort = false;

                List<PointLatLngAlt> wplist  = new List<PointLatLngAlt>();

                for (int row = 0; row < dtOriginal.Rows.Count; row++)
                {
                    double x = fs.Vertex[row * 2];
                    double y = fs.Vertex[row * 2 + 1];

                    double z = -1;
                    float wp = 0;

                    try
                    {
                        if (dtOriginal.Columns.Contains("ELEVATION"))
                            z = (float)Convert.ChangeType(dtOriginal.Rows[row]["ELEVATION"], TypeCode.Single);
                    }
                    catch (Exception ex)
                    {
                        log.Error(ex);
                    }

                    try
                    {
                        if (z == -1 && dtOriginal.Columns.Contains("alt"))
                            z = (float)Convert.ChangeType(dtOriginal.Rows[row]["alt"], TypeCode.Single);
                    }
                    catch (Exception ex)
                    {
                        log.Error(ex);
                    }

                    try
                    {
                        if (z == -1)
                            z = fs.Z[row];
                    }
                    catch (Exception ex)
                    {
                        log.Error(ex);
                    }


                    try
                    {
                        if (dtOriginal.Columns.Contains("wp"))
                        {
                            wp = (float)Convert.ChangeType(dtOriginal.Rows[row]["wp"], TypeCode.Single);
                            dosort = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Error(ex);
                    }

                    if (reproject)
                    {
                        double[] xyarray = { x, y };
                        double[] zarray = { z };

                        Reproject.ReprojectPoints(xyarray, zarray, pStart, pESRIEnd, 0, 1);


                        x = xyarray[0];
                        y = xyarray[1];
                        z = zarray[0];
                    }

                    PointLatLngAlt pnt = new PointLatLngAlt(x, y, z, wp.ToString());

                    wplist.Add(pnt);
                }

                if (dosort)
                    wplist.Sort();

                foreach (var item in wplist)
                {
                    AddCommand(MAVLink.MAV_CMD.WAYPOINT, 0, 0, 0, 0, item.Lat, item.Lng, item.Alt);
                }

                quickadd = false;

                isSendChange = true;
                writeKML();
                isSendChange = false;

                MainMap.ZoomAndCenterMarkers("WPOverlay");
            }
        }
        #endregion

        #region LoadWP Google Earth KML
        private void LoadKMLFile(string file)
        {
            if (file != "")
            {
                try
                {
                    string kml = "";
                    string tempdir = "";
                    if (file.ToLower().EndsWith("kmz"))
                    {
                        ZipFile input = new ZipFile(file);

                        tempdir = Path.GetTempPath() + Path.DirectorySeparatorChar + Path.GetRandomFileName();
                        input.ExtractAll(tempdir, ExtractExistingFileAction.OverwriteSilently);

                        string[] kmls = Directory.GetFiles(tempdir, "*.kml");

                        if (kmls.Length > 0)
                        {
                            file = kmls[0];

                            input.Dispose();
                        }
                        else
                        {
                            input.Dispose();
                            return;
                        }
                    }

                    var sr = new StreamReader(File.OpenRead(file));
                    kml = sr.ReadToEnd();
                    sr.Close();

                    // cleanup after out
                    if (tempdir != "")
                        Directory.Delete(tempdir, true);

                    kml = kml.Replace("<Snippet/>", "");

                    var parser = new Parser();

                    parser.ElementAdded += processKMLMission;
                    parser.ParseString(kml, false);
                }
                catch (Exception ex)
                {
                    DevComponents.DotNetBar.MessageBoxEx.Show(Strings.Bad_KML_File + ex);
                }
            }
        }

        #endregion

        #endregion
        #endregion

        #region 全局参数
        public delegate void PositionChangeHandle(PointLatLngAlt position);
        public delegate void StringChangeHandle(string str);
        public PositionChangeHandle CurrentChange;

        #region NoHandle
        private bool onlyChangeValue = false;

        #region 接口函数
        public void BeginQuickChange()
        {
            onlyChangeValue = true;
        }


        public void EndQuickChange()
        {
            onlyChangeValue = false;
        }
        #endregion

        #endregion


        #region Home
        private PointLatLngAlt homePosition = new PointLatLngAlt();

        public PositionChangeHandle HomeChange;

        #region SetHome

        #region SetHome 接口函数
        private delegate void SetHomeInThread(PointLatLngAlt home);
        public void SetHomeHandle(PointLatLngAlt home)
        {
            if (this.InvokeRequired)
            {
                SetHomeInThread inThread = new SetHomeInThread(SetHomeHandle);
                this.Invoke(inThread, new object[] { home });
            }
            else
            {
                BeginQuickChange();
                SetHomeHere(home);
                EndQuickChange();
            }
        }
        #endregion

        #region SetHome 入口函数
        private void SetHomeHere(PointLatLngAlt position)
        {
            homePosition = position;
            writeKML();
            if (onlyChangeValue)
                return;
            HomeChange?.Invoke(position);
        }
        #endregion
        #endregion
        #endregion

        #region CoordSystem
        private enum CoordsSystems
        {
            WGS84,
            UTM,
            MGRS
        }
        private CoordsSystems coordSystem = CoordsSystems.WGS84;

        public StringChangeHandle coordChange;

        #region SetCoordSystem

        #region 接口函数
        private delegate void SetCoordSystemInThread(string coord);
        public void SetCoordSystemHandle(string coord)
        {
            if (this.InvokeRequired)
            {
                SetCoordSystemInThread inThread = new SetCoordSystemInThread(SetCoordSystemHandle);
                this.Invoke(inThread);
            }
            else
            {
                BeginQuickChange();
                SetCoordSystem(coord);
                EndQuickChange();
            }
        }
        #endregion

        #region 入口函数
        private void SetCoordSystem(string coord)
        {
            switch (coord.ToString())
            {
                case "WGS84":
                    coordSystem = CoordsSystems.WGS84;
                    break;
                case "UTM":
                    coordSystem = CoordsSystems.UTM;
                    break;
                case "MGRS":
                    coordSystem = CoordsSystems.MGRS;
                    break;
                default:
                    coordSystem = CoordsSystems.WGS84;
                    break;
            }
            if (onlyChangeValue)
                return;
            coordChange?.Invoke(coordSystem.ToString());
        }
        #endregion
        #endregion
        #endregion

        #endregion

        private List<Locationwp> GetCommandList()
        {
            List<Locationwp> commands = new List<Locationwp>();

            for (int a = 0; a < Commands.Rows.Count - 0; a++)
            {
                var temp = DataViewtoLocationwp(a);

                commands.Add(temp);
            }

            return commands;
        }

        #region WPList
        public bool isSendChange = false;
        public delegate void WPListChangeHandle(List<PointLatLngAlt> wpList);
        public WPListChangeHandle WPListChange;

        #region SetWPList

        #region SetWPList 对外接口
        private delegate void SetWPListInThread(List<PointLatLngAlt> wpList);
        public void SetWPListHandle(List<PointLatLngAlt> wpList)
        {
            if (this.InvokeRequired)
            {
                SetWPListInThread inThread = new SetWPListInThread(SetWPListHandle);
                this.Invoke(inThread, new object[] { wpList });
            }
            else
            {
                SetWPList(wpList);
            }
        }
        #endregion

        #region SetWPList 入口函数
        private void SetWPList(List<PointLatLngAlt> wpList)
        {
            int counter = 0;
            int have = Commands.Rows.Count;
            foreach (var wp in wpList)
            {
                if (counter < have)
                    SetWPPoint(wp.Lat, wp.Lng, (int)wp.Alt, counter);
                else
                    AddWPPoint(wp.Lat, wp.Lng, (int)wp.Alt);
                counter++;
            }
            while (counter < have)
            {
                DeleteWPPoint(counter);
                have--;
            }

            isSendChange = true;
            writeKML();
            isSendChange = false;
        }
        #endregion

        #region WPPoint
        #region AddWPPoint
        public void AddWPPoint(double lat, double lng, int alt)
        {
            selectedrow = Commands.Rows.Add();

            if ((MAVLink.MAV_MISSION_TYPE)cmb_missiontype.SelectedValue == MAVLink.MAV_MISSION_TYPE.RALLY)
            {
                Commands.Rows[selectedrow].Cells[Command.Index].Value = MAVLink.MAV_CMD.RALLY_POINT.ToString();
                ChangeColumnHeader(MAVLink.MAV_CMD.RALLY_POINT.ToString());
            }
            else if ((MAVLink.MAV_MISSION_TYPE)cmb_missiontype.SelectedValue == MAVLink.MAV_MISSION_TYPE.FENCE)
            {
                Commands.Rows[selectedrow].Cells[Command.Index].Value = MAVLink.MAV_CMD.FENCE_CIRCLE_EXCLUSION.ToString();
                Commands.Rows[selectedrow].Cells[Param1.Index].Value = 5;
                ChangeColumnHeader(MAVLink.MAV_CMD.FENCE_CIRCLE_EXCLUSION.ToString());
            }
            else if (splinemode)
            {
                Commands.Rows[selectedrow].Cells[Command.Index].Value = MAVLink.MAV_CMD.SPLINE_WAYPOINT.ToString();
                ChangeColumnHeader(MAVLink.MAV_CMD.SPLINE_WAYPOINT.ToString());
            }
            else
            {
                Commands.Rows[selectedrow].Cells[Command.Index].Value = MAVLink.MAV_CMD.WAYPOINT.ToString();
                ChangeColumnHeader(MAVLink.MAV_CMD.WAYPOINT.ToString());
            }

            setfromMap(lat, lng, alt);
        }
        #endregion
        #region SetWPPoint
        public void SetWPPoint(double lat, double lng, int alt, int index)
        {
            selectedrow = index;

            if ((MAVLink.MAV_MISSION_TYPE)cmb_missiontype.SelectedValue == MAVLink.MAV_MISSION_TYPE.RALLY)
            {
                Commands.Rows[selectedrow].Cells[Command.Index].Value = MAVLink.MAV_CMD.RALLY_POINT.ToString();
                ChangeColumnHeader(MAVLink.MAV_CMD.RALLY_POINT.ToString());
            }
            else if ((MAVLink.MAV_MISSION_TYPE)cmb_missiontype.SelectedValue == MAVLink.MAV_MISSION_TYPE.FENCE)
            {
                Commands.Rows[selectedrow].Cells[Command.Index].Value = MAVLink.MAV_CMD.FENCE_CIRCLE_EXCLUSION.ToString();
                Commands.Rows[selectedrow].Cells[Param1.Index].Value = 5;
                ChangeColumnHeader(MAVLink.MAV_CMD.FENCE_CIRCLE_EXCLUSION.ToString());
            }
            else if (splinemode)
            {
                Commands.Rows[selectedrow].Cells[Command.Index].Value = MAVLink.MAV_CMD.SPLINE_WAYPOINT.ToString();
                ChangeColumnHeader(MAVLink.MAV_CMD.SPLINE_WAYPOINT.ToString());
            }
            else
            {
                Commands.Rows[selectedrow].Cells[Command.Index].Value = MAVLink.MAV_CMD.WAYPOINT.ToString();
                ChangeColumnHeader(MAVLink.MAV_CMD.WAYPOINT.ToString());
            }

            setfromMap(lat, lng, alt);
        }
        #endregion
        #region DeletePoint
        public void DeleteWPPoint(int index)
        {
            quickadd = true;

            if (selectedrow == index)
            {
                selectedrow = 0;
                // mono fix
                try
                {
                    Commands.CurrentCell = null;
                }
                catch { }
            }
            Commands.Rows.RemoveAt(index);

            quickadd = false;

            writeKML();
        }
        #endregion
        #region SetFromMap
        /// <summary>
        /// Actualy Sets the values into the datagrid and verifys height if turned on
        /// </summary>
        /// <param name="lat"></param>
        /// <param name="lng"></param>
        /// <param name="alt"></param>
        public void setfromMap(double lat, double lng, int alt, double p1 = -1)
        {
            if (selectedrow > Commands.RowCount)
            {
                DevComponents.DotNetBar.MessageBoxEx.Show("无效操作");
                return;
            }

            try
            {
                if (!quickadd)
                {
                    // get current command list
                    var currentlist = GetCommandList();
                    // remove the current blank row that has not been populated yet
                    currentlist.RemoveAt(selectedrow);
                    // add history
                    history.Add(currentlist);
                    historyChange?.Invoke(history.Count);
                }
            }
            catch (Exception ex)
            {
                DevComponents.DotNetBar.MessageBoxEx.Show("检测到无效条目\n" + ex.Message, Strings.ERROR);
            }

            // remove more than 40 revisions
            if (history.Count > 40)
            {
                history.RemoveRange(0, history.Count - 40);
            }

            DataGridViewTextBoxCell cell;
            if (alt == -2 && Commands.Columns[Alt.Index].HeaderText.Equals("Alt"))
            {
                if (CHK_verifyheight.Checked && (altmode)CMB_altmode.SelectedValue != altmode.Terrain) //Drag with verifyheight // use srtm data
                {
                    cell = Commands.Rows[selectedrow].Cells[Alt.Index] as DataGridViewTextBoxCell;
                    float ans;
                    if (float.TryParse(cell.Value.ToString(), out ans))
                    {
                        ans = (int)ans;

                        DataGridViewTextBoxCell celllat = Commands.Rows[selectedrow].Cells[Lat.Index] as DataGridViewTextBoxCell;
                        DataGridViewTextBoxCell celllon = Commands.Rows[selectedrow].Cells[Lon.Index] as DataGridViewTextBoxCell;
                        int oldsrtm =
                            (int)
                            ((srtm.getAltitude(double.Parse(celllat.Value.ToString()),
                                 double.Parse(celllon.Value.ToString())).alt) * CurrentState.multiplieralt);
                        int newsrtm = (int)((srtm.getAltitude(lat, lng).alt) * CurrentState.multiplieralt);
                        int newh = (int)(ans + newsrtm - oldsrtm);

                        cell.Value = newh;

                        cell.DataGridView.EndEdit();
                    }
                }
            }
            if (Commands.Columns[Lat.Index].HeaderText.Equals("Lat"))
            {
                cell = Commands.Rows[selectedrow].Cells[Lat.Index] as DataGridViewTextBoxCell;
                cell.Value = lat.ToString("0.0000000");
                cell.DataGridView.EndEdit();
            }
            if (Commands.Columns[Lon.Index].HeaderText.Equals("Long"))
            {
                cell = Commands.Rows[selectedrow].Cells[Lon.Index] as DataGridViewTextBoxCell;
                cell.Value = lng.ToString("0.0000000");
                cell.DataGridView.EndEdit();
            }
            if (alt != -1 && alt != -2 && Commands.Columns[Alt.Index].HeaderText.Equals("Alt"))
            {
                cell = Commands.Rows[selectedrow].Cells[Alt.Index] as DataGridViewTextBoxCell;

                {
                    //    double result;
                    //    bool pass = double.TryParse(TXT_homealt.Text, out result);

                    //    if (pass == false)
                    //    {
                    //        DevComponents.DotNetBar.MessageBoxEx.Show("Home点信息必须包含高度");
                    //        string homealt = "100";
                    //        if (DialogResult.Cancel == InputBox.Show("Home高度信息", "Home高度", ref homealt))
                    //            return;
                    //        if (double.TryParse(homealt, out double homeAlt))
                    //        {
                    //            SetHomeHere(new PointLatLngAlt(
                    //                double.Parse(TXT_homelat.Text),
                    //                double.Parse(TXT_homelng.Text),
                    //                homeAlt));
                    //        }
                    //        else
                    //        {
                    //            SetHomeHere(new PointLatLngAlt(
                    //                double.Parse(TXT_homelat.Text),
                    //                double.Parse(TXT_homelng.Text),
                    //                double.Parse(TXT_DefaultAlt.Text)));
                    //        }
                    //    }
                    //    int results1;
                    //    if (!int.TryParse(TXT_DefaultAlt.Text, out results1))
                    //    {
                    //        DevComponents.DotNetBar.MessageBoxEx.Show("默认高度无效");
                    //        return;
                    //    }

                    //    if (results1 == 0)
                    //    {
                    //        string defalt = "100";
                    //        if (DialogResult.Cancel == InputBox.Show("默认高度", "默认高度", ref defalt))
                    //            return;
                    //        TXT_DefaultAlt.Text = defalt;
                    //    }
                }

                cell.Value = TXT_DefaultAlt.Text;

                float ans;
                if (float.TryParse(cell.Value.ToString(), out ans))
                {
                    ans = (int)ans;
                    if (alt != 0) // use passed in value;
                        cell.Value = alt.ToString();
                    if (ans == 0) // default
                        cell.Value = 50;
                    if (ans == 0 && (MainV2.comPort.MAV.cs.firmware == Firmwares.ArduCopter2))
                        cell.Value = 15;

                    // not online and verify alt via srtm
                    if (CHK_verifyheight.Checked) // use srtm data
                    {
                        // is absolute but no verify
                        if ((altmode)CMB_altmode.SelectedValue == altmode.Absolute)
                        {
                            //abs
                            cell.Value =
                                ((srtm.getAltitude(lat, lng).alt) * CurrentState.multiplieralt +
                                 int.Parse(TXT_DefaultAlt.Text)).ToString();
                        }
                        else if ((altmode)CMB_altmode.SelectedValue == altmode.Terrain)
                        {
                            cell.Value = int.Parse(TXT_DefaultAlt.Text);
                        }
                        else
                        {
                            //relative and verify
                            cell.Value =
                                ((int)(srtm.getAltitude(lat, lng).alt) * CurrentState.multiplieralt +
                                 int.Parse(TXT_DefaultAlt.Text) -
                                 (int)
                                 srtm.getAltitude(MainV2.comPort.MAV.cs.PlannedHomeLocation.Lat,
                                     MainV2.comPort.MAV.cs.PlannedHomeLocation.Lng).alt * CurrentState.multiplieralt)
                                .ToString();
                        }
                    }

                    cell.DataGridView.EndEdit();
                }
                else
                {
                    DevComponents.DotNetBar.MessageBoxEx.Show("高度无效");
                    cell.Style.BackColor = Color.Red;
                }
            }

            // convert to utm
            convertFromGeographic(lat, lng);

            // Add more for other params
            if (Commands.Columns[Param1.Index].HeaderText.Equals("Delay") && p1 != -1)
            {
                cell = Commands.Rows[selectedrow].Cells[Param1.Index] as DataGridViewTextBoxCell;
                cell.Value = p1;
                cell.DataGridView.EndEdit();
            }

            writeKML();
            Commands.EndEdit();
        }
        #endregion
        #endregion

        #endregion

        #region GetWPList
        public List<PointLatLngAlt> GetWPList(string altitudeMode = "Relative")
        {
            List<PointLatLngAlt> wpList = new List<PointLatLngAlt>();
            int count = Commands.Rows.Count;
            double totalAlt = 0;
            for (int i = 0; i < count; i++)
            {
                GetWPPoint(i, out PointLatLngAlt wp);
                totalAlt += srtm.getAltitude(wp.Lat, wp.Lng).alt * CurrentState.multiplieralt;
                wpList.Add(wp);
            }
            double baseAlt = totalAlt / count;
            for (int i = 0; i < count; i++)
            {
                switch (altitudeMode.ToLower())
                {
                    case "relative":
                        wpList[i].Tag2 = "Relative";
                        break;
                    case "absolute":
                        wpList[i].Alt = wpList[i].Alt + baseAlt;
                        wpList[i].Tag2 = "Absolute";
                        break;
                    case "terrain":
                        wpList[i].Alt = wpList[i].Alt + baseAlt -
                            srtm.getAltitude(wpList[i].Lat, wpList[i].Lng).alt * CurrentState.multiplieralt;
                        wpList[i].Tag2 = "Terrain";
                        break;
                }
            }

            return wpList;
        }

        #endregion

        #endregion
    }
}