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

    let toCelsius temp = temp - 273.15

    //member this.spinner : ProgressBar

    override this.OnCreate(bundle) = 
        base.OnCreate(bundle)
        // Set our view from the "main" layout resource
        this.SetContentView(Resource_Layout.Main)

        // Get views
        let spinner = this.FindViewById<ProgressBar>(Resource_Id.progressBar)
        let dateText = this.FindViewById<TextView>(Resource_Id.dateText)
        let cityText = this.FindViewById<TextView>(Resource_Id.cityText)
        let descriptionText = this.FindViewById<TextView>(Resource_Id.descriptionText)
        let tempText = this.FindViewById<TextView>(Resource_Id.tempText)

        if this.isOnline() then
            // Call openweathermap API to get weather
            let url = @"http://api.openweathermap.org/data/2.5/weather?q=Turku,fi"

            // http://tomasp.net/blog/csharp-fsharp-async-intro.aspx/
            // http://developer.xamarin.com/recipes/android/web_services/consuming_services/call_a_rest_web_service/
            let downloadUrl(url:string) = async {
                let request = HttpWebRequest.Create(url)
                use! response = request.AsyncGetResponse()
                use stream = response.GetResponseStream()
                let json = JsonObject.Load(stream)
                let weather = {
                    city = json.Item("name").ToString()
                    temp = float (json.Item("main").Item("temp").ToString())
                    description = "cloudy"
                }
                // Update text values
                dateText.Text <- System.DateTime.Now.ToShortDateString()
                cityText.Text <- weather.city
                descriptionText.Text <- weather.description
                tempText.Text <- toCelsius(weather.temp).ToString()
                spinner.Visibility <- ViewStates.Gone
            }

            do downloadUrl(url) |> Async.RunSynchronously
    
    // Check if device has internet connection
    member this.isOnline() = 
        match this.GetSystemService(Context.ConnectivityService) with 
        | :? ConnectivityManager as cm -> (cm.ActiveNetworkInfo) <> null
        | _ -> false