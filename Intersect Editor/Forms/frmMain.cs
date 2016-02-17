﻿/*
    Intersect Game Engine (Editor)
    Copyright (C) 2015  JC Snider, Joe Bridges
    
    Website: http://ascensiongamedev.com
    Contact Email: admin@ascensiongamedev.com 

    This program is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation; either version 2 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License along
    with this program; if not, write to the Free Software Foundation, Inc.,
    51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
*/
using System;
using System.IO;
using System.Windows.Forms;
using Intersect_Editor.Classes;
using WeifenLuo.WinFormsUI.Docking;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Intersect_Editor.Forms
{
    public partial class frmMain : Form
    {
        //General Editting Variables
        bool _tMouseDown;

        //Cross Thread Delegates
        public delegate void TryOpenEditor(int editorIndex);
        public TryOpenEditor EditorDelegate;
        public delegate void HandleDisconnect();
        public HandleDisconnect DisconnectDelegate;

        //Editor References
        private frmAnimation _animationEditor;
        private FrmItem _itemEditor;
        private frmNpc _npcEditor;
        private frmResource _resourceEditor;
        private frmSpell _spellEditor;
        private frmClass _classEditor;
        private frmQuest _questEditor;
        private frmProjectile _projectileEditor;

        //Initialization & Setup Functions
        public frmMain()
        {
            InitializeComponent();
            Globals.MapGridWindow = new frmGridView();
            Globals.MapListWindow = new frmMapList();
            Globals.MapListWindow.Show(dockLeft, DockState.DockRight);
            Globals.MapLayersWindow = new frmMapLayers();
            Globals.MapLayersWindow.Init();
            Globals.MapLayersWindow.Show(dockLeft, DockState.DockLeft);

            Globals.MapEditorWindow = new frmMapEditor();
            Globals.MapEditorWindow.Show(dockLeft, DockState.Document);
        }
        private void frmMain_Load(object sender, EventArgs e)
        {
            // Set form object properties based on constants to prevent user inputting invalid options.
            InitFormObjects();

            //Init Delegates
            EditorDelegate = new TryOpenEditor(TryOpenEditorMethod);
            DisconnectDelegate = new HandleDisconnect(HandleServerDisconnect);

            // Initilise the editor.
            InitEditor();

            //Init Map Properties
            InitMapProperties();
            Show();
        }
        private void FrmMain_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == (Keys.Control | Keys.Z))
            {
                toolStripBtnUndo_Click(null, null);
            }
            else if (e.KeyData == (Keys.Control | Keys.Y))
            {
                toolStripBtnRedo_Click(null, null);
            }
            else if (e.KeyData == (Keys.Control | Keys.X))
            {
                toolStripBtnCut_Click(null, null);
            }
            else if (e.KeyData == (Keys.Control | Keys.C))
            {
                toolStripBtnCopy_Click(null, null);
            }
            else if (e.KeyData == (Keys.Control | Keys.V))
            {
                toolStripBtnPaste_Click(null, null);
            }
        }
        private void InitFormObjects()
        {
            Globals.MapLayersWindow.scrlMap.Maximum = Globals.GameMaps.Length;
            Globals.MapLayersWindow.scrlX.Maximum = Globals.MapWidth;
            Globals.MapLayersWindow.scrlY.Maximum = Globals.MapHeight;
            Globals.MapLayersWindow.scrlMapItem.Maximum = Constants.MaxItems;
        }
        private void InitMapProperties()
        {
            DockPane unhiddenPane = dockLeft.Panes[0];
            Globals.MapPropertiesWindow = new frmMapProperties();
            Globals.MapPropertiesWindow.Show(unhiddenPane, DockAlignment.Bottom, .4);
            Globals.MapPropertiesWindow.Init(Globals.CurrentMap);
            Globals.MapEditorWindow.DockPanel.Focus();
        }
        private void InitEditor()
        {
            Graphics.InitSfml(this);
            Sounds.Init();
            Globals.InEditor = true;
            GrabMouseDownEvents();
        }
        protected override void OnClosed(EventArgs e)
        {
            Globals.MapGridWindow.Dispose();
            base.OnClosed(e);
            Application.Exit();
        }
        public void EnterMap(int mapNum)
        {
            Globals.CurrentMap = mapNum;
            if (Globals.MapPropertiesWindow != null) { Globals.MapPropertiesWindow.Init(Globals.CurrentMap); }
            if (mapNum > -1)
            {
                if (Globals.GameMaps[mapNum] != null)
                {
                    Text = @"Intersect Editor - Map# " + mapNum + @" " + Globals.GameMaps[mapNum].MyName + @" Revision: " + Globals.GameMaps[mapNum].Revision;
                }
                Globals.MapEditorWindow.picMap.Visible = false;
                Globals.MapEditorWindow.ResetUndoRedoStates();
                PacketSender.SendNeedMap(Globals.CurrentMap);
                PacketSender.SendNeedGrid(Globals.CurrentMap);
            }
            else
            {
                Text = @"Intersect Editor";
                Globals.MapEditorWindow.picMap.Visible = false;
                Globals.MapEditorWindow.ResetUndoRedoStates();
            }
            Graphics.TilePreviewUpdated = true;
            Graphics.LightsChanged = true;
        }
        private void GrabMouseDownEvents()
        {
            GrabMouseDownEvents(this);
        }
        private void GrabMouseDownEvents(Control e)
        {
            foreach (Control t in e.Controls)
            {
                if (t.GetType() == typeof (MenuStrip))
                {
                    foreach (ToolStripMenuItem t1 in ((MenuStrip) t).Items)
                    {
                        t1.MouseDown += MouseDownHandler;
                    }
                    t.MouseDown += MouseDownHandler;
                }
                else if (t.GetType() == typeof (PropertyGrid))
                {
                }
                else
                {
                    GrabMouseDownEvents(t);
                }
            }
            e.MouseDown += MouseDownHandler;
        }
        public void MouseDownHandler(object sender, MouseEventArgs e)
        {
            if (sender != Globals.MapEditorWindow && sender != Globals.MapEditorWindow.pnlMapContainer &&
                sender != Globals.MapEditorWindow.picMap)
            {
                Globals.MapEditorWindow.PlaceSelection();
            }
        }

        //Update
        public void Update()
        {
            if (Globals.CurrentMap > -1)
            {

                if (Globals.GameMaps[Globals.CurrentMap] != null)
                {
                    toolStripLabelCoords.Text = @" CurX: " + Globals.CurTileX + @" CurY: " + Globals.CurTileY;
                    toolStripLabelRevision.Text = @"Revision: " + Globals.GameMaps[Globals.CurrentMap].Revision;
                    if (Text != @"Intersect Editor - Map# " + Globals.CurrentMap + @" " + Globals.GameMaps[Globals.CurrentMap].MyName)
                    {
                        Text = @"Intersect Editor - Map# " + Globals.CurrentMap + @" " + Globals.GameMaps[Globals.CurrentMap].MyName;
                    }
                }
            }

            //Process the Undo/Redo Buttons
            if (Globals.MapEditorWindow.MapUndoStates.Count > 0)
            {
                toolStripBtnUndo.Enabled = true;
                undoToolStripMenuItem.Enabled = true;
            }
            else
            {
                toolStripBtnUndo.Enabled = false;
                undoToolStripMenuItem.Enabled = false;
            }
            if (Globals.MapEditorWindow.MapRedoStates.Count > 0)
            {
                toolStripBtnRedo.Enabled = true;
                redoToolStripMenuItem.Enabled = true;
            }
            else
            {
                toolStripBtnRedo.Enabled = false;
                redoToolStripMenuItem.Enabled = false;
            }

            //Process the Fill/Erase Buttons
            if (Globals.CurrentLayer <= Constants.LayerCount)
            {
                toolStripBtnFill.Enabled = true;
                fillToolStripMenuItem.Enabled = true;
                toolStripBtnErase.Enabled = true;
                eraseLayerToolStripMenuItem.Enabled = true;
            }
            else
            {
                toolStripBtnFill.Enabled = false;
                fillToolStripMenuItem.Enabled = false;
                toolStripBtnErase.Enabled = false;
                eraseLayerToolStripMenuItem.Enabled = false;
            }

            //Process the Tool Buttons
            toolStripBtnPen.Enabled = false;
            toolStripBtnSelect.Enabled = true;
            toolStripBtnRect.Enabled = false;
            toolStripBtnEyeDrop.Enabled = false;
            switch (Globals.CurrentLayer)
            {
                case Constants.LayerCount: //Attributes
                    toolStripBtnPen.Enabled = true;
                    toolStripBtnRect.Enabled = true;
                    break;
                case Constants.LayerCount + 1: //Lights
                    Globals.CurrentTool = (int)Enums.EdittingTool.Selection;
                    break;
                case Constants.LayerCount + 2: //Events
                    Globals.CurrentTool = (int)Enums.EdittingTool.Selection;
                    break;
                case Constants.LayerCount + 3: //NPCS
                    Globals.CurrentTool = (int)Enums.EdittingTool.Selection;
                    break;
                default:
                    toolStripBtnPen.Enabled = true;
                    toolStripBtnRect.Enabled = true;
                    toolStripBtnEyeDrop.Enabled = true;
                    break;
            }

            switch (Globals.CurrentTool)
            {
                case (int)Enums.EdittingTool.Pen:
                    if (!toolStripBtnPen.Checked) { toolStripBtnPen.Checked = true; }
                    if (toolStripBtnSelect.Checked) { toolStripBtnSelect.Checked = false; }
                    if (toolStripBtnRect.Checked) { toolStripBtnRect.Checked = false; }
                    if (toolStripBtnEyeDrop.Checked) { toolStripBtnEyeDrop.Checked = false; }

                    if (toolStripBtnCut.Enabled) { toolStripBtnCut.Enabled = false; }
                    if (toolStripBtnCopy.Enabled) { toolStripBtnCopy.Enabled = false; }
                    if (cutToolStripMenuItem.Enabled) { cutToolStripMenuItem.Enabled = false; }
                    if (copyToolStripMenuItem.Enabled) { copyToolStripMenuItem.Enabled = false; }
                        break;
                case (int)Enums.EdittingTool.Selection:
                    if (toolStripBtnPen.Checked) { toolStripBtnPen.Checked = false; }
                    if (!toolStripBtnSelect.Checked) { toolStripBtnSelect.Checked = true; }
                    if (toolStripBtnRect.Checked) { toolStripBtnRect.Checked = false; }
                    if (toolStripBtnEyeDrop.Checked) { toolStripBtnEyeDrop.Checked = false; }

                    if (!toolStripBtnCut.Enabled) { toolStripBtnCut.Enabled = true; }
                    if (!toolStripBtnCopy.Enabled) { toolStripBtnCopy.Enabled = true; }
                    if (!cutToolStripMenuItem.Enabled) { cutToolStripMenuItem.Enabled = true; }
                    if (!copyToolStripMenuItem.Enabled) { copyToolStripMenuItem.Enabled = true; }
                    break;
                case (int)Enums.EdittingTool.Rectangle:
                    if (toolStripBtnPen.Checked) { toolStripBtnPen.Checked = false; }
                    if (toolStripBtnSelect.Checked) { toolStripBtnSelect.Checked = false; }
                    if (!toolStripBtnRect.Checked) { toolStripBtnRect.Checked = true; }
                    if (toolStripBtnEyeDrop.Checked) { toolStripBtnEyeDrop.Checked = false; }

                    if (toolStripBtnCut.Enabled) { toolStripBtnCut.Enabled = false; }
                    if (toolStripBtnCopy.Enabled) { toolStripBtnCopy.Enabled = false; }
                    if (cutToolStripMenuItem.Enabled) { cutToolStripMenuItem.Enabled = false; }
                    if (copyToolStripMenuItem.Enabled) { copyToolStripMenuItem.Enabled = false; }
                    break;
                case (int)Enums.EdittingTool.Droppler:
                    if (toolStripBtnPen.Checked) { toolStripBtnPen.Checked = false; }
                    if (toolStripBtnSelect.Checked) { toolStripBtnSelect.Checked = false; }
                    if (toolStripBtnRect.Checked) { toolStripBtnRect.Checked = false; }
                    if (!toolStripBtnEyeDrop.Checked) { toolStripBtnEyeDrop.Checked = true; }

                    if (toolStripBtnCut.Enabled) { toolStripBtnCut.Enabled = false; }
                    if (toolStripBtnCopy.Enabled) { toolStripBtnCopy.Enabled = false; }
                    if (cutToolStripMenuItem.Enabled) { cutToolStripMenuItem.Enabled = false; }
                    if (copyToolStripMenuItem.Enabled) { copyToolStripMenuItem.Enabled = false; }
                    break;
            }

            if (Globals.HasCopy)
            {
                toolStripBtnPaste.Enabled = true;
                pasteToolStripMenuItem.Enabled = true;
            }
            else
            {
                toolStripBtnPaste.Enabled = false;
                pasteToolStripMenuItem.Enabled = false;
            }

            if (Globals.Dragging)
            {
                if (Globals.MainForm.ActiveControl.GetType() == typeof(WeifenLuo.WinFormsUI.Docking.DockPane))
                {
                    Control ctrl = ((WeifenLuo.WinFormsUI.Docking.DockPane)Globals.MainForm.ActiveControl).ActiveControl;
                    if (ctrl != Globals.MapEditorWindow)
                    {
                        Globals.MapEditorWindow.PlaceSelection();
                    }
                }
            }
        }

        //Disconnection
        private void HandleServerDisconnect()
        {
            //Offer to export map
            if (Globals.CurrentMap > -1 && Globals.GameMaps[Globals.CurrentMap] != null)
            {
                if (MessageBox.Show("You have been disconnected from the server! Would you like to export this map before closing this editor?", "Disconnected -- Export Map?", MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
                {
                    exportMapToolStripMenuItem_Click(null, null);
                    Application.Exit();
                }
                else
                {
                    Application.Exit();
                }
            }
            else
            {
                MessageBox.Show(@"Disconnected!");
                Application.Exit();
            }
        }

        //MenuBar Functions -- File
        private void saveMapToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(@"Are you sure you want to save this map?", @"Save Map", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                PacketSender.SendMap(Globals.CurrentMap);
            }
        }
        private void newMapToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (
                MessageBox.Show(@"Are you sure you want to create a new, unconnected map?", @"New Map",
                    MessageBoxButtons.YesNo) != DialogResult.Yes) return;
            if (MessageBox.Show(@"Do you want to save your current map?", @"Save current map?", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                PacketSender.SendMap(Globals.CurrentMap);
            }
            PacketSender.SendCreateMap(-1, Globals.CurrentMap, null);
        }
        private void exportMapToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog fileDialog = new SaveFileDialog();
            fileDialog.Filter = "Intersect Map|*.imap";
            fileDialog.Title = "Export Map";
            fileDialog.ShowDialog();

            if (fileDialog.FileName != "")
            {
                File.WriteAllBytes(fileDialog.FileName, Globals.GameMaps[Globals.CurrentMap].Save());
            }
        }
        private void importMapToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Filter = "Intersect Map|*.imap";
            fileDialog.Title = "Import Map";
            fileDialog.ShowDialog();

            if (fileDialog.FileName != "")
            {
                Globals.MapEditorWindow.PrepUndoState();
                Globals.GameMaps[Globals.CurrentMap].Load(File.ReadAllBytes(fileDialog.FileName),true);
                Globals.MapEditorWindow.AddUndoState();
            }
        }
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
        //Edit
        private void fillToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Globals.CurrentLayer <= Constants.LayerCount)
            {
                Globals.MapEditorWindow.FillLayer();
            }
        }
        private void eraseLayerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Globals.CurrentLayer <= Constants.LayerCount)
            {
                Globals.MapEditorWindow.EraseLayer();
            }
            return;
            if (
                MessageBox.Show(@"Are you sure you want to erase this layer?", @"Erase Layer", MessageBoxButtons.YesNo) !=
                DialogResult.Yes) return;
            for (var x = 0; x < Globals.MapWidth; x++)
            {
                for (var y = 0; y < Globals.MapHeight; y++)
                {
                    Globals.CurTileX = x;
                    Globals.CurTileY = y;
                    Globals.MapEditorWindow.picMap_MouseDown(null, new MouseEventArgs(MouseButtons.Right, 1, x * Globals.TileWidth + Globals.TileWidth, y * Globals.TileHeight + Globals.TileHeight, 0));
                    Globals.MapEditorWindow.picMap_MouseUp(null, new MouseEventArgs(MouseButtons.None, 0, 0, 0, 0));
                }
            }
        }
        private void allLayersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Globals.SelectionType = (int)Enums.SelectionTypes.AllLayers;
            allLayersToolStripMenuItem.Checked = true;
            currentLayerOnlyToolStripMenuItem.Checked = false;
        }
        private void currentLayerOnlyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Globals.SelectionType = (int)Enums.SelectionTypes.CurrentLayer;
            allLayersToolStripMenuItem.Checked = false;
            currentLayerOnlyToolStripMenuItem.Checked = true;
        }
        private void undoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            toolStripBtnUndo_Click(null, null);
        }
        private void redoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            toolStripBtnRedo_Click(null, null);
        }
        private void cutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            toolStripBtnCut_Click(null, null);
        }
        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            toolStripBtnCopy_Click(null, null);
        }
        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            toolStripBtnPaste_Click(null, null);
        }
        //View
        private void hideDarknessToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Graphics.HideDarkness = !Graphics.HideDarkness;
            hideDarknessToolStripMenuItem.Checked = !Graphics.HideDarkness;
        }
        private void hideFogToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Graphics.HideFog = !Graphics.HideFog;
            hideFogToolStripMenuItem.Checked = !Graphics.HideFog;
        }
        private void hideOverlayToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Graphics.HideOverlay = !Graphics.HideOverlay;
            hideOverlayToolStripMenuItem.Checked = !Graphics.HideOverlay;
        }
        private void hideTilePreviewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Graphics.HideTilePreview = !Graphics.HideTilePreview;
            hideTilePreviewToolStripMenuItem.Checked = !Graphics.HideTilePreview;
        }
        private void hideResourcesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Graphics.HideResources = !Graphics.HideResources;
            hideResourcesToolStripMenuItem.Checked = !Graphics.HideResources;
        }
        //Content Editors
        private void itemEditorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PacketSender.SendItemEditor();
        }
        private void npcEditorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PacketSender.SendNpcEditor();
        }
        private void spellEditorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PacketSender.SendSpellEditor();
        }
        private void animationEditorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PacketSender.SendAnimationEditor();
        }
        private void resourceEditorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PacketSender.SendResourceEditor();
        }
        private void classEditorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PacketSender.SendClassEditor();
        }
        private void questEditorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PacketSender.SendQuestEditor();
        }
        private void projectileEditorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PacketSender.SendProjectileEditor();
        }
        //Help
        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            frmAbout aboutfrm = new frmAbout();
            aboutfrm.ShowDialog();
        }

        //ToolStrip Functions
        private void toolStripBtnNewMap_Click(object sender, EventArgs e)
        {
            newMapToolStripMenuItem_Click(null, null);
        }
        private void toolStripBtnSaveMap_Click(object sender, EventArgs e)
        {
            saveMapToolStripMenuItem_Click(null, null);
        }
        private void toolStripBtnUndo_Click(object sender, EventArgs e)
        {
            var tmpMap = Globals.GameMaps[Globals.CurrentMap];
            if (Globals.MapEditorWindow.MapUndoStates.Count > 0)
            {
                tmpMap.Load(Globals.MapEditorWindow.MapUndoStates[Globals.MapEditorWindow.MapUndoStates.Count - 1]);
                Globals.MapEditorWindow.MapRedoStates.Add(Globals.MapEditorWindow.CurrentMapState);
                Globals.MapEditorWindow.CurrentMapState = Globals.MapEditorWindow.MapUndoStates[Globals.MapEditorWindow.MapUndoStates.Count - 1];
                Globals.MapEditorWindow.MapUndoStates.RemoveAt(Globals.MapEditorWindow.MapUndoStates.Count - 1);
                Globals.MapPropertiesWindow.Update();
                Graphics.TilePreviewUpdated = true;
            }
        }
        private void toolStripBtnRedo_Click(object sender, EventArgs e)
        {
            var tmpMap = Globals.GameMaps[Globals.CurrentMap];
            if (Globals.MapEditorWindow.MapRedoStates.Count > 0)
            {
                tmpMap.Load(Globals.MapEditorWindow.MapRedoStates[Globals.MapEditorWindow.MapRedoStates.Count - 1]);
                Globals.MapEditorWindow.MapUndoStates.Add(Globals.MapEditorWindow.CurrentMapState);
                Globals.MapEditorWindow.CurrentMapState = Globals.MapEditorWindow.MapRedoStates[Globals.MapEditorWindow.MapRedoStates.Count - 1];
                Globals.MapEditorWindow.MapRedoStates.RemoveAt(Globals.MapEditorWindow.MapRedoStates.Count - 1);
                Globals.MapPropertiesWindow.Update();
                Graphics.TilePreviewUpdated = true;
            }
        }
        private void toolStripBtnFill_Click(object sender, EventArgs e)
        {
            if (Globals.CurrentLayer <= Constants.LayerCount)
            {
                Globals.MapEditorWindow.FillLayer();
            }
        }
        private void toolStripBtnErase_Click(object sender, EventArgs e)
        {
            if (Globals.CurrentLayer <= Constants.LayerCount)
            {
                Globals.MapEditorWindow.EraseLayer();
            }
        }
        private void toolStripBtnScreenshot_Click(object sender, EventArgs e)
        {
            SaveFileDialog fileDialog = new SaveFileDialog();
            fileDialog.Filter = "Png Image|*.png|JPeg Image|*.jpg|Bitmap Image|*.bmp|Gif Image|*.gif";
            fileDialog.Title = "Save a screenshot of the map";
            fileDialog.ShowDialog();

            if (fileDialog.FileName != "")
            {
                Graphics.ScreenShotMap().SaveToFile(fileDialog.FileName);
            }
        }
        private void toolStripBtnPen_Click(object sender, EventArgs e)
        {
            Globals.CurrentTool = (int)Enums.EdittingTool.Pen;
        }
        private void toolStripBtnSelect_Click(object sender, EventArgs e)
        {
            Globals.CurrentTool = (int)Enums.EdittingTool.Selection;
        }
        private void toolStripBtnRect_Click(object sender, EventArgs e)
        {
            Globals.CurrentTool = (int)Enums.EdittingTool.Rectangle;
            Globals.CurMapSelX = -1;
            Globals.CurMapSelY = -1;
        }
        private void toolStripBtnEyeDrop_Click(object sender, EventArgs e)
        {
            Globals.CurrentTool = (int)Enums.EdittingTool.Droppler;
            Globals.CurMapSelX = -1;
            Globals.CurMapSelY = -1;
        }
        private void toolStripBtnCopy_Click(object sender, EventArgs e)
        {
            if (Globals.CurrentTool != (int)Enums.EdittingTool.Selection) { return; }
            Globals.MapEditorWindow.Copy();
        }
        private void toolStripBtnPaste_Click(object sender, EventArgs e)
        {
            if (!Globals.HasCopy) { return; }
            Globals.MapEditorWindow.Paste();
        }
        private void toolStripBtnCut_Click(object sender, EventArgs e)
        {
            if (Globals.CurrentTool != (int)Enums.EdittingTool.Selection) { return; }
            Globals.MapEditorWindow.Cut();
        }

        //Cross Threading Delegate Methods
        private void TryOpenEditorMethod(int editorIndex)
        {
            if (Globals.CurrentEditor == -1)
            {
                switch (editorIndex)
                {
                    case (int)Enums.EditorTypes.Animation:
                        if (_animationEditor == null || _animationEditor.Visible == false)
                        {
                            _animationEditor = new frmAnimation();
                            _animationEditor.InitEditor();
                            _animationEditor.Show();
                        }
                        break;
                    case (int)Enums.EditorTypes.Item:
                        if (_itemEditor == null || _itemEditor.Visible == false)
                        {
                            _itemEditor = new FrmItem();
                            _itemEditor.InitEditor();
                            _itemEditor.Show();
                        }
                        break;
                    case (int)Enums.EditorTypes.Npc:
                        if (_npcEditor == null || _npcEditor.Visible == false)
                        {
                            _npcEditor = new frmNpc();
                            _npcEditor.InitEditor();
                            _npcEditor.Show();
                        }
                        break;
                    case (int)Enums.EditorTypes.Resource:
                        if (_resourceEditor == null || _resourceEditor.Visible == false)
                        {
                            _resourceEditor = new frmResource();
                            _resourceEditor.InitEditor();
                            _resourceEditor.Show();
                        }
                        break;
                    case (int)Enums.EditorTypes.Spell:
                        if (_spellEditor == null || _spellEditor.Visible == false)
                        {
                            _spellEditor = new frmSpell();
                            _spellEditor.InitEditor();
                            _spellEditor.Show();
                        }
                        break;
                    case (int)Enums.EditorTypes.Class:
                        if (_classEditor == null || _classEditor.Visible == false)
                        {
                            _classEditor = new frmClass();
                            _classEditor.InitEditor();
                            _classEditor.Show();
                        }
                        break;
                    case (int)Enums.EditorTypes.Quest:
                        if (_questEditor == null || _questEditor.Visible == false)
                        {
                            _questEditor = new frmQuest();
                            _questEditor.InitEditor();
                            _questEditor.Show();
                        }
                        break;
                    case (int)Enums.EditorTypes.Projectile:
                        if (_projectileEditor == null || _projectileEditor.Visible == false)
                        {
                            _projectileEditor = new frmProjectile();
                            _projectileEditor.InitEditor();
                            _projectileEditor.Show();
                        }
                        break;
                    default:
                        return;
                }
                Globals.CurrentEditor = editorIndex;
            }

        }

    }
}