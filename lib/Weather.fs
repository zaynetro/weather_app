namespace Weather_v1

open System
open System.Json

open Helpers

[<Measure>] type K
[<Measure>] type degC
[<Measure>] type degF

type TempValue = 
    | Kelvin of float<K>
    | Celsius of float<degC>
    | Fahrenheit of float<degF>

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
    date : DateTime
}

type CityType = {
    id : int
    name : string
    coords : LatLngType
    country : string
}

type CityWeatherType = {
    city : CityType
    weather : WeatherType
}

type ForecastType = {
    city : CityType
    list : WeatherType[]
}

module Weather = 
    // Temperature scales transformation
    let KelvinToCelsius temp = System.Math.Round(temp - 273.15)
    let KelvinToFahrenheit temp = System.Math.Round(temp * 1.8 - 459.67)

    // Convert temperatures using units of measure
    let convertKtoC (temp:float<K>) = temp * 1.0<degC/K> - 273.15<degC>
    let convertKtoF (temp:float<K>) = temp * 1.8<degF/K> - 459.67<degF>

    let convertStringtoK str = let temp = (float str) * 1.0<K> in str

    // Add scale to the temperature
    let formatDegrees (scale:string) (temp:float<'u>) =
        let sign = 
            if temp > 0.0<_> then "+" 
            elif temp < 0.0<_> then "-"
            else "" // zero case
        sign + abs(temp).ToString() + "°" + scale

    // Transform Kelvin to string with degrees mark
    let KelvinToCelsiusString = KelvinToCelsius >> formatDegrees "C"
    let KelvinToFahrenheitString = KelvinToFahrenheit >> formatDegrees "F"

    let convertKtoCString = convertKtoC >> formatDegrees "C"

    // Format range
    let formatRange formatter min max =  formatter min + " — " + formatter max

    // Format humidity (add percent mark)
    let formatHumidity h = h.ToString() + "%"

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
        let temp = json.["main"]
        let wind = json.["wind"]
        let desc = json.["weather"].[0].["description"]
        let weather = {
            temp = jsonToTemp json.["main"]
            wind = jsonToWind json.["wind"]
            description = removeQuotes (json.["weather"].[0].["description"].ToString())
            date = unixTimeToDateTime (float (json.["dt"]))
        }
        weather

    // Get City variable from JsonValue
    let jsonToCity (json:JsonValue) =
        let city = {
            id = int (json.["id"])
            name = removeQuotes (json.["name"].ToString())
            coords = jsonToLatLng json.["coord"]
            country = // Handle two different inputs
                if json.["sys"].ContainsKey "country" then removeQuotes (json.["sys"].["country"].ToString())
                else removeQuotes (json.["country"].ToString())
        }
        city

    // Get CityWeather from JsonValue
    let jsonToCityWeather (json:JsonValue) =
        let cityWeather = {
            city = jsonToCity json
            weather = jsonToWeather json
        }
        cityWeather

    // Get Forecast from JsonValue
    let jsonToForecast (json:JsonValue) =
        let forecast = {
            city = jsonToCity json.["city"]
            list = [| for i in 0 .. (json.["list"].Count - 1) -> jsonToWeather (json.["list"].Item i) |]
        }
        forecast