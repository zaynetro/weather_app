namespace Weather_v1

open System.Net
open System.Json

module Helpers = 

    // Load JSON file from url
    let loadJSON(url:string) = async {
        let request = HttpWebRequest.Create(url)
        use! response = request.AsyncGetResponse()
        use stream = response.GetResponseStream()
        return JsonObject.Load(stream)
    }

    // Remove quotes from the string caused by JSONValue.toString()
    let removeQuotes(str:string) = 
        let copy = str.Replace("\"", "");
        copy
