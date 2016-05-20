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
            //QFSimilarity();    
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

                foreach (string t in terms)
                    if (wQuery.Contains(t)) RQF[t] += multiplier;
            }

            Console.ReadKey();
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
