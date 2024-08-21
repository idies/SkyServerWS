using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sciserver_webService.Common;
using Sciserver_webService.ConeSearch;
using Sciserver_webService.SDSSFields;
using System.Net;
using System.Web;

namespace Sciserver_webService.Common
{
    public class ProcessDataSet
    {

        String query = "";
        String TaskName = "";
        String format = "";
        bool IsSuccess;
        Dictionary<string, string> ExtraInfo = null;
        string ErrorMessage = "";
        net.ivoa.VOTable.VOTABLE vot;

        String positionType = "";
        String queryType = "";
        String TechnicalErrorInfo = null;
        String TechnicalErrorInfoAll = null;


        public ProcessDataSet(string query, string format, string TaskName, Dictionary<string, string> ExtraInfo, string ErrorMessage, bool IsSuccess, string positionType, string queryType, string TechnicalErrorInfo, string TechnicalErrorInfoAll)
        {
            this.query = query;
            this.format = format;
            this.TaskName = TaskName;
            this.ExtraInfo = ExtraInfo;
            this.ErrorMessage = ErrorMessage;
            this.IsSuccess = IsSuccess;
            this.positionType = positionType;
            this.queryType = queryType; 
            this.TechnicalErrorInfo = TechnicalErrorInfo;
            this.TechnicalErrorInfoAll = TechnicalErrorInfoAll;
        }
        
        public HttpContent GetContent(DataSet ds)
        {

            var response = new HttpResponseMessage();
            string SaveResult = "";
            ExtraInfo.TryGetValue("SaveResult", out SaveResult);


            if (this.IsSuccess)
            {
                if (this.ExtraInfo["FormatFromUser"] != "html")
                {
                    NewSDSSFields sf = new NewSDSSFields();
                    switch (positionType)
                    {

                        case "cone":
                            DefaultCone cstest = new DefaultCone();
                            vot = cstest.ConeSearch(ds);
                            response.Content = new StringContent(ToXML(vot), Encoding.UTF8, KeyWords.contentXML);
                            if (SaveResult == "true")
                                response.Content.Headers.Add("Content-Disposition", "attachment;filename=\"result.xml\"");
                            break;

                        case "FieldArray":

                            response.Content = new StringContent(ToXML(sf.FieldArray(ds)), Encoding.UTF8, KeyWords.contentXML);
                            if (SaveResult == "true")
                                response.Content.Headers.Add("Content-Disposition", "attachment;filename=\"result.xml\"");

                            break;

                        case "FieldArrayRect":

                            response.Content = new StringContent(ToXML(sf.FieldArrayRect(ds)), Encoding.UTF8, KeyWords.contentXML);
                            if (SaveResult == "true")
                                response.Content.Headers.Add("Content-Disposition", "attachment;filename=\"result.xml\"");

                            break;

                        case "ListOfFields":

                            response.Content = new StringContent(ToXML(sf.ListOfFields(ds)), Encoding.UTF8, KeyWords.contentXML);
                            if (SaveResult == "true")
                                response.Content.Headers.Add("Content-Disposition", "attachment;filename=\"result.xml\"");

                            break;

                        case "UrlsOfFields":

                            response.Content = new StringContent(ToXML(sf.UrlOfFields(ds)), Encoding.UTF8, KeyWords.contentXML);
                            if (SaveResult == "true")
                                response.Content.Headers.Add("Content-Disposition", "attachment;filename=\"result.xml\"");

                            break;

                        default:
                            Action<Stream, HttpContent, TransportContext> WriteToStream = null;
                            BinaryFormatter fmt;
                            ds.RemotingFormat = SerializationFormat.Binary;
                            fmt = new BinaryFormatter();
                            WriteToStream = (stream, foo, bar) => { fmt.Serialize(stream, ds); stream.Close(); };
                            response.Content = new PushStreamContent(WriteToStream, new MediaTypeHeaderValue((KeyWords.contentDataset)));
                            break;
                    }

                }
                else
                {
                    response.Content = new StringContent(getHtmlContent(ds));
                }
            }
            else // not IsSuccess
            {
                if (ExtraInfo["DoReturnHtml"].ToLower() == "true") 
                {
                    response.Content = new StringContent(getHtmlError(ErrorMessage));
                }
                else
                {
                    if (String.IsNullOrEmpty(TechnicalErrorInfo))
                        response.Content = new StringContent(ErrorMessage);
                    else
                        response.Content = new StringContent(ErrorMessage + " \nTechnical Info:" + TechnicalErrorInfo);
                }
            }
            return response.Content;
        }


        private String getHtmlError(string ErrorMessage)
        {
            if (ExtraInfo["syntax"] == "Syntax") // in case user want only to verify the syntax of the sql command:
            {
                return getSyntaxHTMLresult(null, ErrorMessage, TechnicalErrorInfo, TechnicalErrorInfoAll);
                //return getINCORRECTsyntaxHTMLresult(ErrorMessage);
            }
            else // in case we want to run que sql command and retrrieve the resultset:
            {
                return getGenericHTMLerror(ErrorMessage);
            }
        }


        private String getHtmlContent(DataSet ds)
        {
            this.query = ExtraInfo["QueryForUserDisplay"] == "" ? this.query : ExtraInfo["QueryForUserDisplay"];
            try
            {
                if (ExtraInfo["syntax"] == "Syntax") // in case user want only to verify the syntax of the sql command:
                {
                    return getSyntaxHTMLresult(ds, null, null, null);
                    //return getOKsyntaxHTMLresult(ds);
                }
                else if (ExtraInfo["fp"] == "only") // in case user want to run the "is in footprint?" query
                {
                    return getFootprintTableHTMLresult(ds);
                }
                else // in case we want to run que sql command and retrrieve the resultset:
                {
                    return getTableHTMLresult(ds);
                }
            }
            catch (Exception e)
            {
                throw(e);
            }
        }

        private string getFootprintTableHTMLresult(DataSet ds)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("<html><head>\n");
            sb.AppendFormat("<title>SDSS Footprint Check</title>\n");
            sb.AppendFormat("</head><body bgcolor=white>\n");
            sb.AppendFormat("<h2>SDSS Footprint Check</h2>");
            sb.AppendFormat("<H3> <font>" + ds.Tables[0].Rows[0][0].ToString() + "</font></H3>\n");
            sb.AppendFormat("</BODY></HTML>\n");
            return sb.ToString();
        }

        private string getOKsyntaxHTMLresult(DataSet ds)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("<html><head>\n");
            sb.AppendFormat("<title>SDSS Query Syntax Check</title>\n");
            sb.AppendFormat("</head><body bgcolor=white>\n");
            sb.AppendFormat("<h2>SQL Syntax Check</h2>");
            sb.AppendFormat("<H3> <font color=green>" + ds.Tables[0].Rows[0][0].ToString() + "</font></H3>\n");
            sb.AppendFormat("<h3>Your SQL command was: <br><pre>" + WebUtility.HtmlEncode(ExtraInfo["QueryForUserDisplay"]) + "</pre></h3><hr>"); // writes command
            sb.AppendFormat("</BODY></HTML>\n");
            return sb.ToString();
        }

        private string getINCORRECTsyntaxHTMLresult(string ErrorMessage)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("<html><head>\n");
            sb.AppendFormat("<title>SDSS Query Syntax Check</title>\n");
            sb.AppendFormat("</head><body bgcolor=white>\n");
            sb.AppendFormat("<h2>SQL Syntax Check</h2>");
            sb.AppendFormat("<H3 BGCOLOR=pink><font color=red>SQL returned the following error: <br>     " + WebUtility.HtmlEncode(ErrorMessage) + "</font></H3>");
            sb.AppendFormat("<h3>Your SQL command was: <br><pre>" + WebUtility.HtmlEncode(ExtraInfo["QueryForUserDisplay"]) + "</pre></h3><hr>"); // writes command
            sb.AppendFormat("</BODY></HTML>\n");
            return sb.ToString();
        }


        private string getSyntaxHTMLresult(DataSet ds, string errorMessage, string technicalErrorInfo, string technicalErrorInfoAll)
        {
            string HtmlContent = "";
            HtmlContent += "<html><head>";
            HtmlContent += "<title>SDSS Query Syntax Check</title>";
            HtmlContent += "</head><body bgcolor=white>";
            HtmlContent += "<h2>SQL Syntax Check</h2>";
            if (ds != null)// OK syntax
                HtmlContent += "<H3> <font color=green>" + WebUtility.HtmlEncode(ds.Tables[0].Rows[0][0].ToString()) + "</font></H3>";
            else if(!String.IsNullOrEmpty(errorMessage))
                HtmlContent += "<H3 BGCOLOR=pink><font color=red>SQL returned the following error: <br>     " + WebUtility.HtmlEncode(errorMessage) + "</font></H3>";
            else
                HtmlContent += "<H3> <font color=green> no message </font></H3>";
            HtmlContent += "<h3>Your SQL command was: <br><pre>" + WebUtility.HtmlEncode(ExtraInfo["QueryForUserDisplay"]) + "</pre></h3><hr>"; // writes command
            if (!String.IsNullOrEmpty(technicalErrorInfoAll))
            {
                HtmlContent += "<br><br> <form method =\"POST\" target=\"_blank\" name=\"bugreportform\" action=\"" + ConfigurationManager.AppSettings["BugReportURL"] + "\">";
                Dictionary<string, string> ErrorFields = JsonConvert.DeserializeObject<Dictionary<string, string>>(technicalErrorInfoAll);
                foreach (string key in ErrorFields.Keys)
                {
                    HtmlContent += "<input type=\"hidden\" name=\"popz_" + key + "\" id=\"popz_" + key + "\" value=\"" + WebUtility.HtmlEncode(ErrorFields[key]) + "\" />";
                }
                //HtmlContent += "<input type=\"hidden\" name=\"popz_bugreport\" id=\"popz_bugreport\" value=\"" + WebUtility.HtmlEncode(technicalErrorInfoAll) + "\" />";
                HtmlContent += "<input id=\"submit\" type=\"submit\" value=\"Click to Report Error\">";
                HtmlContent += "</form>";
            }
            if (!String.IsNullOrEmpty(TechnicalErrorInfo))
                HtmlContent += "<br>Technical info: <br> " + WebUtility.HtmlEncode(TechnicalErrorInfo);

            HtmlContent += "</BODY></HTML>";
            return HtmlContent;
        }


        private string getGenericHTMLerror(string ErrorMessage)
        {
            string HtmlContent = "";
            HtmlContent += "<html><head>\n";
            HtmlContent += "<title>SDSS error message</title>\n";
            HtmlContent += "</head><body bgcolor=white>\n";
            HtmlContent += "<h2>SDSS error message</h2>";
            HtmlContent += "<H3 BGCOLOR=pink><font color=red>" +  WebUtility.HtmlEncode(ErrorMessage) + "</font></H3>";
            //sb.AppendFormat("<H3 BGCOLOR=pink><font color=red> Some tips: <br> No multiple SQL commands allowed     </font></H3>");
            HtmlContent += "<h3>Your SQL command was: <br><pre>" + WebUtility.HtmlEncode(ExtraInfo["QueryForUserDisplay"]) + "</pre></h3><hr>"; // writes command

            if (!String.IsNullOrEmpty(TechnicalErrorInfoAll))
            {
                HtmlContent += "<br><br> <form method =\"POST\" target=\"_blank\" name=\"bugreportform\" action=\"" + ConfigurationManager.AppSettings["BugReportURL"] + "\">";
                Dictionary<string, string> ErrorFields = JsonConvert.DeserializeObject<Dictionary<string, string>>(TechnicalErrorInfoAll);
                foreach (string key in ErrorFields.Keys)
                {
                    HtmlContent += "<input type=\"hidden\" name=\"popz_" + key + "\" id=\"popz_" + key + "\" value=\"" + WebUtility.HtmlEncode(ErrorFields[key]) + "\" />";
                }
                //HtmlContent += "<input type=\"hidden\" name=\"popz_bugreport\" id=\"popz_bugreport\" value=\"" + WebUtility.HtmlEncode(TechnicalErrorInfoAll) + "\" />";
                HtmlContent += "<input id=\"submit\" type=\"submit\" value=\"Click to Report Error\">";
                HtmlContent += "</form>";
            }
            if (!String.IsNullOrEmpty(TechnicalErrorInfo))
                HtmlContent += "<br>Technical info: <br> " + WebUtility.HtmlEncode(TechnicalErrorInfo);

            HtmlContent += "</BODY></HTML>\n";
            return HtmlContent;
        }

        public string getCasJobsSubmitHTMLresult(string JobID, string TableName, string Token)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("<html><head>\n");
            sb.AppendFormat("<title>SDSS Query Results</title>\n");
            sb.AppendFormat("</head><body bgcolor=white>\n");
            sb.AppendFormat("<h2>SDSS query Results </h2>");
            sb.AppendFormat("<H3> <font color=green> Your query is being executed as a job in CasJobs with JobID = " + JobID + " and <br> the result stored into table \"" + TableName + "\" in database \"MyDB\". </font></H3>\n");
            sb.AppendFormat("<br><form method =\"POST\" target=\"_blank\" name=\"casjobsform\" action=\"" + ConfigurationManager.AppSettings["CASJobs"] + "jobdetails.aspx?id=" + JobID + "\">");
            sb.AppendFormat("<input type=\"hidden\" name=\"token\" id=\"token\" value=\"" + Token + "\" />");
            sb.AppendFormat("<input id=\"submit\" type=\"submit\" value=\"See job in CasJobs\">");
            sb.AppendFormat("</form>");

            sb.AppendFormat("<h3>Your SQL command was: <br><pre>" + WebUtility.HtmlEncode(ExtraInfo["QueryForUserDisplay"]) + "</pre></h3><hr>"); // writes command
            sb.AppendFormat("</BODY></HTML>\n");
            return sb.ToString();
        }

        public string getTableSubmitHTMLresult(string TableName, string Token)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("<html><head>\n");
            sb.AppendFormat("<title>SDSS Query Results</title>\n");
            sb.AppendFormat("</head><body bgcolor=white>\n");
            sb.AppendFormat("<h2>SDSS query Results </h2>");
            sb.AppendFormat("<H3> <font color=green> Your query result has been stored into table \"" + TableName + "\" in CasJobs database \"MyDB\". </font></H3>\n");
            sb.AppendFormat("<br><form method =\"POST\" target=\"GoToMyDB\" name=\"casjobsform\" action=\"" + ConfigurationManager.AppSettings["CASJobs"] + "MyDB.aspx\">");
            sb.AppendFormat("<input type=\"hidden\" name=\"token\" id=\"token\" value=\"" + Token + "\" />");
            sb.AppendFormat("<input id=\"submit\" type=\"submit\" value=\"Go to MyDB\">");
            sb.AppendFormat("</form>");

            sb.AppendFormat("<h3>Your SQL command was: <br><pre>" + WebUtility.HtmlEncode(ExtraInfo["QueryForUserDisplay"]) + "</pre></h3><hr>"); // writes command
            sb.AppendFormat("</BODY></HTML>\n");
            return sb.ToString();
        }

        public string getTableReSubmitHTMLresult(string TableName, string Token)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("<html><head>\n");
            sb.AppendFormat("<title>SDSS Query Results</title>\n");
            sb.AppendFormat("</head><body bgcolor=white>\n");
            sb.AppendFormat("<h2>SDSS query Results </h2>");
            sb.AppendFormat("<form method =\"POST\" id=\"form1\" target=\"GoToMyDB\" name=\"casjobsform\" action=\"" + ConfigurationManager.AppSettings["CASJobs"] + "MyDB.aspx\">");
            sb.AppendFormat("<h3><font color=green> Table \"" + TableName + "\" already exists in MyDB. Try changing the table name or </font>   <input type=\"hidden\" name=\"token\" id=\"token\" value=\"" + Token + "\" />");
            sb.AppendFormat("<input id=\"submit\" type=\"submit\" value=\"see table in MyDB\"></h3>");
            sb.AppendFormat("</form>");
            sb.AppendFormat("<h3>Your SQL command was: <br><pre>" + WebUtility.HtmlEncode(ExtraInfo["QueryForUserDisplay"]) + "</pre></h3><hr>"); // writes command
            sb.AppendFormat("</BODY></HTML>\n");
            return sb.ToString();
        }

        public string getGenericCasJobsHTMLerror(string ErrorMessage)
        {
            string HtmlContent = "";
            HtmlContent += "<html><head>\n";
            HtmlContent += "<title>SDSS error message</title>\n";
            HtmlContent += "</head><body bgcolor=white>\n";
            HtmlContent += "<h2>Error message from CasJobs:</h2>";
            HtmlContent += "<H3 BGCOLOR=pink><font color=red>" + WebUtility.HtmlEncode(ErrorMessage) + "</font></H3>";
            //sb.AppendFormat("<H3 BGCOLOR=pink><font color=red> Some tips: <br> No multiple SQL commands allowed     </font></H3>");
            HtmlContent += "<h3>Your SQL command was: <br><pre>" + WebUtility.HtmlEncode(ExtraInfo["QueryForUserDisplay"]) + "</pre></h3><hr>"; // writes command

            if (!String.IsNullOrEmpty(TechnicalErrorInfoAll))
            {
                HtmlContent += "<br><br> <form method =\"POST\" target=\"_blank\" name=\"bugreportform\" action=\"" + ConfigurationManager.AppSettings["BugReportURL"] + "\">";
                Dictionary<string, string> ErrorFields = JsonConvert.DeserializeObject<Dictionary<string, string>>(TechnicalErrorInfoAll);
                foreach (string key in ErrorFields.Keys)
                {
                    HtmlContent += "<input type=\"hidden\" name=\"popz_" + key + "\" id=\"popz_" + key + "\" value=\"" + WebUtility.HtmlEncode(ErrorFields[key]) + "\" />";
                }
                //HtmlContent += "<input type=\"hidden\" name=\"popz_bugreport\" id=\"popz_bugreport\" value=\"" + WebUtility.HtmlEncode(TechnicalErrorInfoAll) + "\" />";
                HtmlContent += "<input id=\"submit\" type=\"submit\" value=\"Click to Report Error\">";
                HtmlContent += "</form>";
            }
            if (!String.IsNullOrEmpty(TechnicalErrorInfo))
                HtmlContent += "<br>Technical info: <br> " + WebUtility.HtmlEncode(TechnicalErrorInfo);

            HtmlContent += "</BODY></HTML>\n";
            return HtmlContent;
        }




        private string getTableHTMLresult(DataSet ds)
        {

            string ColumnName = "";

            string[] Queries = { this.query };
            char[] splitter = { ';' };
            if (this.query.Contains(splitter[0].ToString()))
            {
                Queries = this.query.Split(splitter);
            }
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("<html><head>\n");
            sb.AppendFormat("<title>SDSS Query Results</title>\n");
            sb.AppendFormat("</head><body bgcolor=white>\n");
            int NumTables = ds.Tables.Count;
            string[] QueryTitle = new string[NumTables];
            if ((this.TaskName.Contains(KeyWords.RadialQuery) || this.TaskName.Contains(KeyWords.RectangularQuery)) && NumTables == 2)
            {
                QueryTitle[0] = "Imaging";
                QueryTitle[1] = "Infrared Spectra";
            }

            for (int t = 0; t < Math.Min(NumTables, Queries.Length); t++)
            {
                string[] runs = null, reruns = null, camcols = null, fields = null, plates = null, mjds = null, spreruns = null, fibers = null;
                bool run = false, rerun = false, camcol = false, field = false, dasFields = false;
                bool plate = false, mjd = false, sprerun = false, fiber = false, dasSpectra = false;
                int runI = -1, rerunI = -1, camcolI = -1, fieldI = -1;
                int plateI = -1, mjdI = -1, sprerunI = -1, fiberI = -1;
                int DefaultSpRerun = int.Parse(KeyWords.defaultSpRerun);

                sb.AppendFormat("<h1>" + QueryTitle[t] + "</h1>");
                sb.AppendFormat("<h3>Your SQL command was: <br><pre>" + Queries[t] + "</pre></h3>"); // writes command
                int NumRows = ds.Tables[t].Rows.Count;
                if (NumRows == 0)
                {
                    sb.AppendFormat("<h3><br><font color=red>No entries have been found</font> </h3>");
                }
                else
                {

                    sb.AppendFormat("<h3>Your query output (max " + (Int64.Parse(KeyWords.MaxRows)).ToString("c0").Remove(0, 1) + " rows): <br></h3>"); // writes command
                    for (int r = 0; r < NumRows; r++)
                    {
                        int NumColumns = ds.Tables[t].Columns.Count;
                        if (r == 0)// filling the first row with the names of the columns
                        {
                            sb.AppendFormat("<table border='1' BGCOLOR=cornsilk>\n");
                            sb.AppendFormat("<tr align=center>");
                            for (int c = 0; c < NumColumns; c++)
                            {
                                ColumnName = ds.Tables[t].Columns[c].ColumnName;
                                sb.AppendFormat("<td><font size=-1>{0}</font></td>", ColumnName);
                                switch (ColumnName.ToLower())
                                {
                                    case "run":
                                        run = true;
                                        runI = c;
                                        break;
                                    case "rerun":
                                        rerun = true;
                                        rerunI = c;
                                        break;
                                    case "camcol":
                                        camcol = true;
                                        camcolI = c;
                                        break;
                                    case "field":
                                        field = true;
                                        fieldI = c;
                                        break;
                                    case "plate":
                                        plate = true;
                                        plateI = c;
                                        break;
                                    case "mjd":
                                        mjd = true;
                                        mjdI = c;
                                        break;
                                    case "sprerun":
                                        sprerun = true;
                                        sprerunI = c;
                                        break;
                                    case "fiberid":
                                        fiber = true;
                                        fiberI = c;
                                        break;
                                    default:
                                        break;
                                }
                            }
                            sb.AppendFormat("</tr>");
                            if (run == true && rerun == true && camcol == true && field == true)
                            {
                                dasFields = true;
                                runs = new string[NumRows];
                                reruns = new string[NumRows];
                                camcols = new string[NumRows];
                                fields = new string[NumRows];
                            }
                            if (plate == true && mjd == true && fiber == true)
                            {
                                dasSpectra = true;
                                plates = new string[NumRows];
                                mjds = new string[NumRows];
                                spreruns = new string[NumRows];
                                fibers = new string[NumRows];
                            }
                        }

                        sb.AppendFormat("<tr align=center BGCOLOR=#eeeeff>");
                        for (int c = 0; c < NumColumns; c++)
                        {
                            var val = ds.Tables[t].Rows[r][c];
                            if (val.GetType() == typeof(System.Byte[]))// if column is of binary datatype, follow some estra steps to convert to string.
                            {
                                string res = BitConverter.ToString((byte[])(val));
                                int maxlen = 128+128/2-1;// 128 is the maximum length of the binary string shown to the user.
                                if (res.Length > maxlen)
                                    res = res.Substring(0, maxlen).Replace("-","") + "...";
                                else
                                    res = res.Replace("-","");
                                sb.AppendFormat("<td nowrap><font size=-1>{0}</font></td>", res);
                            }
                            else// easily convert ot string.
                                sb.AppendFormat("<td nowrap><font size=-1>{0}</font></td>", val.ToString());
                        }
                        sb.AppendFormat("</tr>");

                        if (dasFields == true)
                        {
                            runs[r] = ds.Tables[t].Rows[r][runI].ToString();
                            if (rerunI > -1) reruns[r] = ds.Tables[t].Rows[r][rerunI].ToString();
                            camcols[r] = ds.Tables[t].Rows[r][camcolI].ToString();
                            fields[r] = ds.Tables[t].Rows[r][fieldI].ToString();
                        }
                        if (dasSpectra == true)
                        {
                            plates[r] = ds.Tables[t].Rows[r][plateI].ToString();
                            if (sprerun == true)
                                spreruns[r] = ds.Tables[t].Columns[sprerunI].ColumnName;
                            else
                                spreruns[r] = "" + DefaultSpRerun;
                            mjds[r] = ds.Tables[t].Rows[r][mjdI].ToString();
                            fibers[r] = ds.Tables[t].Rows[r][fiberI].ToString();
                        }

                    }
                    if (KeyWords.dasUrlBaseImaging.Length > 1 && KeyWords.dasUrlBaseSpec.Length > 1 && (dasFields == true || dasSpectra == true))
                    {
                        sb.AppendFormat("<p><table><tr>\n");
                        var str = "";
                        if (dasFields == true && dasSpectra == true)
                            str = "(s)";
                        if (dasSpectra == true)
                        {
                            sb.AppendFormat("<tr><td colspan=2><h3>Use the button" + str + " below to upload the results of the above query to the SAS and from there  click <i>Search</i> to download or view the spectra:</h3></td></tr>"); 
                            sb.AppendFormat("<td><form method='post' action='" + KeyWords.dasUrlBaseSpec + "optical/spectrum/search?tab=bulk&run2d=any'/>\n");
                            sb.AppendFormat("<input type='hidden' name='plate_mjd_fiberid_csv' value='");
                            for (int i = 0; i < NumRows; i++)
                                sb.AppendFormat(plates[i] + "," + mjds[i] + "," + fibers[i] + "\n");
                            sb.AppendFormat("'/>\n");
                            sb.AppendFormat("<input type='submit' name='submitPMF' value='Submit'/>Upload list of spectra to SAS\n");
                            sb.AppendFormat("</form></td>");
                        }
                        sb.AppendFormat("</tr></table>");
                    }
                    sb.AppendFormat("</TABLE>");
                    if (dasFields == true)
                    {
                        int dr = Int32.Parse(ConfigurationManager.AppSettings["DataRelease"].ToLower().Replace("dr", ""));
                        dr = dr <= 17 ? dr : 17;
                        string prefix = dr == 12 ? "boss" : "eboss";
                        sb.AppendFormat("<h3>To download the photometric frames as FITS files, click on the button below to get a file with download URLs, and feed it to rsync by running:</h3>  <mark>rsync -avzL --files-from=download_rsync.txt rsync://dtn.sdss.org/dr" + dr + " dr" + dr + " </mark><br>");
                        sb.AppendFormat("<input type='button' id='btn' value='Get rsync file'>");
                        sb.AppendFormat("<script>");
                        sb.Append(@"document.getElementById('btn').addEventListener('click', function() {
                        let text = ''; let run = null; let rerun = null; let field = null;
                        let bands = ['u', 'g', 'r' ,'i', 'z'];");
                        sb.Append("let data = [");
                        for (int i = 0; i < NumRows; i++)
                        {
                            sb.Append("[" + reruns[i] + "," + runs[i] + "," + fields[i] + "," + camcols[i] + "]");
                            if (i < NumRows - 1)
                                sb.Append(",");
                        }
                        sb.Append("];");
                        sb.Append(@"for(const d of data){
                            for(const band of bands){
                                text = text + '" + prefix + @"/photoObj/frames/' + d[0] + '/' + d[1] + '/' + d[3] + '/frame-' + band + '-' + ('000000' + d[1]).slice(-6) + '-' + d[3] + '-' + ('0000' + d[2]).slice(-4) + '.fits.bz2' + '\n'
                            }};");
                        sb.Append(@"
                        let filename = 'download_rsync.txt';
                        let element = document.createElement('a');
                        element.setAttribute('href', 'data:text/plain;charset=utf-8,' + encodeURIComponent(text));
                        element.setAttribute('download', filename);
                        document.body.appendChild(element);
                        element.click();
                        document.body.removeChild(element);
                        }, false);");
                        sb.AppendFormat("</script>");
                    }


                }
                sb.AppendFormat("<hr>");
            }
            sb.AppendFormat("</BODY></HTML>\n");
            return sb.ToString();
        }



        /// <summary>
        ///  This is specifically used for the sdss vo services which are returning random types of xml outputs
        /// </summary>
        /// <returns></returns>
        public string ToXML(Object o)
        {
            var stringwriter = new System.IO.StringWriter();
            var serializer = new XmlSerializer(o.GetType());
            serializer.Serialize(stringwriter, o);
            return stringwriter.ToString();
        }












    }
}