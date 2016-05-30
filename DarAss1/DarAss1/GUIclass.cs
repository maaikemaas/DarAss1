using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DarAss1
{
    public partial class GUIclass : Form
    {
        private TextBox input;
        private Label output;
        private Button searchbutton, clearbutton, preprocessbutton, skipbutton;
        MainSearch proc;
        Preprocessor p;

        public GUIclass(Preprocessor p)
        {
            this.p = p;
            proc = new MainSearch(p);

            this.Text = "Ranking on query results";
            this.BackColor = Color.Beige;
            this.ClientSize = new Size(700, 600);
            input = new TextBox();
            output = new Label();
            preprocessbutton = new Button();
            searchbutton = new Button();
            clearbutton = new Button();
            skipbutton = new Button();

            input.Location = new Point(20, 20);
            input.Size = new Size(400, 150);
            input.Multiline = true;

            output.Location = new Point(20, 200);
            output.Size = new Size(450, 300);
            output.Text = "Type your query and click on the search button.";

            preprocessbutton.Location = new Point(550, 20);
            preprocessbutton.Size = new Size(100, 60);
            preprocessbutton.BackColor = Color.Crimson;
            preprocessbutton.Text = "preprocessing";
            preprocessbutton.Click += this.klik;

            skipbutton.Location = new Point(550, 100);
            skipbutton.Size = new Size(100, 60);
            skipbutton.BackColor = Color.Crimson;
            skipbutton.Text = "skip preprocessing with metaload";
            skipbutton.Click += this.klik;

            searchbutton.Location = new Point(550, 180);
            searchbutton.Size = new Size(100, 60);
            searchbutton.BackColor = Color.Crimson;
            searchbutton.Text = "rank";
            searchbutton.Click += this.klik;

            clearbutton.Location = new Point(550, 260);
            clearbutton.Size = new Size(100, 60);
            clearbutton.BackColor = Color.Crimson;
            clearbutton.Text = "clear field";
            clearbutton.Click += this.klik;


            this.Controls.Add(input);
            this.Controls.Add(output);
            this.Controls.Add(preprocessbutton);
            this.Controls.Add(searchbutton);
            this.Controls.Add(clearbutton);
            this.Controls.Add(skipbutton);

        }

        private void klik(object o, EventArgs ea)
        {
            ChooseButton(((Button)o).Text[0]);
            output.Text =  proc.GiveOutput;            
        }

        private void ChooseButton(char c)
        {
            if (c == 'r')
                proc.Search(input.Text);
            else if (c == 'c')
                Clear();
            else if (c == 's')
            {
                input.Text = "Using Metaload.txt to fill MetaDB..."; input.Refresh();
                p.metaLoadFill(p.metadbconnect);
                input.Text = "Done!";
                this.Controls.Remove(skipbutton);
                this.Controls.Remove(preprocessbutton);
                Label l = new Label();
                l.Text = "Preprocessing already done!";
                l.Location = new Point(550, 90);
                l.ForeColor = Color.Black;
                this.Controls.Add(l);
            }
            else
            {
                input.Text = "Preprocessing...";  input.Refresh();
                p.fillMetaDB(p.dbconnect, p.metadbconnect);
                input.Text = "Done!";
                this.Controls.Remove(preprocessbutton);
                this.Controls.Remove(skipbutton);
                Label l = new Label();
                l.Text = "Preprocessing already done!";
                l.Location = new Point(550, 90);
                l.ForeColor = Color.Black;
                this.Controls.Add(l);
            }              
        }

        private void Clear()
        {
            input.Text = "";    
            proc.Clear();
        }
    }
}
