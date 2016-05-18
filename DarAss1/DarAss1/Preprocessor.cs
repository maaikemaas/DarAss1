﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.IO;

namespace DarAss1
{
    class Preprocessor
    {
        int k = 10;

        public Preprocessor(string mainQuery)       // Constructor for the preprocessor, takes the query the user fired as parameter
        {
            SQLiteConnection.CreateFile("MetaDB.sqlite");       // Create the Meta DB file
            SQLiteConnection meta_connection = new SQLiteConnection("Data Source=MetaDB.sqlite;Version=3;");
            meta_connection.Open();

            string buildMetaDB = new StreamReader("buildMetaDBtable.txt").ReadToEnd();      // Read from the text file with the CREATE TABLE statements
            SQLiteCommand cmd = new SQLiteCommand(buildMetaDB, meta_connection);
            cmd.ExecuteNonQuery();                                                          // Execute the command and fill the MetaDB.sqlite file with 1 table

            QFSimilarity(mainQuery, meta_connection);                                       // Calculate all QF Similarity values, and fills the MetaDB                              
        }

        public void QFSimilarity(string query, SQLiteConnection meta_connection)      // Updates the metaDB with the QF similarity between the query the user fired and every tuple in the main DB
        {
            // First we calculate the RQF for every term in the user's query
            List<string> terms = new List<string>();
            Dictionary<string, int> RQF = new Dictionary<string, int>();

            terms.Add("id"); terms.Add("mpg"); terms.Add("cylinders"); terms.Add("displacement"); terms.Add("horsepower"); terms.Add("weight");    

            StreamReader workload = new StreamReader("workload.txt");               // Read the workload
            string line1 = workload.ReadLine();
            string[] line2 = workload.ReadLine().Split();
            int uniqueQueries = Int32.Parse(line2[0]);                              // No. of unique queries in the workload


            for (int i = 0; i < uniqueQueries; i++)
            {
                string[] wQuery = workload.ReadLine().Split();
                int multiplier = Int32.Parse(wQuery[0]);

                foreach(string t in terms)
                    if (wQuery.Contains(t)) RQF[t] += multiplier;
            }

            Console.ReadKey();
        }
    }
}