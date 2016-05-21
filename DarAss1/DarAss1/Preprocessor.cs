using System;
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
        public Preprocessor()       // Constructor for the preprocessor, takes the query the user fired as parameter
        {
            SQLiteConnection.CreateFile("MetaDB.sqlite");       // Create the Meta DB file
            SQLiteConnection meta_connection = new SQLiteConnection("Data Source=MetaDB.sqlite;Version=3;");
            meta_connection.Open();

            string buildMetaDB = new StreamReader("buildMetaDBtable.txt").ReadToEnd();      // Read from the text file with the CREATE TABLE statements
            SQLiteCommand cmd = new SQLiteCommand(buildMetaDB, meta_connection);
            cmd.ExecuteNonQuery();                                                          // Execute the command and fill the MetaDB.sqlite file with 1 table
            
            QFSimilarity(meta_connection);                                       // Calculate all QF Similarity values, and fills the MetaDB                              
        }

        public void QFSimilarity(SQLiteConnection meta_connection)      // Updates the metaDB with the QF similarity between the query the user fired and every tuple in the main DB
        {
            // First we calculate the RQF for every term in the user's query

            // WIP: Dit werkt alleen als de zoekwaardes al in de MetaDB ge-inserted zijn!! Ook werkt het nog niet met zoekwaardes met 2 woorden (zoals "skylark 320", en Real numbers)

            StreamReader workload = new StreamReader("workload.txt");               // Read the workload
            string line1 = workload.ReadLine();
            string[] line2 = workload.ReadLine().Split();
            int uniqueQueries = Int32.Parse(line2[0]);                              // No. of unique queries in the workload

            for (int i = 0; i < uniqueQueries; i++)                                 // Loop over alle unieke queries in de workload heen
            {
                string[] wQuery = workload.ReadLine().Split();
                int multiplier = Int32.Parse(wQuery[0]);                            // Eerste getal van een query in de workload, hoevaak hij voorkomt
                bool nextWordIsAttr = false;    
                bool nextWordIsVal = false;
                bool nextWordInClause = false;
                string currentAttr = "";
                string currentVal = "";
                string inClauseString = "";
                int incount = 0;
                for(int j = 1; j < wQuery.Length; j++)
                {
                    string word = wQuery[j];                         // Huidige woord in de regel

                    if (nextWordIsAttr) currentAttr = word; nextWordIsAttr = false; // Dit woord is een attribuutnaam
                    if (nextWordIsVal)                                              // Dit woord is een zoekwaarde
                    { 
                        currentVal += " " + word; 
                        if(word.EndsWith("'"))                // Voor zoekwaarden met meerdere woorden, blijf de currentVal uitbreiden tot de ', dan stopt de waarde
                        {
                            nextWordIsVal = false;
                            string upd = "UPDATE " + currentAttr + " SET qf = qf + " + multiplier + " WHERE value = " + currentVal.Trim('(', ')') + ";";
                            SQLiteCommand cmd = new SQLiteCommand(upd, meta_connection);
                            cmd.ExecuteNonQuery();
                            currentVal = "";
                        }
                    }
                    if(nextWordInClause)
                    {
                        string tempword = String.Copy(word);
                        inClauseString += " " + tempword.Trim('(', ',', '\'', ' ');
                        if (tempword.EndsWith(")"))
                        {
                            nextWordInClause = false;
                            nextWordIsVal = false;
                            string updjstr = "INSERT INTO jacc VALUES (" + incount + ", '" + inClauseString.Trim(')', '\'') + "', " + multiplier + ");";
                            SQLiteCommand updj = new SQLiteCommand(updjstr, meta_connection);
                            updj.ExecuteNonQuery();
                            inClauseString = "";
                            currentVal = "";
                            incount++;
                        }
                    }

                    if (word == "WHERE") nextWordIsAttr = true;         // Deze statements bepalen of het volgende woord in de loop een attribuutnaam danwel zoekwaarde is
                    if (word == "AND") nextWordIsAttr = true;
                    if (word == "=") nextWordIsVal = true;
                    if (word == ",") nextWordIsVal = true;
                    if (word == "IN") { nextWordIsVal = true; nextWordInClause = true; }
                }
            }
        }
    }
}
