using System;
using System.Windows.Forms;

namespace Teo_s_Cavemaker
{
    public partial class Form2 : Form
    {
        public Form1 Cavemaker { get; set; }

        public Form2(Form1 Cavemaker)
        {
            InitializeComponent();
            this.Cavemaker = Cavemaker;
            widthBox.Value = Form1.CaveWidth;
            heightBox.Value = Form1.CaveHeight;
            caveNameBox.Text = Form1.CaveName;
            terrainBox.Text = Form1.Terrain;
            waterBox.Text = Form1.Water;
        }

        private void ApplyCaveChanges(object sender, EventArgs e)
        {
            Form1.CaveHeight = (int)heightBox.Value;
            Form1.CaveWidth = (int)widthBox.Value;
            Cavemaker.CreateCavePanel(true);
            Form1.CaveName = caveNameBox.Text;
            Form1.Terrain = terrainBox.Text;
            Form1.Water = waterBox.Text;
            Close();
        }
    }
}
