using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DarAss1
{
    public class MainSearch
    {
        private string outputText;

        //constructor method for mainsearch class
        public MainSearch (Preprocessor p)
        { }

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
            outputText = "Dit is de input: " + input + ".";
        }



    }


}
