﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.IO;
using System.Windows.Forms;
using System.Drawing;


namespace DarAss1
{
    class Program
    {
        static void Main()
        {
            //make connection to database
            SQLiteConnection.CreateFile("mainDB.sqlite");
            SQLiteConnection m_dbConnection;
            m_dbConnection = new SQLiteConnection("Data Source=mainDB.sqlite;Version=3;");
            m_dbConnection.Open();

            //input file with sql things
            string input = new StreamReader("autotable.txt").ReadToEnd();

            //create table in sql with input text
            SQLiteCommand command = new SQLiteCommand(input, m_dbConnection);
            command.ExecuteNonQuery();

            Preprocessor p = new Preprocessor(m_dbConnection);

            //p.createMetaDB(p.dbconnect, p.metadbconnect);
            //p.metaLoadFill(p.dbconnect, p.metadbconnect);
           
            //string test_query = "k = 6, brand = 'volkswagen', cylinders = '6', mpg = '45';";

            GUIclass scherm;
            scherm = new GUIclass(p);
            Application.Run(scherm);
            
            Console.ReadKey();
        }

    }
}
