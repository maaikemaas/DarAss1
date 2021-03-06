﻿using System;
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
        public List<string> mainAttributes = new List<string>()        // List which contains all attribute names of autompg
        {"mpg","cylinders","displacement","horsepower","weight","acceleration","model_year","origin","brand","model","type"};

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
            //put the result of the querying here
            outputText = topk(input, processor.dbconnect, processor.metadbconnect);
        }

        //method for finding top k results
        public string topk(string mainQuery, SQLiteConnection MainDBConnection, SQLiteConnection MetaDBConnection)
        {
            SQLiteCommand findTotal = new SQLiteCommand("SELECT COUNT(*) FROM autompg", MainDBConnection);      // Total # of entries in the main db
            int totalReader = Convert.ToInt32(findTotal.ExecuteScalar());

            int k = 10;                                     // k default 10 if not given by user
            List<string> attr = new List<string>();         // Will contain all attributes specified in user query
            List<string> vals = new List<string>();         // Values corresponding to List<string> attr
            string[] querywords = mainQuery.Split();
            bool nextWordIsAttr = true;
            bool nextWordIsVal = false;
            int startLoop = 0;
            if (querywords[0] == "k")
            {
                k = Int32.Parse(querywords[2].Trim(','));
                startLoop = 3;
            }
            for (int i = startLoop; i < querywords.Length; i++)             // Filling List<string> attr, List<string> vals
            {
                string word = querywords[i];
                if (nextWordIsAttr) attr.Add(filterChars(word)); nextWordIsAttr = false;
                if (nextWordIsVal) vals.Add(filterChars(word)); nextWordIsVal = false;

                if (word == "=") nextWordIsVal = true;
                if (word.EndsWith(",")) nextWordIsAttr = true;
            }

            string createAuxTable = "CREATE TABLE aux ( tupid integer NOT NULL, sim real NOT NULL, PRIMARY KEY(tupid) );";   // Create new table to hold all scores
            SQLiteCommand cmd = new SQLiteCommand(createAuxTable, MainDBConnection);
            cmd.ExecuteNonQuery();

            for (int i = 1; i <= totalReader; i++)                      // Loop through all main db entries to score them 
            {
                double similarity = 0;
                for (int at = 0; at < attr.Count; at++)
                {
                    double qfSim;
                    double idfSim;
                    string curAtt = attr[at];
                    if (curAtt != "brand" && curAtt != "model" && curAtt != "type")        // Numerical QF/IDF
                    {
                        qfSim = calcNumericalQFSim(MainDBConnection, MetaDBConnection, curAtt, vals[at]);
                        idfSim = calcNumericalIDFSim(MainDBConnection, MetaDBConnection, curAtt, vals[at]);
                    }
                    else
                    {
                        qfSim = calcQFSimilarity(MainDBConnection, MetaDBConnection, i, curAtt, vals[at]);       // Catagorical QF
                        idfSim = getIDFSim(MainDBConnection, MetaDBConnection, curAtt, vals[at]);
                    }
                    similarity += (qfSim * idfSim);
                    //similarity += qfSim;
                }

                for (int a = 0; a < mainAttributes.Count; a++)           // Add global importance of missing attributes
                {
                    if (attr.Contains(mainAttributes[a]) == false)       // mainAttributes[a] is missing from user query
                    {
                        SQLiteCommand retrval = new SQLiteCommand("SELECT " + mainAttributes[a] + " FROM autompg WHERE id = " + i, MainDBConnection);
                        var tupleval = retrval.ExecuteScalar();
                        double missingqf;
                        if (mainAttributes[a] != "brand" && mainAttributes[a] != "model" && mainAttributes[a] != "type")
                            missingqf = calcNumericalQFSim(MainDBConnection, MetaDBConnection, mainAttributes[a], Convert.ToString(tupleval));
                        else
                            missingqf = calcMissingQF(MetaDBConnection, mainAttributes[a], Convert.ToString(tupleval));
                        if (!double.IsNaN(missingqf)) similarity += (missingqf > 1) ? Math.Log10(missingqf) : missingqf;
                    }
                }

                SQLiteCommand UpdateSim = new SQLiteCommand("INSERT INTO aux VALUES (" + i + ", " + similarity + ");", MainDBConnection);
                UpdateSim.ExecuteNonQuery();
            }

            StringBuilder result = new StringBuilder();

            result.Append("id | mpg | cyl. | displ. | bph | weight | 0-100 | year | origin | brand | model | type \n \n");

            SQLiteCommand select_topk = new SQLiteCommand("SELECT * FROM aux ORDER BY sim DESC LIMIT + " + k, MainDBConnection);
            SQLiteDataReader topkreader = select_topk.ExecuteReader();
            while(topkreader.Read())
            {
                int id = topkreader.GetInt32(0);
                SQLiteCommand retrievetuple = new SQLiteCommand("SELECT * FROM autompg WHERE id = " + id, MainDBConnection);
                SQLiteDataReader tuplereader = retrievetuple.ExecuteReader();
                int rank = 0;
                while(tuplereader.Read())
                {
                    for(int i = 0; i < 12; i++)
                    {
                        result.Append(tuplereader.GetValue(i) + " | ");
                    }
                }
                result.Append("\n");
            }

            SQLiteCommand dropcommand = new SQLiteCommand("DROP TABLE aux", MainDBConnection);
            dropcommand.ExecuteNonQuery();

            return result.ToString();
        }

        public static double calcNumericalQFSim(SQLiteConnection MainDBConnection, SQLiteConnection MetaDBConnection, string attr, string val)
        {
            double value = Convert.ToDouble(val);
            SQLiteCommand RetrieveMax = new SQLiteCommand("SELECT MAX(qf) FROM " + attr, MetaDBConnection);
            double maxRQF = Convert.ToDouble(RetrieveMax.ExecuteScalar());
            SQLiteCommand RetrieveRQF = new SQLiteCommand("SELECT qf FROM " + attr + " WHERE value = '" + value + "'", MetaDBConnection);

            var rqf = RetrieveRQF.ExecuteScalar();
            if (rqf == null)
            {
                double interp = numericalRetrieve(MetaDBConnection, "qf", attr, value);
                return (interp / maxRQF);
            }
            else
            {
                return (Convert.ToDouble(rqf) / maxRQF);
            }
        }

        public static double calcNumericalIDFSim(SQLiteConnection MainDBConnection, SQLiteConnection MetaDBConnection, string attr, string val)
        {
            double value = Convert.ToDouble(val);
            SQLiteCommand RetrieveMax = new SQLiteCommand("SELECT MAX(idf) FROM " + attr, MetaDBConnection);
            double maxIDF = Convert.ToDouble(RetrieveMax.ExecuteScalar());
            SQLiteCommand RetrieveIDF = new SQLiteCommand("SELECT idf FROM " + attr + " WHERE value = '" + value + "'", MetaDBConnection);

            var idf = RetrieveIDF.ExecuteScalar();
            if (idf == null)
            {
                double interp = numericalRetrieve(MetaDBConnection, "idf", attr, value);
                return (interp / maxIDF);
            }
            else
            {
                return (Convert.ToDouble(idf) / maxIDF);
            }
        }

        public static double numericalRetrieve(SQLiteConnection MetaDBConnection, string qforidf, string attr, double value)
        {
            // Find closest one!
            SQLiteCommand FindAboveId = new SQLiteCommand("SELECT MIN(value) FROM " + attr + " WHERE " + qforidf + " >= 0 AND value >= " + value, MetaDBConnection);
            double aboveID = Convert.ToDouble(FindAboveId.ExecuteScalar());
            SQLiteCommand FindBelowId = new SQLiteCommand("SELECT MAX(value) FROM " + attr + " WHERE " + qforidf + " >= 0 AND value <= " + value, MetaDBConnection);
            double belowID = Convert.ToDouble(FindBelowId.ExecuteScalar());
            SQLiteCommand FindAboveQF = new SQLiteCommand("SELECT " + qforidf + " FROM " + attr + " WHERE value = " + aboveID, MetaDBConnection);
            double aboveQF = Convert.ToDouble(FindAboveQF.ExecuteScalar());
            SQLiteCommand FindBelowQF = new SQLiteCommand("SELECT " + qforidf + " FROM " + attr + " WHERE value = " + belowID, MetaDBConnection);
            double belowQF = Convert.ToDouble(FindBelowQF.ExecuteScalar());
            double diff = aboveID - belowID;
            double qfdiff = aboveQF - belowQF;
            double interpolatedrqf = (qfdiff / diff) * (value - belowID) + belowQF;

            return interpolatedrqf;
        }

        public static float calcQFSimilarity(SQLiteConnection MainDBConnection, SQLiteConnection MetaDBConnection, int i, string attr, string val)   // Only for catagorical
        {
            SQLiteCommand RetrieveVal = new SQLiteCommand("SELECT " + attr + " FROM autompg WHERE id = " + i, MainDBConnection);
            var tupleValue = RetrieveVal.ExecuteScalar();
            SQLiteCommand RetrieveRQFQuery = new SQLiteCommand("SELECT qf FROM " + attr + " WHERE value = '" + val + "'", MetaDBConnection);
            int rqfQuery = Convert.ToInt32(RetrieveRQFQuery.ExecuteScalar());
            SQLiteCommand RetrieveRQFMax = new SQLiteCommand("SELECT MAX(qf) FROM " + attr, MetaDBConnection);
            int rqfMax = Convert.ToInt32(RetrieveRQFMax.ExecuteScalar());
            float qfQuery = (float)rqfQuery / (float)rqfMax;

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

            SQLiteCommand QuerySetUserCmd = new SQLiteCommand("SELECT queryid, freq FROM jacc WHERE terms LIKE '%" + val + "%'", MetaDBConnection);
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

            if(val == Convert.ToString(tupleValue)) finalQFSim = finalQFSim * (float)1.01;
            return finalQFSim;
        }

        public static double calcMissingQF(SQLiteConnection MetaDBConnection, string attr, string value)        // Also only for categorical
        {
            SQLiteCommand retr = new SQLiteCommand("SELECT qf FROM " + attr + " WHERE value = '" + value + "'", MetaDBConnection);
            int rqf = Convert.ToInt32(retr.ExecuteScalar());
            return rqf;
        }

    //returns the idf of the query item
    public static double getIDFSim(SQLiteConnection MainDBConnection, SQLiteConnection MetaDBConnection, string attr, string val)
        {
            double idf = 0;
            SQLiteCommand command = new SQLiteCommand("SELECT idf from " + attr + " WHERE value = '" + val + "'", MetaDBConnection);
            SQLiteDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                idf = reader.GetDouble(0);
            }

            return idf;
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
