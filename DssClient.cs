//============================================================================
//DssClient class responsibilities:
//Connection:
//  Connect (and authenticate) to the DSS REST server, by creating an extraction context.
//  Retrieve the session token from the extraction context.
//
//  Create an instrument list.
//  Append identifiers to an instrument list (with validation result).
//  Create an instrument list and populate it with identifiers.
//DSS On Demand extractions:
//  Create and run a Terms and Conditions extraction.
//  Create and run a Edentity Hierarchy extraction
//============================================================================
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;

using Microsoft.VisualBasic.FileIO;
using System.Text.RegularExpressions;

using DataScope.Select.Api.Extractions;
using DataScope.Select.Api.Extractions.SubjectLists;
using DataScope.Select.Api.Content;
using DataScope.Select.Api.Extractions.ReportTemplates;
using DataScope.Select.Api.Extractions.Schedules;
using DataScope.Select.Api;
using DataScope.Select.Api.Core;
using DataScope.Select.Api.Extractions.ReportExtractions;
using DataScope.Select.Api.Extractions.ExtractionRequests;
using DataScope.Select.Core.RestApi;

namespace RTH_PageExtractor
{
    class DssClient
    {
        private ExtractionsContext extractionsContext;

        private Uri dssUri;
        public DssClient(Uri uri)
        {
            dssUri = uri;
        }

        public void ConnectToServer(string dssUserName, string dssUserPassword)
        {
            extractionsContext = new ExtractionsContext(dssUri, dssUserName, dssUserPassword);
            DebugPrintLine("Successfully connect to server.");
        }

        public string SessionToken
        {
            //The session token is only generated if the server connection is successful.
            get { return extractionsContext.SessionToken; }
        }

        //Create and run an on demand extraction:
        public RawExtractionResult CreateAndRunTickHistoryRawExtraction(
            String pageRIC)
        {
            DebugPrintLine("Creating TickHistoryRawExtraction request");
            var extractionRequest = new TickHistoryRawExtractionRequest()
            {
                IdentifierList = InstrumentIdentifierList.Create(
                new[]
                {
                    new InstrumentIdentifier { Identifier = pageRIC, IdentifierType = IdentifierType.Ric }
                },
                new InstrumentValidationOptions
                {
                    AllowHistoricalInstruments = true
                }, false),
                Condition = new TickHistoryRawCondition()
                {
                    DaysAgo = null,
                    MessageTimeStampIn = TickHistoryTimeOptions.GmtUtc,
                    QueryEndDate = DateTime.UtcNow,
                    QueryStartDate = DateTime.UtcNow - TimeSpan.FromDays(7),
                    ReportDateRangeType = ReportDateRangeType.Range,
                    ExtractBy = TickHistoryExtractByMode.Ric,
                    SortBy = TickHistorySort.SingleByRic,
                    DomainCode = TickHistoryRawDomain.MarketPrice,
                    DisplaySourceRIC = false
                }
            };

            //Disable automatic decompression, so application will receive data in gzip compression format.
            extractionsContext.Options.AutomaticDecompression = false;

            DebugPrintLine("ExtractRaw request sent");
            //Extract - NOTE: If the extraction request takes more than 30 seconds the async mechansim will be used.  See Key Mechanisms 
            return extractionsContext.ExtractRaw(extractionRequest);
        }
       
        public StreamReader DownloadResult(RawExtractionResult extractionResult)
        {
            DebugPrintLine("Download the report");
            var streamResponse = extractionsContext.GetReadStream(extractionResult);
            using (var gzip = new ICSharpCode.SharpZipLib.GZip.GZipInputStream(streamResponse.Stream))
            {
                //Decompress data
                return new StreamReader(gzip, Encoding.UTF8);
            }
        }

        public void ProcessPageData(StreamReader streamReader)
        {
            DebugPrintLine("Process data from the report");
            String[] page = null;

            //Parse csv format
            using (var csvParser = new TextFieldParser(streamReader))
            {
                csvParser.CommentTokens = new string[] { "#" };
                csvParser.SetDelimiters(new string[] { "," });
                csvParser.HasFieldsEnclosedInQuotes = true;
                csvParser.TrimWhiteSpace = false;

                while (!csvParser.EndOfData)
                {
                    // Read current line fields, pointer moves to the next line.
                    // sample data
                    // FXFX,Market Price,2017-03-18T06:13:08.656280191Z,Raw,REFRESH,,,,,131,51,27039,40
                    // ,,,FID,215,,ROW64_1,2149 CCY PAGE NAME * REUTER SPOT RATES     * CCY HI*ASIA*LO FXFX,
                    // ,,,FID,216,,ROW64_2,"2100 EUR      SE BANKEN    NYC 1.0741/47   * EUR                ",
                    // ,,,FID,217,,ROW64_3,"2101 GBP BKNY BKofNYMellon NYC 1.2394/04   * GBP                ",
                    string[] fields = csvParser.ReadFields();
                    //process only the field-value row.
                    if (fields[0] == "" && fields[4] != "")
                    {
                        int fieldId = Convert.ToInt32(fields[4]);
                        string value = fields[7];
                        //verify if the field is page ROW field.
                        if ((fieldId <= 339 && fieldId >= 315) || (fieldId <= 228 && fieldId >= 215))
                        {
                            if (fieldId <= 339 && fieldId >= 315)
                            {
                                //Initialize page array for both page types
                                if (page == null)
                                {
                                    page = new string[25];
                                    for (int i = 0; i < 25; i++)
                                    {
                                        page[i] = "".PadRight(80); ;
                                    }
                                }
                                fieldId = fieldId - 315;
                            }
                            else
                            {
                                if (page == null)
                                {
                                    page = new string[14];
                                    for (int i = 0; i < 14; i++)
                                    {
                                        page[i] = "".PadRight(64);
                                    }
                                }
                                fieldId = fieldId - 215;
                            }

                            string pattern = @"(\x1B\x5B|\x9B|\x5B)([0-9]+)\x60([^\x1B^\x5B^\x9B]+)";

                            Regex rgx = new Regex(pattern, RegexOptions.IgnoreCase);
                            MatchCollection matches = rgx.Matches(value);

                            if (matches.Count > 0)
                            {
                                //Partial field update
                                for (var i = 0; i < matches.Count; i++)
                                {
                                    var group = matches[i].Groups;
                                    var partialIndex = Convert.ToInt32(group[2].ToString());
                                    var partialValue = group[3].ToString();
                                  
                                    // replace updated value at the position 
                                    page[fieldId] = page[fieldId].Remove(partialIndex, partialValue.Length);
                                    page[fieldId] = page[fieldId].Insert(partialIndex, partialValue);
                                }
                            }
                            else
                            {
                                // replace entire field with updated value.
                                page[fieldId] = value;
                            }
                        }
                    }
                }

                DebugPrintLine("Page data:");
                //display page based on raw data received.
                if (page != null)
                    foreach (var row in page)
                    {
                        DebugPrintLine(row);
                    }
                else
                    DebugPrintLine("There is no page data received");
                DebugPrintAndWaitForEnter("");
            }
        }

        //=====================================================================
        //Helper methods
        //=====================================================================
        void DebugPrintAndWaitForEnter(string messageToPrint)
        {
            Console.WriteLine(messageToPrint);
            Console.WriteLine("Press Enter to continue");
            Console.ReadLine();
        }
        static void DebugPrintLine(string messageToPrint)
        {
            Console.WriteLine(messageToPrint);
        }

    }          
}
