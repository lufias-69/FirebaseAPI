using Newtonsoft.Json; //com.unity.nuget.newtonsoft-json
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;


/// <summary>
/// Represents a class for interacting with Firestore API.
/// </summary>
public class FirestoreAPI
{
    private readonly string projectId;
    private readonly string baseUrl;

    // Initialize the FirestoreAPI with project ID
    /// <summary>
    /// Initializes the FirestoreAPI with the specified project ID.
    /// </summary>
    /// <param name="projectId">The project ID.</param>
    public FirestoreAPI(string projectId)
    {
        this.projectId = projectId;
        this.baseUrl = $"https://firestore.googleapis.com/v1/projects/{projectId}/databases/(default)/documents/";
    }

    #region Get Data

    /// <summary>
    /// Retrieves data from Firestore based on the specified path.
    /// </summary>
    /// <param name="path">The path to the document.</param>
    /// <param name="onSuccess">The callback function to invoke on success.</param>
    /// <param name="onError">The callback function to invoke on error.</param>
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

    /// <summary>
    /// Retrieves a specific field's value from Firestore based on the specified path and field name.
    /// </summary>
    /// <param name="path">The path to the document.</param>
    /// <param name="fieldName">The name of the field.</param>
    /// <param name="onSuccess">The callback function to invoke on success.</param>
    /// <param name="onError">The callback function to invoke on error.</param>
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
    #endregion

    #region Set Data
    
    /// <summary>
    /// Sets a specific field's value in Firestore based on the specified path and field name.
    /// </summary>
    /// <param name="path">The path to the document.</param>
    /// <param name="fieldName">The name of the field.</param>
    /// <param name="value">The value to set.</param>
    /// <param name="onSuccess">The callback function to invoke on success.</param>
    /// <param name="onError">The callback function to invoke on error.</param>
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
        else if (value is Array array)
        {
            fieldType = "arrayValue";
            StringBuilder arrayBuilder = new StringBuilder();
            arrayBuilder.Append("{ \"values\": [");

            foreach (var item in array)
            {
                if (item is string strItem)
                {
                    arrayBuilder.Append($"{{ \"stringValue\": \"{strItem}\" }},");
                }
                else if (item is int || item is long)
                {
                    arrayBuilder.Append($"{{ \"integerValue\": {item} }},");
                }
                else if (item is float || item is double)
                {
                    arrayBuilder.Append($"{{ \"doubleValue\": {item} }},");
                }
                else if (item is bool boolItem)
                {
                    arrayBuilder.Append($"{{ \"booleanValue\": {boolItem.ToString().ToLower()} }},");
                }
                else
                {
                    onError?.Invoke("Unsupported array element type.");
                    return;
                }
            }

            // Remove the trailing comma and close the JSON object
            if (arrayBuilder[arrayBuilder.Length - 1] == ',')
                arrayBuilder.Remove(arrayBuilder.Length - 1, 1);

            arrayBuilder.Append("]}");
            fieldValue = arrayBuilder.ToString();
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

    /// <summary>
    /// Sets multiple fields' values in Firestore based on the specified path and dictionary of field-value pairs.
    /// </summary>
    /// <param name="path">The path to the document.</param>
    /// <param name="dict">The dictionary of field-value pairs.</param>
    /// <param name="onSuccess">The callback function to invoke on success.</param>
    /// <param name="onError">The callback function to invoke on error.</param>
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

    /// <summary>
    /// Converts the Firestore API response data to the specified type.
    /// </summary>
    /// <typeparam name="T">The type to convert the response data to.</typeparam>
    /// <param name="data">The response data that you got from firestore.</param>
    /// <returns>The converted response data.</returns>
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
                            // Check if the field is an array or a generic list
                            if (field.FieldType.IsArray)
                            {
                                var elementType = field.FieldType.GetElementType();
                                var array = ParseArray(t.arrayValue, elementType);
                                field.SetValue(temp, array);
                            }
                            else if (field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(List<>))
                            {
                                var elementType = field.FieldType.GetGenericArguments()[0];
                                var list = ParseArray(t.arrayValue, elementType, true);
                                field.SetValue(temp, list);
                            }
                            else
                            {
                                throw new Exception($"Error setting document: {field.FieldType.Name} type not supported");
                            }
                            break;
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

    private static object ParseArray(ArrayValue arrayValue, Type elementType, bool isList = false)
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

    private static string ToJSON(Dictionary<string, object> fields)
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
            else if (field.Value is IEnumerable<object> array)
            {
                // Handle arrays
                formattedFields.Append(HandleArray(field.Key, array));
            }
            else
            {
                // Handle other data types as needed
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

    private static string HandleArray(string fieldName, IEnumerable<object> array)
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
        public ArrayValue arrayValue { get; set; } // Updated to ArrayValue type
    }

    [System.Serializable]
    public class ArrayValue
    {
        public List<FirestoreField> values { get; set; }
    }
    #endregion

    #endregion
}
