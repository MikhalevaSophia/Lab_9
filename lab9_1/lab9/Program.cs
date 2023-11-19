using Microsoft.VisualBasic;
using System;
using System.Data;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http.Headers;

namespace lab9
{
    class Program
    {
        static readonly Mutex mutex = new Mutex();

        static async Task Main()
        {
            List<string> list = new List<string>();

            using (StreamReader reader = new StreamReader("ticker.txt"))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    list.Add(line);
                }
            }
            using (HttpClient client = new HttpClient())
            {
                List<Task> tasks = new List<Task>();
                foreach (string ticker in list)
                {
                    tasks.Add(GetDataForTickets(client, ticker));
                }
                await Task.WhenAll(tasks);
            }
        }
        static async Task GetDataForTickets(HttpClient client, string ticker)
        {
            try
            {
                DateTime sdate = DateTime.Now.AddYears(-1);
                DateTime edate = DateTime.Now;

                long sunixdate = ((DateTimeOffset)sdate).ToUnixTimeSeconds();
                long eunixdate = ((DateTimeOffset)edate).ToUnixTimeSeconds();

                string url = $"https://query1.finance.yahoo.com/v7/finance/download/{ticker}?period1={sunixdate}&period2={eunixdate}&interval=1d&events=history&includeAdjustedClose=true\r\n";

                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                string csvdata = await response.Content.ReadAsStringAsync();

                string[] strings = csvdata.Split('\n');
                double totalAveragePrice = 0.0;
                int totalRowPrice = 0;

                for (int i = 1; i < strings.Length - 1; i++)
                {
                    try
                    {
                        string[] values = strings[i].Split(',');

                        double high = Convert.ToDouble(values[2], new System.Globalization.CultureInfo("en-US"));
                        double low = Convert.ToDouble(values[3], new System.Globalization.CultureInfo("en-US"));
                        double averagePrice = (high + low) / 2;

                        totalAveragePrice += averagePrice;
                        totalRowPrice++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
                if (totalRowPrice > 0)
                {
                    double totalAverage = totalAveragePrice / totalRowPrice;
                    string result = $"{ticker}:{totalAverage}";

                    mutex.WaitOne();
                    try
                    {
                        File.AppendAllText("result.txt", result + Environment.NewLine);
                    }
                    finally
                    {
                        mutex.ReleaseMutex();
                    }
                    Console.WriteLine($"Средняя цена акции для {ticker} за год: {totalAverage}");
                }
                else
                {
                    Console.WriteLine($"Для {ticker} нет данных за год.");
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine ($"Ошибка при обработке {ticker}: {ex.Message}");
            }
        }
    }
}