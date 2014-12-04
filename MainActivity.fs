namespace Weather_v1

open System
open Android.App
open Android.Content
open Android.OS
open Android.Runtime
open Android.Views
open Android.Widget
open Android.Net
open System.Net
open System.Json

type Weather = { 
   city : string
   temp : float
   description : string
}

[<Activity(Label = "Weather_v1", MainLauncher = true)>]
type MainActivity() = 
    inherit Activity()

    let toCelsius temp = System.Math.Round(temp - 273.15)
    let formatDegrees temp = 
        let temp = toCelsius(temp)
        let mutable out = temp.ToString()
        if temp > 0.0 then
            out <- "+" + out
        out + "°C"
            
    let mutable city = "Turku"

    // http://tomasp.net/blog/csharp-fsharp-async-intro.aspx/
    // http://developer.xamarin.com/recipes/android/web_services/consuming_services/call_a_rest_web_service/
    let loadJSON(url:string) = async {
        let request = HttpWebRequest.Create(url)
        use! response = request.AsyncGetResponse()
        use stream = response.GetResponseStream()
        return JsonObject.Load(stream)
    }

    override this.OnCreate(bundle) =                 
        base.OnCreate(bundle)
        // Set our view from the "main" layout resource
        this.SetContentView(Resource_Layout.Main)

        // Get views
        let dateText = this.FindViewById<TextView>(Resource_Id.dateText)
        let cityText = this.FindViewById<TextView>(Resource_Id.cityText)
        let descriptionText = this.FindViewById<TextView>(Resource_Id.descriptionText)
        let tempText = this.FindViewById<TextView>(Resource_Id.tempText)

        // Set up spinner
        let spinner = this.FindViewById<Spinner>(Resource_Id.citiesSpinner)
        // Load list of cities from somewhere
        let citiesList = [|"Turku"; "Helsinki"; "Tampere"|]
        let citiesAdapter = new ArrayAdapter(this, Android.Resource.Layout.SimpleSpinnerItem, citiesList)
        citiesAdapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem)
        spinner.Adapter <- citiesAdapter

        // Handle new item selection 
        spinner.ItemSelected.Add(fun e -> 
            let selected = spinner.GetItemAtPosition(e.Position)
            city <- selected.ToString()

            if this.isOnline() then
                // Call openweathermap API to get weather
                let url = @"http://api.openweathermap.org/data/2.5/weather?q=" + city + ",fi"
                let json =
                    loadJSON(url) 
                    |> Async.RunSynchronously
                
                let weather = {
                    city = json.Item("name").ToString()
                    temp = float (json.Item("main").Item("temp").ToString())
                    description = json.Item("weather").[0].Item("description").ToString()
                }

                this.RunOnUiThread(fun () -> 
                    // Update text values   
                    dateText.Text <- System.DateTime.Now.ToShortDateString()
                    cityText.Text <- weather.city
                    descriptionText.Text <- weather.description
                    tempText.Text <- formatDegrees(weather.temp)
                )

        )

    // Check if device has internet connection
    member this.isOnline() = 
        match this.GetSystemService(Context.ConnectivityService) with 
        | :? ConnectivityManager as cm -> (cm.ActiveNetworkInfo) <> null
        | _ -> false
