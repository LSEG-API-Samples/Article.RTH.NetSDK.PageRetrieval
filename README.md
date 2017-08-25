# Article.TRTH.NetSDK.PageRetrieval
# Overview #

The following article provides developers a method to use TRTH REST API to retrieve page-based data which mainly used for the Speed Guides or including other page RICs. This could be used as TRTH API migration guide of the existing GetPage method of the TRTH SOAP API.

## Page-based data ##
Page-based data contains text or numbers over-the-counter traded instruments and emerging markets. The data are delivered through Real-time feed row by row, each row is identified by a unique field. The page length is limited to a specific size identified by field that data is delivered on. For example, the ROW64\_1 field is defined to have 64 characters as maximum while ROW80\_1 field is defined to have 80 characters as maximum. There are several sizes of page available. Below are the sizes used broadly in Elektron Real-time feed. 

- **Large page (80 x 25)**

    This page type has 80 characters across x 25 rows down. Page’s content will be delivered in fields between ROW80\_1 and ROW80\_25.

    ![](https://thehub.thomsonreuters.com/servlet/JiveServlet/download/2211500-5-2041101/PAGE80.png)

- **Small page (64 x 14)**

    This page type has 64 characters across x 14 rows down. Page’s content will be delivered in fields between ROW64\_1 and ROW64\_14.

    ![](https://thehub.thomsonreuters.com/servlet/JiveServlet/download/2211500-5-2041004/PAGE64.png)

## Page update ##
As each row of page is provided in a specific field, page row will be updated independently through a field’s update. However, there are two types of update.


### 1) Full update ###
If page row is totally changed, update row can be received in plain text. The value of update will replace all existing value in the row.
	
### 2) Partial field update ###
Normally, the updates for specific fields would be accomplished by sending the whole data field; however with some large fields this can be inefficient when only a few characters within the data field are to be updated. In such cases, real-time feed can send an only changed data with special sequence called **intra-field positioning sequence**, so that the minimum number of characters are transmitted for such update.

The syntax of an intra-field positioning sequence is as follow: 

    <CSI>n<HPA> 
**where:**

Character|Descrciption
------------ | -------------
&lt;CSI&gt;| the control sequence introducer (this can be a one or two-byte sequence, either Hex 9B or Hex 1B5B)
n | an ASCII numeric string representing the cursor offset position relative to the start of the field. The offset position starts at 0.
&lt;HPA&gt; | the Horizontal Position Adjust character which terminates the intra-field position sequence (Hex 60, which is the character ’).

**For example:**

Below is the initial value received for ROW64\_6 field. The update received contains 3 sets of sequence characters. The first set replaces the row at offset 0 with “1700” string. The second set replaces the row at offset 14 with “ZUERCHER KB  ZUR” string and so on.

Partial update value:
    
    ROW64_6: <CSI>0<HPA>1700<CSI>14<HPA>ZUERCHER KB  ZUR<CSI>31<HPA>0.7695/11

Initial value:
    
    ROW64_6: "2100 AUD      BKofNYMellon NYC 0.7695/11   * AUD                " 
    Offset    0123456789012345678901234567890123456789012345678901234567890123
                        1         2         3         4         5         6
    
Updated value:

    ROW64_6: "1700 AUD      ZUERCHER KB  ZUR 0.7695/11   * AUD                "
    Offset    0123456789012345678901234567890123456789012345678901234567890123 
                        1         2         3         4         5         6

**Other sequence character**

There is also other sequence character called "**Character Repetition**" which rarely be used, but occationally you might receive it from page. To save bandwidth, the feed can replace strings of the same repeated character with a special short sequence. The Character Repetition can also be used as combination with the intra-field positioning sequence.

The syntax of an Character Repetition sequence is as follow: 

    <char><CSI>n<REP>
**where:**

Character|Descrciption
------------ | -------------
&lt;char&gt;|is the character to be repeated (e.g. a blank space)
&lt;CSI&gt;|is the control sequence introducer (this can be a one or two-byte sequence, either Hex 9B or Hex 1B5B)
n|an ASCII numeric string representing the number of times to repeat the character
&lt;REP&gt;|the repeat function sequence termination (Hex 62 which is an ASCII “b”)

**For example:** "A&lt;CSI&gt;5&lt;REP&gt;" means "A**AAAAA**"


## Retrieve Page via TRTH REST API. ##
To retrieve page through the TRTH v2 API, the application can use the ExtractRaw endpoint against the Raw report template, and specify the page’s RIC. In the request, set AllowHistoricalInstrument to true, and set the date range to at least 7 days.

The date range is important, since the way that data will be delivered from Raw report template is different from the GetPage method’s and DSS GUI’s. The data contains every tick of Refresh and Update messages sent between the date ranges. The Refresh and Update message is the concept of real-time data where:


- **Refreshes** present the current state of an entire page. It supplies a snapshot of the page at one point in time. Most pages are refreshed at least once in seven days, although some pages are refreshed at a different rate. 


- **Updates** indicate changes to an individual page row, or to individual page characters. (Different pages use different kinds of updates.) Updates to a page are usually issued more frequently than refreshes. 

This means that if the date range is too short, it is possible that there is no Refresh message sent between the specific date ranges. Without Refresh message, application cannot retrieve full page data. 
As page retrieval often retrieves a combination of updates and refreshes, application needs to process each kind accordingly to get latest values of a page. Also, the updates usually contains the special control characters, so application needs to understand the sequence character mentioned in the section above.

Below is the result example of page: FXFX

    FXFX,Market Price,2017-03-18T06:13:08.656280191Z,Raw,REFRESH,,,,,131,51,27039,40
    ,,,FID,1,,PROD_PERM,131,
    ,,,FID,2,,RDNDISPLAY,132,
    ,,,FID,104,,BOND_TYPE,0,"   "
    ,,,FID,215,,ROW64_1,2149 CCY PAGE NAME * REUTER SPOT RATES     * CCY HI*ASIA*LO FXFX,
    ,,,FID,216,,ROW64_2,"2100 EUR      SE BANKEN    NYC 1.0741/47   * EUR                ",
    ,,,FID,217,,ROW64_3,"2101 GBP BKNY BKofNYMellon NYC 1.2394/04   * GBP                ",
    ,,,FID,218,,ROW64_4,"2059 CHF COB1 Commerzbank  FFT 0.9985/88   * CHF                ",
    ,,,FID,219,,ROW64_5,"2100 JPY BCFX BARCLAYS     LON 112.70/73   * JPY                ",
    ,,,FID,220,,ROW64_6,"2100 AUD BKNY BKofNYMellon NYC 0.7695/11   * AUD                ",
    ,,,FID,221,,ROW64_7,"2100 CAD NBCX NT BK CANADA MON 1.3348/50   * CAD                ",
    ,,,FID,222,,ROW64_8,"2059 DKK BCFX BARCLAYS     LON 6.9214/24   * DKK                ",
    ,,,FID,223,,ROW64_9,"2100 NOK      SE BANKEN    NYC 8.4721/91   * NOK                ",
    ,,,FID,224,,ROW64_10,----------------------------------------------------------------,
    ,,,FID,225,,ROW64_11,XAU     1228.40/1229.90* ED3  1.02/ 1.15 * FED        * WGVS 30Y,
    ,,,FID,226,,ROW64_12,XAG LMX  17.31/17.41   * US30Y YTM  3.11 * 0.90- 0.93 * 97.28/29,
    ,,,FID,227,,ROW64_13,"                                                                ",
    ,,,FID,228,,ROW64_14,"                                                                ",
    FXFX,Market Price,2017-03-18T06:45:00.113085499Z,Raw,UPDATE,UNSPECIFIED,,,,131,,46,9
    ,,,FID,215,,ROW64_1,2149 CCY PAGE NAME * REUTER SPOT RATES     * CCY HI*EURO*LO FXFX,
    ,,,FID,216,,ROW64_2,"2100 EUR      SE BANKEN    NYC 1.0741/47   * EUR                ",
    ,,,FID,217,,ROW64_3,"2101 GBP BKNY BKofNYMellon NYC 1.2394/04   * GBP                ",
    ,,,FID,218,,ROW64_4,"2059 CHF COB1 Commerzbank  FFT 0.9985/88   * CHF                ",
    ,,,FID,219,,ROW64_5,"2100 JPY BCFX BARCLAYS     LON 112.70/73   * JPY                ",
    ,,,FID,220,,ROW64_6,"2100 AUD BKNY BKofNYMellon NYC 0.7695/11   * AUD                ",
    ,,,FID,221,,ROW64_7,"2100 CAD NBCX NT BK CANADA MON 1.3348/50   * CAD                ",
    ,,,FID,222,,ROW64_8,"2059 DKK BCFX BARCLAYS     LON 6.9214/24   * DKK                ",
    ,,,FID,223,,ROW64_9,"2100 NOK      SE BANKEN    NYC 8.4721/91   * NOK                ",
    FXFX,Market Price,2017-03-18T17:00:00.249523072Z,Raw,UPDATE,UNSPECIFIED,,,,131,,94,9
    ,,,FID,215,,ROW64_1,[52`AMER,
    ,,,FID,216,,ROW64_2,[50`1.0741[58`1.0738,
    ,,,FID,217,,ROW64_3,[50`1.2397[58`1.2393,
    ,,,FID,218,,ROW64_4,[50` [5b[58` [5b,
    ,,,FID,219,,ROW64_5,[50` [5b[58` [5b,
    ,,,FID,220,,ROW64_6,[50`0.7695[58`0.7695,
    ,,,FID,221,,ROW64_7,[50`1.3348[58`1.3348,
    ,,,FID,222,,ROW64_8,[50` [5b[58` [5b,
    ,,,FID,223,,ROW64_9,[49` 8.4721[57` 8.4718,

#Implementation#
This tutorial describes the way to process the combination of Refreshes and Updates of page data. The .Net SDK is used for page retrieval. However, the implementation method to retrieve Page data will not be mentioned in this article. For data retrieval, please see the [REST API Tutorial 8: On Demand raw data extraction](https://developers.thomsonreuters.com/thomson-reuters-tick-history-trth/thomson-reuters-tick-history-trth-rest-api/learning?content=13663&type=learning_material_item).    


### 1. Decompress data ###

The Raw extraction actually returns data in a gzip compression file. In this sample, we will use 3rd party library, [ICSharpCode](https://icsharpcode.github.io/SharpZipLib/), to decompress the file, so the application has to disable automatic decompress of SDK by changing AutomaticDecompression to false.

    DebugPrintLine("Download the report");
    var streamResponse = extractionsContext.GetReadStream(extractionResult);
    using (var gzip = new ICSharpCode.SharpZipLib.GZip.GZipInputStream(streamResponse.Stream))
    {
        //Decompress data
        return new StreamReader(gzip, Encoding.UTF8);
    }

### 2. Extract field-value pair ###

The gzip file will be decompressed to a file in Comma-separated values (CSV) format. I use the [TextFieldParser class](https://msdn.microsoft.com/en-us/library/microsoft.visualbasic.fileio.textfieldparser.aspx) to help access to specific column in the data. Below is the example of the format. The field ID is in the fifth column (index:4), while the value is in the eighth column (index:7).

> FXFX,Market Price,2017-03-18T06:13:08.656280191Z,Raw,REFRESH,,,,,131,51,27039,40
> 
> ,,,FID,215,,ROW64_1,2149 CCY PAGE NAME * REUTER SPOT RATES     * CCY HI*ASIA*LO FXFX,

    //Parse csv format
    using (var csvParser = new TextFieldParser(streamReader))
    {
        csvParser.CommentTokens = new string[] { "#" };
        csvParser.SetDelimiters(new string[] { "," });
        csvParser.HasFieldsEnclosedInQuotes = true;
        csvParser.TrimWhiteSpace = false;

        while (!csvParser.EndOfData)
        {
            string[] fields = csvParser.ReadFields();
            //process only the field-value row.
            if (fields[0] == "" && fields[4] != "")
            {
                int fieldId = Convert.ToInt32(fields[4]);
                string value = fields[7];

### 3. Allocate String array to store page data ###

This example supports two sizes of page; 80x25 and 64x14. It will verify the received field, and then allocates arrays of string with initial blank space characters.

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

### 4. Verify the partial field update ###

In this step, the application can extract the field id and value of each row. Next, I use [Regex](https://msdn.microsoft.com/en-us/library/system.text.regularexpressions.regex.aspx) class to help identify the intra-field positioning sequence with regular expression. The *Matches() method* will return [MatchCollection](https://msdn.microsoft.com/en-us/library/system.text.regularexpressions.matchcollection.aspx) class containing all matched strings. The *MatchCollection.Count* property represents number of string that matches with the regular expression. If the *Count* is zero, this supposes to be full update string.

    string pattern = @"(\x1B\x5B|\x9B|\x5B)([0-9]+)\x60([^\x1B^\x5B^\x9B]+)";

    Regex rgx = new Regex(pattern, RegexOptions.IgnoreCase);
    MatchCollection matches = rgx.Matches(value);

    if (matches.Count > 0)
    {
        //Partial field update
        ...
    }
    else
    {
        // replace entire field with updated value.
        page[fieldId] = value;
    }

### 5.	Apply update ###

With the *MatchCollection.Groups* property, application can get the matched strings separated by subexpression which are defined by enclosing a portion of the regular expression pattern in parentheses. Below is the example.

**Input string:**

> **ROW64_6:** &lt;CSI&gt;0&lt;HPA&gt;1700&lt;CSI&gt;14&lt;HPA&gt;ZUERCHER KB  ZUR&lt;CSI&gt;31&lt;HPA&gt;0.7695/11

**Output:**

> MatchCollection[0].Group[0] = &lt;CSI&gt;0&lt;HPA&gt;1700
>
> MatchCollection[0].Group[1] = &lt;CSI&gt;
> 
> MatchCollection[0].Group[2] = 0
> 
> MatchCollection[0].Group[3] = 1700
> 
> &#8230;
> 
> MatchCollection[2].Group[0] = &lt;CSI&gt;31&lt;HPA&gt0.7695/11
> 
> MatchCollection[2].Group[1] = &lt;CSI&gt;
> 
> MatchCollection[2].Group[2] = 31
> 
> MatchCollection[2].Group[3] = 0.7695/11

After the application gets both offset and string value, it then replace the string value at the specific index.

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
	
### 6.	Display updated page ###

Finally, the application displays the latest updated ROW of a page RIC (ROW80_1-ROW80_25 for ROW80 and ROW64_1-ROW64_14 for ROW64).

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

Below is the sample of output from &lt;FXFX&gt; page.

![](https://thehub.thomsonreuters.com/servlet/JiveServlet/download/2211500-5-2041102/FXFX.png)

## Additional notes ##

-	#### AutomaticDecompression ####
The Raw extraction actually returns data in a gzip compression file. According to the advisory in [PCN: 9347](https://my.thomsonreuters.com/MTRFPCNDetailNotification?pcnid=9347), to avoid a third-party decompression incompatibility that may be providing you with incomplete data, AutomaticDecompression should be disabled, and then use recommended SharpZipLib library to decompress the result instead.

-	#### Missing control character ####
In some cases, partial field update contains only Hex 5B instead of Hex 1B5B for the <CSI> special character. To avoid this case, the only Hex 5B has been added to the regular expression as well.
