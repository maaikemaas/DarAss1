using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.IO;

namespace DarAss1
{
    class Program
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

            Preprocessor p = new Preprocessor();

            SQLiteConnection meta_dbConnection = new SQLiteConnection("Data Source=MetaDB.sqlite;Version=3");
            meta_dbConnection.Open();

            string testQuery = "k = 6, brand = 'volkswagen', cylinders = 6, mpg = 39";
            //topk(testQuery, m_dbConnection, meta_dbConnection);

            topk(testQuery, m_dbConnection, meta_dbConnection);

            Console.ReadKey();
        }

        static void topk(string mainQuery, SQLiteConnection MainDBConnection, SQLiteConnection MetaDBConnection)
        {
            SQLiteCommand findTotal = new SQLiteCommand("SELECT COUNT(*) FROM autompg", MainDBConnection);
            int totalReader = Convert.ToInt32(findTotal.ExecuteScalar());

            int k = 10;
            List<string> attr = new List<string>();
            List<string> vals = new List<string>();
            string[] querywords = mainQuery.Split();
            bool nextWordIsAttr = true;
            bool nextWordIsVal = false;
            int startLoop = 0;
            if(querywords[0] == "k")
            {
                k = Int32.Parse(querywords[2].Trim(','));
                startLoop = 3;
            }
            for(int i = startLoop; i < querywords.Length; i++)
            {
                string word = querywords[i];
                if (nextWordIsAttr) attr.Add(word); nextWordIsAttr = false;
                if (nextWordIsVal) vals.Add(word); nextWordIsVal = false;

                if (word == "=") nextWordIsVal = true;
                if (word.EndsWith(",")) nextWordIsAttr = true;
            }

            string createAuxTable = "CREATE TABLE aux ( tupid integer NOT NULL, sim real NOT NULL, PRIMARY KEY(tupid) );";
            SQLiteCommand cmd = new SQLiteCommand(createAuxTable, MainDBConnection);
            cmd.ExecuteNonQuery();

            for (int i = 1; i <= totalReader; i++)
            {
                int similarity = 0;
                for(int at = 0; at < attr.Count; at++)         
                {
                    SQLiteCommand RetrieveVal = new SQLiteCommand("SELECT " + attr[at] + " FROM autompg WHERE id = " + i, MainDBConnection);
                    var tupleValue = RetrieveVal.ExecuteScalar();
                    SQLiteCommand RetrieveRQF = new SQLiteCommand("SELECT qf FROM " + attr[at] + " WHERE value = " + tupleValue, MetaDBConnection);
                    int rqfTuple = Convert.ToInt32(RetrieveRQF.ExecuteScalar());
                    SQLiteCommand RetrieveRQFQuery = new SQLiteCommand("SELECT qf FROM " + attr[at] + " WHERE value = " + vals[at], MetaDBConnection);
                    int rqfQuery = Convert.ToInt32(RetrieveRQF.ExecuteScalar());
                    SQLiteCommand RetrieveRQFMax = new SQLiteCommand("SELECT MAX(qf) FROM " + attr[at], MetaDBConnection);
                    int rqfMax = Convert.ToInt32(RetrieveRQFMax.ExecuteScalar());
                    float qf = (float)rqfQuery / (float)rqfMax;

                    //WIP Jaccard Coëfficient: Intersection and Union
                    /*
                    SQLiteCommand QuerySetTupleCmd = new SQLiteCommand("SELECT queryid FROM jacc WHERE terms LIKE " + '%' + tupleValue + '%', MetaDBConnection);
                    SQLiteDataReader qstReader = QuerySetTupleCmd.ExecuteReader();
                    List<int> queries = new List<int>();
                    while(qstReader.Read())
                    {
                        string queryset = qstReader.GetString(1);
                        int querysetFreq = qstReader.GetInt32(2);
                    }
                    */
                }
                SQLiteCommand UpdateSim = new SQLiteCommand("INSERT INTO aux VALUES (" + i + ", " + similarity + ");");
            }



            string select_topk = "SELECT * FROM autompg GROUP BY ";
        }
    }
}
