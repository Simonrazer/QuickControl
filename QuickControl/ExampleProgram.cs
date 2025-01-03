namespace Example
{
  struct SomeStruct{
    public double yeah;
    public string yahoo {get;set;}
  }
  public class Banana{
    public double yeah{get;set;}
    public string yahoo = "asdadad";

    public Dictionary<int, string> test{get;set;}

    private SomeStruct[] arraytest;

    private int privateField;


    public Banana(){
      arraytest = new SomeStruct[2];
      arraytest[0].yeah = 0.0;
      arraytest[0].yahoo = "yeah man";
    }
  }



  public class ExampleProgram
  {
    
    public static void Main (string[] args)
    {
      Banana b = new Banana();
      
      b.yahoo = "ASD";
      b.yeah = 1;
      b.test = new Dictionary<int, string>();
      b.test.Add(0, "dicted");

      QuickControl.BrowserDisplayServer bd = new QuickControl.BrowserDisplayServer("http://localhost:8000/", b);
      bd.StartThread();

      Console.ReadLine();
      Console.WriteLine("main ended");
      
    }
  }
}