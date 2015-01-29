namespace weather_app

open System
open Android.App
open Android.Content
open Android.OS
open Android.Runtime
open Android.Views
open Android.Widget

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

