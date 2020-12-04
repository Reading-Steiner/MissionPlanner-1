﻿using DevComponents.DotNetBar;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Design;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using VPS.Utilities;

namespace VPS.Controls.LoadAndSave
{
    public partial class LoadPolygon : DevComponents.DotNetBar.Office2007Form
    {
        public LoadPolygon()
        {
            InitializeComponent();
            this.Load += LoadPolygon_Load;

            advPropertyGrid1.Tag = advPropertyGrid1.PropertySort;
            advPropertyGrid1.PropertySort = DevComponents.DotNetBar.ePropertySort.Categorized;
        }

        private void LoadPolygon_Load(object sender, EventArgs e)
        {
            BindingDataSource();
        }

        private void OpenFile_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog fd = new OpenFileDialog())
            {
                fd.Filter = "Google Earth KML(*kml;*.kmz) |*.kml;*.kmz|ShapeFile(*.shp)|*.shp";
                if (Directory.Exists(Settings.Instance["WPFileDirectory"] ?? ""))
                    fd.InitialDirectory = Settings.Instance["WPFileDirectory"];
                var result = fd.ShowDialog();

                string file = fd.FileName;
                if (result == DialogResult.OK && File.Exists(file))
                {
                    Settings.Instance["WPFileDirectory"] = Path.GetDirectoryName(file);
                    switch (fd.FilterIndex)
                    {
                        case 1:
                            {
                                var data = VPS.CustomFile.KML.ReadKML(file);
                                BindingDataSource(file, data);
                            }
                            break;
                        case 2:
                            {
                                var data = VPS.CustomFile.SHP.ReadSHP(file);
                                BindingDataSource(file, data);
                            }
                            break;
                        default:
                            BindingDataSource(file);
                            break;
                    }
                }
            }
        }

        #region 文件解析

        #endregion

        private void BindingDataSource(string file = "")
        {
            info = new LoadPolygonInfo();
            info.fileName = file;
            info.fileType = CustomFile.UniversalMethod.GetFileType(file);

            advPropertyGrid1.SelectedObject = info;
        }

        private void BindingDataSource(string file, CustomFile.SHP.SHPDataSet data)
        {
            info = new LoadSHPPolygonInfo();
            info.fileName = file;
            info.fileType = CustomFile.UniversalMethod.GetFileType(file);
            (info as LoadPolygonFileInfo).fileSize = CustomFile.UniversalMethod.GetFileSize(file);
            (info as LoadPolygonFileInfo).createTime = CustomFile.UniversalMethod.GetFileCreate(file);
            (info as LoadPolygonFileInfo).modifyTime = CustomFile.UniversalMethod.GetFileModify(file);

            (info as LoadSHPPolygonInfo).coordinates = data.coordinates;
            (info as LoadSHPPolygonInfo).featureType = data.featureType;
            (info as LoadSHPPolygonInfo).features = new FeaturesInfo(data.points);
            advPropertyGrid1.SelectedObject = info;
        }

        private void BindingDataSource(string file, CustomFile.KML.KMLDataSet data)
        {
            info = new LoadKMLPolygonInfo();
            info.fileName = file;
            info.fileType = CustomFile.UniversalMethod.GetFileType(file);
            (info as LoadPolygonFileInfo).fileSize = CustomFile.UniversalMethod.GetFileSize(file);
            (info as LoadPolygonFileInfo).createTime = CustomFile.UniversalMethod.GetFileCreate(file);
            (info as LoadPolygonFileInfo).modifyTime = CustomFile.UniversalMethod.GetFileModify(file);

            (info as LoadKMLPolygonInfo).coordinates = data.coordinates;
            (info as LoadKMLPolygonInfo).features = new FeaturesInfo(data.points);
            advPropertyGrid1.SelectedObject = info;
        }

        LoadPolygonInfo info;

        public List<PointLatLngAlt> GetWPList()
        {
            if (info is LoadSHPPolygonInfo)
            {
                var data = info as LoadSHPPolygonInfo;
                if (data.features.features.Count > 0 && data.features.Current != -1)
                {
                    return data.features[data.features.Current];
                }
            }

            if (info is LoadKMLPolygonInfo)
            {
                var data = info as LoadKMLPolygonInfo;
                if (data.features.features.Count > 0 && data.features.Current != -1)
                {
                    return data.features[data.features.Current];
                }
            }

            return new List<PointLatLngAlt>();
        }
    }

    [TypeConverter(typeof(PropertySorter))]
    class LoadPolygonInfo
    {
        [Category("打开文件"), DisplayName("文件")]
        [PropertyOrder(0b00000001)]
        [Editor(typeof(CustomControls.ContentUITypeEditor), typeof(UITypeEditor))]
        public string fileName { get; set; }

        [Browsable(false)]
        public string fileType { get; set; }
    }

    [TypeConverter(typeof(PropertySorter))]
    class LoadPolygonFileInfo : LoadPolygonInfo
    {
        [Category("文件信息"), DisplayName("文件大小"), ReadOnly(false)]
        [PropertyOrder(0b00010001)]
        public string fileSize { get; set; }

        [Category("文件信息"), DisplayName("创建时间"), ReadOnly(false)]
        [PropertyOrder(0b00010010)]
        public string createTime { get; set; }

        [Category("文件信息"), DisplayName("修改时间"), ReadOnly(false)]
        [PropertyOrder(0b00010011)]
        public string modifyTime { get; set; }
    }

    [TypeConverter(typeof(PropertySorter))]
    class LoadSHPPolygonInfo : LoadPolygonFileInfo
    {
        [Category("打开文件"), DisplayName("文件类型"), ReadOnly(true)]
        [PropertyOrder(0b00000010)]
        public string openFileType { set; get; } = "ShapeFile";

        [Category("要素信息"), DisplayName("要素集合")]
        [PropertyOrder(0b00100001)]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        [Editor(typeof(CustomControls.PositionListUITypeEditor), typeof(UITypeEditor))]
        public FeaturesInfo features { get; set; }

        [Category("要素信息"), DisplayName("要素类型"), ReadOnly(true)]
        [PropertyOrder(0b00100010)]
        public string featureType { get; set; } = "";

        [Category("要素信息"), DisplayName("投影坐标系")]
        [PropertyOrder(0b00100011)]
        [Editor(typeof(CustomControls.ContentUITypeEditor), typeof(UITypeEditor))]
        public string coordinates { get; set; }
    }

    [TypeConverter(typeof(PropertySorter))]
    class LoadKMLPolygonInfo : LoadPolygonFileInfo
    {
        [Category("打开文件"), DisplayName("文件类型"), ReadOnly(true)]
        [PropertyOrder(0b00000010)]
        public string openFileType { set; get; } = "Google Earth KML";

        [Category("要素信息"), DisplayName("要素集合")]
        [PropertyOrder(0b00100001)]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        [Editor(typeof(CustomControls.PositionListUITypeEditor), typeof(UITypeEditor))]
        public FeaturesInfo features { get; set; }

        [Category("要素信息"), DisplayName("投影坐标系")]
        [PropertyOrder(0b00100010)]
        [Editor(typeof(CustomControls.ContentUITypeEditor), typeof(UITypeEditor))]
        public string coordinates { get; set; }
    }
}
