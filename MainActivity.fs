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

[<AllowNullLiteral>] 
type ForecastListAdapter(context:Context, forecast) =
    inherit BaseExpandableListAdapter()

    override this.GetChild(groupPosition, childPosition) = new Java.Lang.Object()

    override this.GetChildId(groupPosition, childPosition) = int64 childPosition

    override this.GetChildrenCount(groupPosition) = 1

    override this.GetChildView(groupPosition, childPosition, idLastChild, convertView, parent) = 
        let mutable view = convertView
        if view = null then
            let inflator = LayoutInflater.FromContext context
            view <- inflator.Inflate(Resource_Layout.ForecastChild, parent, false)

        let item = forecast.list.[groupPosition]
        // Get views
        let rangeText = view.FindViewById<TextView>(Resource_Id.rangeText)
        let pressureText = view.FindViewById<TextView>(Resource_Id.pressureText)
        let humidityText = view.FindViewById<TextView>(Resource_Id.humidityText)
        let descriptionText = view.FindViewById<TextView>(Resource_Id.descriptionText)
        // Set values
        rangeText.Text <- formatRange convertKtoCString item.temp.min item.temp.max
        pressureText.Text <- formatPressure item.temp.pressure
        humidityText.Text <- formatHumidity item.temp.humidity
        descriptionText.Text <- capitalize item.description
        view

    override this.GetGroup(groupPosition) = new Java.Lang.Object()

    override this.GetGroupId(groupPosition) = int64 groupPosition

    override this.GetGroupView(groupPosition, isExpanded, convertView, parent) =
        let mutable view = convertView
        if view = null then
            let inflator = LayoutInflater.FromContext context
            view <- inflator.Inflate(Resource_Layout.ForecastGroup, parent, false)

        let item = forecast.list.[groupPosition]
        // Get views
        let dayText = view.FindViewById<TextView>(Resource_Id.dayText)
        let tempText = view.FindViewById<TextView>(Resource_Id.tempText)
        // Set values
        dayText.Text <- item.date.ToLocalTime().ToString()
        tempText.Text <- convertKtoCString item.temp.cur
        view

    override this.IsChildSelectable(groupPosition, childPosition) = false

    override this.GroupCount with get() = Array.length forecast.list

    override this.HasStableIds with get() = false


[<Activity(Label = "Weather_v1", MainLauncher = true)>]
type MainActivity() = 
    inherit Activity()
    // Save selected city
    let mutable cityName = "Turku"
    [<DefaultValue>] val mutable city:CityWeatherType
    [<DefaultValue>] val mutable forecast:ForecastType

    // views
    let mutable tempText:TextView = null
    let mutable dateText:TextView = null
    let mutable descriptionText:TextView = null
    let mutable progressBar:ProgressBar = null
    let mutable spinner:Spinner = null
    let mutable celsiusScale:RadioButton = null
    let mutable fahrenheitScale:RadioButton = null
    let mutable forecastList:ExpandableListView = null

    let mutable forecastAdapter = null
    
    // Init activity  
    override this.OnCreate(bundle) =
        base.OnCreate(bundle)

        // Set our view from the "main" layout resource
        this.SetContentView(Resource_Layout.Main)

        // Get views
        dateText <- this.FindViewById<TextView>(Resource_Id.dateText)
        descriptionText <- this.FindViewById<TextView>(Resource_Id.descriptionText)
        tempText <- this.FindViewById<TextView>(Resource_Id.tempText)
        progressBar <- this.FindViewById<ProgressBar>(Resource_Id.progressBar)
        spinner <- this.FindViewById<Spinner>(Resource_Id.citiesSpinner)
        celsiusScale <- this.FindViewById<RadioButton>(Resource_Id.celsiusScale)
        fahrenheitScale <- this.FindViewById<RadioButton>(Resource_Id.fahrenheitScale)
        forecastList <- this.FindViewById<ExpandableListView>(Resource_Id.forecastList)

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

        // Handle new item selection
        spinner.ItemSelected.Add(this.triggerCitySelect)

        let scale = getSavedVal "scale"
        this.RunOnUiThread(fun () ->
            // Check saved scale
            match scale with
            | "F" -> fahrenheitScale.Checked <- true
            | _ -> celsiusScale.Checked <- true
        )

        // Radio buttons click listeners
        celsiusScale.Click.AddHandler(new EventHandler(this.triggerDegreeScale))
        fahrenheitScale.Click.AddHandler(new EventHandler(this.triggerDegreeScale))
    
    // Check if device has internet connection
    member this.isOnline() = hasInternetConnection this

    // Set temperature value
    member this.setTempVal scale =
        let value =
            match scale with
            | "C" -> convertKtoCString this.city.weather.temp.cur
            | "F" -> convertKtoFString this.city.weather.temp.cur
            | _ -> ""
        
        this.RunOnUiThread(fun () ->
            tempText.Text <- value
        )

    // Trigger temp scale change
    member this.triggerDegreeScale sender e =
        let radio = sender :?> RadioButton
        let scale = radio.Text.Chars(1).ToString()
        saveVal "scale" scale
        this.setTempVal scale

    // Trigger city selection
    member this.triggerCitySelect e =
        // Show progress bar
        this.RunOnUiThread(fun () ->
            progressBar.Visibility <- ViewStates.Visible
        )

        // Define background process function
        let backgroundLoad() =
            // Call openweathermap API to get weather json
            let url = @"http://api.openweathermap.org/data/2.5/weather?q=" + cityName + ",fi"
            this.city <-
                loadJSON (url) 
                |> Async.RunSynchronously  
                |> jsonToCityWeather
            
            this.setTempVal (getSavedVal "scale")
            // Update UI
            this.RunOnUiThread(fun () ->
                dateText.Text <- System.DateTime.Now.ToShortDateString()
                descriptionText.Text <- this.city.weather.description
            )

            // Load forecast in different thread
            let thread = new Thread(new ThreadStart(this.loadForecast))
            thread.Start()
        
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
            alert.SetTitle("No internet connection")
                 .SetMessage("You device is not connected to the internet")
                 .SetNegativeButton("Done", fun sender e -> ()) |> ignore
            this.RunOnUiThread(fun () ->
                alert.Show() |> ignore
                progressBar.Visibility <- ViewStates.Invisible
            )

    member this.loadForecast() =
        // Call openweathermap API to get forecast json
        let url = @"http://api.openweathermap.org/data/2.5/forecast?q=" + cityName + ",fi"
        this.forecast <-
            loadJSON (url) 
            |> Async.RunSynchronously  
            |> jsonToForecast
        
        // Forecast adapter
        forecastAdapter <- new ForecastListAdapter(this, this.forecast)

        // Update UI
        this.RunOnUiThread(fun () ->
            progressBar.Visibility <- ViewStates.Invisible
            forecastList.SetAdapter(forecastAdapter)
        )
