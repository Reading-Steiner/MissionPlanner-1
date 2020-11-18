﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VPS.Utilities;

namespace VPS.WP
{
    class WPGlobalData
    {
        static public WPGlobalData instance = null;

        public WPGlobalData()
        {
            instance = this;
            LoadConfig();
        }


        #region 参数
        private void LoadConfig()
        {
            PointLatLngAlt defHome = new PointLatLngAlt();
            double defLeft = 0; double defRight = 0;
            double defTop = 0; double defBottom = 0;

            foreach (string key in Settings.Instance.Keys)
            {
                switch (key)
                {
                    case "Main_DefaultLayer":
                        SetLayer(Settings.Instance[key], true);
                        break;
                    case "Main_DefaultHomeLat":
                        {
                            if (double.TryParse(Settings.Instance[key], out double lat))
                                defHome.Lat = lat;
                        }
                        break;
                    case "Main_DefaultHomeLng":
                        {
                            if (double.TryParse(Settings.Instance[key], out double lng))
                                defHome.Lng = lng;
                        }
                        break;
                    case "Main_DefaultHomeAlt":
                        {
                            if (double.TryParse(Settings.Instance[key], out double alt))
                               defHome.Alt = alt;
                        }
                        break;
                    case "Main_DefaultHomeFrame":
                        defHome.Tag2 = "" + Settings.Instance[key];
                        break;
                    case "Main_DefaultPointLeft":
                        {
                            if (double.TryParse(Settings.Instance[key], out double left))
                                defLeft = left;
                        }
                        break;
                    case "Main_DefaultPointTop":
                        {
                            if (double.TryParse(Settings.Instance[key], out double top))
                                defTop = top;
                        }
                        break;
                    case "Main_DefaultPointRight":
                        {
                            if (double.TryParse(Settings.Instance[key], out double right))
                                defRight = right;
                        }
                        break;
                    case "Main_DefaultPointBottom":
                        {
                            if (double.TryParse(Settings.Instance[key], out double bottom))
                                defBottom = bottom;
                        }
                        break;
                }
            }
            if (!string.IsNullOrEmpty(defaultLayerPath))
                SetLayerLimit(
                    GMap.NET.RectLatLng.FromLTRB(defLeft, defTop, defRight, defBottom),
                    defHome, true);

        }

        public void SaveConfig()
        {
            if (this.defaultLayerPath != null)
            {
                Settings.Instance["Main_DefaultLayer"] = this.defaultLayerPath;
                if (this.defaultHome != null)
                {
                    Settings.Instance["Main_DefaultHomeLat"] = this.defaultHome.Lat.ToString();
                    Settings.Instance["Main_DefaultHomeLng"] = this.defaultHome.Lng.ToString();
                    Settings.Instance["Main_DefaultHomeAlt"] = this.defaultHome.Alt.ToString();
                    Settings.Instance["Main_DefaultHomeFrame"] = this.defaultHome.Tag2.ToString();
                }

                Settings.Instance["Main_DefaultPointLeft"] = this.defaultRect.Left.ToString();
                Settings.Instance["Main_DefaultPointTop"] = this.defaultRect.Top.ToString();
                Settings.Instance["Main_DefaultPointRight"] = this.defaultRect.Right.ToString();
                Settings.Instance["Main_DefaultPointBottom"] = this.defaultRect.Bottom.ToString();
            }
        }
        #endregion

        #region QUICKADD 快速添加标记
        private bool quickAdd = false;

        #region 接口函数
        public void BegionQuick()
        {
            quickAdd = true;
        }

        public void EndQuick()
        {
            quickAdd = false;
        }
        #endregion

        #region 执行函数
        public bool IsExecuteOverSetting()
        {
            return !quickAdd;
        }

        #endregion

        #region 修改航点反应函数
        public void ExecuteWPStartSetting()
        {
            AddHistory();
        }

        public void ExecuteWPOverSetting()
        {
            WPListChange?.Invoke();
        }

        public void ExecutePolyOverSetting()
        {
            PolygonListChange?.Invoke();
        }
        #endregion

        #endregion

        public delegate void ChangeHandle();
        public delegate void PositionChangeHandle(PointLatLngAlt position);
        public delegate void CountChangeHandle(int count);

        #region WPLIST 航点
        private List<PointLatLngAlt> wpList = new List<PointLatLngAlt>();
        public ChangeHandle WPListChange;

        #region 获取航点
        public List<PointLatLngAlt> GetWPList()
        {
            List<PointLatLngAlt> retList = new List<PointLatLngAlt>(wpList);

            if ((retList.Count <= 0) || retList[0].Tag != VPS.WP.WPCommands.HomeCommand)
                retList.Insert(0, GetHomePosition());

            return retList;
        }
        #endregion

        #region 设置航点
        public void SetWPListHandle(List<PointLatLngAlt> list)
        {
            if (IsExecuteOverSetting())
            {
                AddHistory();
            }
            wpList = new List<PointLatLngAlt>(list);
            if (wpList.Count > 0)
            {
                if (wpList[0].Tag == WPCommands.HomeCommand)
                {
                    if (wpList[0] != GetHomePosition())
                        SetHomePosition(wpList[0]);
                    wpList.RemoveAt(0);
                }

            }

            if (IsExecuteOverSetting())
            {
                WPListChange?.Invoke();
            }
        }
        #endregion

        #region 扩充航点
        public void AppendWPListHandle(List<PointLatLngAlt> list)
        {
            if (IsExecuteOverSetting())
            {
                AddHistory();
            }
            var apWPList = new List<PointLatLngAlt>(list);
            if (apWPList.Count > 0)
            {
                if (apWPList[0].Tag == WPCommands.HomeCommand)
                {
                    wpList.RemoveAt(0);
                }
            }

            wpList.AddRange(apWPList);

            if (IsExecuteOverSetting())
            {
                WPListChange?.Invoke();
            }
        }
        #endregion

        #region 清空航点
        public void ClearWPListHandle()
        {
            if (IsExecuteOverSetting())
            {
                AddHistory();
            }

            wpList.Clear();

            if (IsExecuteOverSetting())
            {
                WPListChange?.Invoke();
            }
        }
        #endregion

        #region 添加航点
        public int AddWPHandle(PointLatLngAlt wp)
        {
            if (IsExecuteOverSetting())
            {
                AddHistory();
            }

            int index = wpList.Count;
            wpList.Add(new PointLatLngAlt(wp));

            if (IsExecuteOverSetting())
            {
                WPListChange?.Invoke();
            }

            return index;
        }
        #endregion

        #region 插入航点
        public void InsertWPHandle(int index, PointLatLngAlt wp)
        {
            if (IsExecuteOverSetting())
            {
                AddHistory();
            }

            if (index < 0)
                index = 0;
            if (index >= wpList.Count)
                wpList.Add(new PointLatLngAlt(wp));
            else
                wpList.Insert(index, new PointLatLngAlt(wp));

            if (IsExecuteOverSetting())
            {
                WPListChange?.Invoke();
            }
        }
        #endregion

        #region 修改航点
        public void SetWPHandle(int index, PointLatLngAlt wp)
        {
            if (IsExecuteOverSetting())
            {
                AddHistory();
            }

            if (index < 0)
                index = 0;

            if (index >= wpList.Count)
                wpList.Add(new PointLatLngAlt(wp));
            else
                wpList[index] = new PointLatLngAlt(wp);

            if (IsExecuteOverSetting())
            {
                WPListChange?.Invoke();
            }
        }
        #endregion

        #region 删除航点
        public void DeleteWPHandle(int index)
        {
            if (IsExecuteOverSetting())
            {
                AddHistory();
            }

            if (index < 0 || index >= wpList.Count)
                return;
            wpList.RemoveAt(index);

            if (IsExecuteOverSetting())
            {
                WPListChange?.Invoke();
            }
        }
        #endregion

        #region 摘取航点
        public PointLatLngAlt GetWPPoint(int index)
        {
            if (index < 0)
                index = (index % wpList.Count + wpList.Count) % wpList.Count;
            if (index >= wpList.Count)
                index = index % wpList.Count;
            return new PointLatLngAlt(wpList[index]);
        }
        #endregion

        #region 获取航点数
        public int GetWPCount()
        {
            return wpList.Count;
        }
        #endregion

        #endregion

        #region WPList 历史记录
        private List<List<PointLatLngAlt>> history = new List<List<PointLatLngAlt>>();
        public CountChangeHandle historyChange;

        #region 添加记录
        public void AddHistory()
        {
            List<PointLatLngAlt> wpHistory = new List<PointLatLngAlt>(GetWPList());

            history.Add(wpHistory);

            while (history.Count > 40)
                history.RemoveAt(0);

            historyChange?.Invoke(history.Count);
        }
        #endregion

        #region 撤销记录
        public void UndoHistory()
        {
            if (history.Count > 0)
            {
                int no = history.Count - 1;
                var pop = history[no];
                history.RemoveAt(no);

                BegionQuick();
                SetWPListHandle(pop);
                EndQuick();

                WPListChange?.Invoke();
                historyChange?.Invoke(history.Count);
            }
        }
        #endregion

        #endregion

        #region WPLIST 计算数据

        #region 航点列表去Home
        public static List<PointLatLngAlt> WPListRemoveHome(List<PointLatLngAlt> list)
        {
            List<PointLatLngAlt> wpList = new List<PointLatLngAlt>(list);
            if (wpList.Count > 0 && wpList[0].Tag == VPS.WP.WPCommands.HomeCommand)
                wpList.RemoveAt(0);
            return wpList;
        }
        #endregion

        #region 航点列表高度框架统一
        public static List<PointLatLngAlt> WPListChangeAltFrame(List<PointLatLngAlt> list, string altitudeMode = "")
        {
            List<PointLatLngAlt> wpList = new List<PointLatLngAlt>(list);
            List<PointLatLngAlt> retWPList = new List<PointLatLngAlt>();

            double baseAlt = GetBaseAlt(wpList);
            foreach (var wp in wpList)
            {
                double alt = srtm.getAltitude(wp.Lat, wp.Lng).alt * CurrentState.multiplieralt;
                switch (altitudeMode)
                {
                    case "Relative":
                        switch (wp.Tag2)
                        {
                            case "Relative":
                                break;
                            case "Absolute":
                                wp.Alt = wp.Alt - baseAlt;
                                break;
                            case "Terrain":
                                wp.Alt = wp.Alt + alt - baseAlt;
                                break;
                            default:
                                break;
                        }
                        wp.Tag2 = "Relative";
                        retWPList.Add(wp);
                        break;
                    case "Absolute":
                        switch (wp.Tag2)
                        {
                            case "Relative":
                                wp.Alt = wp.Alt + baseAlt;
                                break;
                            case "Absolute":
                                break;
                            case "Terrain":
                                wp.Alt = wp.Alt + alt;
                                break;
                            default:
                                wp.Alt = wp.Alt + baseAlt;
                                break;
                        }
                        wp.Tag2 = "Absolute";
                        retWPList.Add(wp);
                        break;
                    case "Terrain":
                        switch (wp.Tag2)
                        {
                            case "Relative":
                                wp.Alt = wp.Alt + baseAlt - alt;
                                break;
                            case "Absolute":
                                wp.Alt = wp.Alt - alt;
                                break;
                            case "Terrain":
                                break;
                            default:
                                wp.Alt = wp.Alt + baseAlt - alt;
                                break;
                        }
                        wp.Tag2 = "Terrain";
                        retWPList.Add(wp);
                        break;
                    default:
                        retWPList.Add(wp);
                        break;
                }
            }

            return retWPList;
        }
        #endregion

        #region 航点列表求航摄基线
        public static double GetBaseAlt(List<PointLatLngAlt> wpLists)
        {
            var wpList = WPListRemoveHome(wpLists);
            double totalAlt = 0;
            int doubleWP = 0;
            foreach (var wp in wpList)
            {
                if (VPS.WP.WPCommands.CoordsWPCommands.Contains(wp.Tag))
                {
                    if (wp.Lat == 0 && wp.Lng == 0)
                        continue;
                    totalAlt += srtm.getAltitude(wp.Lat, wp.Lng).alt * CurrentState.multiplieralt;
                    doubleWP++;
                }
            }
            return totalAlt / Math.Max(1, doubleWP);
        }
        #endregion

        #endregion

        #region HOME 初始位置
        public ChangeHandle HomeChange;

        #region 获取初始位置
        public PointLatLngAlt GetHomePosition()
        {
            if (currentHome == null)
                return null;
            PointLatLngAlt retHome = new PointLatLngAlt(currentHome);
            if (retHome.Tag != WPCommands.HomeCommand)
                retHome.Tag = WPCommands.HomeCommand;
            return retHome;
        }
        #endregion

        #region 设置初始位置
        public void SetHomePosition(PointLatLngAlt position)
        {
            currentHome = new PointLatLngAlt(position);

            if(!string.IsNullOrEmpty(defaultLayerPath) &&
                !string.IsNullOrEmpty(currentLayerPath) &&
                defaultLayerPath == currentLayerPath)
            {
                defaultHome = new PointLatLngAlt(position);
            }


            if (IsExecuteOverSetting())
            {
                HomeChange?.Invoke();
                AddHistory();
            }
        }
        #endregion

        #endregion

        #region CURRENT 当前位置
        public PositionChangeHandle CurrentChange;
        public void SetCurrentPosition(PointLatLngAlt position)
        {
            CurrentChange?.Invoke(position);
        }
        #endregion

        #region LAYER 图层信息
        private string currentLayerPath = null;
        private PointLatLngAlt currentHome = new PointLatLngAlt();
        private GMap.NET.RectLatLng currentRect = new GMap.NET.RectLatLng();
        private string defaultLayerPath = null;
        private PointLatLngAlt defaultHome = null;
        private GMap.NET.RectLatLng defaultRect = new GMap.NET.RectLatLng();

        //public GDAL.GDAL.GeoBitmap currentLayer;

        #region 设置图层信息
        public void SetLayer(string path, bool isDefault = true)
        {
            currentLayerPath = path;
            if (isDefault || defaultLayerPath == null)
                defaultLayerPath = path;
        }

        public void SetLayerLimit(GMap.NET.RectLatLng rect,PointLatLngAlt home, bool isDefault = true)
        {
            currentRect = rect;
            SetHomePosition(home);
            if (isDefault || defaultHome == null)
            {
                defaultRect = rect;
                defaultHome = new PointLatLngAlt(home);
                if (defaultHome.Tag != WPCommands.HomeCommand)
                    defaultHome.Tag = WPCommands.HomeCommand;
            }
        }
        #endregion

        #region 获取图层信息
        public string GetLayer()
        {
            if (currentLayerPath == null)
                return null;
            return currentLayerPath;
        }

        public string GetDefaultLayer()
        {
            if (defaultLayerPath == null)
                return null;
            return defaultLayerPath;
        }

        public PointLatLngAlt GetLayerHome()
        {
            return GetHomePosition();
        }

        public PointLatLngAlt GetDefaultLayerHome()
        {
            if (defaultHome == null)
                return null;
            PointLatLngAlt retHome = new PointLatLngAlt(defaultHome);
            if (retHome.Tag != WPCommands.HomeCommand)
                retHome.Tag = WPCommands.HomeCommand;
            return retHome;
        }

        public GMap.NET.RectLatLng GetLayerRect()
        {
            return currentRect;
        }

        public GMap.NET.RectLatLng GetDefaultLayerRect()
        {
            return defaultRect;
        }
        #endregion

        #region 是否为默认图层
        public bool IsDefaultLayer(string layer)
        {
            if (defaultLayerPath == null || layer == null)
                return false;
            return defaultLayerPath == layer;
        }
        #endregion

        #region 默认图层已失效
        public void DefaultLayerInvalid()
        {
            defaultLayerPath = null;
        }
        #endregion

        #endregion

        #region POLYGON 区域点
        private List<PointLatLngAlt> polyList = new List<PointLatLngAlt>();
        public ChangeHandle PolygonListChange;

        #region 设置区域点
        public void SetPolyListHandle(List<PointLatLngAlt> polygonList)
        {
            polyList = new List<PointLatLngAlt>(polygonList);

            if (IsExecuteOverSetting())
            {
                PolygonListChange?.Invoke();
            }
        }
        #endregion

        #region 获取区域点
        public List<PointLatLngAlt> GetPolygList()
        {
            List<PointLatLngAlt> polygonList = new List<PointLatLngAlt>(polyList);
            return polygonList;
        }
        #endregion

        #region 添加区域点
        public void AddPolyHandle(PointLatLngAlt poly)
        {
            PointLatLngAlt polygon = new PointLatLngAlt(poly);
            polyList.Add(polygon);

            if (IsExecuteOverSetting())
            {
                PolygonListChange?.Invoke();
            }
        }
        #endregion

        #region 插入区域点
        public void InsertPolyHandle(int index, PointLatLngAlt poly)
        {
            PointLatLngAlt polygon = new PointLatLngAlt(poly);
            polyList.Insert(index, polygon);

            if (IsExecuteOverSetting())
            {
                PolygonListChange?.Invoke();
            }
        }
        #endregion

        #region 移动区域点
        public void MovePolyHandle(int index, PointLatLngAlt poly)
        {
            PointLatLngAlt polygon = new PointLatLngAlt(poly);
            polyList[index] =  polygon;

            if (IsExecuteOverSetting())
            {
                PolygonListChange?.Invoke();
            }
        }
        #endregion

        #region 删除区域点
        public void DeletePolyHandle(int index)
        {
            polyList.RemoveAt(index);

            if (IsExecuteOverSetting())
            {
                PolygonListChange?.Invoke();
            }
        }
        #endregion

        #region 抽取区域点
        public PointLatLngAlt GetPolyPoint(int index)
        {
            if (index < 0)
                index = (index % polyList.Count + polyList.Count) % polyList.Count;
            if (index >= polyList.Count)
                index = index % polyList.Count;
            return new PointLatLngAlt(polyList[index]);
        }
        #endregion

        #region 清空区域点
        public void ClearPolyHandle()
        {
            polyList.Clear();

            if (IsExecuteOverSetting())
            {
                PolygonListChange?.Invoke();
            }
        }
        #endregion

        #region 获取区域点数
        public int GetPolyCount()
        {
            return polyList.Count;
        }
        #endregion

        #endregion
    }
}
