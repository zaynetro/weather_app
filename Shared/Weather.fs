namespace Weather_v1

module Weather = 

    type Weather = { 
       city : string
       temp : float
       description : string
    }

    // Temperature scales transformation
    let KelvinToCelsius temp = System.Math.Round(temp - 273.15)
    let KelvinToFahrenheit temp = System.Math.Round(temp * 1.8 - 459.67)

    // Add scale to the temperature
    let formatDegrees scale temp =
        let sign = if temp > 0.0 then "+" else "-"
        sign + temp.ToString() + "°" + scale

    // Transform Kelvin to string with degrees mark
    let KelvinToCelsiusString = KelvinToCelsius >> formatDegrees "C"
    let KelvinToFahrenheitString = KelvinToFahrenheit >> formatDegrees "F"