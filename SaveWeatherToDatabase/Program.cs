using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;

namespace SaveWeatherToDatabase
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Program p = new Program();
            while (true)
            {
                DateTime now = DateTime.Now;

                // Calculate the time until the next full hour
                int minutesUntilNextHour = 60 - now.Minute;
                int secondsUntilNextHour = minutesUntilNextHour * 60 - now.Second;
                int millisecondsUntilNextHour = secondsUntilNextHour * 1000;

                Console.WriteLine($"Waiting for {minutesUntilNextHour} minutes until the next full hour...");

                // Wait until the next full hour
                Thread.Sleep(millisecondsUntilNextHour);

                // Run the functions
                string finalData = p.sendRequest();
                Console.WriteLine(finalData);

                // Sleep for 5 minutes
                Thread.Sleep(300000);
            }
        }

        static void insertData(string temp, string humidity, string windDirection, string windSpeed, string windSpeedOfGust, int year, int month, int day, int hour, int minute)
        {
            // Connection variables
            string host = "your-host";
            string user = "your-user";
            string password = "your-password"; 
            string database = "your-database-name";

            string connString = $"Server={host};Database={database};User ID={user};Password={password};";


            // Starting a connection
            using (MySqlConnection conn = new MySqlConnection(connString))
            {
                try
                {
                    Console.WriteLine("Opening Connection...");
                    conn.Open();
                    Console.WriteLine("Connection Successful!");

                    // Adding MySQL commands into query
                    string sqlQuery = "INSERT INTO Data (Year,Month,Day,Hour,Temp,Humidity,WindDirection,WindSpeed,PlaceID) VALUES(?Year,?Month,?Day,?Hour,?Temp,?Humidity,?WindDirection,?WindSpeed,?PlaceID)";
                    using (MySqlCommand cmd = new MySqlCommand(sqlQuery, conn))
                    {
                        cmd.Parameters.Add("?Year", MySqlDbType.Int64).Value = year;
                        cmd.Parameters.Add("?Month", MySqlDbType.Int64).Value = month;
                        cmd.Parameters.Add("?Day", MySqlDbType.Int64).Value = day;
                        cmd.Parameters.Add("?Hour", MySqlDbType.Int64).Value = hour;
                        cmd.Parameters.Add("?Temp", MySqlDbType.VarChar).Value = temp;
                        cmd.Parameters.Add("?Humidity", MySqlDbType.VarChar).Value = humidity;
                        cmd.Parameters.Add("?WindDirection", MySqlDbType.VarChar).Value = windDirection;
                        cmd.Parameters.Add("?WindSpeed", MySqlDbType.VarChar).Value = windSpeed;
                        cmd.Parameters.Add("?PlaceID", MySqlDbType.VarChar).Value = "1";
                        cmd.ExecuteNonQuery();
                    }
                    // Displaying current time in console
                    Console.WriteLine($"Year: {year} | Month: {month} | Day: {day} | Hour: {hour}:{minute}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                }
            }
        }

        public string sendRequest()
        {
            // Variables for coordinates
            string lat = "59.2174684";
            string lon = "10.8756048";

            // Request variable
            var httpWebRequest = (HttpWebRequest)WebRequest.Create($"https://api.met.no/weatherapi/nowcast/2.0/complete?lat={lat}&lon={lon}");

            try
            {
                // Creating a request
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "GET";
                httpWebRequest.UserAgent = "bolle"; // Fill with anything random, field cannot be empty
                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    // Fetching result from request and parsing it
                    var result = streamReader.ReadToEnd();
                    JObject jObj = JObject.Parse(result);
                    JToken data = jObj.SelectToken("properties.timeseries[0].data.instant.details");


                    // Displaying results into variables
                    string temp = data.Value<string>("air_temperature");
                    string humidity = data.Value<string>("relative_humidity");
                    string windDirection = data.Value<string>("wind_from_direction");
                    string windSpeed = data.Value<string>("wind_speed");
                    string windSpeedOfGust = data.Value<string>("wind_speed_of_gust");

                    // Fetching current time
                    DateTime now = DateTime.Now;

                    // Displaying current time as variable
                    int year = now.Year;
                    int month = now.Month;
                    int day = now.Day;
                    int hour = now.Hour;
                    int minute = now.Minute;

                    insertData(temp, humidity, windDirection, windSpeed, windSpeedOfGust, year, month, day, hour, minute);

                    return "Success! Inserting data...";
                }
            }
            catch (Exception ex)
            {
                return ("Error: " + ex.Message);
            }
        }
    }
}
