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
        private Button searchbutton, clearbutton, preprocessbutton;
        MainSearch proc;
        Preprocessor p;

        public GUIclass(Preprocessor p)
        {
            this.p = p;
            proc = new MainSearch(p);

            this.Text = "Ranking on query results";
            this.BackColor = Color.Beige;
            this.ClientSize = new Size(600, 600);
            input = new TextBox();
            output = new Label();
            preprocessbutton = new Button();
            searchbutton = new Button();
            clearbutton = new Button();

            input.Location = new Point(20, 20);
            input.Size = new Size(400, 150);
            input.Multiline = true;

            output.Location = new Point(20, 200);
            output.Size = new Size(300, 80);
            output.Text = "Type your query and click on the search button.";

            preprocessbutton.Location = new Point(450, 20);
            preprocessbutton.Size = new Size(100, 60);
            preprocessbutton.BackColor = Color.Crimson;
            preprocessbutton.Text = "preprocessing";
            preprocessbutton.Click += this.klik;

            searchbutton.Location = new Point(450, 100);
            searchbutton.Size = new Size(100, 60);
            searchbutton.BackColor = Color.Crimson;
            searchbutton.Text = "search";
            searchbutton.Click += this.klik;

            clearbutton.Location = new Point(450, 180);
            clearbutton.Size = new Size(100, 60);
            clearbutton.BackColor = Color.Crimson;
            clearbutton.Text = "clear field";
            clearbutton.Click += this.klik;


            this.Controls.Add(input);
            this.Controls.Add(output);
            this.Controls.Add(preprocessbutton);
            this.Controls.Add(searchbutton);
            this.Controls.Add(clearbutton);

        }

        private void klik(object o, EventArgs ea)
        {
            ChooseButton(((Button)o).Text[0]);
            output.Text =  proc.GiveOutput;            
        }

        private void ChooseButton(char c)
        {
            if (c == 's')
                proc.Search(input.Text);
            else if (c == 'c')
                Clear();
            else
            {
                input.Text = "Preprocessing...";  input.Refresh();
                p.fillMetaDB(p.dbconnect, p.metadbconnect);
                input.Text = "Preprocessing done.";
            }              
        }

        private void Clear()
        {
            input.Text = "";    
            proc.Clear();
        }
    }
}
