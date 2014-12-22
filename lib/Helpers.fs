namespace Weather_v1

open System
open System.Net
open System.Json
open Android.App
open Android.Content
open Android.Net

module Helpers = 

    // Load JSON file from url
    let loadJSON(url:string) = async {
        let request = HttpWebRequest.Create(url)
        use! response = request.AsyncGetResponse()
        use stream = response.GetResponseStream()
        return JsonObject.Load(stream)
    }

    // Return stored city
    let getSavedVal (key:string) = 
        let name = Application.Context.GetString(Resource_String.shared_pref_name)
        let prefs = Application.Context.GetSharedPreferences(name, FileCreationMode.Private)
        prefs.GetString(key, null)
    
    // Update stored city
    let saveVal (key:string) value = 
        let name = Application.Context.GetString(Resource_String.shared_pref_name)
        let sharedPref = Application.Context.GetSharedPreferences(name, FileCreationMode.Private)
        let prefEditor = sharedPref.Edit()
        prefEditor.PutString(key, value)
                  .Commit() |> ignore

    // Check if device has internet connection
    let hasInternetConnection (context:Context) = 
        match context.GetSystemService(Context.ConnectivityService) with
        | :? ConnectivityManager as cm -> (cm.ActiveNetworkInfo) <> null
        | _ -> false

    // Convert unix timestamp to DateTime
    let unixTimeToDateTime unix =
        let date = (new DateTime(1970, 1, 1)).AddSeconds(unix)
        date

    let capitalize (str:string) = str.[0].ToString().ToUpper() + str.Substring(1)

    // Remove quotes from the string caused by JSONValue.toString()
    let removeQuotes(str:string) = 
        let str' = str.Replace("\"", "");
        str'
