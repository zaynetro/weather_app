namespace Weather_v1

open System
open Android.App
open Android.Content
open Android.OS
open Android.Runtime
open Android.Views
open Android.Widget
open Android.Net
open Android.Graphics
open System.Net
open System.Json
open System.Threading

// Load modules
open Helpers
open Weather

[<Activity(Label = "Weather_v1", MainLauncher = true)>]
type MainActivity() = 
    inherit Activity()
    // Save selected city
    let mutable city = "Turku"
    let mutable graph:LinearLayout = null
    
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
        graph <- this.FindViewById<LinearLayout>(Resource_Id.graph)
        let progressBar = this.FindViewById<ProgressBar>(Resource_Id.progressBar)

        // Append adapter to spinner
        let citiesList = Array.sort [| "Turku"; "Helsinki"; "Tampere"; "Oulu"; "Rovaniemi" |]
        let citiesAdapter = new ArrayAdapter(this, Resource_Layout.city_spinner, citiesList)
        citiesAdapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem)
        spinner.Adapter <- citiesAdapter

        // Set saved previously city
        city <- getSelectedCity
        match city with
        | null -> ()
        | _ ->
            let index = citiesList |> Array.findIndex (fun elem -> elem = city)
            spinner.SetSelection(index)
        
        this.drawBar()

        // Handle new item selection 
        spinner.ItemSelected.Add(fun e ->
            // Show progress bar
            this.RunOnUiThread(fun () ->
                progressBar.Visibility <- ViewStates.Visible
            )

            // Define background process function
            let backgroundLoad () =
                // Call openweathermap API to get weather json
                let url = @"http://api.openweathermap.org/data/2.5/weather?q=" + city + ",fi"
                let fetchedCity = 
                    loadJSON (url) 
                    |> Async.RunSynchronously  
                    |> jsonToCity
                
                // Update UI
                this.RunOnUiThread(fun () ->
                    progressBar.Visibility <- ViewStates.Invisible
                    // Update text values
                    dateText.Text <- System.DateTime.Now.ToShortDateString()
                    cityText.Text <- fetchedCity.name
                    descriptionText.Text <- fetchedCity.weather.description
                    tempText.Text <- KelvinToCelsiusString fetchedCity.weather.temp.cur
                )
            
            // Save selected city
            let selected = spinner.GetItemAtPosition(e.Position)
            city <- selected.ToString()
            saveSelectedCity (city) |> ignore

            if this.isOnline() then              
                // Load weather in different thread
                let thread = new Thread(new ThreadStart(backgroundLoad))
                thread.Start()                
            else
                let alert = new AlertDialog.Builder(this)
                alert.SetTitle("No internet connection") |> ignore
                alert.SetMessage("You device is not connected to the internet") |> ignore
                (*alert.SetNegativeButton("Done", fun (sender, e) ->
                    // close dialog
                    printf("body")
                ) |> ignore*)
                this.RunOnUiThread(fun () ->
                    alert.Show() |> ignore
                    progressBar.Visibility <- ViewStates.Invisible
                )
        )
    
    // Check if device has internet connection
    member this.isOnline() = 
        match this.GetSystemService(Context.ConnectivityService) with
        | :? ConnectivityManager as cm -> (cm.ActiveNetworkInfo) <> null
        | _ -> false

    member this.drawBar() = 
        let elem = new LinearLayout(this)
        let param = new ViewGroup.LayoutParams(30, 400)
        elem.LayoutParameters <- param
        elem.SetBackgroundColor(Color.White)
        graph.AddView(elem)

    (*
    Load weather forecast from http://api.openweathermap.org/data/2.5/forecast?q=Turku,fi
    *)
