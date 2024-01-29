using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FishialAPI
{
    public class RootObject
    {
        [JsonProperty("results")]
        public List<Result> Results { get; set; }
    }
    public class Result
    {
        [JsonProperty("species")]
        public List<Species> Species { get; set; }
    }
    public class Species
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("accuracy")]
        public double Accuracy { get; set; }

        [JsonProperty("fishangler-data")]
        public FishanglerData FishanglerData { get; set; }
    }
    public class FishanglerData
    {
        [JsonProperty("title")]
        public string Title { get; set; }
    }

    internal class FishResult
    {
        public event EventHandler<string> Message;
        private void Notify(string message)
        {
            Message?.Invoke(this, message);
        }
        public Task FishResults(string JsonResponse)
        {
            RootObject rootObject = JsonConvert.DeserializeObject<RootObject>(JsonResponse);
            if (rootObject.Results == null || !rootObject.Results.Any())
            {
                Notify("No fish recognized");
            }
            foreach (var result in rootObject.Results)
            {
                foreach (var species in result.Species)
                {
                    Notify("Name: " + species.Name);
                    Notify("Accuracy: " + species.Accuracy);
                    Notify("Common name: " + species.FishanglerData.Title);
                    Notify("");
                }
            }
            return Task.Factory.StartNew(() => { });
        }
    }
}
