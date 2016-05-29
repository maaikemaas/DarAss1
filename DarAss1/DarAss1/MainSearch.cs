using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;

namespace DarAss1
{
    public class MainSearch
    {
        private string outputText;
        public Preprocessor processor;

        //constructor method for mainsearch class
        public MainSearch(Preprocessor p)
        {
            this.processor = p;
        }

        //return outputfield
        public string GiveOutput
        {
            get { return outputText; }
        }

        //clear the outputfield
        public void Clear()
        {
            outputText = "";
        }


        //main searchmethod. Gets input and 'returns' the results
        public void Search(string input)
        {
            topk(input, processor.dbconnect, processor.metadbconnect);
            //put the result of the querying here
            outputText = "Dit is de input: " + input + ".";
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
            if (querywords[0] == "k")
            {
                k = Int32.Parse(querywords[2].Trim(','));
                startLoop = 3;
            }
            for (int i = startLoop; i < querywords.Length; i++)
            {
                string word = querywords[i];
                if (nextWordIsAttr) attr.Add(filterChars(word)); nextWordIsAttr = false;
                if (nextWordIsVal) vals.Add(filterChars(word)); nextWordIsVal = false;

                if (word == "=") nextWordIsVal = true;
                if (word.EndsWith(",")) nextWordIsAttr = true;
            }

            string createAuxTable = "CREATE TABLE aux ( tupid integer NOT NULL, sim real NOT NULL, PRIMARY KEY(tupid) );";
            SQLiteCommand cmd = new SQLiteCommand(createAuxTable, MainDBConnection);
            cmd.ExecuteNonQuery();

            for (int i = 1; i <= totalReader; i++)
            {
                float similarity = 0;
                for (int at = 0; at < attr.Count; at++)
                {
                    float qfSim = calcQFSimilarity(MainDBConnection, MetaDBConnection, i, at, attr, vals);
                    similarity += qfSim;
                }
                SQLiteCommand UpdateSim = new SQLiteCommand("INSERT INTO aux VALUES (" + i + ", " + similarity + ");");
            }



            string select_topk = "SELECT * FROM autompg GROUP BY ";
        }

        public static float calcQFSimilarity(SQLiteConnection MainDBConnection, SQLiteConnection MetaDBConnection, int i, int at, List<string> attr, List<string> vals)
        {
            SQLiteCommand RetrieveVal = new SQLiteCommand("SELECT " + attr[at] + " FROM autompg WHERE id = " + i, MainDBConnection);
            var tupleValue = RetrieveVal.ExecuteScalar();
            SQLiteCommand RetrieveRQF = new SQLiteCommand("SELECT qf FROM " + attr[at] + " WHERE value = '" + tupleValue + "'", MetaDBConnection);
            int rqfTuple = Convert.ToInt32(RetrieveRQF.ExecuteScalar());
            SQLiteCommand RetrieveRQFQuery = new SQLiteCommand("SELECT qf FROM " + attr[at] + " WHERE value = '" + vals[at] + "'", MetaDBConnection);
            int rqfQuery = Convert.ToInt32(RetrieveRQFQuery.ExecuteScalar());
            SQLiteCommand RetrieveRQFMax = new SQLiteCommand("SELECT MAX(qf) FROM " + attr[at], MetaDBConnection);
            int rqfMax = Convert.ToInt32(RetrieveRQFMax.ExecuteScalar());
            float qfQuery = (float)rqfQuery / (float)rqfMax;
            float qfTuple = (float)rqfTuple / (float)rqfMax;


            //Jaccard Coëfficient: Intersection and Union

            SQLiteCommand QuerySetTupleCmd = new SQLiteCommand("SELECT queryid, freq FROM jacc WHERE terms LIKE '%" + tupleValue + "%'", MetaDBConnection);
            SQLiteDataReader qstReader = QuerySetTupleCmd.ExecuteReader();
            List<Tuple<int, int>> queriesTuple = new List<Tuple<int, int>>();        // Item1 = id of query in workload, Item2 = frequency of that query
            while (qstReader.Read())
            {
                int queryId = qstReader.GetInt32(0);
                int querysetFreq = qstReader.GetInt32(1);
                queriesTuple.Add(new Tuple<int, int>(queryId, querysetFreq));
            }

            SQLiteCommand QuerySetUserCmd = new SQLiteCommand("SELECT queryid, freq FROM jacc WHERE terms LIKE '%" + vals[at] + "%'", MetaDBConnection);
            SQLiteDataReader qsuReader = QuerySetUserCmd.ExecuteReader();
            List<Tuple<int, int>> queriesUser = new List<Tuple<int, int>>();
            while (qsuReader.Read())
            {
                int queryId = qsuReader.GetInt32(0);
                int querysetFreq = qsuReader.GetInt32(1);
                queriesUser.Add(new Tuple<int, int>(queryId, querysetFreq));
            }
            List<Tuple<int, int>> intersection = queriesTuple.Intersect<Tuple<int, int>>(queriesUser, new TupleEqualityComparer()).ToList<Tuple<int, int>>();
            List<Tuple<int, int>> union = queriesTuple.Union<Tuple<int, int>>(queriesUser, new TupleEqualityComparer()).ToList<Tuple<int, int>>();
            int intersectionSize = 0;
            int unionSize = 0;
            foreach (Tuple<int, int> t in intersection) intersectionSize += t.Item2;
            foreach (Tuple<int, int> t in union) unionSize += t.Item2;

            float jaccardCoef = (float)intersectionSize / (float)unionSize;
            float finalQFSim = jaccardCoef * rqfQuery;
            return finalQFSim;
        }

        public static string filterChars(string input)
        {
            StringBuilder build = new StringBuilder();
            foreach (char c in input)
            {
                if (c != '(' && c != ')' && c != '\'' && c != '\'' && c != ',' && c != ';')
                {
                    build.Append(c);
                }
            }
            return build.ToString();
        }
    }
}
