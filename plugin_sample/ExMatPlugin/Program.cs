namespace ExMatPlugin
{
    internal class Program
    {
        public static int Main(string[] args)
        {
            System.Console.WriteLine("This solution includes a sampel plugin!");
            System.Console.WriteLine("Use -plugin:\"{path_to_dll}\" console parameter with exmat to include this plugin");
            System.Console.WriteLine("Press any key to quit...");
            System.Console.ReadKey(true);
            return args.Length;
        }
    }
}
