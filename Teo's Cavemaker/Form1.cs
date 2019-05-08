using System;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Threading.Tasks;

namespace Teo_s_Cavemaker
{
    public partial class Form1 : Form
    {
        public static string CaveName { get; set; }
        public static string Terrain { get; set; }
        public static string Water { get; set; }
        public static int CaveWidth { get; set; }
        public static int CaveHeight { get; set; }
        public static int TileSize { get; set; }
        public static bool CleanUpNeeded { get; set; }
        public static bool MouseIsDown { get; set; }
        public static (int, int) UpperLeftCorner { get; set; }
        public static (int, int) BottomRightCorner { get; set; }
        public static List<CaveTile> SelectedTiles { get; set; }
        public static CaveLayoutPanel clp { get; set; }
        public enum DragOrientation { None, SE, SW, NW, NE }
        public static DragOrientation CurrentOrientation { get; set; }
        public static Dictionary<string, char> TilesDictionary;
        public static TableLayoutPanel SelectedRow;

        public class CaveLayoutPanel : TableLayoutPanel
        {
            public CaveLayoutPanel()
            {
                DoubleBuffered = true;
                SelectedTiles = new List<CaveTile>();
            }
        }

        public class CaveTile : Panel
        {
            public string TileName { get; set; }
            public Bitmap CaveBitmap { get; set; }
            public bool Selected { get; set; }

            public CaveTile()
            {
                Margin = new Padding(0);
                AllowDrop = true;
                DoubleBuffered = true;
                Selected = false;
                TileName = "Black Tile";
                CaveBitmap = new Bitmap("Resources/Tiles/Black Tile.png");
            }

            public CaveTile(CaveTile ct)
            {
                Margin = new Padding(0);
                AllowDrop = true;
                DoubleBuffered = true;
                Selected = ct.Selected;
                CaveBitmap = ct.CaveBitmap;
                Name = ct.Name;
                Tag = ct.Tag;
                TileName = ct.TileName;
            }

            public char CharAlias()
            {
                return TilesDictionary[TileName];
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                Graphics g = e.Graphics;
                g.DrawImage(CaveBitmap, new Rectangle(0, 0, TileSize, TileSize));
            }

            protected override void OnDragDrop(DragEventArgs drgevent)
            {
                MouseIsDown = false;
                CleanUpNeeded = true;
            }

            protected override void OnMouseDown(MouseEventArgs e)
            {
                UpperLeftCorner = ((int, int))Tag;
                SelectTiles();
                DoDragDrop(-1, DragDropEffects.None);
            }

            protected void SelectTiles()
            {
                MouseIsDown = true;
                if (CleanUpNeeded)
                {
                    ApplyNegative(SelectedTiles);
                    CleanUpNeeded = false;
                    foreach (CaveTile ct in SelectedTiles)
                        ct.Selected = false;
                    SelectedTiles = new List<CaveTile>();
                }
                (int, int) CurrentCorner = ((int, int))Tag;
                bool Augment;
                int x1 = CurrentCorner.Item1, x2 = UpperLeftCorner.Item1, y1 = CurrentCorner.Item2, y2 = UpperLeftCorner.Item2, x3 = BottomRightCorner.Item1, y3 = BottomRightCorner.Item2;
                if (x2 == x3 && y2 == y3)
                    CurrentOrientation = DragOrientation.None;
                if ((x1 > x2 && y1 >= y2) || ((x1 == x2 || y1 == y2) && CurrentOrientation == DragOrientation.SE))
                {
                    CurrentOrientation = DragOrientation.SE;
                    Augment = x1 >= x3 && y1 >= y3;
                }
                else if ((x1 <= x2 && y1 > y2) || ((x1 == x2 || y1 == y2) && CurrentOrientation == DragOrientation.SW))
                {
                    CurrentOrientation = DragOrientation.SW;
                    Augment = x1 <= x3 && y1 >= y3;
                }
                else if ((x1 > x2 && y1 <= y2) || ((x1 == x2 || y1 == y2) && CurrentOrientation == DragOrientation.NE))
                {
                    CurrentOrientation = DragOrientation.NE;
                    Augment = x1 >= x3 && y1 <= y3;
                }
                else
                {
                    CurrentOrientation = DragOrientation.NW;
                    Augment = x1 <= x3 && y1 <= y3;
                }
                List<CaveTile> Tiles = GetMissingTiles(CurrentCorner, CurrentOrientation, Augment);
                if (Augment)
                    SelectedTiles.AddRange(Tiles);
                else
                    foreach (CaveTile ct in Tiles)
                        SelectedTiles.Remove(ct);
                ApplyNegative(Tiles);
            }

            protected override void OnDragEnter(DragEventArgs drgevent)
            {
                drgevent.Effect = DragDropEffects.All;
                base.OnDragEnter(drgevent);
                if (MouseIsDown)
                    SelectTiles();
            }

            protected override void OnGiveFeedback(GiveFeedbackEventArgs gfbevent)
            {
                base.OnGiveFeedback(gfbevent);
                gfbevent.UseDefaultCursors = false;
            }
        }

        protected static List<CaveTile> GetMissingTiles((int, int) cc, DragOrientation dro, bool a)
        {
            List<CaveTile> Tiles = a ? new List<CaveTile>() : new List<CaveTile>(SelectedTiles);
            bool horizontal = dro == DragOrientation.NE || dro == DragOrientation.SE;
            int IncH = horizontal ? 1 : -1;
            bool vertical = dro == DragOrientation.SE || dro == DragOrientation.SW;
            int IncV = vertical ? 1 : -1;
            for (int i = UpperLeftCorner.Item1; horizontal ? i <= cc.Item1 : i >= cc.Item1; i += IncH)
                for (int j = UpperLeftCorner.Item2; vertical ? j <= cc.Item2 : j >= cc.Item2; j += IncV)
                {
                    CaveTile current = clp.Controls[i.ToString() + "," + j.ToString()] as CaveTile;
                    if (a)
                    {
                        if (SelectedTiles.IndexOf(current) == -1)
                            Tiles.Add(current);
                    }
                    else
                        Tiles.Remove(current);
                }
            foreach (var ct in Tiles)
                ct.Selected = a;
            BottomRightCorner = cc;
            return Tiles;
        }

        protected static void ApplyNegative(List<CaveTile> ctl)
        {
            foreach (CaveTile ct in ctl)
            {
                Bitmap b = new Bitmap(ct.CaveBitmap);
                for (int i = 0; i < b.Width; ++i)
                    for (int j = 0; j < b.Height; ++j)
                    {
                        Color c = b.GetPixel(i, j);
                        b.SetPixel(i, j, Color.FromArgb(255 - c.R, 255 - c.G, 255 - c.B));
                    }
                ct.CaveBitmap = b;
                ct.Invalidate();
            }
        }

        private void MoveTilesToTheCave(object sender, EventArgs e)
        {
            TableLayoutPanel row = sender as TableLayoutPanel;
            MoveTiles(row);
        }

        private void ParentMoveTile(object sender, EventArgs e)
        {
            Control c = null;
            if (sender is PictureBox)
                c = sender as PictureBox;
            else if (sender is Label)
                c = sender as Label;
            TableLayoutPanel row = c.Parent as TableLayoutPanel;
            MoveTiles(row);
        }

        private void MoveTiles(TableLayoutPanel row)
        {
            string TileName = row.Tag.ToString();
            foreach (CaveTile SelectedTile in SelectedTiles)
            {
                SelectedTile.CaveBitmap = new Bitmap(TileName);
                SelectedTile.TileName = row.Name.Split('.').First();
            }
            ApplyNegative(SelectedTiles);
        }

        public void CreateCavePanel(bool ResizeMode = false)
        {
            CaveLayoutPanel clpNew = new CaveLayoutPanel()
            {
                RowCount = CaveHeight,
                ColumnCount = CaveWidth,
                AutoSize = true,
                AllowDrop = true,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.Single
            };
            List<CaveTile> ToInvert = new List<CaveTile>();

            for (int i = 0; i < CaveWidth; ++i)
                clpNew.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, TileSize));
            for (int i = 0; i < CaveHeight; ++i)
                clpNew.RowStyles.Add(new RowStyle(SizeType.Absolute, TileSize));

            CaveTile[,] array2d = new CaveTile[CaveWidth, CaveHeight];
            Parallel.For(0, CaveWidth, i =>
            {
                Parallel.For(0, CaveHeight, j =>
               {
                   CaveTile ct;
                   if (!ResizeMode || i >= clp.ColumnCount || j >= clp.RowCount)
                   {
                       ct = new CaveTile()
                       {
                           Tag = (i, j),
                           Name = i.ToString() + "," + j.ToString()
                       };
                   }
                   else
                   {
                       ct = new CaveTile(clp.Controls[i.ToString() + "," + j.ToString()] as CaveTile);
                       if (ct.Selected)
                       {
                           ct.Selected = false;
                           ToInvert.Add(ct);
                       }
                   }
                   array2d[i, j] = ct;
               });
            });
            
            for (int i = 0; i < CaveWidth; ++i)
                for (int j = 0; j < CaveHeight; ++j)
                    clpNew.Controls.Add(array2d[i, j], i, j);

            if (ResizeMode)
                panel1.Controls.Remove(clp);
            panel1.Controls.Add(clpNew);
            clp = clpNew;
            if (ResizeMode)
            {
                ApplyNegative(ToInvert);
                SelectedTiles = new List<CaveTile>();
            }
        }

        private void PrepareDictionary()
        {
            TilesDictionary = new Dictionary<string, char>
            {
                { "Wooden Crate", 'b' },
                { "Steel Crate", 'k' },
                { "Black Tile", ' ' },
                { "Terrain", 'x' },
                { "Boulder", 'o' },
                { "Door", 'D' },
                { "Gem", 'g' },
                { "Hannah", '#' },
                { "Left Arrow", '<' },
                { "Middle Platform", '=' },
                { "Right Arrow", '>' },
                { "Stalactite", 'w' },
                { "Stalagmite", 'm' },
                { "Steel Dynamite", '|' },
                { "Treasure", '+' },
                { "Wooden Dynamite", '/' }
            };
        }

        public Form1()
        {
            InitializeComponent();
            CaveWidth = 15;
            CaveHeight = 10;
            TileSize = 35;
            CaveName = "New_cave";
            Terrain = "1";
            Water = "clear";
            clp = null;
            PrepareDictionary();
            CreateCavePanel();

            DirectoryInfo di = new DirectoryInfo("Resources/Tiles");
            var tiles = di.GetFiles();

            TableLayoutPanel tilesList = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 1,
                RowCount = tiles.Count()
            };
            panel2.Controls.Add(tilesList);

            for (int i = 0; i < tiles.Count(); ++i)
            {
                FileInfo fi = tiles[i];
                TableLayoutPanel row = new TableLayoutPanel
                {
                    AutoSize = true,
                    ColumnCount = 2,
                    RowCount = 1,
                    Tag = fi.FullName,
                    Name = fi.Name
                };
                row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
                row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70F));
                row.Click += Focus;
                row.Enter += EnterFocus;
                row.Leave += LeaveFocus;
                row.MouseEnter += HoverColor;
                row.MouseLeave += LeaveColor;
                row.DoubleClick += MoveTilesToTheCave;

                PictureBox pb = new PictureBox()
                {
                    ImageLocation = fi.FullName,
                    Height = 35,
                    Width = 35,
                    SizeMode = PictureBoxSizeMode.StretchImage
                };
                pb.MouseEnter += HoverRedirect;
                pb.MouseLeave += LeaveRedirect;
                pb.Click += ParentFocus;
                pb.DoubleClick += ParentMoveTile;
                row.Controls.Add(pb, 0, 0);

                Label l = new Label()
                {
                    Text = fi.Name.Split('.').First(),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Dock = DockStyle.Fill
                };
                l.MouseEnter += HoverRedirect;
                l.MouseLeave += LeaveRedirect;
                l.Click += ParentFocus;
                l.DoubleClick += ParentMoveTile;
                row.Controls.Add(l, 1, 0);
                row.RowStyles.Add(new RowStyle(SizeType.AutoSize));

                tilesList.Controls.Add(row, 0, i);
                tilesList.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            }

            tilesList.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        }

        private void HoverColor(object sender, EventArgs e)
        {
            TableLayoutPanel tlp = sender as TableLayoutPanel;
            if (tlp.BackColor == SystemColors.Control)
                tlp.BackColor = SystemColors.ActiveCaption;
        }

        private void LeaveColor(object sender, EventArgs e)
        {
            TableLayoutPanel tlp = sender as TableLayoutPanel;
            if (tlp.BackColor == SystemColors.ActiveCaption)
                tlp.BackColor = SystemColors.Control;
        }

        private void HoverRedirect(object sender, EventArgs e)
        {
            Control c = null;
            if (sender is PictureBox)
                c = (PictureBox)sender;
            else if (sender is Label)
                c = (Label)sender;
            TableLayoutPanel tlp = c.Parent as TableLayoutPanel;
            if (tlp.BackColor == SystemColors.Control)
                tlp.BackColor = SystemColors.ActiveCaption;
        }

        private void LeaveRedirect(object sender, EventArgs e)
        {
            Control c = null;
            if (sender is PictureBox)
                c = (PictureBox)sender;
            else if (sender is Label)
                c = (Label)sender;
            TableLayoutPanel tlp = c.Parent as TableLayoutPanel;
            if (tlp.BackColor == SystemColors.ActiveCaption)
                tlp.BackColor = SystemColors.Control;
        }

        private void EnterFocus(object sender, EventArgs e)
        {
            TableLayoutPanel tlp = sender as TableLayoutPanel;
            if (!(tlp is null))
            {
                tlp.BackColor = SystemColors.MenuHighlight;
                SelectedRow = tlp;
            }
        }

        private void LeaveFocus(object sender, EventArgs e)
        {
            TableLayoutPanel tlp = sender as TableLayoutPanel;
            if (!(tlp is null))
                tlp.BackColor = SystemColors.Control;
        }

        private void ParentFocus(object sender, EventArgs e)
        {
            Control c = null;
            if (sender is PictureBox)
                c = (PictureBox)sender;
            else if (sender is Label)
                c = (Label)sender;
            TableLayoutPanel tlp = c.Parent as TableLayoutPanel;
            tlp.Focus();
        }

        private void Focus(object sender, EventArgs e)
        {
            Control c = sender as Control;
            c.Focus();
        }

        private void ChangeCaveOptions(object sender, EventArgs e)
        {
            Form2 options = new Form2(this);
            options.ShowDialog();
        }

        private void CaveSaveAs(object sender, EventArgs e)
        {
            SaveFileDialog save = new SaveFileDialog
            {
                FileName = CaveName + ".txt",
                Filter = "Text File | *.txt"
            };
            if (save.ShowDialog() == DialogResult.OK)
            {
                StreamWriter write = new StreamWriter(save.OpenFile());
                write.WriteLine(CaveName);
                write.WriteLine("terrain " + Terrain);
                write.WriteLine("background 1");
                write.Write("water " + Water);
                for (int i = 0; i < CaveHeight; ++i)
                {
                    write.WriteLine();
                    for (int j = 0; j < CaveWidth; ++j)
                        write.Write((clp.Controls[j.ToString() + "," + i.ToString()] as CaveTile).CharAlias());
                }
                write.Close();
            }
        }

        private void HandleInsertDelete(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Insert:
                    MoveTiles(SelectedRow);
                    break;
                case Keys.Delete:
                    foreach (CaveTile SelectedTile in SelectedTiles)
                    {
                        SelectedTile.CaveBitmap = new Bitmap("Resoures/tiles/Black Tile.png");
                        SelectedTile.TileName = "Black Tile";
                    }
                    ApplyNegative(SelectedTiles);
                    break;
            }
        }
    }
}
