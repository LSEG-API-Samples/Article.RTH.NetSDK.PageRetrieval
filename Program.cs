//============================================================================
//TRTH REST API Sample Application
//
//This Application implements steps to retreive Record Page via TRTH REST API.
//
//============================================================================
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace TRTH_PageExtractor
{
    class Program
    {
        //=====================================================================
        //Member declarations
        //=====================================================================
        private static string dssUserName = "YourDSSUserId";
        private static string dssUserPassword = "YourDSSPassword";
        private static Uri dssUri = new Uri("https://selectapi.datascope.refinitiv.com/RestApi/v1/");
        private static string pageRIC = "FXFX";

        //=====================================================================
        //Main program entry
        //=====================================================================
        static void Main(string[] args)
        {
            try
            {
                DssClient client = new DssClient(dssUri);

                //Connect to server
                client.ConnectToServer(dssUserName, dssUserPassword);

                //Create TickHistoryRawExtraction Request template
                var rawExtractionResult = client.CreateAndRunTickHistoryRawExtraction(pageRIC);

                //Download data stream
                var streamResponse = client.DownloadResult(rawExtractionResult);

                //Process page data
                client.ProcessPageData(streamResponse);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return;
            }
        }
    }
}
