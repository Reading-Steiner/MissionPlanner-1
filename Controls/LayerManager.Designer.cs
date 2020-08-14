﻿

using GMap.NET.CacheProviders;
using System;

namespace VPS.Controls
{
    partial class LayerManager
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LayerManager));
            this.LayerInfoList = new System.Windows.Forms.ListView();
            this.PathColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.LngColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.LatColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.AltCoulmn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.ScaleColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.SuspendLayout();
            // 
            // LayerInfoList
            // 
            resources.ApplyResources(this.LayerInfoList, "LayerInfoList");
            this.LayerInfoList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.PathColumn,
            this.LngColumn,
            this.LatColumn,
            this.AltCoulmn,
            this.ScaleColumn});
            this.LayerInfoList.Groups.AddRange(new System.Windows.Forms.ListViewGroup[] {
            ((System.Windows.Forms.ListViewGroup)(resources.GetObject("LayerInfoList.Groups"))),
            ((System.Windows.Forms.ListViewGroup)(resources.GetObject("LayerInfoList.Groups1")))});
            this.LayerInfoList.Name = "LayerInfoList";
            this.LayerInfoList.UseCompatibleStateImageBehavior = false;
            this.LayerInfoList.View = System.Windows.Forms.View.Details;
            // 
            // PathColumn
            // 
            resources.ApplyResources(this.PathColumn, "PathColumn");
            // 
            // LngColumn
            // 
            resources.ApplyResources(this.LngColumn, "LngColumn");
            // 
            // LatColumn
            // 
            resources.ApplyResources(this.LatColumn, "LatColumn");
            // 
            // AltCoulmn
            // 
            resources.ApplyResources(this.AltCoulmn, "AltCoulmn");
            // 
            // ScaleColumn
            // 
            resources.ApplyResources(this.ScaleColumn, "ScaleColumn");
            // 
            // LayerManager
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.LayerInfoList);
            this.Name = "LayerManager";
            this.ResumeLayout(false);

            
        }

        #endregion

        private void Init()
        {
            this.SelectedGroup = this.LayerInfoList.Groups[0];
            this.NormalGroup = this.LayerInfoList.Groups[1];
            
            for (int i =0; i < MemoryLayerCache.Count; i++)
            {
                System.Windows.Forms.ListViewItem item = new System.Windows.Forms.ListViewItem();
                var layerInfo = MemoryLayerCache.GetLayerFromMemoryCache(i);
                if (layerInfo == null)
                    continue;
                item.Text = layerInfo.GetValueOrDefault().Layer;
                item.SubItems.Add(layerInfo.GetValueOrDefault().Lng.ToString());
                item.SubItems.Add(layerInfo.GetValueOrDefault().Lat.ToString());
                item.SubItems.Add(layerInfo.GetValueOrDefault().Alt.ToString());
                item.SubItems.Add(layerInfo.GetValueOrDefault().ScaleFormat);
                if (layerInfo.GetValueOrDefault().Equals(MemoryLayerCache.GetSelectedLayerFromMemoryCache().GetValueOrDefault()))
                    this.SelectedGroup.Items.Add(item);
                else
                    this.NormalGroup.Items.Add(item);
                this.LayerInfoList.Items.Add(item);
            }
        }



        private System.Windows.Forms.ListView LayerInfoList;
        private System.Windows.Forms.ColumnHeader PathColumn;
        private System.Windows.Forms.ColumnHeader LngColumn;
        private System.Windows.Forms.ColumnHeader LatColumn;
        private System.Windows.Forms.ColumnHeader ScaleColumn;

        private System.Windows.Forms.ListViewGroup SelectedGroup;
        private System.Windows.Forms.ListViewGroup NormalGroup;
        private System.Windows.Forms.ColumnHeader AltCoulmn;
    }
}