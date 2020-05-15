﻿using GeoCoordinatePortable;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace or_satellite.Service
{
    public class LocationSearch
    {
        string folderPath = "";
        const string CoordFile = "coordfile.txt";
        const string TemperatureFile = "temperature.txt";
        const string HumidityFile = "humidity.txt";
        const string SeaPressureFile = "sea_pressure.txt";
        const string OzoneFile = "total_ozone.txt";
        const double KelvinMinusValue = 272.15;
        private string MinRangeCalculator(string input)
        {
            const double range = 0.5;
            double _input = Convert.ToDouble(input);
            double _minInput = _input - range;
            return _minInput.ToString().Split('.')[0];
        }
        private string MaxRangeCalculator(string input)
        {
            double range = 0.5;
            double _input = Convert.ToDouble(input);
            double _maxInput = _input + range;
            return _maxInput.ToString().Split('.')[0];
        }

        public string Search(string latitude, string longitude, string date = "")
        {
            System.Globalization.CultureInfo customCulture = (System.Globalization.CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";

            System.Threading.Thread.CurrentThread.CurrentCulture = customCulture;

            Stopwatch stopwatch = new Stopwatch();
            List<string> listItems = new List<string>();
            folderPath = "/app/Copernicus/Processed/" + date + "/";


            decimal lat = 0;
            decimal Long = 0;
            string maxLatRange = MaxRangeCalculator(latitude);
            string maxLongRange = MaxRangeCalculator(longitude);
            string minLatRange = MinRangeCalculator(latitude);
            string minLongRange = MinRangeCalculator(longitude);

            if (decimal.TryParse(latitude, out lat) && decimal.TryParse(longitude, out Long))
            {

            }

            #region CheckFilesExist 
            if (File.Exists($"{folderPath}{CoordFile}") && File.Exists($"{folderPath}{TemperatureFile}") && File.Exists($"{folderPath}{HumidityFile}") && File.Exists($"{folderPath}{SeaPressureFile}") && File.Exists($"{folderPath}{OzoneFile}"))
            {

            }
            else
            {
                Console.WriteLine("Invalid folder or missing files.");
                LocationObject locErr = new LocationObject(0, "", 0, 0, 0, 0, false, stopwatch.Elapsed);
                // return locErr;
                return "Invalid folder or missing files.";
            }
            #endregion

            stopwatch.Start();
            string data = File.ReadAllText($"{folderPath}{CoordFile}");
            listItems.AddRange(data.Split('\n'));

            List<string> FilteredCoordList = listItems.Where(x => x.StartsWith(minLatRange) || x.StartsWith(maxLatRange)).ToList();

            FilteredCoordList = FilteredCoordList.Where(x => x.Split(',')[1].StartsWith(minLongRange) || x.Split(',')[1].StartsWith(maxLongRange)).ToList();

            List<GeoCoordinate> coordinateList = new List<GeoCoordinate>();
            foreach (string item in FilteredCoordList)
            {

                string newLat = item.Split(',')[0];
                string newLong = item.Split(',')[1];


                while (newLat.Replace("-", "").Length < 7)
                {
                    newLat += "0";
                }
                if (newLat.Length > 7)
                {
                    newLat = newLat.Substring(0, 7);
                }
                while (newLong.Replace("-", "").Length < 8)
                {
                    newLong += "0";
                }
                if (newLong.Length > 8)
                {
                    newLong = newLong.Substring(0, 8);
                }

                newLong = newLong.Insert((newLong.Length - 7), ".");
                newLat = newLat.Insert((newLat.Length - 5), ".");
                coordinateList.Add(new GeoCoordinate(Convert.ToDouble(newLat), Convert.ToDouble(newLong)));
                Console.WriteLine($"{newLat},{newLong}");

            }

            if (coordinateList.Count == 0)
            {
                stopwatch.Stop();
                LocationObject locErr = new LocationObject(0, "", 0, 0, 0, 0, false, stopwatch.Elapsed);
                string errorObject = JsonConvert.SerializeObject(locErr);
                Console.WriteLine(errorObject);
                // return locErr;
                return errorObject;
            }

            List<GeoCoordinate> SortedCoordinateListBasedOnDistance = coordinateList.OrderBy(x => x.GetDistanceTo(new GeoCoordinate(Convert.ToDouble(latitude), Convert.ToDouble(longitude)))).ToList();
            //Console.WriteLine($"{SortedCoordinateListBasedOnDistance[0].Latitude},{SortedCoordinateListBasedOnDistance[0].Longitude}".Replace(".", ""));
            string stringToSearch = $"{SortedCoordinateListBasedOnDistance[0].Latitude},{SortedCoordinateListBasedOnDistance[0].Longitude}".Replace(".", "");

            int FoundCoordIndex = listItems.IndexOf($"{stringToSearch}");
            //temperature
            double FoundTemperature;
            if (double.TryParse(File.ReadLines($"{folderPath}{TemperatureFile}").Skip(FoundCoordIndex).Take(1).First(), out FoundTemperature))
            {
                FoundTemperature = FoundTemperature - KelvinMinusValue;
            }
            //Humidity
            double FoundHumidity;
            if (double.TryParse(File.ReadLines($"{folderPath}{HumidityFile}").Skip(FoundCoordIndex).Take(1).First(), out FoundHumidity))
            {
                FoundHumidity = Math.Round(FoundHumidity, 1);
            }
            //sea level pressure
            double foundSeaLevelPressure;
            if (double.TryParse(File.ReadLines($"{folderPath}{SeaPressureFile}").Skip(FoundCoordIndex).Take(1).First(), out foundSeaLevelPressure))
            {
                foundSeaLevelPressure = Math.Round(foundSeaLevelPressure, 1);
            }
            //Total Ozone
            double foundOzone;
            if (double.TryParse(File.ReadLines($"{folderPath}{OzoneFile}").Skip(FoundCoordIndex).Take(1).First(), out foundOzone))
            {

            }

            stopwatch.Stop();

            LocationObject loc = new LocationObject(Math.Round((SortedCoordinateListBasedOnDistance[0].GetDistanceTo(new GeoCoordinate(Convert.ToDouble(latitude), Convert.ToDouble(longitude))) / 1000), 2), $"{SortedCoordinateListBasedOnDistance[0].Latitude },{ SortedCoordinateListBasedOnDistance[0].Longitude}", Math.Round(FoundTemperature, 1), FoundHumidity, foundSeaLevelPressure, foundOzone, true, stopwatch.Elapsed);
            string jsonResult = JsonConvert.SerializeObject(loc);
            // return loc;
            return jsonResult;
        }
    }
}