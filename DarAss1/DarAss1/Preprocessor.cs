using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.IO;

namespace DarAss1
{
    public class Preprocessor
    {
        int k = 10;
        static public double countAutompg = 395;
        SQLiteConnection metadbconnect;

        // Constructor for the preprocessor, takes the query the user fired as parameter
        public Preprocessor(SQLiteConnection dbconnection)       
        {
            //makes sure doubles have a dot instead of a comma
            System.Globalization.CultureInfo dotProblem = (System.Globalization.CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
            dotProblem.NumberFormat.NumberDecimalSeparator = ".";
            System.Threading.Thread.CurrentThread.CurrentCulture = dotProblem;
            
            //create metaDB with tables
            SQLiteConnection meta_connection = createMetaDB();
            this.metadbconnect = meta_connection;

            //fill metaDB with IDF's
            IDFfill(dbconnection, meta_connection);
            //fill metaDB with QF's
            //QFSimilarity();    
        }

        // Updates the metaDB with the QF similarity between the query the user fired and every tuple in the main DB
        public void QFSimilarity(string query, SQLiteConnection meta_connection)      
        {
        }

        //fills the metadb with idf's
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
                        string input = "INSERT into " + attributeArray[t] + " VALUES('" + value + "', -1 , " + idf + ")";
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

        //create the metadb and the tables
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
