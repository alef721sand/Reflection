using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;

class F
{
    public int i1;
    public int i2;
    public int i3;
    public int i4;
    public int i5;

    public static F Get() => new F { i1 = 1, i2 = 2, i3 = 3, i4 = 4, i5 = 5 };
}

class Program
{
    static void Main()
    {
        const int iterations = 100000;

        F instance = F.Get();
        // мой рефлекшен (сериализация)
        Stopwatch sw = Stopwatch.StartNew();
        string manualSerialized = string.Empty;
        for (int i = 0; i < iterations; i++)
        {
            manualSerialized = ReflectionSerialize(instance);
        }
        sw.Stop();
        long manualSerializationTime = sw.ElapsedMilliseconds;
        sw.Restart();
        Console.WriteLine($"Мой рефлекшен:\n{manualSerialized}");
        Console.WriteLine($"Время сериализации: {manualSerializationTime} ms");
        sw.Stop();
        var consoleOutputTime = sw.ElapsedMilliseconds;
        Console.WriteLine($"Время вывода в консоль: {consoleOutputTime} ms");
        
        // NewtonsoftJson
        sw.Restart();
        string jsonSerialized = string.Empty;
        for (int i = 0; i < iterations; i++)
        {
            jsonSerialized = JsonConvert.SerializeObject(instance);
        }
        sw.Stop();
        long jsonSerializationTime = sw.ElapsedMilliseconds;
        Console.WriteLine($"NewtonsoftJson: {jsonSerialized}");
        Console.WriteLine($"Время на сериализацию: {jsonSerializationTime} ms");
        Console.WriteLine($"Результат сравнения(NewtonsoftJson & Мой рефлекшен: {jsonSerializationTime - manualSerializationTime} ms");


        // мой рефлекшен (десериализация)
        sw.Restart();
        F deserializedInstance = null;
        for (int i = 0; i < iterations; i++)
        {
            deserializedInstance = ReflectionDeserialize<F>(manualSerialized);
        }

        sw.Stop();
        long manualDeserializationTime = sw.ElapsedMilliseconds;
        Console.WriteLine($"Десериализация моя: i1={deserializedInstance.i1}, i2={deserializedInstance.i2}, i3={deserializedInstance.i3}, i4={deserializedInstance.i4}, i5={deserializedInstance.i5}");
        Console.WriteLine($"Время десериализации: {manualDeserializationTime} ms");


        // 5. JSON Deserialization
        Console.WriteLine("\nJSON Deserialization:");
        sw.Restart();

        for (int i = 0; i < iterations; i++)
        {
            deserializedInstance = JsonConvert.DeserializeObject<F>(jsonSerialized);
        }

        sw.Stop();
        long jsonDeserializationTime = sw.ElapsedMilliseconds;
        Console.WriteLine($"Десериализация JSON Object: i1={deserializedInstance.i1}, i2={deserializedInstance.i2}, i3={deserializedInstance.i3}, i4={deserializedInstance.i4}, i5={deserializedInstance.i5}");
        Console.WriteLine($"Время десериализации JSON: {jsonDeserializationTime} ms");
    }

    static string ReflectionSerialize(object obj)
    {
        Type type = obj.GetType();
        PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
        StringBuilder sb = new StringBuilder();
        var names = fields.Select(p => $"{p.Name}").ToList();
        names.AddRange(properties.Select(p => p.Name).ToList());
        string fieldNames = string.Join(";", names) + ";";
        
        var values = fields.Select(p => p.GetValue(obj)).ToList();
        values.AddRange(properties.Select(p => p.GetValue(obj)));
        var fieldValues = string.Join(";", values) + ";";

        sb.AppendLine(fieldNames);
        sb.AppendLine(fieldValues);
        return sb.ToString();
    }

    static T ReflectionDeserialize<T>(string serializedString) where T : new()
    {
        T result = new T();
        Type type = typeof(T);
        PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
        List<string> lines = new List<string>(serializedString.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None));
        string[] fieldNames = lines[0].Split(';', StringSplitOptions.RemoveEmptyEntries);
        string[] fieldValues = lines[1].Split(';', StringSplitOptions.RemoveEmptyEntries);


        for (var i=0;i < fieldNames.Length; i++)
        {
            var property = properties.FirstOrDefault(p => p.Name == fieldNames[i]);
            if (property != null)
            {
                object value = Convert.ChangeType(fieldValues[i], property.PropertyType);
                property.SetValue(result, value);
            }
            else
            {
                var field = fields.FirstOrDefault(p => p.Name == fieldNames[i]);
                if (field != null)
                {
                    object value = Convert.ChangeType(fieldValues[i], field.FieldType);
                    field.SetValue(result, value);
                }
            }
        }
        return result;
    }
}
