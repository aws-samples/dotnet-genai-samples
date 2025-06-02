using ModelContextProtocol.Server;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Newtonsoft.Json;
using YahooFinanceApi;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.IO.Compression;

namespace FinanceMCPServer
{

     public class DividendInfo
    {
        public object? Date { get; set; }
        public decimal Amount { get; set; }
    }

    [McpServerToolType]
    public static class FinanceTools
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private static readonly string addSymbol = "%2C";
        static readonly string BaseURL = "https://yfapi.net/v6/finance/quote?symbols=";

        // Define valid periods and intervals as static readonly fields
        private static readonly string[] ValidPeriods = { "1d", "5d", "1mo", "3mo", "6mo", "1y", "2y", "5y", "10y", "ytd", "max" };
        private static readonly string[] ValidIntervals = { "1m", "2m", "5m", "15m", "30m", "60m", "90m", "1h", "1d", "5d", "1wk", "1mo", "3mo" };



        static FinanceTools()
        {
            _httpClient = new HttpClient();
            // Add required headers
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
            _httpClient.DefaultRequestHeaders.Add("Accept", "*/*");
            _httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
            _httpClient.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
            _httpClient.DefaultRequestHeaders.Add("Connection", "keep-alive");
        }

        [McpServerTool, Description("Get stock price info")]
        public static async Task<string> GetStockPrice(
            HttpClient client,
            [Description("Stock symbols to query")] string[] stocks)
        {
            if (stocks == null || stocks.Length == 0)
            {
                return "No stock symbols provided";
            }

            string symbol = stocks[0]; // Use the first stock symbol from the array
            string URL = BaseURL + symbol;
            Security stock = null;

            try
            {
                // Attempt to fetch stock data using the provided symbol
                var securities = await Yahoo.Symbols(symbol).Fields(Field.Symbol, Field.RegularMarketPrice).QueryAsync();
                stock = securities[symbol];
                Console.WriteLine($"{symbol}'s stock price is: {stock[Field.RegularMarketPrice]}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return $"An error occurred: {ex.Message}";
            }

            // Results result = JsonConvert.DeserializeObject<Results>(data);
            // Uncomment and define Results class as needed

            return $"{symbol} stock price is {stock[Field.RegularMarketPrice]}";
        }

        /// <summary>
        /// Gets stock history data from Yahoo Finance API
        /// </summary>
        /// <param name="symbol">Stock symbol to query</param>
        /// <param name="period">Time period for history data</param>
        /// <param name="interval">Time interval for data points</param>
        /// <returns>Stock history data as string</returns>
        [McpServerTool, Description("Get stock history data")]
        public static async Task<List<DividendInfo>> GetStockHistory(
            HttpClient client,
            [Description("Stock symbol to query")] string symbol,
            [Description("StartDate)")] DateTime startDate,
            [Description("EndDate)")] DateTime endDate)
        {
            try
            {
                // Configure Yahoo Finance API with custom HttpClient
                var period1 = ((DateTimeOffset)startDate).ToUnixTimeSeconds();
                var period2 = ((DateTimeOffset)endDate).ToUnixTimeSeconds();

                var url = $"https://query1.finance.yahoo.com/v8/finance/chart/{Uri.EscapeDataString(symbol)}?" +
                     $"period1={period1}&period2={period2}&interval=1d&events=div";

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

               // var jsonString = await response.Content.ReadAsStringAsync();

                // Handle potential compression
            var content = await response.Content.ReadAsStreamAsync();
            string jsonString;

            if (response.Content.Headers.ContentEncoding.Contains("gzip"))
            {
                using var gzipStream = new GZipStream(content, CompressionMode.Decompress);
                using var reader = new StreamReader(gzipStream);
                jsonString = await reader.ReadToEndAsync();
            }
            else
            {
                using var reader = new StreamReader(content);
                jsonString = await reader.ReadToEndAsync();
            }

                return ParseDividendData(jsonString);
            }
            catch (Exception ex)
            {
                return null;
            }
        }



        private static List<DividendInfo> ParseDividendData(string jsonResponse)
        {
            var dividends = new List<DividendInfo>();
            var jsonObject = JObject.Parse(jsonResponse);

            var events = jsonObject["chart"]["result"][0]["events"];
            if (events != null && events["dividends"] != null)
            {
                var dividendEvents = events["dividends"].ToObject<JObject>();

                foreach (var dividend in dividendEvents)
                {
                    var timestamp = long.Parse(dividend.Key);
                    var amount = dividend.Value["amount"].Value<decimal>();

                    dividends.Add(new DividendInfo
                    {
                        Date = DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime,
                        Amount = amount
                    });
                }
            }
            return dividends.OrderByDescending(d => d.Date).ToList();
        }

  
         
    }

   
}
   
