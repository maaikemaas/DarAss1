using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.IO;

namespace DarAss1
{
    public class Program
    {
        static void Main(string[] args)
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

            string test_query = "k = 6, brand = 'volkswagen', cylinders = '6', mpg = '45';";
            Preprocessor p = new Preprocessor(test_query);
            
            Console.ReadKey();
        }

        public void IDFsim(SQLiteConnection db, string query)
        {
            string[] querystring = query.Split(" ");
            List<string> querylist = new List<string>();
            

            SQLiteCommand command = new SQLiteCommand("select ")
            //if attribute = categoric
            double idf = IDFcat();

            //SqlConnection sqlConnection1 = new SqlConnection("Your Connection String");
            //SqlCommand cmd = new SqlCommand();
            //SqlDataReader reader;

            //cmd.CommandText = "SELECT * FROM Customers";
            //cmd.CommandType = CommandType.Text;
            //cmd.Connection = sqlConnection1;

            //sqlConnection1.Open();

            //reader = cmd.ExecuteReader();
            //// Data is accessible through the DataReader object here.

            //sqlConnection1.Close();

            //if attribute = numeric, take another
            //or just make new table/column in database with categories
        }

        public double IDFcat()
        {
            return 1.0;
        }
    }
}
