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
    let mutable cityName = "Turku"
    let mutable graph:LinearLayout = null
    [<DefaultValue>] val mutable city:CityType
    let mutable tempText:TextView = null
    
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
        prefEditor.PutString(key, value) |> ignore
        prefEditor.Commit() |> ignore
    
    // Init activity  
    override this.OnCreate(bundle) =
        base.OnCreate(bundle)

        // Set our view from the "main" layout resource
        this.SetContentView(Resource_Layout.Main)

        // Get views
        let dateText = this.FindViewById<TextView>(Resource_Id.dateText)
        let cityText = this.FindViewById<TextView>(Resource_Id.cityText)
        let descriptionText = this.FindViewById<TextView>(Resource_Id.descriptionText)
        tempText <- this.FindViewById<TextView>(Resource_Id.tempText)
        let spinner = this.FindViewById<Spinner>(Resource_Id.citiesSpinner)
        graph <- this.FindViewById<LinearLayout>(Resource_Id.graph)
        let progressBar = this.FindViewById<ProgressBar>(Resource_Id.progressBar)
        let scaleSwitch = this.FindViewById<Switch>(Resource_Id.scaleSwitch)

        // Append adapter to spinner
        let citiesList = Array.sort [| "Turku"; "Helsinki"; "Tampere"; "Oulu"; "Rovaniemi" |]
        let citiesAdapter = new ArrayAdapter(this, Resource_Layout.city_spinner, citiesList)
        citiesAdapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem)
        spinner.Adapter <- citiesAdapter

        // Set saved previously city
        cityName <- getSavedVal "city"
        match cityName with
        | null -> ()
        | _ ->
            let index = citiesList |> Array.findIndex (fun elem -> elem = cityName)
            spinner.SetSelection(index)
        
        this.drawBar()

        scaleSwitch.CheckedChange.Add(this.toggleDegreeScale)

        // Handle new item selection
        spinner.ItemSelected.Add(fun e ->
            // Show progress bar
            this.RunOnUiThread(fun () ->
                progressBar.Visibility <- ViewStates.Visible
            )

            // Define background process function
            let backgroundLoad () =
                // Call openweathermap API to get weather json
                let url = @"http://api.openweathermap.org/data/2.5/weather?q=" + cityName + ",fi"
                this.city <-
                    loadJSON (url) 
                    |> Async.RunSynchronously  
                    |> jsonToCity
                
                // Update UI
                this.RunOnUiThread(fun () ->
                    progressBar.Visibility <- ViewStates.Invisible
                    dateText.Text <- System.DateTime.Now.ToShortDateString()
                    cityText.Text <- this.city.name
                    descriptionText.Text <- this.city.weather.description
                )

                // Set scale to saved value => update temperature value
                let scale = getSavedVal "scale"
                this.RunOnUiThread(fun () ->
                    // When setting true toggle event is triggered,
                    // but when setting false - not (why?)
                    scaleSwitch.Checked <- true
                    match scale with
                    | "false" -> scaleSwitch.Toggle()
                    | _ -> ()
                )
            
            // Save selected city
            let selected = spinner.GetItemAtPosition(e.Position)
            cityName <- selected.ToString()
            saveVal "city" cityName

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
    
    // Experiments
    member this.drawBar() = 
        let elem = new LinearLayout(this)
        let param = new ViewGroup.LayoutParams(30, 400)
        elem.LayoutParameters <- param
        elem.SetBackgroundColor(Color.White)
        graph.AddView(elem)

    // Trigger scaleSwitch change event
    member this.toggleDegreeScale (e:CompoundButton.CheckedChangeEventArgs) =
        let saveScale = saveVal "scale"
        // TO DO: move saveScale to separate block
        let value =
            if e.IsChecked then
                saveScale "true"
                KelvinToCelsiusString this.city.weather.temp.cur
            else
                saveScale "false"
                KelvinToFahrenheitString this.city.weather.temp.cur

        this.RunOnUiThread(fun () ->
            tempText.Text <- value
        )

    (*
    Load weather forecast from http://api.openweathermap.org/data/2.5/forecast?q=Turku,fi
    *)
