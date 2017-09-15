﻿using Android.App;
using Android.Widget;
using Android.OS;
using Android.Locations;
using XamarinWeather.Model;
using Android.Runtime;
using System;
using Newtonsoft.Json;
using WeatherXamarinMultiplatform.Droid;
using Square.Picasso;
using Android.Content;
using System.Collections.Generic;
using System.Linq;

namespace XamarinWeather
{
    [Activity(Label = "XamarinWeather", MainLauncher = true,Theme = "@style/Theme.AppCompat.Light.NoActionBar")]
    public class MainActivity : Activity,ILocationListener
    {
        TextView city, lastUpdate, humidity, time, description, celsius,errorText;
        ImageView image;

        LocationManager locationManager;
        string provider;
        static double lat, lng;
        OpenWeatherMap openWeatherMap=new OpenWeatherMap();

        protected override void OnCreate(Bundle savedInstanceState){
            base.OnCreate(savedInstanceState);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

			city = FindViewById<TextView>(Resource.Id.tv_city);
			humidity = FindViewById<TextView>(Resource.Id.tv_humidity);
			time = FindViewById<TextView>(Resource.Id.tv_time);
			description = FindViewById<TextView>(Resource.Id.tv_description);
			lastUpdate = FindViewById<TextView>(Resource.Id.tv_last_update);
			celsius = FindViewById<TextView>(Resource.Id.tv_celsius);
            errorText = FindViewById<TextView>(Resource.Id.tv_error_text);
			image = FindViewById<ImageView>(Resource.Id.imageView);

            initLocationManager();
        }

        private void initLocationManager()
        {
			locationManager = (LocationManager)GetSystemService(Context.LocationService);
            Criteria criteria = new Criteria{
                Accuracy = Accuracy.Fine
            };
			provider = locationManager.GetBestProvider(criteria, false);
            //IList<String> acceptableProviders = locationManager.GetProviders(criteria, true);
            //if(acceptableProviders.Any()){
            //    provider = acceptableProviders.First();
            //}else{
            //    provider = string.Empty;
            //}
			Location location = locationManager.GetLastKnownLocation(provider);
			OnLocationChanged(location);
        }

        protected override void OnResume(){
            base.OnResume();
            locationManager.RequestLocationUpdates(provider,0,0,this);

        }

        protected override void OnPause()
        {
            base.OnPause();
            locationManager.RemoveUpdates(this);
        }

        public void OnLocationChanged(Location location)
        {
            if (location == null){
                System.Diagnostics.Debug.WriteLine(GetString(Resource.String.error_no_location_lable));
                errorText.Text = GetString(Resource.String.error_no_location_message);
                startRequestWeather("53,9","27,57");;
            }else {
                lat = Math.Round(location.Latitude, 4);
                lng = Math.Round(location.Longitude, 4);
                startRequestWeather(lat.ToString(), lng.ToString());
            }
        }

        public void OnProviderDisabled(string provider){}

        public void OnProviderEnabled(string provider){}

        public void OnStatusChanged(string provider, [GeneratedEnum] Availability status, Bundle extras){}

        public void startRequestWeather(string lat,string lng){
            new GetWeather(this, openWeatherMap).Execute(NetworkHelper.NetworkHelper.GetRequestUrl(lat, lng));
        }

        private class GetWeather : AsyncTask<String, Java.Lang.Void, String>{
            private ProgressDialog pd = new ProgressDialog(Application.Context);
            private MainActivity activity;
            OpenWeatherMap openWeatherMap;
            public GetWeather(MainActivity activity,OpenWeatherMap openWeatherMap){
                this.openWeatherMap = openWeatherMap;
                this.activity = activity;
            }
            protected override void OnPreExecute()
            {
                base.OnPreExecute();
                pd.Window.SetType(Android.Views.WindowManagerTypes.SystemAlert);
                pd.SetTitle(Resource.String.dialog_title);
                pd.Show();
            }

            protected override string RunInBackground(params string[] @params)
            {
                string stream = null;
                string urlString = @params[0];
            	NetworkHelper.NetworkHelper http = new NetworkHelper.NetworkHelper();
                stream = http.getWeatherData(urlString);
                return stream;
            }
            protected override void OnPostExecute(string result){
                base.OnPostExecute(result);
                if (result.Contains("Error: Not found city") || String.IsNullOrEmpty(result)){
                    pd.Dismiss();
                    return;
                }
                openWeatherMap = JsonConvert.DeserializeObject<OpenWeatherMap>(result);
                System.Diagnostics.Debug.WriteLine(result);
                pd.Dismiss();

                //Add Data
                if (openWeatherMap != null){
                    //Show Weather Data
                    activity.city.Text = $"{openWeatherMap.name},{openWeatherMap.sys.country}";
                    activity.lastUpdate.Text = $"Last Updated: {DateTime.Now.ToString("dd MM yyyy HH:mm")}";
                    activity.description.Text = $"{openWeatherMap.weather[0].description}";
                    activity.humidity.Text = $"Humidity: {openWeatherMap.main.humidity} %";
                    activity.time.Text = $"Sunrise: {Utils.Utils.UnixTimeStampToDateTime(openWeatherMap.sys.sunrise)}/Sunset: {Utils.Utils.UnixTimeStampToDateTime(openWeatherMap.sys.sunset)}";
                    var temp = openWeatherMap.main.temp - 273.16;
                    activity.celsius.Text = $"Temp: {Math.Round(temp)} °C";

                    if(!String.IsNullOrEmpty(openWeatherMap.weather[0].icon)){
                        Picasso.With(activity.ApplicationContext)
                               .Load(NetworkHelper.NetworkHelper.GetImageUrl(openWeatherMap.weather[0].icon))
                               .Into(activity.image);
                    }
                }
            }
        }
    }
}

