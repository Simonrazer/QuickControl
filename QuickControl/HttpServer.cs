
using System.Text;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Reflection;
using System.Collections;

namespace QuickControl{
public class IncludePrivateFieldsAndProperties : DefaultContractResolver
{
    protected override IList<JsonProperty> CreateProperties(System.Type type, MemberSerialization memberSerialization)
    {
        var props = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        List<JsonProperty> jsonProps = new List<JsonProperty>();

        foreach (var prop in props)
        {
            jsonProps.Add(base.CreateProperty(prop, memberSerialization));
        }

        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            bool include = true;
            foreach (var atr in field.CustomAttributes)
            {
                if (atr.AttributeType.AssemblyQualifiedName == null) continue;
                if (atr.AttributeType.AssemblyQualifiedName.Contains("CompilerGenerated"))
                {
                    include = false;
                    break;
                }
            }
            if (include)
                jsonProps.Add(base.CreateProperty(field, memberSerialization));
        }

        jsonProps.ForEach(p => { p.Writable = true; p.Readable = true; });
        return jsonProps;
    }
}
public class BrowserDisplayServer
{
    JsonSerializerSettings settings;
    string url;
    object state;
    public BrowserDisplayServer(string u, object st)
    {
        settings = new JsonSerializerSettings()
        {
            ContractResolver = new IncludePrivateFieldsAndProperties()
        };
        url = u;
        state = st;
    }

    public BrowserDisplayServer(JsonSerializerSettings s, string u, object st)
    {
        settings = s;
        url = u;
        state = st;
    }

    public void StartThread()
    {
        Thread thread1 = new Thread(Start);
        thread1.Start();
    }

    public void Start()
    {   
        // Create a Http server and start listening for incoming connections
        HttpServer server = new HttpServer(url, Website.txt, (string msgFromBrowser) => arrangeChange(state, msgFromBrowser), () => stateChanger(state));
        Console.WriteLine("Listening for connections on {0}", server.url);

        // Handle requests
        Task listenTask = server.HandleIncomingConnections();
        listenTask.GetAwaiter().GetResult();

        // Close the listener
        server.listener.Close();
    }

    public PropertyInfo? GetProp(object src, string propName)
    {
        return src.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    }
    public FieldInfo? GetField(object src, string propName)
    {
        return src.GetType().GetField(propName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    }

    public object? ConvertSave(string val, Type t)
    {
        object? res;
        try
        {
            res = Convert.ChangeType(val, t);
        }
        catch (Exception)
        {
            return null;
        }
        return res;
    }

    public object Change(object start, string[] path, string val)
    {
        PropertyInfo? prop = GetProp(start, path[0]);
        FieldInfo? field = GetField(start, path[0]);
        if (prop == null && field == null) return start;
        object? elemval;
        Type t;
        if (prop != null)
        {
            elemval = prop.GetValue(start, null);
            t = prop.PropertyType;
        }
        else if (field != null)
        {
            elemval = field.GetValue(start);
            t = field.FieldType;
        }
        else return start;
        if (elemval == null) return start;
        
        if (elemval is Array || (t.HasElementType && t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IList<>)))
        {
            //Array
            Type? arT = t.GetElementType();
            if (arT == null) return start;
            int index;
            bool couldParse = int.TryParse(path[1], out index);
            if (!couldParse) return start;
            if (arT.IsPrimitive || arT == typeof(decimal) || arT == typeof(string))
            {
                //Primitive
                object? converted = ConvertSave(val, arT);
                Array? ar = elemval as Array;
                if (ar == null) return start;
                if (converted != null) ar.SetValue(converted, index);
            }
            else
            {
                //Not Primitive
                Array? ar = elemval as Array;
                if (ar == null) return start;
                object? a = ar.GetValue(index);
                if (a != null) {
                    ar.SetValue(Change(a, path[2..path.Length], val), index);
                }
            }
        }
        else if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Dictionary<,>))
        {
            //Dictionary
            Type[] ts = t.GetGenericArguments();//key, value
            IDictionary dict = (IDictionary)elemval;
            //dict[Convert.ChangeType(path[1], ts[0])];

            if (ts[1].IsPrimitive || ts[1] == typeof(decimal) || ts[1] == typeof(string))
            {
                object? converted1 = ConvertSave(path[1], ts[0]);
                object? converted2 = ConvertSave(val, ts[1]);
                if (converted1 != null && converted2 != null) dict[converted1] = converted2;
            }
            else
            {
                object? converted = ConvertSave(path[1], ts[0]);
                if (converted != null)
                {
                    object? a = dict[converted];
                    if (a != null) dict[converted] = Change(a, path[2..path.Length], val);
                }
            }
        }
        else if (t.IsPrimitive || t == typeof(decimal) || t == typeof(string))
        {
            //Primitive
            object? converted = ConvertSave(val, t);
            if (converted == null) return start;
            if (prop != null) prop.SetValue(start, converted);
            else if (field != null) field.SetValue(start, converted); //else if is always true but maketh warning go away
        }
        else
        {
            //Object
            return Change(elemval, path[1..path.Length], val);
        }
        return start;
    }

    public void arrangeChange(object start, string msg)
    {
        string[] a = msg.Split("\n");
        string[] b = a[0].Split("->");
        start = Change(start, b[1..b.Length], a[1]);
    }

    public string stateChanger(object start)
    {
        return JsonConvert.SerializeObject(start, settings);
    }
}

// Filename:  HttpServer.cs        
// Author:    Benjamin N. Summerton <define-private-public>        
// License:   Unlicense (http://unlicense.org/)

//Edited By Simon Klüpfel 2024



public delegate void MessageCallback(string type);
public delegate string StateStringifyer();
class HttpServer
{
    public HttpListener listener;
    public string url;
    public MessageCallback stateChanger;
    public StateStringifyer stateStringifyer;
    public string pageData;

    public HttpServer(string u, string pd, MessageCallback sC, StateStringifyer sS)
    {
        url = u;
        pageData = pd;
        listener = new HttpListener();
        listener.Prefixes.Add(url);
        listener.Start();
        stateChanger = sC;
        stateStringifyer = sS;
    }

    public async Task HandleIncomingConnections()
    {
        bool runServer = true;

        // While a user hasn't visited the `shutdown` url, keep on handling requests
        while (runServer)
        {
            // Will wait here until we hear from a connection
            HttpListenerContext ctx = await listener.GetContextAsync();

            // Peel out the requests and response objects
            HttpListenerRequest req = ctx.Request;
            HttpListenerResponse resp = ctx.Response;

            if(req.Url == null) continue;
            if ((req.HttpMethod == "POST") && (req.Url.AbsolutePath == "/shutdown"))
            {
                Console.WriteLine("Shutdown requested");
                runServer = false;
            }

            // Make sure we don't increment the page views counter if `favicon.ico` is requested
            if (req.Url.AbsolutePath == "/favicon.ico")
                continue;

            // Get Request Body					
            Byte[] bytes = new Byte[65535];
            int length = ctx.Request.InputStream.Read(bytes, 0, bytes.Length);
            var incommingData = new byte[length];
            Array.Copy(bytes, 0, incommingData, 0, length);
            string requestMessage = Encoding.ASCII.GetString(incommingData);

            bool sendState = false;
            string state = "";
            if (requestMessage.Length > 0)
            {
                sendState = true;
                Console.WriteLine(requestMessage);
                if (requestMessage == "GETSTATE")
                {
                    state = stateStringifyer();
                }
                else
                {
                    Console.WriteLine(requestMessage);
                    stateChanger(requestMessage);
                    state = stateStringifyer();
                }
            }




            // Print out some info about the request
            Console.WriteLine(req.Url.ToString());
            Console.WriteLine(req.HttpMethod);
            Console.WriteLine(req.UserHostName);
            Console.WriteLine(req.UserAgent);
            Console.WriteLine();

            // If `shutdown` url requested w/ POST, then shutdown the server after serving the page

            // Write the response info
            string disableSubmit = !runServer ? "disabled" : "";
            byte[] data;
            if (sendState)
            {
                data = Encoding.UTF8.GetBytes(state);
                resp.ContentType = "json";
            }
            else
            {
                data = Encoding.UTF8.GetBytes(pageData);
                resp.ContentType = "text/html";
            }
            resp.ContentEncoding = Encoding.UTF8;
            resp.ContentLength64 = data.LongLength;

            // Write out to the response stream (asynchronously), then close it
            await resp.OutputStream.WriteAsync(data, 0, data.Length);
            resp.Close();
        }
    }
}
}