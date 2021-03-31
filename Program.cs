using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using HtmlAgilityPack;
using ScrapySharp.Exceptions;
using ScrapySharp.Network;
using System.Reflection;

namespace ScrapetheData
{
    class Program
    {
        static readonly string uriString = "https://srh.bankofchina.com/search/whpj/searchen.jsp";

        static void Main()
        {
            //var htmlDoc = new HtmlDocument();

            List<string> currencyList = new List<string>();

            currencyList = CurrencyDetails();

            string startDate = DateTime.Now.AddDays(-2).ToString("yyyy-MM-dd");
            string endDate = DateTime.Now.ToString("yyyy-MM-dd");

            foreach (var currency in currencyList)
            {
                try
                {
                    string htmlResponse = string.Empty;
                    //HtmlDocument htmlDoc = new HtmlDocument();
                    var htmlDoc = ExtractHTMLDocument(startDate, endDate, currency, 1, ref htmlResponse);

                    ExtractData(htmlDoc, startDate, endDate, currency, 1);

                    var totalRecords = Convert.ToInt32(htmlResponse.Split("var m_nRecordCount = ")[1].Split(';')[0]);
                    var totalPages = totalRecords % 20 == 0 ? totalRecords / 20 : (totalRecords / 20) + 1;
                    for (int pageNumber = 2; pageNumber <= totalPages; pageNumber++)
                    {
                        htmlDoc = ExtractHTMLDocument(startDate, endDate, currency, pageNumber, ref htmlResponse);

                        ExtractData(htmlDoc, startDate, endDate, currency, pageNumber);
                    }
                }
                catch (Exception ex)
                {
                    LogException(ex.Message, MethodBase.GetCurrentMethod().Name);
                }
            }
        }

        private static List<string> CurrencyDetails()
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uriString); ;
            WebResponse response = null;
            StreamReader reader = null;

            List<string> currencyList = new List<string>();

            var htmlDoc = new HtmlDocument();
            try
            {
                request.Method = "GET";
                response = request.GetResponse();
                reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
                htmlDoc.LoadHtml(reader.ReadToEnd());

                var currencies = htmlDoc.DocumentNode.SelectNodes("//select[@id='pjname'] //option");
                foreach (var item in currencies)
                {
                    if (item.InnerText.ToString().Trim().Length == 3)
                        currencyList.Add(item.OuterHtml.Substring(15, 3));
                }
            }
            catch (WebException wex)
            {
                LogException(wex.Message, MethodBase.GetCurrentMethod().Name);
            }

            finally
            {
                response.Close();
                reader.Close();
            }
            return currencyList;
        }

        private static void ExtractData(HtmlDocument htmlDoc, string startDate, string endDate, string currency, int pageNumber)
        {
            string tableData = string.Empty;
            try
            {
                var table = htmlDoc.DocumentNode.Descendants("table").Where(d => d.Attributes.Contains("bgcolor") && d.Attributes["bgcolor"].Value.Contains("#EAEAEA")).First();
                var rows = table.Descendants("tr");

                int count = 0;
                foreach (var tr in rows)
                {
                    if (pageNumber == 1 && count == 0)
                    {
                        //    continue;
                        tableData += String.Join(",", tr.Descendants("td").Select(x => x.InnerText)) + Environment.NewLine;
                    }
                    else
                    {

                    }
                }
                WriteToFile(startDate, endDate, currency, tableData);
            }
            catch (Exception ex)
            {
                // LogException(ex.Message, MethodBase.GetCurrentMethod().Name);
                tableData = ex.Message;
                WriteToFile(startDate, endDate, currency, tableData);
            }
        }

        private static void WriteToFile(string startDate, string endDate, string currency, string data)
        {
            try
            {
                string path = string.Empty;

                path = Directory.GetCurrentDirectory() + @"\Output";

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                path += "/" + currency + "-" + startDate + "-" + endDate + ".txt";
                if (!File.Exists(path))
                {
                    File.WriteAllText(path, data);
                }
                else
                {
                    File.AppendAllText(path, data);
                }
            }
            catch (Exception ex)
            {
                LogException(ex.Message, MethodBase.GetCurrentMethod().Name);
            }
        }

        private static HtmlDocument ExtractHTMLDocument(string startDate, string endDate, string currency, int pageNumber, ref string htmlResponse)
        {
            WebClient myWebClient = new WebClient();
            NameValueCollection myNameValueCollection = new NameValueCollection();
            HtmlDocument htmlDoc = new HtmlDocument();
            try
            {
                myNameValueCollection.Add("erectDate", startDate);
                myNameValueCollection.Add("nothing", endDate);
                myNameValueCollection.Add("pjname", Convert.ToString(currency));
                myNameValueCollection.Add("page", Convert.ToString(pageNumber));

                Uri myUri = new Uri(uriString, UriKind.Absolute);

                byte[] responseArray = myWebClient.UploadValues(myUri, myNameValueCollection);
                htmlResponse = Encoding.ASCII.GetString(responseArray);
                htmlDoc.LoadHtml(htmlResponse);
            }
            catch (WebException wex)
            {
                LogException(wex.Message, MethodBase.GetCurrentMethod().Name);
            }
            finally
            {

            }
            return htmlDoc;

        }
        private static void LogException(string message, string name)
        {
            string path = Directory.GetCurrentDirectory() + "/ErrorLog.txt";

            if (!File.Exists(path))
            {
                File.WriteAllText(path, name + new string('-', 25) + Environment.NewLine + message + new string('-', 50));
            }
            else
            {
                File.AppendAllText(path, name + new string('-', 25) + Environment.NewLine + message + new string('-', 50));
            }

        }
    }
}
///1. Get the list of Currency and loop the currency 
///2. Test the output using sdate and edate
///3. Identify the total count
///4. Loop the pages
///5. Read the iFrame data
///6. Write into Text file