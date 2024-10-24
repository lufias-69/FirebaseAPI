using Newtonsoft.Json; //com.unity.nuget.newtonsoft-json
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using UnityEngine;

/// <summary>
/// Represents a class for interacting with Firestore API.
/// </summary>
public class FirestoreAPI
{
    private readonly string projectId;
    private readonly string baseUrl;

    public FirestoreAPI(string projectId)
    {
        this.projectId = projectId;
        this.baseUrl = $"https://firestore.googleapis.com/v1/projects/{projectId}/databases/(default)/documents/";
    }

    public string GetPushID()
    {
        return Guid.NewGuid().ToString();
    }

    // done
    public async void ReadData<T>(string path, Action<T> onSuccess, Action<string> onError = null)
    {
        string url = $"{baseUrl}{path}/";

        using (HttpClient client = new HttpClient())
        {
            HttpResponseMessage response = await client.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                string data = await response.Content.ReadAsStringAsync();
                onSuccess?.Invoke(ConvertResponse<T>(data));
            }
            else
            {
                onError?.Invoke(response.ReasonPhrase);
                throw new Exception($"Error getting document: {response.ReasonPhrase}");
            }
        }
    }

    //done
    public async void ReadData(string path, string fieldName, Action<string> onSuccess, Action<string> onError = null)
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
                    else if (!string.IsNullOrEmpty(jsonResponse.fields[fieldName].timestampValue))
                    {
                        onSuccess?.Invoke(jsonResponse.fields[fieldName].timestampValue);
                    }
                    //else if (!string.IsNullOrEmpty(jsonResponse.fields[fieldName].arrayValue))
                    //{
                    //    onSuccess?.Invoke(jsonResponse.fields[fieldName].arrayValue);
                    //}
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
    
    
    //done
    public async void SetData(string path, Dictionary<string, object> dict, Action onSuccess = null, Action<string> onError = null)
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
                //string data = await response.Content.ReadAsStringAsync();
                onSuccess?.Invoke();
            }
            else
            {
                string errorData = await response.Content.ReadAsStringAsync();
                onError?.Invoke($"Error setting document: {response.ReasonPhrase}, Details: {errorData}");
                throw new Exception($"Error setting document: {response.ReasonPhrase}, Details: {errorData}");
            }
        }
    }

    //done
    public async void SetData(string path, string fieldName, object value, Action onSuccess = null, Action<string> onError = null)
    {
        Dictionary<string, object> dict = new Dictionary<string, object> { { fieldName, value } };

        string url = $"{baseUrl}{path}?updateMask.fieldPaths={string.Join(",", dict.Keys)}";

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
                //string data = await response.Content.ReadAsStringAsync();
                onSuccess?.Invoke();
            }
            else
            {
                string errorData = await response.Content.ReadAsStringAsync();
                onError?.Invoke($"Error setting document: {response.ReasonPhrase}, Details: {errorData}");
                throw new Exception($"Error setting document: {response.ReasonPhrase}, Details: {errorData}");
            }
        }
    }

    public async void DeleteData(string path, Action<string> onSuccess = null, Action<string> onError = null)
    {
        string url = $"{baseUrl}{path}";

        using (HttpClient client = new HttpClient())
        {
            HttpRequestMessage request = new HttpRequestMessage
            {
                Method = HttpMethod.Delete,  // Use DELETE method
                RequestUri = new Uri(url)
            };

            HttpResponseMessage response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                onSuccess?.Invoke("Document deleted successfully");
            }
            else
            {
                string errorData = await response.Content.ReadAsStringAsync();
                onError?.Invoke($"Error deleting document: {response.ReasonPhrase}, Details: {errorData}");
                throw new Exception($"Error deleting document: {response.ReasonPhrase}, Details: {errorData}");
            }
        }
    }

    public async void ValidatePath(string path, Action<bool> onSuccess, Action<string> onError = null)
    {
        string url = $"{baseUrl}{path}";

        using (HttpClient client = new HttpClient())
        {
            HttpResponseMessage response = await client.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                onSuccess?.Invoke(true);  // Document exists
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                onSuccess?.Invoke(false);  // Document does not exist
            }
            else
            {
                string errorData = await response.Content.ReadAsStringAsync();
                onError?.Invoke($"Error validating document: {response.ReasonPhrase}, Details: {errorData}");
                throw new Exception($"Error validating document: {response.ReasonPhrase}, Details: {errorData}");
            }
        }
    }

    #region Query
    public async void QueryItems(string path, string collectionName, DateTime filterTime, Action<List<string>> onSuccess, Action<string> onError = null)
    {
        string url = path.Equals("") ? $"{baseUrl[..^1]}:runQuery" : $"{baseUrl}{path}:runQuery";

        string formattedTime = filterTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

        string jsonRequest = $@"{{
    ""structuredQuery"": {{
        ""from"": [{{ ""collectionId"": ""{collectionName}"" }}],
        ""where"": {{
            ""fieldFilter"": {{
                ""field"": {{ ""fieldPath"": ""creationTime"" }},
                ""op"": ""GREATER_THAN"",
                ""value"": {{ ""timestampValue"": ""{formattedTime}"" }}
            }}
        }},
        ""orderBy"": [{{ ""field"": {{ ""fieldPath"": ""creationTime"" }}, ""direction"": ""ASCENDING"" }}]
    }}
}}";

        using (HttpClient client = new HttpClient())
        {
            HttpContent content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            try
            {
                HttpResponseMessage response = await client.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    string responseData = await response.Content.ReadAsStringAsync();
                    onSuccess?.Invoke(ConvertQueryResponse(responseData));
                }
                else
                {
                    string errorData = await response.Content.ReadAsStringAsync();
                    Debug.LogError($"Error fetching documents: {response.ReasonPhrase}, Details: {errorData}");
                    onError?.Invoke($"Error fetching documents: {response.ReasonPhrase}, Details: {errorData}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Exception: {ex.Message}");
                onError?.Invoke($"Exception: {ex.Message}");
            }
        }
    }


    public async void QueryItems(string path, string collectionName, int amount, Action<List<string>> onSuccess, Action<string> onError = null)
    {
        string url = path.Equals("") ? $"{baseUrl[..^1]}:runQuery" : $"{baseUrl}{path}:runQuery";

        string jsonRequest = $@"{{
        ""structuredQuery"": {{
            ""from"": [{{ ""collectionId"": ""{collectionName}"" }}],
            ""orderBy"": [{{ ""field"": {{ ""fieldPath"": ""creationTime"" }}, ""direction"": ""DESCENDING"" }}],
            ""limit"": {amount}
        }}
    }}";

        using (HttpClient client = new HttpClient())
        {
            HttpContent content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            try
            {
                HttpResponseMessage response = await client.PostAsync(url, content); // Use the modified URL

                if (response.IsSuccessStatusCode)
                {
                    string responseData = await response.Content.ReadAsStringAsync();
                    onSuccess?.Invoke(ConvertQueryResponse(responseData));
                }
                else
                {
                    string errorData = await response.Content.ReadAsStringAsync();
                    Debug.LogError($"Error fetching documents: {response.ReasonPhrase}, Details: {errorData}");
                    onError?.Invoke($"Error fetching documents: {response.ReasonPhrase}, Details: {errorData}"); 
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Exception: {ex.Message}");
                onError?.Invoke($"Exception: {ex.Message}");
            }
        }
    }

    List<string> ConvertQueryResponse(string jsonResponse)
    {
        var documents = JsonConvert.DeserializeObject<List<QueryResponse>>(jsonResponse);

        List<string> documentNames = new List<string>();
        foreach (var doc in documents)
        {
            documentNames.Add(doc.document.name.Split('/').Last());
        }

        return documentNames;
    }

    class QueryResponse
    {
        public Doc document { get; set; }

        public class Doc
        {
            public string name { get; set; }
            public Fields fields { get; set; }

            public class Fields
            {
                public Timestamp creationTime { get; set; }
                public class Timestamp
                {
                    public string timestampValue { get; set; }
                }
            }
        }

    }

    
    #endregion

    #region Helper
    private T ConvertResponse<T>(string data)
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
                    if (t.mapValue != null && t.mapValue.fields != null)
                    {
                        // Handle nested object mapping
                        var nestedObject = ParseNestedObject(field.FieldType, t.mapValue.fields);
                        field.SetValue(temp, nestedObject);
                    }
                    else
                    {
                        switch (field.FieldType.Name)
                        {
                            case nameof(String):
                                field.SetValue(temp, t.stringValue);
                                break;
                            case nameof(Int32):
                                field.SetValue(temp, int.Parse(t.integerValue));
                                break;
                            case nameof(Boolean):
                                field.SetValue(temp, bool.Parse(t.booleanValue));
                                break;
                            case nameof(Double):
                                field.SetValue(temp, double.Parse(t.doubleValue));
                                break;
                            case nameof(DateTime):
                                field.SetValue(temp, DateTime.Parse(t.timestampValue, null, System.Globalization.DateTimeStyles.RoundtripKind));
                                break;
                            default:
                                throw new Exception($"Error setting document: {field.FieldType.Name} type not supported");
                        }
                    }
                }
                else
                {
                    Console.Error.WriteLine($"Field '{field.Name}' not found in the document");
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
            }
        }

        return temp;
    }

    private object ParseNestedObject(Type objectType, Dictionary<string, FirestoreField> mapFields)
    {
        // Create an instance of the nested object type
        var nestedObject = Activator.CreateInstance(objectType);

        // Get fields of the nested object type
        var fields = objectType.GetFields(BindingFlags.Public | BindingFlags.Instance);

        foreach (var field in fields)
        {
            if (mapFields.TryGetValue(field.Name, out var fieldValue))
            {
                switch (field.FieldType.Name)
                {
                    case nameof(String):
                        field.SetValue(nestedObject, fieldValue.stringValue);
                        break;
                    case nameof(Int32):
                        field.SetValue(nestedObject, int.Parse(fieldValue.integerValue));
                        break;
                    case nameof(Boolean):
                        field.SetValue(nestedObject, bool.Parse(fieldValue.booleanValue));
                        break;
                    case nameof(Double):
                        field.SetValue(nestedObject, double.Parse(fieldValue.doubleValue));
                        break;
                    case nameof(DateTime):
                        field.SetValue(nestedObject, DateTime.Parse(fieldValue.timestampValue, null, System.Globalization.DateTimeStyles.RoundtripKind));
                        break;
                    default:
                        if (fieldValue.mapValue != null && fieldValue.mapValue.fields != null)
                        {
                            // Recursively handle further nested objects
                            var nested = ParseNestedObject(field.FieldType, fieldValue.mapValue.fields);
                            field.SetValue(nestedObject, nested);
                        }
                        else
                        {
                            Console.Error.WriteLine($"Unsupported type: {field.FieldType.Name}");
                        }
                        break;
                }
            }
            else
            {
                Console.Error.WriteLine($"Field '{field.Name}' not found in nested object");
            }
        }

        return nestedObject;
    }
    private object ParseArray(ArrayValue arrayValue, Type elementType, bool isList = false)
    {
        var values = arrayValue.values;
        var array = Array.CreateInstance(elementType, values.Count);

        for (int i = 0; i < values.Count; i++)
        {
            var element = values[i];
            object parsedValue = null;

            if (elementType == typeof(string))
            {
                parsedValue = element.stringValue;
            }
            else if (elementType == typeof(int))
            {
                parsedValue = int.Parse(element.integerValue);
            }
            else if (elementType == typeof(bool))
            {
                parsedValue = bool.Parse(element.booleanValue);
            }
            else if (elementType == typeof(double))
            {
                parsedValue = double.Parse(element.doubleValue);
            }
            else if (elementType == typeof(DateTime))
            {
                parsedValue = DateTime.Parse(element.timestampValue, null, System.Globalization.DateTimeStyles.RoundtripKind);
            }
            else
            {
                throw new Exception($"Error parsing array element: {elementType.Name} type not supported");
            }

            array.SetValue(parsedValue, i);
        }

        // If it's a List<> instead of an array, convert to a list
        if (isList)
        {
            var listType = typeof(List<>).MakeGenericType(elementType);
            var list = Activator.CreateInstance(listType);
            var addMethod = listType.GetMethod("Add");
            foreach (var item in array)
            {
                addMethod.Invoke(list, new[] { item });
            }
            return list;
        }

        return array;
    }

    public string ToJSON(Dictionary<string, object> fields)
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
            else if (field.Value is DateTime dateTimeValue) // Handle DateTime values
            {
                string timestampValue = dateTimeValue.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'");
                formattedFields.Append($"\"{field.Key}\":{{\"timestampValue\":\"{timestampValue}\"}},");
            }
            else if (field.Value is IEnumerable<object> array)
            {
                // Handle arrays
                formattedFields.Append(HandleArray(field.Key, array));

            }
            else if (field.Value is Dictionary<string, object> nestedDict) // Handle nested dictionaries
            {
                // Recursive call to handle nested dictionaries
                formattedFields.Append($"\"{field.Key}\":{{\"mapValue\":{ToJSON(nestedDict)}}},");
            }
            else
            {
                // Handle other unsupported data types
                Console.Error.WriteLine($"Unsupported data type for field '{field.Key}'");
            }
        }

        if (fields.Count > 0)
        {
            formattedFields.Length--; // Remove trailing comma
        }

        formattedFields.Append("}}");
        return formattedFields.ToString();
    }


    private string HandleArray(string fieldName, IEnumerable<object> array)
    {
        var formattedFields = new StringBuilder();
        formattedFields.Append($"\"{fieldName}\":{{\"arrayValue\":{{\"values\":[");

        foreach (var item in array)
        {
            if (item is string strItem)
            {
                formattedFields.Append($"{{\"stringValue\":\"{strItem}\"}},");
            }
            else if (item is int || item is long)
            {
                formattedFields.Append($"{{\"integerValue\":\"{item}\"}},");
            }
            else if (item is float || item is double)
            {
                formattedFields.Append($"{{\"doubleValue\":{item}}},");
            }
            else if (item is bool boolItem)
            {
                formattedFields.Append($"{{\"booleanValue\":{boolItem.ToString().ToLower()}}},");
            }
            else
            {
                Console.Error.WriteLine($"Unsupported array element type for field '{fieldName}'");
                return null; // Stop processing if an unsupported array type is encountered
            }
        }

        // Remove the trailing comma from the array and close the array JSON
        if (array.Any())
        {
            formattedFields.Length--; // Remove trailing comma
        }

        formattedFields.Append("]}},");
        return formattedFields.ToString();
    }

    #region Class
    [System.Serializable]
    public class FirestoreDocument
    {
        public Dictionary<string, FirestoreField> fields { get; set; }
    }

    [System.Serializable]
    public class FirestoreField
    {
        public string stringValue { get; set; }
        public string integerValue { get; set; }
        public string booleanValue { get; set; }
        public string doubleValue { get; set; }
        public string timestampValue { get; set; }
        public ArrayValue arrayValue { get; set; }
        public MapValue mapValue { get; set; }
    }

    public class MapValue
    {
        public Dictionary<string, FirestoreField> fields { get; set; }
    }


    [System.Serializable]
    public class ArrayValue
    {
        public List<FirestoreField> values { get; set; }
    }
    #endregion

    #endregion
}
