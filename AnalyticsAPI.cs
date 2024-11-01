using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

public class AnalyticsAPI
{
    readonly string measurementID;
    readonly string apiSecret;
    readonly string clientID;
    const string endpointURL = "https://www.google-analytics.com/mp/collect";
    
    readonly bool showLog;
    
    public AnalyticsAPI(string measurementID, string apiSecret, string clientID, bool showLog = true)
    {
        this.measurementID = measurementID;
        this.apiSecret = apiSecret;
        this.clientID = clientID;
        this.showLog = showLog;
    }

    /// <summary>
    /// Logs an event to Google Analytics.
    /// </summary>
    /// <param name="eventName">The name of the event to log.</param>
    /// <param name="value">The value associated with the event.</param>    
    public async void LogEvent(string eventName, object value = null)
    {
        string jsonPayload = CreateEventJson(eventName, value);
        await SendEventAsync(jsonPayload, eventName, value.ToString());
    }

    private async Task SendEventAsync(string jsonPayload, string eventName, string value)
    {
        try
        {            
            string url = $"{endpointURL}?measurement_id={measurementID}&api_secret={apiSecret}";

            using (HttpClient client = new HttpClient())
            {
                HttpContent content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                
                HttpResponseMessage response = await client.PostAsync(url, content);
                
                if (response.IsSuccessStatusCode)
                {
                    if (showLog) Console.WriteLine($"Event sent to Google Analytics\n{eventName}: {value}");                    
                }
                else
                {
                    throw new Exception($"Error sending analytics event: {response.ReasonPhrase}");
                }
            }

            
        }
        catch (HttpRequestException e)
        {
            throw new Exception($"Request error: {e.Message}");
        }
    }

    private string CreateEventJson(string eventName, object value)
    {
        string json = "{";
        json += "\"client_id\": \"" + clientID + "\",";
        json += "\"events\": [{";
        json += "\"name\": \"" + eventName + "\",";
        json += "\"params\": {";

        if (value != null)
        {
            if (value is int || value is float || value is double)
            {
                json += "\"value\": " + value;
            }
            else if (value is bool)
            {
                json += "\"value\": " + ((bool)value ? "true" : "false");
            }
            else if (value is string)
            {
                json += "\"value\": \"" + value.ToString() + "\"";
            }
            else
            {
                json += "\"value\": \"" + value.ToString() + "\"";
            }
        }

        json += "}}]}";

        return json;
    }
}
