using RimWorld;
using Verse;

namespace TestMod
{
    [StaticConstructorOnStartup]
    public static class MyMod
    {
        static MyMod() //our constructor
        {
            Log.Message("Hello World!"); //Outputs "Hello World!" to the dev console.
            string message = "";
            
        }
    }
}