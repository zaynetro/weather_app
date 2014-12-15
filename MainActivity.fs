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

// Load modules
open Helpers
open Weather

[<Activity(Label = "Weather_v1", MainLauncher = true)>]
type MainActivity() = 
    inherit Activity()

    // Save selected city
    let mutable city = "Turku"

    // Return stored city
    let getSelectedCity =
        let name = Application.Context.GetString(Resource_String.shared_pref_name)
        let prefs = Application.Context.GetSharedPreferences(name, FileCreationMode.Private)          
        prefs.GetString("City", null)

    // Update stored city
    let saveSelectedCity(city:string) = 
        let name = Application.Context.GetString(Resource_String.shared_pref_name)
        let sharedPref = Application.Context.GetSharedPreferences(name, FileCreationMode.Private)
        let prefEditor = sharedPref.Edit()
        prefEditor.PutString("City", city) |> ignore
        prefEditor.Commit()

    // Init activity
    override this.OnCreate(bundle) =
        base.OnCreate(bundle)

        // Set our view from the "main" layout resource
        this.SetContentView(Resource_Layout.Main)

        // Get views
        let dateText = this.FindViewById<TextView>(Resource_Id.dateText)
        let cityText = this.FindViewById<TextView>(Resource_Id.cityText)
        let descriptionText = this.FindViewById<TextView>(Resource_Id.descriptionText)
        let tempText = this.FindViewById<TextView>(Resource_Id.tempText)
        let spinner = this.FindViewById<Spinner>(Resource_Id.citiesSpinner)

        // Append adapter to spinner
        let citiesList = Array.sort [|"Turku"; "Helsinki"; "Tampere"; "Oulu"; "Rovaniemi"|]

        let citiesAdapter = new ArrayAdapter(this, Resource_Layout.city_spinner, citiesList)
        citiesAdapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem)
        spinner.Adapter <- citiesAdapter

        // Set saved previously city
        city <- getSelectedCity
        let index = 
            citiesList
            |> Array.findIndex(fun elem -> elem = city)
        spinner.SetSelection(index)

        // Handle new item selection 
        spinner.ItemSelected.Add(fun e ->
            let selected = spinner.GetItemAtPosition(e.Position)
            city <- selected.ToString()

            saveSelectedCity(city) |> ignore

            if this.isOnline() then
                // Call openweathermap API to get weather json
                let url = @"http://api.openweathermap.org/data/2.5/weather?q=" + city + ",fi"
                let json =
                    loadJSON(url)
                    |> Async.RunSynchronously
                
                let weather = {
                    city = string (json.["name"])
                    temp = float (json.["main"].["temp"])
                    description = removeQuotes(json.["weather"].[0].["description"].ToString())
                }

                this.RunOnUiThread(fun () ->
                    // Update text values
                    dateText.Text <- System.DateTime.Now.ToShortDateString()
                    cityText.Text <- weather.city
                    descriptionText.Text <- weather.description
                    tempText.Text <- KelvinToCelsiusString weather.temp
                )

        )

    // Check if device has internet connection
    member this.isOnline() =
        match this.GetSystemService(Context.ConnectivityService) with
        | :? ConnectivityManager as cm -> (cm.ActiveNetworkInfo) <> null
        | _ -> false
