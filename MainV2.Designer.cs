﻿using System;
using System.IO;

namespace MissionPlanner
{
    partial class MainV2
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
            Console.WriteLine("mainv2_Dispose");
            if (PluginThreadrunner != null)
                PluginThreadrunner.Dispose();
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainV2));
            this.MainMenu = new System.Windows.Forms.MenuStrip();
            this.CTX_mainmenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.autoHideToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.fullScreenToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.readonlyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.connectionOptionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.connectionListToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuFlightPlannerOpen = new System.Windows.Forms.ToolStripButton();
            this.MenuFlightPlannerClose = new System.Windows.Forms.ToolStripButton();
            this.Separator1 = new System.Windows.Forms.ToolStripSeparator();
            this.MenuLoadLayer = new MissionPlanner.Controls.HLToolStripButton();
            this.MenuZoomToLayer = new MissionPlanner.Controls.HLToolStripButton();
            this.MenuLayerManager = new MissionPlanner.Controls.HLToolStripButton();
            this.Separator2 = new System.Windows.Forms.ToolStripSeparator();
            this.MenuDrawPolygon = new MissionPlanner.Controls.HLToolStripButton();
            this.MenuClearPolygon = new MissionPlanner.Controls.HLToolStripButton();
            this.Separator3 = new System.Windows.Forms.ToolStripSeparator();
            this.Separator4 = new System.Windows.Forms.ToolStripSeparator();
            this.MenuConnect = new System.Windows.Forms.ToolStripButton();
            this.toolStripConnectionControl = new MissionPlanner.Controls.ToolStripConnectionControl();
            this.MenuInitConfig = new System.Windows.Forms.ToolStripButton();
            this.MenuConfigTune = new System.Windows.Forms.ToolStripButton();
            this.MenuSimulation = new System.Windows.Forms.ToolStripButton();
            this.MenuHelp = new System.Windows.Forms.ToolStripButton();
            this.MenuArduPilot = new System.Windows.Forms.ToolStripButton();
            this.menu = new MissionPlanner.Controls.MyButton();
            this.panel1 = new System.Windows.Forms.Panel();
            this.status1 = new MissionPlanner.Controls.Status();
            this.MainMenu.SuspendLayout();
            this.CTX_mainmenu.SuspendLayout();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // MainMenu
            // 
            resources.ApplyResources(this.MainMenu, "MainMenu");
            this.MainMenu.ContextMenuStrip = this.CTX_mainmenu;
            this.MainMenu.GripMargin = new System.Windows.Forms.Padding(0);
            this.MainMenu.ImageScalingSize = new System.Drawing.Size(45, 39);
            this.MainMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuFlightPlannerOpen,
            this.MenuFlightPlannerClose,
            this.Separator1,
            this.MenuLoadLayer,
            this.MenuZoomToLayer,
            this.MenuLayerManager,
            this.Separator2,
            this.MenuDrawPolygon,
            this.MenuClearPolygon,
            this.Separator3,
            this.Separator4,
            this.MenuConnect,
            this.toolStripConnectionControl});
            this.MainMenu.Name = "MainMenu";
            this.MainMenu.ShowItemToolTips = true;
            this.MainMenu.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.MainMenu_ItemClicked);
            this.MainMenu.MouseLeave += new System.EventHandler(this.MainMenu_MouseLeave);
            // 
            // CTX_mainmenu
            // 
            this.CTX_mainmenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.autoHideToolStripMenuItem,
            this.fullScreenToolStripMenuItem,
            this.readonlyToolStripMenuItem,
            this.connectionOptionsToolStripMenuItem,
            this.connectionListToolStripMenuItem});
            this.CTX_mainmenu.Name = "CTX_mainmenu";
            resources.ApplyResources(this.CTX_mainmenu, "CTX_mainmenu");
            // 
            // autoHideToolStripMenuItem
            // 
            this.autoHideToolStripMenuItem.CheckOnClick = true;
            this.autoHideToolStripMenuItem.Name = "autoHideToolStripMenuItem";
            resources.ApplyResources(this.autoHideToolStripMenuItem, "autoHideToolStripMenuItem");
            this.autoHideToolStripMenuItem.Click += new System.EventHandler(this.autoHideToolStripMenuItem_Click);
            // 
            // fullScreenToolStripMenuItem
            // 
            this.fullScreenToolStripMenuItem.CheckOnClick = true;
            this.fullScreenToolStripMenuItem.Name = "fullScreenToolStripMenuItem";
            resources.ApplyResources(this.fullScreenToolStripMenuItem, "fullScreenToolStripMenuItem");
            this.fullScreenToolStripMenuItem.Click += new System.EventHandler(this.fullScreenToolStripMenuItem_Click);
            // 
            // readonlyToolStripMenuItem
            // 
            this.readonlyToolStripMenuItem.CheckOnClick = true;
            this.readonlyToolStripMenuItem.Name = "readonlyToolStripMenuItem";
            resources.ApplyResources(this.readonlyToolStripMenuItem, "readonlyToolStripMenuItem");
            this.readonlyToolStripMenuItem.Click += new System.EventHandler(this.readonlyToolStripMenuItem_Click);
            // 
            // connectionOptionsToolStripMenuItem
            // 
            this.connectionOptionsToolStripMenuItem.Name = "connectionOptionsToolStripMenuItem";
            resources.ApplyResources(this.connectionOptionsToolStripMenuItem, "connectionOptionsToolStripMenuItem");
            this.connectionOptionsToolStripMenuItem.Click += new System.EventHandler(this.connectionOptionsToolStripMenuItem_Click);
            // 
            // connectionListToolStripMenuItem
            // 
            this.connectionListToolStripMenuItem.Name = "connectionListToolStripMenuItem";
            resources.ApplyResources(this.connectionListToolStripMenuItem, "connectionListToolStripMenuItem");
            this.connectionListToolStripMenuItem.Click += new System.EventHandler(this.connectionListToolStripMenuItem_Click);
            // 
            // MenuFlightPlannerOpen
            // 
            this.MenuFlightPlannerOpen.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.MenuFlightPlannerOpen.ForeColor = System.Drawing.SystemColors.ControlLight;
            resources.ApplyResources(this.MenuFlightPlannerOpen, "MenuFlightPlannerOpen");
            this.MenuFlightPlannerOpen.Margin = new System.Windows.Forms.Padding(0);
            this.MenuFlightPlannerOpen.Name = "MenuFlightPlannerOpen";
            this.MenuFlightPlannerOpen.Click += new System.EventHandler(this.FlightPlannerShow);
            // 
            // MenuFlightPlannerClose
            // 
            this.MenuFlightPlannerClose.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.MenuFlightPlannerClose.ForeColor = System.Drawing.SystemColors.ControlLight;
            resources.ApplyResources(this.MenuFlightPlannerClose, "MenuFlightPlannerClose");
            this.MenuFlightPlannerClose.Margin = new System.Windows.Forms.Padding(0);
            this.MenuFlightPlannerClose.Name = "MenuFlightPlannerClose";
            this.MenuFlightPlannerClose.Click += new System.EventHandler(this.MenuFlightPlannerClose_Click);
            // 
            // Separator1
            // 
            this.Separator1.Name = "Separator1";
            resources.ApplyResources(this.Separator1, "Separator1");
            // 
            // MenuLoadLayer
            // 
            this.MenuLoadLayer.BottomTransparent = 4;
            this.MenuLoadLayer.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.MenuLoadLayer.ForeColor = System.Drawing.SystemColors.ControlLight;
            this.MenuLoadLayer.HightLightBackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(0)))));
            resources.ApplyResources(this.MenuLoadLayer, "MenuLoadLayer");
            this.MenuLoadLayer.Margin = new System.Windows.Forms.Padding(0);
            this.MenuLoadLayer.MyChecked = false;
            this.MenuLoadLayer.Name = "MenuLoadLayer";
            this.MenuLoadLayer.TopTransparent = 8;
            this.MenuLoadLayer.Click += new System.EventHandler(this.MenuLoadLayer_Click);
            // 
            // MenuZoomToLayer
            // 
            this.MenuZoomToLayer.BottomTransparent = 4;
            this.MenuZoomToLayer.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.MenuZoomToLayer.ForeColor = System.Drawing.SystemColors.ControlLight;
            this.MenuZoomToLayer.HightLightBackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(0)))));
            resources.ApplyResources(this.MenuZoomToLayer, "MenuZoomToLayer");
            this.MenuZoomToLayer.Margin = new System.Windows.Forms.Padding(0);
            this.MenuZoomToLayer.MyChecked = false;
            this.MenuZoomToLayer.Name = "MenuZoomToLayer";
            this.MenuZoomToLayer.TopTransparent = 8;
            this.MenuZoomToLayer.Click += new System.EventHandler(this.MenuZoomToLayer_Click);
            // 
            // MenuLayerManager
            // 
            this.MenuLayerManager.BottomTransparent = 4;
            this.MenuLayerManager.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.MenuLayerManager.ForeColor = System.Drawing.SystemColors.ControlLight;
            this.MenuLayerManager.HightLightBackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(0)))));
            this.MenuLayerManager.Image = global::MissionPlanner.Properties.Resources._default;
            this.MenuLayerManager.Margin = new System.Windows.Forms.Padding(0);
            this.MenuLayerManager.MyChecked = false;
            this.MenuLayerManager.Name = "MenuLayerManager";
            resources.ApplyResources(this.MenuLayerManager, "MenuLayerManager");
            this.MenuLayerManager.TopTransparent = 8;
            this.MenuLayerManager.Click += new System.EventHandler(this.MenuLayerManager_Click);
            // 
            // Separator2
            // 
            this.Separator2.Name = "Separator2";
            resources.ApplyResources(this.Separator2, "Separator2");
            // 
            // MenuDrawPolygon
            // 
            this.MenuDrawPolygon.BottomTransparent = 4;
            this.MenuDrawPolygon.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.MenuDrawPolygon.ForeColor = System.Drawing.SystemColors.ControlLight;
            this.MenuDrawPolygon.HightLightBackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(0)))));
            this.MenuDrawPolygon.Image = global::MissionPlanner.Properties.Resources._default;
            this.MenuDrawPolygon.Margin = new System.Windows.Forms.Padding(0);
            this.MenuDrawPolygon.MyChecked = false;
            this.MenuDrawPolygon.Name = "MenuDrawPolygon";
            resources.ApplyResources(this.MenuDrawPolygon, "MenuDrawPolygon");
            this.MenuDrawPolygon.TopTransparent = 8;
            this.MenuDrawPolygon.Click += new System.EventHandler(this.MenuDrawPolygon_Click);
            // 
            // MenuClearPolygon
            // 
            this.MenuClearPolygon.BottomTransparent = 4;
            this.MenuClearPolygon.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.MenuClearPolygon.ForeColor = System.Drawing.SystemColors.ControlLight;
            this.MenuClearPolygon.HightLightBackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(0)))));
            this.MenuClearPolygon.Image = global::MissionPlanner.Properties.Resources._default;
            this.MenuClearPolygon.Margin = new System.Windows.Forms.Padding(0);
            this.MenuClearPolygon.MyChecked = false;
            this.MenuClearPolygon.Name = "MenuClearPolygon";
            resources.ApplyResources(this.MenuClearPolygon, "MenuClearPolygon");
            this.MenuClearPolygon.TopTransparent = 8;
            this.MenuClearPolygon.Click += new System.EventHandler(this.MenuClearPolygon_Click);
            // 
            // Separator3
            // 
            this.Separator3.Name = "Separator3";
            resources.ApplyResources(this.Separator3, "Separator3");
            // 
            // Separator4
            // 
            this.Separator4.Name = "Separator4";
            resources.ApplyResources(this.Separator4, "Separator4");
            // 
            // MenuConnect
            // 
            this.MenuConnect.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.MenuConnect.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.MenuConnect.ForeColor = System.Drawing.SystemColors.ControlLight;
            resources.ApplyResources(this.MenuConnect, "MenuConnect");
            this.MenuConnect.Margin = new System.Windows.Forms.Padding(0);
            this.MenuConnect.Name = "MenuConnect";
            this.MenuConnect.Click += new System.EventHandler(this.MenuConnect_Click);
            // 
            // toolStripConnectionControl
            // 
            this.toolStripConnectionControl.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            resources.ApplyResources(this.toolStripConnectionControl, "toolStripConnectionControl");
            this.toolStripConnectionControl.ForeColor = System.Drawing.Color.Black;
            this.toolStripConnectionControl.Margin = new System.Windows.Forms.Padding(0);
            this.toolStripConnectionControl.Name = "toolStripConnectionControl";
            this.toolStripConnectionControl.MouseLeave += new System.EventHandler(this.MainMenu_MouseLeave);
            // 
            // MenuInitConfig
            // 
            this.MenuInitConfig.ForeColor = System.Drawing.SystemColors.ControlLight;
            resources.ApplyResources(this.MenuInitConfig, "MenuInitConfig");
            this.MenuInitConfig.Margin = new System.Windows.Forms.Padding(0);
            this.MenuInitConfig.Name = "MenuInitConfig";
            this.MenuInitConfig.Click += new System.EventHandler(this.MenuSetup_Click);
            // 
            // MenuConfigTune
            // 
            this.MenuConfigTune.ForeColor = System.Drawing.SystemColors.ControlLight;
            resources.ApplyResources(this.MenuConfigTune, "MenuConfigTune");
            this.MenuConfigTune.Margin = new System.Windows.Forms.Padding(0);
            this.MenuConfigTune.Name = "MenuConfigTune";
            this.MenuConfigTune.Click += new System.EventHandler(this.MenuTuning_Click);
            // 
            // MenuSimulation
            // 
            this.MenuSimulation.ForeColor = System.Drawing.SystemColors.ControlLight;
            resources.ApplyResources(this.MenuSimulation, "MenuSimulation");
            this.MenuSimulation.Margin = new System.Windows.Forms.Padding(0);
            this.MenuSimulation.Name = "MenuSimulation";
            this.MenuSimulation.Click += new System.EventHandler(this.MenuSimulation_Click);
            // 
            // MenuHelp
            // 
            this.MenuHelp.ForeColor = System.Drawing.SystemColors.ControlLight;
            resources.ApplyResources(this.MenuHelp, "MenuHelp");
            this.MenuHelp.Margin = new System.Windows.Forms.Padding(0);
            this.MenuHelp.Name = "MenuHelp";
            this.MenuHelp.Click += new System.EventHandler(this.MenuHelp_Click);
            // 
            // MenuArduPilot
            // 
            this.MenuArduPilot.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            resources.ApplyResources(this.MenuArduPilot, "MenuArduPilot");
            this.MenuArduPilot.BackColor = System.Drawing.Color.Transparent;
            this.MenuArduPilot.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.MenuArduPilot.ForeColor = System.Drawing.Color.White;
            this.MenuArduPilot.Image = global::MissionPlanner.Properties.Resources._0d92fed790a3a70170e61a86db103f399a595c70;
            this.MenuArduPilot.Margin = new System.Windows.Forms.Padding(0);
            this.MenuArduPilot.Name = "MenuArduPilot";
            this.MenuArduPilot.Click += new System.EventHandler(this.MenuArduPilot_Click);
            // 
            // menu
            // 
            resources.ApplyResources(this.menu, "menu");
            this.menu.Name = "menu";
            this.menu.UseVisualStyleBackColor = true;
            this.menu.MouseEnter += new System.EventHandler(this.menu_MouseEnter);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.status1);
            this.panel1.Controls.Add(this.MainMenu);
            resources.ApplyResources(this.panel1, "panel1");
            this.panel1.Name = "panel1";
            this.panel1.MouseLeave += new System.EventHandler(this.MainMenu_MouseLeave);
            // 
            // status1
            // 
            resources.ApplyResources(this.status1, "status1");
            this.status1.Name = "status1";
            this.status1.Percent = 0D;
            // 
            // MainV2
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.menu);
            this.KeyPreview = true;
            this.MainMenuStrip = this.MainMenu;
            this.Name = "MainV2";
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.MainV2_KeyDown);
            this.Resize += new System.EventHandler(this.MainV2_Resize);
            this.MainMenu.ResumeLayout(false);
            this.MainMenu.PerformLayout();
            this.CTX_mainmenu.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        public System.Windows.Forms.ToolStripSeparator Separator1;
        public System.Windows.Forms.ToolStripSeparator Separator2;
        public System.Windows.Forms.ToolStripSeparator Separator3;
        public System.Windows.Forms.ToolStripSeparator Separator4;

        public System.Windows.Forms.ToolStripButton MenuFlightPlannerOpen;
        public System.Windows.Forms.ToolStripButton MenuFlightPlannerClose;

        public Controls.HLToolStripButton MenuLoadLayer;
        public Controls.HLToolStripButton MenuZoomToLayer;
        public Controls.HLToolStripButton MenuLayerManager;
        public Controls.HLToolStripButton MenuDrawPolygon;
        public Controls.HLToolStripButton MenuClearPolygon;

        public System.Windows.Forms.ToolStripButton MenuInitConfig;
        public System.Windows.Forms.ToolStripButton MenuSimulation;
        public System.Windows.Forms.ToolStripButton MenuConfigTune;
        public System.Windows.Forms.ToolStripButton MenuConnect;
        private Controls.ToolStripConnectionControl toolStripConnectionControl;
        private Controls.MyButton menu;
        public System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.ContextMenuStrip CTX_mainmenu;
        private System.Windows.Forms.ToolStripMenuItem autoHideToolStripMenuItem;
        public System.Windows.Forms.MenuStrip MainMenu;
        private System.Windows.Forms.ToolStripMenuItem fullScreenToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem readonlyToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem connectionOptionsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem connectionListToolStripMenuItem;
        public System.Windows.Forms.ToolStripButton MenuHelp;
        public System.Windows.Forms.ToolStripButton MenuArduPilot;
        public Controls.Status status1;
    }
}