using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using System.Text;
using UnityEngine;

public class FirestoreAPI
{
    private readonly string projectId;
    private readonly string baseUrl;

    // Initialize the FirestoreAPI with project ID
    public FirestoreAPI(string projectId)
    {
        this.projectId = projectId;
        this.baseUrl = $"https://firestore.googleapis.com/v1/projects/{projectId}/databases/(default)/documents/";
    }

    #region Get Data
    public async void GetData(string path, Action<string> onSuccess, Action<string> onError = null)
    {
        string url = $"{baseUrl}{path}/";

        using (HttpClient client = new HttpClient())
        {
            HttpResponseMessage response = await client.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                string data = await response.Content.ReadAsStringAsync();
                onSuccess?.Invoke(data); // Returns the document data
            }
            else
            {
                onError?.Invoke(response.ReasonPhrase);
                throw new Exception($"Error getting document: {response.ReasonPhrase}");
            }
        }
    }

    public async void GetData(string path, string fieldName, Action<string> onSuccess, Action<string> onError = null)
    {
        string url = $"{baseUrl}{path}?mask.fieldPaths={fieldName}";

        using (HttpClient client = new HttpClient())
        {
            HttpResponseMessage response = await client.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                string data = await response.Content.ReadAsStringAsync();

                // Parse the JSON to extract the specific field's value
                var jsonResponse = JsonConvert.DeserializeObject<FirestoreDocument>(data);

                if (jsonResponse.fields != null && jsonResponse.fields.ContainsKey(fieldName))
                {
                    if (!string.IsNullOrEmpty(jsonResponse.fields[fieldName].stringValue))
                    {
                        onSuccess?.Invoke(jsonResponse.fields[fieldName].stringValue);
                    }
                    else if (!string.IsNullOrEmpty(jsonResponse.fields[fieldName].integerValue))
                    {
                        onSuccess?.Invoke(jsonResponse.fields[fieldName].integerValue);
                    }
                    else if (!string.IsNullOrEmpty(jsonResponse.fields[fieldName].doubleValue))
                    {
                        onSuccess?.Invoke(jsonResponse.fields[fieldName].doubleValue);
                    }
                    else if (!string.IsNullOrEmpty(jsonResponse.fields[fieldName].booleanValue))
                    {
                        onSuccess?.Invoke(jsonResponse.fields[fieldName].booleanValue);
                    }
                    else
                    {
                        onError?.Invoke($"Field '{fieldName}' has no value.");
                    }
                }
                else
                {
                    onError?.Invoke($"Field '{fieldName}' not found in document.");
                }
            }
            else
            {
                onError?.Invoke(response.ReasonPhrase);
                throw new Exception($"Error fetching field: {response.ReasonPhrase}");
            }
        }
    }
    #endregion

    #region Set Data
    public async void SetData(string path, string fieldName, object value, Action<string> onSuccess, Action<string> onError = null)
    {
        string url = $"{baseUrl}{path}?updateMask.fieldPaths={fieldName}";

        // Determine the Firestore type based on the object type
        string fieldType = "";
        string fieldValue = "";

        if (value is string)
        {
            fieldType = "stringValue";
            fieldValue = $"\"{value}\"";
        }
        else if (value is int || value is long)
        {
            fieldType = "integerValue";
            fieldValue = value.ToString();
        }
        else if (value is float || value is double)
        {
            fieldType = "doubleValue";
            fieldValue = value.ToString();
        }
        else if (value is bool)
        {
            fieldType = "booleanValue";
            fieldValue = value.ToString().ToLower();
        }
        else
        {
            onError?.Invoke("Unsupported data type.");
            return;
        }

        // Create the JSON payload with the appropriate Firestore type
        string jsonData = $"{{ \"fields\": {{ \"{fieldName}\": {{ \"{fieldType}\": {fieldValue} }} }} }}";

        using (HttpClient client = new HttpClient())
        {
            HttpContent content = new StringContent(jsonData, Encoding.UTF8, "application/json");

            // Add a header to simulate a PATCH request
            HttpRequestMessage request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,  // Use POST instead of PATCH
                RequestUri = new Uri(url),
                Content = content
            };
            request.Headers.Add("X-HTTP-Method-Override", "PATCH");  // Method override

            HttpResponseMessage response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                string data = await response.Content.ReadAsStringAsync();
                onSuccess?.Invoke(data);  // Invoke success callback
            }
            else
            {
                string error = await response.Content.ReadAsStringAsync();
                onError?.Invoke(error);   // Invoke error callback if provided
                throw new Exception($"Error updating field: {response.ReasonPhrase}");
            }
        }
    }


    public async void SetData(string path, Dictionary<string, object> dict, Action<string> onSuccess, Action<string> onError = null)
    {
        string url = $"{baseUrl}{path}";

        string jsonData = ToJSON(dict);

        using (HttpClient client = new HttpClient())
        {
            HttpContent content = new StringContent(jsonData, Encoding.UTF8, "application/json");

            // Add a header to simulate a PATCH request
            HttpRequestMessage request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,  // Use POST instead of PATCH
                RequestUri = new Uri(url),
                Content = content
            };
            request.Headers.Add("X-HTTP-Method-Override", "PATCH");  // Method override

            HttpResponseMessage response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                string data = await response.Content.ReadAsStringAsync();
                onSuccess?.Invoke(data);  // Returns the response after updating the fields
            }
            else
            {
                string errorData = await response.Content.ReadAsStringAsync();
                onError?.Invoke($"Error setting document: {response.ReasonPhrase}, Details: {errorData}");
                throw new Exception($"Error setting document: {response.ReasonPhrase}, Details: {errorData}");
            }
        }
    }

    #endregion

    #region Helper
    public static T ConvertResponse<T>(string data)
    {
        T temp = Activator.CreateInstance<T>();

        var dict = JsonConvert.DeserializeObject<FirestoreDocument>(data);

        Type type = typeof(T);
        FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
        foreach (FieldInfo field in fields)
        {
            try
            {
                if (dict.fields.TryGetValue(field.Name, out var t))
                {
                    switch (field.FieldType.Name)
                    {
                        case nameof(String):
                            field.SetValue(temp, t.stringValue);
                            break;
                        case nameof(Int32):
                            Debug.Log(t.integerValue);
                            field.SetValue(temp, int.Parse(t.integerValue));
                            break;
                        case nameof(Boolean):
                            field.SetValue(temp, bool.Parse(t.booleanValue));
                            break;
                        case nameof(Double):
                            field.SetValue(temp, double.Parse(t.doubleValue));
                            break;

                        default:
                            throw new Exception($"Error setting document: {field.FieldType.Name} type not supported");
                    }
                }
                else
                {
                    Debug.LogWarning($"Field '{field.Name}' not found in the document");
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        return temp;
    }


    private string ToJSON(Dictionary<string, object> fields)
    {
        var formattedFields = new StringBuilder();
        formattedFields.Append("{\"fields\":{");

        foreach (var field in fields)
        {
            if (field.Value is string)
            {
                formattedFields.Append($"\"{field.Key}\":{{\"stringValue\":\"{field.Value}\"}},");
            }
            else if (field.Value is int || field.Value is long)
            {
                formattedFields.Append($"\"{field.Key}\":{{\"integerValue\":\"{field.Value}\"}},");
            }
            else if (field.Value is float || field.Value is double)
            {
                formattedFields.Append($"\"{field.Key}\":{{\"doubleValue\":{field.Value}}},");
            }
            else if (field.Value is bool)
            {
                formattedFields.Append($"\"{field.Key}\":{{\"booleanValue\":{field.Value.ToString().ToLower()}}},");
            }
            else
            {
                // Handle other data types as needed
                Debug.LogWarning($"Unsupported data type for field '{field.Key}'");
            }
        }

        if (fields.Count > 0)
        {
            formattedFields.Length--; // Remove trailing comma
        }

        formattedFields.Append("}}");
        return formattedFields.ToString();
    }

    [Serializable]
    public class FirestoreDocument
    {
        public Dictionary<string, FirestoreValue> fields;
    }

    [Serializable]
    public class FirestoreValue
    {
        public string stringValue;
        public string integerValue;
        public string doubleValue;
        public string booleanValue;
    }
    #endregion
}
