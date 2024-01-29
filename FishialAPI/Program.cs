// See https://aka.ms/new-console-template for more information

namespace FishialAPI
{
    class Program
    {
        static async Task Main()
        {
            string imagepath = "..\\..\\..\\whiting.jpg";
            FishialAPI fishialAPI = new FishialAPI();
            fishialAPI.Message += (sender, message) => Console.WriteLine(message);
            await fishialAPI.ProcessNewImage(imagepath);
        }
    }
}

