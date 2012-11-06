using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Controls.Maps;
using System.Device.Location;

namespace bookSharer
{
    public partial class MainPage : PhoneApplicationPage
    {
        // Constructor
        private int userid = 1;//userid that can be either maintained in the session or in datbase on mobile
        private String username = "Anant";//username that can be either maintained in the session or in datbase on mobile
        public String latitude;//user's latitude
        public String longitude;//user's longitude
        private Int16 nComments;//total number of comments in the given instance
        private int latIndex = 0;//variable maintained to generate latitude
        private int longIndex = 0;//variable maintained to generate logitudes
        private int commIndex = 0;//variable maintained to generate comments
        double[] latArr;//array of latitudes
        double[] longArr;//array of longitudes
        String[] commArr;//Array of top comments
        private int state;//state variable to store if query is to search or to add
        private string search;//search term
        List<Pushpin> pp = new List<Pushpin>();//list of falgs

        /*
         *  function to create flag on map
         */
        public void createFlag(String txt, double lat, double lon)
        {
            Pushpin p = new Pushpin();//innitiate Pushpin data structure

            p.Background = new SolidColorBrush(Colors.Yellow);//setting background color
            p.Foreground = new SolidColorBrush(Colors.Red);//setting fore color

            p.Location = new GeoCoordinate(lat, lon);//setting flag location

            p.Content = txt;//setting text to be displayed on the flag
            pp.Add(p);//add Pushpin to list

            map1.Children.Add(p);//add Pushpin to the map


            map1.SetView(new GeoCoordinate(lat, lon, 200), 9);//set view over the new flag added
        }

        /*
         * Constructor for the class
         */
        public MainPage()
        {
            InitializeComponent();
            this.longArr = new double[10];//Array to store longitudes 
            this.latArr = new double[10];//Array to store latitudes 
            this.commArr = new String[1000];//Array to store top comments
            this.state = 0;//set default state to 0

            //Getting user's location through GPS
            GeoCoordinateWatcher watcher;//Declare GPS watcher
            watcher = new GeoCoordinateWatcher(GeoPositionAccuracy.High); // using high accuracy
            watcher.MovementThreshold = 2;//setting threshhold for watcher
            watcher.PositionChanged += new EventHandler<GeoPositionChangedEventArgs<GeoCoordinate>>(watcher_PositionChanged);//set the function to be called when watcher position is changed
            watcher.Start();//start watcher
        }

        /*
         * Function to be called when watcher position is changed
         */
        void watcher_PositionChanged(object sender, GeoPositionChangedEventArgs<GeoCoordinate> gps)
        {
            this.latitude = gps.Position.Location.Latitude.ToString("0.000");//Setup latitude of user
            this.longitude = gps.Position.Location.Longitude.ToString("0.000");//Setup longitude of user

            //Latitude and logitude can be hard coded in case gps fails in emulator for testing purpose
            //this.latitude = "25"; 
            //this.longitude = "87";
            if (this.latitude != "" && this.longitude != "" && this.latitude!=null && this.longitude!=null)
            {
                //Setting map params
                map1.Center.Latitude = Convert.ToDouble(this.latitude);//setting latitude of map's center
                map1.Center.Longitude = Convert.ToDouble(this.longitude);//setting latitude of map's center
                map1.ZoomLevel = 7;//Setting zoom level
                map1.ZoomBarVisibility = Visibility.Visible;//Setup map Visibility to visible

                this.createFlag("You are here", Convert.ToDouble(this.latitude), Convert.ToDouble(this.longitude));//create flag for user's position
            }
            else
            {
                textBlock1.Visibility = Visibility.Visible;
                textBlock1.Text = "Your Device Does not support GPS.";
            }
            
        }

        /*
         * Function to be called on click event over button1
         */
        private void button1_Click(object sender, RoutedEventArgs e)
        {
            map1.Visibility = Visibility.Collapsed;//make map invisible
            textBox1.Visibility = Visibility.Visible;//make text box visible
            textBox1.Text = "";//clear textbox from previous data
            button2.Visibility = Visibility.Visible;//make button2 visible
            this.state = 1;//set state parameter to one to recognise Add query
            button2.Content = "Share";
        }

        /*
         * function called to insert user's comment to the database through GET request
         */
        public void UpdateDb()
        {
            //making String datatype url for the HTTP call
            String urlRequest = "http://localhost/wanderer/update.php?lat=" + this.latitude + "&long=" + this.longitude + "&userid=" + this.userid + "&username=" + this.username + "&Comments=" + textBox1.Text;
            WebClient client = new WebClient();//innitialising WebClient class
            client.DownloadStringCompleted += new DownloadStringCompletedEventHandler(nothingToDo);//setting up function to be called in case of response
            client.DownloadStringAsync(new Uri(urlRequest));//starting request
        }

        /*
         * Function called to respond to updateDb call.
         * This has been left empty . However this may be used to innitiate certain behaviours on the phone after status update
         */
        public void nothingToDo(object sender, DownloadStringCompletedEventArgs e)
        {
            //button1.Content = e.Result;
        }

        //=========================================================

        /*
         * Function called to get the number of comments on the given instance of search query
         */
        public void GetNComments()
        {
            String urlRequest = "http://localhost/wanderer/wanderer.php?lat=" + this.latitude + "&long=" + this.longitude;//url to send GET request
            if (this.state == 2)//if object state is 2 , that means a search query needs to be sent
                urlRequest += "&search=" + this.search;//concatenate search keyword to the url

            WebClient client = new WebClient();//innitialise new webclient class
            client.DownloadStringCompleted += new DownloadStringCompletedEventHandler(setNComments);//setting up function to be called in case of response
            client.DownloadStringAsync(new Uri(urlRequest));//start Asyncronous HTTP call
        }

        /*
         * Function to be called to respond to the GetNComments request
         * In the workflow of the app , this function is used to innitiate the query to generate a new map through a sequense of sunction called by one another
         */
        private void setNComments(object sender, DownloadStringCompletedEventArgs e)
        {
            this.nComments = Convert.ToInt16(e.Result);//convert string response to Int16 data type
            //button1.Content = Convert.ToString(this.latitude);
            //searchbutton.Content = Convert.ToString(this.longitude);
            foreach (Pushpin item in pp)
                map1.Children.Remove(item);//remove every flag that was previously present
            pp = new List<Pushpin>();//innitialise list to a new list

            this.commIndex = 0;//innitalise comment index to 0
            this.latIndex = 0;//innitialise latitude index to 0
            this.longIndex = 0;//innitialise longitude index to 0
            for (Int16 i = 0; i < this.nComments; i++)//loop runs to generate flags for every comment
            {
                //button1.Content = Convert.ToString(this.nComments);
                this.GetLatitudes(i);//get latitude through HTTP request and store it
                this.GetLongitudes(i);//get longitude through HTTP request and store it
                this.Getcomments(i);//get comments and draw flag on the map
            }
            this.createFlag("You are here", Convert.ToDouble(this.latitude), Convert.ToDouble(this.longitude));//mark user's position on the map
        }
        //=================================================================

        //getting number of Latitudes similar to other HTTP requests above====================================
        public void GetLatitudes(int id)
        {
            String urlRequest = "http://localhost/wanderer/latitude.php?lat=" + this.latitude + "&long=" + this.longitude + "&id=" + Convert.ToString(id);
            if (this.state == 2)
                urlRequest += "&search=" + this.search;

            WebClient client = new WebClient();
            client.DownloadStringCompleted += new DownloadStringCompletedEventHandler(setlatitudes);
            client.DownloadStringAsync(new Uri(urlRequest));
        }
        private void setlatitudes(object sender, DownloadStringCompletedEventArgs e)
        {
            int temp = this.latIndex;
            this.latArr[temp] = Convert.ToDouble(e.Result);
            this.latIndex++;
        }
        //=================================================================

        //getting number of Longitudes similar to other HTTP requests above====================================
        public void GetLongitudes(int id)
        {
            String urlRequest = "http://localhost/wanderer/longitude.php?lat=" + this.latitude + "&long=" + this.longitude + "&id=" + Convert.ToString(id);
            if (this.state == 2)
                urlRequest += "&search=" + this.search;

            WebClient client = new WebClient();
            client.DownloadStringCompleted += new DownloadStringCompletedEventHandler(setlongitudes);
            client.DownloadStringAsync(new Uri(urlRequest));
        }
        private void setlongitudes(object sender, DownloadStringCompletedEventArgs e)
        {
            int temp = this.longIndex;
            this.longArr[temp] = Convert.ToDouble(e.Result);
            this.longIndex++;
        }
        //=================================================================

        //getting number of comments similar to above and draw flag====================================
        public void Getcomments(int id)
        {
            String urlRequest = "http://localhost/wanderer/comment.php?lat=" + this.latitude + "&long=" + this.longitude + "&id=" + Convert.ToString(id);
            if (this.state == 2)
                urlRequest += "&search=" + this.search;

            WebClient client = new WebClient();
            client.DownloadStringCompleted += new DownloadStringCompletedEventHandler(setcomments);
            client.DownloadStringAsync(new Uri(urlRequest));
        }
        private void setcomments(object sender, DownloadStringCompletedEventArgs e)
        {

            int temp = this.commIndex;

            if (this.state != 2 && this.latIndex - 1 >= 0 && this.longIndex - 1>=0) this.createFlag(e.Result, this.latArr[this.latIndex - 1], this.longArr[this.longIndex - 1]);
            else this.createFlag(e.Result, this.latArr[temp], this.longArr[temp]);
            this.commIndex++;
        }
        //=================================================================

        /*
         * innitiate flagging of map by calling function to generate number of comments
         */
        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.latitude != "" && this.longitude != "" && this.latitude != null && this.longitude != null) this.GetNComments();
            else
            {
                textBlock1.Visibility = Visibility.Visible;
                textBlock1.Text = "Your Device Does not support GPS.";
            }
        }

        /*
         * removing map from screen to show text box and search button
         */
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            map1.Visibility = Visibility.Collapsed;
            textBox1.Visibility = Visibility.Visible;
            textBox1.Text = "";
            button2.Visibility = Visibility.Visible;
            button2.Content = "Search";
            this.state = 2;//set object state to 2 to innitiate search queries
        }

        /*
         * Click event handler for search or add button
         */
        private void button2_Click_1(object sender, RoutedEventArgs e)
        {
            //First recognise object state
            if (this.state == 1)//if object state is 1 insert comment into the database
            {
                this.UpdateDb();//update database
                this.state = 0;//set object state to 0
            }
            if (this.state == 2)//if object state is 2 run a search query
            {
                this.search = textBox1.Text;//take search keyword from textbox
                this.GetNComments();//draw new map according to new search query
            }
            //return to the map view
            map1.Visibility = Visibility.Visible;
            textBox1.Visibility = Visibility.Collapsed;
            textBox1.Text = "";
            button2.Visibility = Visibility.Collapsed;


        }

        private void Image_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {

        }
    }
}