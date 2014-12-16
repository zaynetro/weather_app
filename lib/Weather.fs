namespace Weather_v1

open Helpers
open System.Json

type WindType = {
    speed : float
    deg : int
}

type TempType = {
    cur : float
    min : float
    max : float
    humidity : int
    pressure : int
}

type LatLngType = {
    lat : float
    lon : float
}

type WeatherType = {
    temp : TempType
    wind : WindType
    description : string
}

type CityType = {
    id : int
    name : string
    coords : LatLngType
    country : string
    weather : WeatherType
}

module Weather = 
    // Temperature scales transformation
    let KelvinToCelsius temp = System.Math.Round(temp - 273.15)
    let KelvinToFahrenheit temp = System.Math.Round(temp * 1.8 - 459.67)

    // Add scale to the temperature
    let formatDegrees scale temp =
        let sign = 
            if temp > 0.0 then "+" 
            elif temp < 0.0 then "-"
            else "" // zero case
        sign + abs(temp).ToString() + "°" + scale

    // Transform Kelvin to string with degrees mark
    let KelvinToCelsiusString = KelvinToCelsius >> formatDegrees "C"
    let KelvinToFahrenheitString = KelvinToFahrenheit >> formatDegrees "F"

    // Get LatLng variable from JsonValue
    let jsonToLatLng (json:JsonValue) =
        let coord = {
            lat = float (json.["lat"])
            lon = float (json.["lon"])
        }
        coord

    // Get Temp variable from JsonValue
    let jsonToTemp (json:JsonValue) =
        let temp = {
            cur = float (json.["temp"])
            min = float (json.["temp_min"])
            max = float (json.["temp_max"])
            humidity = int (json.["humidity"])
            pressure =  int (json.["pressure"])
        }
        temp

    // Get Wind variable from JsonValue
    let jsonToWind (json:JsonValue) =
        let wind = {
            speed = float (json.["speed"])
            deg = int (json.["deg"])
        }
        wind

    // Get Weather variable from JsonValue
    let jsonToWeather (json:JsonValue) =
        let weather = {
            temp = jsonToTemp json.["main"]
            wind = jsonToWind json.["wind"]
            description = removeQuotes (json.["weather"].[0].["description"].ToString())
        }
        weather

    // Get City variable from JsonValue
    let jsonToCity (json:JsonValue) =
        let city = {
            id = int (json.["id"])
            name = removeQuotes (json.["name"].ToString())
            coords = jsonToLatLng json.["coord"]
            country = removeQuotes (json.["sys"].["country"].ToString())
            weather = jsonToWeather json
        }
        city