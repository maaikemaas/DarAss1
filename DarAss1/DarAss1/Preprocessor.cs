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
        int k = 10;
        public double countAutompg = 395;


        public Preprocessor(SQLiteConnection dbconnection)       // Constructor for the preprocessor, takes the query the user fired as parameter
        {
            System.Globalization.CultureInfo dotProblem = (System.Globalization.CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
            dotProblem.NumberFormat.NumberDecimalSeparator = ".";

            System.Threading.Thread.CurrentThread.CurrentCulture = dotProblem;
            
            //create metaDB with tables
            SQLiteConnection meta_connection = createMetaDB();
            //fill metaDB with IDF's
            IDFfill(dbconnection, meta_connection);
            //fill metaDB with QF's
            QFSimilarity(meta_connection);
        }

        public void QFSimilarity(SQLiteConnection meta_connection)      // Updates the metaDB with the QF similarity between the query the user fired and every tuple in the main DB
        {
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
                for (int j = 1; j < wQuery.Length; j++)
                {
                    string word = wQuery[j];                         // Huidige woord in de regel

                    if (nextWordIsAttr) currentAttr = word; nextWordIsAttr = false; // Dit woord is een attribuutnaam
                    if (nextWordIsVal)                                              // Dit woord is een zoekwaarde
                    {
                        currentVal += " " + word;
                        if (word.EndsWith("'"))                // Voor zoekwaarden met meerdere woorden, blijf de currentVal uitbreiden tot de ', dan stopt de waarde
                        {
                            nextWordIsVal = false;
                            string upd = "UPDATE " + currentAttr + " SET qf = qf + " + multiplier + " WHERE value = " + currentVal.Trim('(', ')') + ";";
                            SQLiteCommand cmd = new SQLiteCommand(upd, meta_connection);
                            cmd.ExecuteNonQuery();
                            currentVal = "";
                        }
                    }
                    if (nextWordInClause)
                    {
                        string tempword = String.Copy(word);
                        inClauseString = filterChars(tempword);
                        if (tempword.EndsWith(")"))
                        {
                            nextWordInClause = false;
                            nextWordIsVal = false;
                            string updjstr = "INSERT INTO jacc VALUES (" + i + ", '" + inClauseString.Trim(')', '\'') + "', " + multiplier + ");";
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

        public string filterChars(string input)
        {
            StringBuilder build = new StringBuilder();
            foreach(char c in input)
            {
                if(c != '(' && c != ')' && c != '\'' && c != '\'')
                {
                    if (c == ',') build.Append(' ');
                    else build.Append(c);
                }
            }
            return build.ToString();
        }

        public void IDFfill(SQLiteConnection connectionDB, SQLiteConnection connectionMetaDB)
        {
            string[] attributeArray = new string[11] { "mpg", "cylinders", "displacement", "horsepower", "weight", "acceleration", "model_year", "origin", "brand", "model", "type" };
            
            //doorloop alle attribuut tabellen
            for(int t=0; t<11;t++)
            {
                //select alle distinct values of the attribute
                SQLiteCommand command = new SQLiteCommand("SELECT DISTINCT " + attributeArray[t] + " from autompg",connectionDB);

                SQLiteDataReader reader = command.ExecuteReader();
                //read every row and add values to the attribute's table
                while(reader.Read())
                    {
                    double idf;
                    string value;

                    try
                    {
                        value = reader.GetString(0);
                    }
                    catch
                    {
                        double doubleValue = reader.GetDouble(0);
                        value = doubleValue.ToString();
                    }


                        idf = calcIDF(attributeArray[t], value, connectionDB);
                        //insert row into attribute table (insert: value,qf,idf)
                        //Console.WriteLine("value: " + value + " table: " + attributeArray[t] + "idf: " + idf);
                        string input = "INSERT into " + attributeArray[t] + " VALUES('" + value + "', -1 , " + idf + ")";
                        //Console.WriteLine(input);
                        //Console.ReadKey();
                        SQLiteCommand command2 = new SQLiteCommand(input, connectionMetaDB);
                        command2.ExecuteNonQuery();
                }
            }
        }

        //for calculating idf for categoric values
        public double calcIDF(string column, string term, SQLiteConnection connectionDB)
        {
            SQLiteCommand command = new SQLiteCommand("SELECT COUNT (*) FROM autompg WHERE " + column + " = '" + term + "'", connectionDB);
            double dF = Convert.ToInt32(command.ExecuteScalar());

            double result = Math.Log((countAutompg/dF), 2);
            return result;
        }


        public SQLiteConnection createMetaDB()
        {
            //make connection to database
            SQLiteConnection.CreateFile("metaDB.sqlite");
            SQLiteConnection metadbConnection;
            metadbConnection = new SQLiteConnection("Data Source=metaDB.sqlite;Version=3;");
            metadbConnection.Open();

            //input file with sql things
            string input = new StreamReader("create_metadb.txt").ReadToEnd();

            //create table in sql with input text
            SQLiteCommand command = new SQLiteCommand(input, metadbConnection);
            command.ExecuteNonQuery();

            return metadbConnection;
        }
    }
}
